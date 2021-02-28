using System;
using Newtonsoft.Json.Linq;
using SearchEngine.Modules;

namespace tester
{
    class Program
    {
        static void Main(string[] args)
        {
            // Convert the time zone in UNIX format given Datetime object
            OfferGenerator OfferHandle = new OfferGenerator();
            JToken offerList = JToken.FromObject(OfferHandle.GenerateOffers());
            OfferHandle.GetTimeFromSeconds(19865653);

        }
    }
}
