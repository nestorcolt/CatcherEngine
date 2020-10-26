using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            get => _speed;
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
        public void GetPool()
        {
            //ApiHelper.AddRequestHeaders(_offersDataHeader);
            //ApiHelper.ApiClient.DefaultRequestHeaders.Clear();
            var result = ApiHelper.GetBlockFromDataBaseAsync(ApiHelper.AssignedBlocks, _offersDataHeader[ApiHelper.TokenKeyConstant]).Result;
            Console.WriteLine(result);
            //string a = "AAAAAAAAAAGut8C5q4wca4H4coiePO3azW0pIDwKpdGBuCupmzlrm2UEllqenkepcAB73qkCNkkEDiCQ9bQW5a4MhoTXj4KyOciGBUEBn7Vthfz9ZBH6meBd2cmOhrX9tqWf4qmUBVLAS9z5EufXwtaypKhTED22PthoC2/9534QzIQqC3Ga/JIZxQ8Hf9J0Kpr8M2hMCO4gYhifMnW5JgjCAr+JJm3Y8ka1IKmAu2hUU2ggnKQ+H+pUoV+IXeHKFXTK9M+NxX27cotg7XFP5wI8VxqAA7aFrLtOFUSn6BDu0tW+uW+KbOJU7wEi+a7avNge4b8m0ujALRBUlkypADxC6/3ZITMdc/ou5cglg2/FHCzYMvroJYuGtcsSrZkr43qaVTuk55jKjjt68mAI|99kuo5yUKTEC/MAPLbtpNUGc/a6be2nZQykjMMYlf+U=";

            //string b = "AAAAAAAAAAGut8C5q4wca4H4coiePO3azW0pIDwKpdGBuCupmzlrm2UEllqenkepcAB73qkCNkkEDiCQ9bQW5a4MhoTXj4KyOciGBUEBn7Vthfz9ZBH6meBd2cmOhrX9tqWf4qmUBVLAS9z5EufXwtGzpKhTED22PthoC2/9534QzIQqC3Ga/JIZxQ8Hf9J0Kpr8M2hMCO4gYhifMnW5JgjCAr+JJm3Y8ka1IKmAu2hUU2ggnKQ+SrpWoFKMXbzKEH+a/M+Nkim5ctJg7yNP4wZnBRqBBLmFobscFUSn6BDu0tW+uW+KbOJU7wEi+a7avNge4b8m0ujALRBUlkypADxC6/3ZITMdc/ou5cglg2/FHCzYMvroJYuGtcsSrZm+QJtVg2NL912aXrvEsVu3|4MlcE4fsvNxgIo0eB3ILscRFvs9K2T89vT0fym4R0rc=";

            Parallel.ForEach(result.Values(), async block =>
            {

                if (block != null && !block.HasValues)
                    return;

                int blockId = (int)block[0]["startTime"];
                string offerId = (string)block[0]["scheduledAssignmentId"];
                Console.WriteLine(block);
                await ApiHelper.DeleteOfferAsync(blockId);


            });
            //Task.Run(() => AcceptOffer(b)).Wait();

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
            HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);
            JObject requestToken = await ApiHelper.GetRequestTokenAsync(response);
            string responseValue = requestToken.GetValue("access_token").ToString();

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
            HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.OwnerEndpointUrl, jsonData);
            JObject requestToken = await ApiHelper.GetRequestTokenAsync(response);

            string androidVersion = requestToken.GetValue("androidVersion").ToString();
            string deviceModel = requestToken.GetValue("deviceModel").ToString();
            string instanceId = requestToken.GetValue("instanceId").ToString();
            string build = requestToken.GetValue("build").ToString();
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

        private async Task AcceptOffer(JToken offer)
        {

            var response = await ApiHelper.AcceptOfferAsync((string)offer[key: "offerId"]);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                // send to owner endpoint accept data to log and send to the user the notification
                Console.WriteLine($"\nOffer has been accepted >> Reason >> {response.StatusCode}");
                _totalAcceptedOffers++;
            }
            else
                Console.WriteLine($"\nSomething went wrong accepting the offer >> Reason >> {response.StatusCode}\n");


        }

        private async Task ValidateOffers(JToken offer)

        {
            JToken serviceAreaId = offer["serviceAreaId"];
            JToken offerPrice = offer["rateInfo"]["priceAmount"];
            JToken startTime = offer["startTime"];

            // The time the offer will be available for pick up at the facility
            int pickUpTimespan = (int)startTime - GetTimestamp();

            // if the validation is not success will try to find in the catch blocks the one did not passed the validation and forfeit them
            if ((float)offerPrice < _minimumPrice && !_areas.Contains((string)serviceAreaId) && pickUpTimespan < _pickUpTimeThreshold)
            {
                var blocksArray = await ApiHelper.GetBlockFromDataBaseAsync(ApiHelper.AssignedBlocks, _offersDataHeader[ApiHelper.TokenKeyConstant]);

                Parallel.ForEach(blocksArray.Values(), async block =>
                {
                    if (block != null && !block.HasValues)
                        return;

                    int blockId = (int)block[0]["startTime"];

                    if (blockId == (int)startTime)
                        await ApiHelper.DeleteOfferAsync(blockId);
                });

            }

            // if pass the validation will sum one to the win stack and send request to owner endpoint
            if ((float)offerPrice >= _minimumPrice && _areas.Contains((string)serviceAreaId) && pickUpTimespan >= _pickUpTimeThreshold)
            {
                _totalValidOffers++;

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
        }

        private async Task GetOffers()
        {
            //ApiHelper.AddRequestHeaders(_offersDataHeader);
            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, _serviceAreaFilterData);
            JObject requestToken = await ApiHelper.GetRequestTokenAsync(response);

            if (response.IsSuccessStatusCode)
            {
                // keep a track how many calls has been made
                _totalApiCalls++;
                JToken offerList = requestToken.GetValue("offerList");

                if (offerList != null && offerList.HasValues)
                {
                    // validate offers
                    Parallel.ForEach(offerList, async offer =>
                    {
                        // Parallel offer validation and accept request.
                        await AcceptOffer(offer);
                        await ValidateOffers(offer);

                        // to track and debug how many offers has shown the request in total of the runtime
                        _totalOffersCounter++;

                    });
                }
            }
            else
            {
                Console.WriteLine($"\nRequest not Successful >> Reason >> {response.StatusCode}\n");
            }
        }

        public void LookingForBlocks()
        {
            ApiHelper.AddRequestHeaders(_offersDataHeader);

            while (true)

            {
                // TODO REMEMBER TO RE-AUTHENTICATE WHEN THE SESSION EXPIRE EVERY HOUR OR SOMETHING. 
                Stopwatch watcher = Stopwatch.StartNew();
                Task.Run(GetOffers);
                Task.Delay(_speed).Wait();

                Console.WriteLine($"Execution Speed: {watcher.Elapsed}  - | Api Calls: {_totalApiCalls} -- Total Offers: {_totalOffersCounter} -- " +
                                  $"Validated Offers: {_totalValidOffers} -- Accepted Offers: {_totalAcceptedOffers}");

                watcher.Restart();

            }

        }

    }
}