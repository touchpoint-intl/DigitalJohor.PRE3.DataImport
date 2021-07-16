using System;
using DigitalJohor.PRE3.BusSekolah;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalJohor.PRE3.DataImport.BusSekolah
{
    public static class StartupConfigureServicesExtension
    {
        public static void AddBusSekolahService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BusSekolahSettings>(configuration.GetSection(nameof(BusSekolahSettings)));
            services.AddScoped<IImportService, BusSekolahService>();
        }
    }
}
