using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FlexCatcher
{
    class BlockCatcher

    // The Engine of the program. Will look for available blocks depending on the parsed data, making API calls to amazon to check for blocks to pick up by drivers.
    // Will used asynchronous programming and multi-threading to speed up the process and the API request.

    {

        private Dictionary<string, string> _offersDataHeader;
        private string _serviceAreaFilterData;
        private readonly string _flexAppVersion;
        private readonly float _minimumPrice;
        private readonly int _pickUpTimeThreshold;
        private readonly string[] _areas;
        private int _totalOffersCounter = 0;
        private int _totalApiCalls = 0;
        private int _totalValidOffers = 0;
        private int _totalAcceptedOffers = 0;
        private readonly string _userId;
        public bool AccessSuccessCode;
        private bool _debug;
        private int _speed;


        public bool Debug
        {
            get => _debug;
            set => _debug = value;
        }

        public float ExecutionSpeed
        {
            get
            {
                return _speed;
            }
            set => _speed = (int)(value * 1000);
        }

        public BlockCatcher(string userId, string flexAppVersion, float minimumPrice, int pickUpTimeThreshold, string[] areas)
        {
            _flexAppVersion = flexAppVersion;
            _minimumPrice = minimumPrice;
            _pickUpTimeThreshold = pickUpTimeThreshold;
            _userId = userId;
            _areas = areas;

            ApiHelper.InitializeClient();

            // Primary methods resolution
            Task.Run(EmulateDevice).Wait();
            Task.Run(GetAccessData).Wait();

            // Set the client service area to sent as extra data with the request on get blocks method
            SetServiceArea();

        }

        private int GetTimestamp()
        {

            TimeSpan time = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }


        private string GetServiceAreaId()
        {
            var result = ApiHelper.GetServiceAuthentication(ApiHelper.ServiceAreaUri, _offersDataHeader[ApiHelper.TokenKeyConstant]).Result;

            if (result.HasValues)
                return (string)result[0];

            return null;
        }

        private void SetServiceArea()
        {

            string serviceAreaId = GetServiceAreaId();
            var filtersDict = new Dictionary<string, object>
            {
                ["serviceAreaFilter"] = new List<string>(),
                ["timeFilter"] = new Dictionary<string, string>(),
            };

            // Id Dictionary to parse to offer headers later
            var serviceDataDictionary = new Dictionary<string, object>

            {
                ["serviceAreaIds"] = new[] { serviceAreaId },
                ["filters"] = filtersDict,

            };

            // MERGE THE HEADERS OFFERS AND SERVICE DATA IN ONE MAIN HEADER DICTIONARY
            _serviceAreaFilterData = JsonConvert.SerializeObject(serviceDataDictionary).Replace("\\", "");
        }

        public async Task GetAccessData()
        {
            var data = new Dictionary<string, object>

            {
                { "userId", _userId },
                { "action", "access_token" }

            };
            string jsonData = JsonConvert.SerializeObject(data);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);
            string responseValue = response.GetValue("access_token").ToString();

            if (responseValue == "failed")
            {
                Console.WriteLine("Session token request failed. Operation aborted.\n");
                AccessSuccessCode = false;
            }

            else
            {
                _offersDataHeader[ApiHelper.TokenKeyConstant] = responseValue;
                Console.WriteLine("Access to the service granted!\n");
                AccessSuccessCode = true;
            }

        }

        private async Task EmulateDevice()
        {
            var data = new Dictionary<string, string>
            {
                { "userId", _userId },
                { "action", "instance_id" }

            };

            String jsonData = JsonConvert.SerializeObject(data);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);

            string androidVersion = response.GetValue("androidVersion").ToString();
            string deviceModel = response.GetValue("deviceModel").ToString();
            string instanceId = response.GetValue("instanceId").ToString();
            string build = response.GetValue("build").ToString();
            string uuid = Guid.NewGuid().ToString();
            int time = GetTimestamp();

            var offerAcceptHeaders = new Dictionary<string, string>
            {
                ["x-flex-instance-id"] = $"{instanceId.Substring(0, 8)}-{instanceId.Substring(8, 4)}-" +
                                         $"{instanceId.Substring(12, 4)}-{instanceId.Substring(16, 4)}-{instanceId.Substring(20, 12)}",
                ["X-Flex-Client-Time"] = time.ToString(),
                ["User-Agent"] = $"Dalvik/2.1.0 (Linux; U; Android {androidVersion}; {deviceModel} {build}) RabbitAndroid/{_flexAppVersion}",
                ["X-Amzn-RequestId"] = uuid,
                ["Host"] = "flex-capacity-na.amazon.com",
                ["Connection"] = "Keep-Alive",
                ["Accept-Encoding"] = "gzip"
            };

            // Set the class field with the new offer headers
            _offersDataHeader = offerAcceptHeaders;
        }


        private void AcceptOffer(JToken offer)
        {
            Console.WriteLine("Accepting Offer: \n\n");
            Console.WriteLine(offer);

            // TODO send post async to accept the offer
            //_totalAcceptedOffers++;
        }

        private void ValidateOffers(JToken offer)
        {
            JToken serviceAreaId = offer["serviceAreaId"];
            JToken offerPrice = offer["rateInfo"]["priceAmount"];
            JToken startTime = offer["startTime"];

            // The time the offer will be available for pick up at the facility
            int pickUpTimespan = (int)startTime - GetTimestamp();


            if ((float)offerPrice >= _minimumPrice && _areas.Contains((string)serviceAreaId) && pickUpTimespan >= _pickUpTimeThreshold)
            {
                _totalValidOffers++;
                AcceptOffer(offer);
            }

            // debug just output information to the console
            if (_debug)
            {
                Console.WriteLine("\nValidation debug Info:");
                string logInfo = $"Service Area -- {(string)serviceAreaId} in -->  Areas List\n" +
                                 $"Price -- {(float)offerPrice} Grater or Equal than {_minimumPrice}\n" +
                                 $"PickUp -- {pickUpTimespan} Grater or Equal than {_pickUpTimeThreshold}";

                Console.WriteLine(logInfo);
                Console.WriteLine("\n\n");
            }
        }

        private async Task GetOffers()
        {

            ApiHelper.AddRequestHeaders(_offersDataHeader);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, _serviceAreaFilterData);
            // keep a track how many calls has been made
            _totalApiCalls++;

            if (ApiHelper.CurrentResponse.IsSuccessStatusCode)
            {
                JToken offerList = response.GetValue("offerList");

                if (offerList.HasValues)
                {
                    // validate offers
                    Parallel.ForEach(offerList, offer =>
                    {
                        // to track and debug how many offers has shown the request in total of the runtime
                        _totalOffersCounter++;

                        // Parallel offer validation and accept request.
                        ValidateOffers(offer);

                    });
                }
            }
        }

        public void LookingForBlocks()
        {
            int counter = 0;

            while (true)

            {

                Stopwatch watcher = Stopwatch.StartNew();
                Task.Delay(_speed).Wait();
                Console.WriteLine(_speed);
                Task.Run(GetOffers);

                Console.WriteLine($"Execution Speed: {watcher.Elapsed}  - | Api Calls: {_totalApiCalls} -- Total Offers: {_totalOffersCounter} -- " +
                                  $"Validated Offers: {_totalValidOffers} -- Accepted Offers: {_totalAcceptedOffers}");

                watcher.Restart();
                counter++;

                //if (counter == 1)
                //    break;

            }
        }

    }
}