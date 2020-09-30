using System;

namespace AmazonFlexServices
{
    class Program
    {
        static void Main(string[] args)
        {
            Engine instance = new Engine();
            instance.Run();
        }
    }



    class Engine

    {
        public void Run()
        {
            Console.WriteLine("Init Method");
        }

    }
}
