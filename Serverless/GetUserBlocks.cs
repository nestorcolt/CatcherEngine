using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using CloudLibrary.Lib;
using CloudLibrary.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;


// The Main program for looking, catching and accepting blocks for the amazon flex service. Automate the process and handle a single user process instance and this needs
// to be run per user request. (Ideally on a Lambda function over the AWS architecture)

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
            UserDto userDto = JsonConvert.DeserializeObject<UserDto>(snsEvent.Records[0].Sns.Message);
            bool result = false;

            try
            {
                result = await _blockCatcher.LookingForBlocks(userDto);
            }
            catch (Exception e)
            {
                await CloudLogger.Log(e.ToString(), userDto.UserId);
            }

            if (!result)
            {
                // update last iteration value (in case we need to skip this user for X period of time while some process occur)
                await DynamoHandler.UpdateUserTimestamp(userDto.UserId, _blockCatcher.GetTimestamp());
            }

            return "OK";
        }
    }
}