using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SearchEngine.Lib;
using SearchEngine.Models;
using System;
using System.Threading;

namespace SearchEngine.Test
{
    class LocalRunner
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBlockCatcher _blockCatcher;

        public LocalRunner(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public LocalRunner() : this(StartUp.Container.BuildServiceProvider())
        {
            _blockCatcher = _serviceProvider.GetService<IBlockCatcher>();
        }

        public void Run()
        {
            // Data to parse
            string userId = "5";
            string userData = DynamoHandler.QueryUser(userId).Result;
            UserDto userDto = JsonConvert.DeserializeObject<UserDto>(userData);

            userDto.TimeZone = "Eastern Standard Time";
            userDto.MinimumPrice = 1000;

            while (true)
            {
                bool result = _blockCatcher.LookingForBlocks(userDto).Result;
                Console.WriteLine($"Iteration Result: {result}");

                if (!result)
                {
                    break;
                }

                // Wait!
                Thread.Sleep(2000);
            }
        }
    }
}
