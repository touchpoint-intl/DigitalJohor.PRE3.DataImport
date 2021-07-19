using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalJohor.PRE3.DataImport.JTKK3
{
    public static class StartupConfigureServicesExtension
    {
        public static void AddJTKK3Service(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JTKK3Settings>(configuration.GetSection(nameof(JTKK3Settings)));
            services.AddScoped<IImportService, JTKK3Service>();
        }
    }
}
