using Microsoft.Extensions.Configuration;

namespace SearchEngine.Configuration
{
    public interface ILambdaConfiguration
    {
        IConfigurationRoot Configuration { get; }
    }
}
