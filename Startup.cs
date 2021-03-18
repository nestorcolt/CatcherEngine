using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SearchEngine.Configuration;
using SearchEngine.Modules;

namespace SearchEngine
{
    public class StartUp
    {
        public static IServiceCollection Container => ConfigureServices(LambdaConfiguration.Configuration);

        private static IServiceCollection ConfigureServices(IConfigurationRoot root)
        {
            var services = new ServiceCollection();

            // Client factory: Typed Client
            services.AddHttpClient<IApiHandler, ApiHandler>(c =>
            {
                c.BaseAddress = new Uri(Constants.ApiBaseUrl);
                c.DefaultRequestHeaders.Accept.Clear();
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            // DI happens here!
            services.AddTransient<IApiHandler, ApiHandler>();
            services.AddTransient<ILambdaConfiguration, LambdaConfiguration>();

            return services;
        }
    }
}
