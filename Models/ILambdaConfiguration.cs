using Microsoft.Extensions.Configuration;

namespace SearchEngine.Models
{
    interface ILambdaConfiguration
    {
        IConfigurationRoot Configuration { get; }
    }
}
