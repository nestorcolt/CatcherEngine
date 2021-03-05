using System;
using System.Collections.Generic;
using System.Globalization;
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
            string timestamp = "ka";
            string name = "User-{0}";
            Console.WriteLine(String.Format(name, "15"));

        }
    }
}
