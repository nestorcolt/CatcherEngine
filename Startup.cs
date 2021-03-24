using CloudLibrary.Controllers;
using CloudLibrary.lib;
using CloudLibrary.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SearchEngine.Configuration;
using System;
using System.Net.Http.Headers;

namespace SearchEngine
{
    public class StartUp
    {
        public static IServiceCollection Container => ConfigureServices(LambdaConfiguration.Configuration);

        private static IServiceCollection ConfigureServices(IConfigurationRoot root)
        {
            // Init services for DI
            var services = new ServiceCollection();

            // Client factory: Typed Client
            services.AddHttpClient<IApiHandler, ApiHandler>(c =>
            {
                c.BaseAddress = new Uri(Constants.ApiBaseUrl);
                c.DefaultRequestHeaders.Accept.Clear();
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            // DI happens here!
            services.AddTransient<ILambdaConfiguration, LambdaConfiguration>();
            services.AddTransient<IAuthenticator, Authenticator>();
            services.AddTransient<IBlockCatcher, BlockCatcher>();

            return services;
        }
    }
}