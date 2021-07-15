using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalJohor.PRE3.DataImport.KetuaKampung
{
    public static class StartupConfigureServicesExtension
    {
        public static void AddKetuaKampungService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KetuaKampungSettings>(configuration.GetSection(nameof(KetuaKampungSettings)));
            services.AddScoped<IImportService, KetuaKampungService>();
        }
    }
}
