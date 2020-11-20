using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CatcherEngine.Modules
{
    class BlockCatcher : Engine
    {
        private readonly SignatureObject _signature = new SignatureObject();
        private readonly Stopwatch _mainTimer = Stopwatch.StartNew();
        private readonly DateTime _startTime = DateTime.Now;

        public async Task<string> GetOffersAsyncHandle(string user)
        {

            if (UserId is null)
                InitializeEngine(userId: user);

            SignRequestHeaders($"{ApiHelper.ApiBaseUrl}{ApiHelper.OffersUri}");

            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.SeekerClient);
            ApiHelper.AddRequestHeaders(_requestDataHeadersDictionary, ApiHelper.CatcherClient);

            var response = await ApiHelper.PostDataAsync(ApiHelper.OffersUri, ServiceAreaFilterData, ApiHelper.SeekerClient);
            TotalApiCalls++;

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await ApiHelper.GetRequestJTokenAsync(response);
                JToken offerList = requestToken.GetValue("offerList");

                if (offerList != null && offerList.HasValues)
                {
                    Thread acceptThread = new Thread(task => AcceptOffers(offerList));
                    // TODO NOT ACCEPTING BLOCKS
                    //acceptThread.Start();

                    TotalOffersCounter += offerList.Count();
                }
            }

            return GetLogMessage(response.StatusCode.ToString());
        }

        private void SignRequestHeaders(string url)
        {
            SortedDictionary<string, string> signatureHeaders = _signature.CreateSignature(url, CurrentUserToken);

            _requestDataHeadersDictionary["X-Amz-Date"] = signatureHeaders["X-Amz-Date"];
            _requestDataHeadersDictionary["X-Flex-Client-Time"] = GetTimestamp().ToString();
            _requestDataHeadersDictionary["X-Amzn-RequestId"] = signatureHeaders["X-Amzn-RequestId"];
            _requestDataHeadersDictionary["Authorization"] = signatureHeaders["Authorization"];
        }

        public async Task AcceptSingleOfferAsync(string offerId)
        {
            var acceptHeader = new Dictionary<string, string>
            {
                {"__type", $"AcceptOfferInput:{ ApiHelper.AcceptInputUrl}"},
                {"offerId", offerId}
            };

            string jsonData = JsonConvert.SerializeObject(acceptHeader);
            HttpResponseMessage response = await ApiHelper.PostDataAsync(ApiHelper.AcceptUri, jsonData, ApiHelper.CatcherClient);

            if (response.IsSuccessStatusCode)
            {
                // send to owner endpoint accept data to log and send to the user the notification
                TotalAcceptedOffers++;
            }

            if (Debug)
                Console.WriteLine($"\nAccept Block Operation Status >> Code >> {response.StatusCode}\n");
        }

        public void AcceptOffers(JToken offerList)
        {
            Parallel.For(0, offerList.Count(), n =>
            {
                Thread accept = new Thread(async task => await AcceptSingleOfferAsync(offerList[n]["offerId"].ToString()));
                accept.Start();
            });
        }

        public string GetLogMessage(string statusCode)
        {
            string message = $"Status: {statusCode}  |  Start Time: {_startTime}  |  On Air: {_mainTimer.Elapsed}  | Api Calls: {TotalApiCalls} | " +
                              $"OFFERS DATA >> Total: {TotalOffersCounter} -- Accepted: {TotalAcceptedOffers} -- Lost: {TotalOffersCounter - TotalAcceptedOffers}";
            return message;
        }

        //public void LookingForBlocksLegacy()
        //{
        //    Stopwatch watcher = Stopwatch.StartNew();

        //    while (true)

        //    {
        //        // start logic here main request
        //        HttpStatusCode statusCode = GetOffersAsyncHandle(UserId).Result;

        //        // custom delay to save request
        //        Thread.Sleep(Speed);

        //        if (Debug)
        //        {
        //            // output log to console
        //            Console.WriteLine($"\nRequest Status >> Reason >> {statusCode}\n");
        //            Console.WriteLine($"Start Time: {_startTime}  |  On Air: {_mainTimer.Elapsed}  |  Execution Speed: {watcher.ElapsedMilliseconds / 1000.0}  - | Api Calls: {TotalApiCalls} |" +
        //                              $"  - OFFERS DATA >> Total: {TotalOffersCounter} -- Accepted: {TotalAcceptedOffers} -- Lost: {TotalOffersCounter - TotalAcceptedOffers}");
        //        }

        //        if (statusCode is HttpStatusCode.Unauthorized || statusCode is HttpStatusCode.Forbidden)
        //        {
        //            GetAccessDataAsync().Wait();
        //            Thread.Sleep(100000);
        //            continue;
        //        }

        //        if (statusCode is HttpStatusCode.BadRequest || statusCode is HttpStatusCode.TooManyRequests)
        //        {
        //            Thread.Sleep(ThrottlingTimeOut);
        //            return;
        //        }

        //        // restart counter to measure performance
        //        watcher.Restart();

        //    }

        //}
    }
}