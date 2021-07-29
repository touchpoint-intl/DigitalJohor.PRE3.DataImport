using DigitalJohor.PRE3.DataImport.Penerima_JM;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalJohor.PRE3.DataImport.Penerima_JM
{
    public static class StartupConfigureServicesExtension
    {
        public static void AddPenerimaPREJMService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PenerimaPREJMSettings>(configuration.GetSection(nameof(PenerimaPREJMSettings)));
            services.AddScoped<IImportService, PenerimaPREJMService>();
        }
    }
}
