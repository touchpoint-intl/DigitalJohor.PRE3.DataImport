using DigitalJohor.PRE3.DataImport.DummyJ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalJohor.PRE3.DataImport.DummyJ
{
    public static class StartupConfigureServicesExtension
    {
        public static void AddDummyJService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DummyJSettings>(configuration.GetSection(nameof(DummyJSettings)));
            services.AddScoped<IImportService, DummyJService>();
        }
    }
}
