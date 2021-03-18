﻿using Microsoft.Extensions.Configuration;
using SearchEngine.Models;
using System.IO;

namespace SearchEngine.Controllers
{
    public class LambdaConfiguration : ILambdaConfiguration
    {
        public static IConfigurationRoot Configuration => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        IConfigurationRoot ILambdaConfiguration.Configuration => Configuration;
    }
}
