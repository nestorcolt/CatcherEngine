﻿using System;
using System.Threading.Tasks;

namespace SearchEngine.Modules
{
    static class CloudLogger
    {
        public static async Task Log(string message, string userId)
        {
            await SnsHandler.PublishToSnsAsync(message, String.Format(Constants.UserLogStreamName, userId), Constants.LogToCloudTopic);
        }
    }
}