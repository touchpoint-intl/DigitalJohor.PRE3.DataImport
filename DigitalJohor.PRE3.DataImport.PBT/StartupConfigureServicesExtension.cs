using DigitalJohor.PRE3.DataImport.PBT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalJohor.PRE3.DataImport.PBT
{
    public static class StartupConfigureServicesExtension
    {
        public static void AddPBTService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PBTSettings>(configuration.GetSection(nameof(PBTSettings)));
            services.AddScoped<IImportService, PBTService>();
        }
    }
}
