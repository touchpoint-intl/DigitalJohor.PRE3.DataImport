using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalJohor.PRE3.DataImport.LHDN
{
    public static class StartupConfigureServicesExtension
    {
        public static void AddLHDNService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<LhdnSettings>(configuration.GetSection(nameof(LhdnSettings)));
            services.AddScoped<IImportService, LhdnService>();
        }
    }
}
