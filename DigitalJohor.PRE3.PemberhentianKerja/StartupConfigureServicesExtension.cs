using DigitalJohor.PRE3.DataImport.PemberhentianKerja;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalJohor.PRE3.DataImport.PemberhentianKerja
{
    public static class StartupConfigureServicesExtension
    {
        public static void AddPBTService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PemberhentianKerjaSettings>(configuration.GetSection(nameof(PemberhentianKerjaSettings)));
            services.AddScoped<IImportService, PemberhentianKerjaService>();
        }
    }
}
