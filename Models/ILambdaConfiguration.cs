using Microsoft.Extensions.Configuration;

namespace SearchEngine.Configuration
{
    interface ILambdaConfiguration
    {
        IConfigurationRoot Configuration { get; }
    }
}
