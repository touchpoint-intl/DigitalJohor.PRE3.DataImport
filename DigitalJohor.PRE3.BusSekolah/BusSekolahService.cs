using System;
using DigitalJohor.PRE3.DataImport;
using DigitalJohor.PRE3.EFCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalJohor.PRE3.BusSekolah
{
    public class BusSekolahService : IImportService
    {
        private readonly DigitalJohorPRE3DbContext _dbContext;
        private readonly ILogger<BusSekolahService> _logger;
        private readonly BusSekolahSettings _settings;

        public BusSekolahService(
            DigitalJohorPRE3DbContext dbContext,
            ILogger<BusSekolahService> logger,
            IOptions<BusSekolahSettings> settingsOptions)
        {
            _dbContext = dbContext;
            _logger = logger;
            _settings = settingsOptions.Value;
        }
    }
}
