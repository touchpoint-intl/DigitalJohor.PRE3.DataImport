using DigitalJohor.PRE3.DataImport.BusSekolah;
using DigitalJohor.PRE3.DataImport.KetuaKampung;
using DigitalJohor.PRE3.EFCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DigitalJohor.PRE3.DataImport
{
    public class Program
    {

        public static async Task Main()
        {
#if DEBUG
            Debugger.Launch();
#endif
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();


            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context,builder) =>
                {
                    var directory = Directory.GetCurrentDirectory();

                    builder.SetBasePath(directory);
                    builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    builder.AddEnvironmentVariables();

                })
                .ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
                {
                    var configuration = context.Configuration;

                    services.AddDigitalJohorPRE3EFCore(configuration);

                    //services.AddKetuaKampungService(configuration);

                    services.AddBusSekolahService(configuration);

                })
                .UseSerilog()
                .Build();


            try
            {
                var importServices = host.Services.GetServices<IImportService>();
                foreach(var importService in importServices)
                {
                    await importService.ImportAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "There was a problem starting the service");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
