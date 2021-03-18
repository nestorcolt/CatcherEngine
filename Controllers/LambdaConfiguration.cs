using System;
using System.Collections.Generic;
using System.Text;
using SearchEngine.Configuration;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace SearchEngine.Configuration
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
