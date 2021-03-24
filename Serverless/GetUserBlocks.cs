using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using CloudLibrary.Lib;
using CloudLibrary.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace SearchEngine.Serverless
{
    class GetUserBlocks
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBlockCatcher _blockCatcher;

        public GetUserBlocks(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public GetUserBlocks() : this(StartUp.Container.BuildServiceProvider())
        {
            _blockCatcher = _serviceProvider.GetService<IBlockCatcher>();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<string> FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            // Function logic
            UserDto userDto = JsonConvert.DeserializeObject<UserDto>(snsEvent.Records[0].Sns.Message);
            bool result = false;

            // handle a possible timeout
            Thread handleTimeOuThread = new Thread(async () => await GetRemainingTime(userDto.UserId, context));
            handleTimeOuThread.Start();

            try
            {
                result = await _blockCatcher.LookingForBlocks(userDto);
            }
            catch (Exception e)
            {
                Thread logThread = new Thread(async () => await CloudLogger.Log(e.ToString(), userDto.UserId));
                logThread.Start();
            }

            if (!result)
            {
                // update last iteration value (in case we need to skip this user for X period of time while some process occur)
                await DynamoHandler.UpdateUserTimestamp(userDto.UserId, _blockCatcher.GetTimestamp());
            }

            return "OK";
        }

        private async Task GetRemainingTime(string userid, ILambdaContext context)
        {
            while (true)
            {
                if (context.RemainingTime.TotalMilliseconds <= 200)
                {
                    // update last iteration value (in case we need to skip this user for X period of time while some process occur)
                    await DynamoHandler.UpdateUserTimestamp(userid, _blockCatcher.GetTimestamp());
                    break;
                }
            }
        }
    }
}