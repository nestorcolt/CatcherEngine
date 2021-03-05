using System;
using System.Collections.Generic;
using System.Text;
using SearchEngine.Modules;

namespace SearchEngine
{
    class Program

    {
        static void Main(string[] args)
        {
            //string q = SqsHandler.GetQueueByName(SqsHandler.Client, "GetUserBlocksQueue").Result;
            //Console.WriteLine(q);
            DynamoHandler.UpdateUserTimestamp("15", 1900).Wait();
        }
    }
}
