using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalJohor.PRE3.DataImport;
using DigitalJohor.PRE3.EFCore;
using DigitalJohor.PRE3.EFCore.Entities.Forms;
using DigitalJohor.PRE3.Enumerations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DigitalJohor.PRE3.DataImport.DummyJ
{
    public class DummyJService : IImportService
    {
        private readonly DigitalJohorPRE3DbContext _dbContext;
        private readonly ILogger<DummyJService> _logger;
        private readonly DummyJSettings _settings;

        public DummyJService(
            DigitalJohorPRE3DbContext dbContext,
            ILogger<DummyJService> logger,
            IOptions<DummyJSettings> settingsOptions)
        {
            _dbContext = dbContext;
            _logger = logger;
            _settings = settingsOptions.Value;
        }

        public async Task ImportAsync()
        {
            var dtos = await GetAllFromOrignalDataSourceAsync();

            foreach (var dto in dtos)
            {
                try
                {
                    await CreateNewFormAsync(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"ImportAsync: {JsonConvert.SerializeObject(dto)}");
                }
            }
        }

        public async Task CreateNewFormAsync(DummyJDTO dto)
        {
            if (string.IsNullOrEmpty(dto.NO_KAD_PENGENALAN))
            {
                throw new InvalidOperationException("IC is required");
            }

            var sanitizeIC = dto.NO_KAD_PENGENALAN.Replace(" ", string.Empty).Replace("-", string.Empty);

            if (string.IsNullOrEmpty(sanitizeIC))
            {
                throw new InvalidOperationException("IC is required");
            }

            if (string.IsNullOrEmpty(dto.Name))
            {
                throw new InvalidOperationException("Name is required");
            }

            var form = await _dbContext.Forms.FirstOrDefaultAsync(x => x.IdentityNumber == sanitizeIC);
            if (form == null)
            {

                form = new Form();
                form.Name = dto.Name;
                form.IdentityNumber = sanitizeIC;

                var birthYear = Convert.ToInt32($"19{(dto.NO_KAD_PENGENALAN.Substring(0, 2))}");
                form.Age = DateTime.Now.Year - birthYear;
                form.Address = dto.Address;

                form.ApplicationId = 1;

                form.CitizenshipId = 1;
                form.IdentityTypeId = 20; //MyKad
                form.BshBpnRecipient = false;
                form.VaccinationStatus = VaccinationStatus.INPROCESS;
                form.BantuanPerkesoRecipient = false;
                form.RefNo = "PRE3000000" + form.IdentityNumber.Substring(form.IdentityNumber.Length - 4);

                await _dbContext.Forms.AddAsync(form);

                var iniId = 32;
                if (!string.IsNullOrEmpty(dto.Initiative))
                {
                    var sanitizeIni = dto.Initiative.Replace(".", string.Empty).Replace("\n", string.Empty);
                    if (sanitizeIni == "MENGURANGKAN BEBAN KEWANGAN SERAMAI 45,567 PENIAGA YANG TERJEJAS AKIBAT PANDEMIK COVID-19")
                    {
                        iniId = 33;
                    }
                    else if (sanitizeIni == "PENYEWA RUMAH PROGRAM PERUMAHAN RAKYAT (PPR) DAN RUMAH SEWA KERAJAAN (RSK) DIBERI PENGECUALIAN BAYARAN SEWA SELAMA SETAHUN SEHINGGA 31 DISEMBER 2021. INISIATIF INI MEMBERI MANFAAT KEPADA SERAMAI 8, 743 PENYEWA.")
                    {
                        iniId = 32;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM1,000 KEPADA 600 PEMANDU PELANCONG NEGERI JOHOR")
                    {
                        iniId = 31;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM1,000 KEPADA 551 PENGUSAHA HOMESTAY DAN CHALET NEGERI JOHOR")
                    {
                        iniId = 29;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM2,000 KEPADA 267 PENGUSAHA HOTEL BAJET NEGERI JOHOR YANG BERDAFTAR DENGAN TOURISM JOHOR")
                    {
                        iniId = 28;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA 19,090 PENYEWA PREMIS MILIK PIHAK BERKUASA TEMPATAN (PBT), PENIAGA PASAR MALAM, PASAR PAGI, PASAR TANI DAN PENJAJA YANG BERDAFTAR DENGAN PBT BAGI MEMBANTU MENGURANGKAN BEBAN KEWANGAN PENIAGA YANG TERJEJAS PENDAPATAN AKIKAT PANDEMIK COVID-19")
                    {
                        iniId = 12;
                    }
                    else if (sanitizeIni == "PROGRAM GEROBOK MAKANAN MANFAAT KEPADA 100 RIBU RAKYAT TERKESAN")
                    {
                        iniId = 25;
                    }
                    else if (sanitizeIni == "ELAUN KHAS KEPADA SOUTHERN VOLUNTEERS (SV) SEBANYAK RM50 SEHARI MELIBATKAN HAMPIR 400 PENGGERAK SETIAP HARI BAGI MENJALANKAN PROGRAM PENDAFTARAN VAKSIN DI SELURUH NEGERI JOHOR")
                    {
                        iniId = 26;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF KEPADA 22,071 PESERTA DALAM SENARAI e-Kasih")
                    {
                        iniId = 17;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA SERAMAI 1,197 PENGUSAHA KANTIN SEKOLAH KERAJAAN DAN SEKOLAH BANTUAN KERAJAAN")
                    {
                        iniId = 19;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA GOLONGAN YANG KEHILANGAN PEKERJAAN ATAU PENDAPATAN")
                    {
                        iniId = 20;
                    }
                    else if (sanitizeIni == "BANTUAN PROGRAM BAKUL MAKANAN BERNILAI RM100 KEPADA 20 RIBU RAKYAT")
                    {
                        iniId = 22;
                    }
                    else if (sanitizeIni == "BAUCAR KFC BERNILAI RM20 DIBERIKAN KEPADA GOLONGAN TERJEJAS SEPERTI MAHASISWA, ANAK YATIM PIATU, WARGA EMAS DAN LAIN-LAIN")
                    {
                        iniId = 24;
                    }
                    else if (sanitizeIni == "BANTUAN BAUCAR MAKANAN BARANGAN RM200 KEPADA 300 RIBU KELUARGA DALAM KATEGORI B40")
                    {
                        iniId = 23;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA 630 KETUA KAMPUNG")
                    {
                        iniId = 21;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA 4,202 PEMEGANG LESEN PREMIS GUNTING RAMBUT ATAU SALUN")
                    {
                        iniId = 14;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA 2,123 PEMANDU BAS DAN VAN YANG BERDAFTAR")
                    {
                        iniId = 13;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM2,000 KEPADA 267 PENGUSAHA HOTEL")
                    {
                        iniId = 11;
                    }
                }

                var initiative = await _dbContext.Initiatives.FirstOrDefaultAsync(x => x.Id == iniId);

                var formInitiative = new FormInitiative();
                formInitiative.Form = form;
                formInitiative.Initiative = initiative;
                formInitiative.FormStatus = FormStatus.Disahkan;

                await _dbContext.FormInitiatives.AddAsync(formInitiative);

                var formInitiativeLog = new FormInitiativeLog();
                formInitiativeLog.FormStatus = FormStatus.Disahkan;
                formInitiativeLog.FormInitiative = formInitiative;
                formInitiativeLog.Remarks = "Data dimuat naik";

                await _dbContext.FormInitiativeLogs.AddAsync(formInitiativeLog);

                await _dbContext.SaveChangesAsync("DummyJ");
            }
            else
            {
                var iniId = 0;
                if (!string.IsNullOrEmpty(dto.Initiative))
                {
                    var sanitizeIni = dto.Initiative.Replace(".", "").Replace("\n", string.Empty);
                    if (sanitizeIni == "MENGURANGKAN BEBAN KEWANGAN SERAMAI 45,567 PENIAGA YANG TERJEJAS AKIBAT PANDEMIK COVID-19")
                    {
                        iniId = 33;
                    }
                    else if (sanitizeIni == "PENYEWA RUMAH PROGRAM PERUMAHAN RAKYAT (PPR) DAN RUMAH SEWA KERAJAAN (RSK) DIBERI PENGECUALIAN BAYARAN SEWA SELAMA SETAHUN SEHINGGA 31 DISEMBER 2021. INISIATIF INI MEMBERI MANFAAT KEPADA SERAMAI 8, 743 PENYEWA")
                    {
                        iniId = 32;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM1,000 KEPADA 600 PEMANDU PELANCONG NEGERI JOHOR")
                    {
                        iniId = 31;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM1,000 KEPADA 551 PENGUSAHA HOMESTAY DAN CHALET NEGERI JOHOR")
                    {
                        iniId = 29;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM2,000 KEPADA 267 PENGUSAHA HOTEL BAJET NEGERI JOHOR YANG BERDAFTAR DENGAN TOURISM JOHOR")
                    {
                        iniId = 28;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA 19,090 PENYEWA PREMIS MILIK PIHAK BERKUASA TEMPATAN (PBT), PENIAGA PASAR MALAM, PASAR PAGI, PASAR TANI DAN PENJAJA YANG BERDAFTAR DENGAN PBT BAGI MEMBANTU MENGURANGKAN BEBAN KEWANGAN PENIAGA YANG TERJEJAS PENDAPATAN AKIKAT PANDEMIK COVID-19")
                    {
                        iniId = 12;
                    }
                    else if (sanitizeIni == "PROGRAM GEROBOK MAKANAN MANFAAT KEPADA 100 RIBU RAKYAT TERKESAN")
                    {
                        iniId = 25;
                    }
                    else if (sanitizeIni == "ELAUN KHAS KEPADA SOUTHERN VOLUNTEERS (SV) SEBANYAK RM50 SEHARI MELIBATKAN HAMPIR 400 PENGGERAK SETIAP HARI BAGI MENJALANKAN PROGRAM PENDAFTARAN VAKSIN DI SELURUH NEGERI JOHOR")
                    {
                        iniId = 26;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF KEPADA 22,071 PESERTA DALAM SENARAI e-Kasih")
                    {
                        iniId = 17;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA SERAMAI 1,197 PENGUSAHA KANTIN SEKOLAH KERAJAAN DAN SEKOLAH BANTUAN KERAJAAN")
                    {
                        iniId = 19;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA GOLONGAN YANG KEHILANGAN PEKERJAAN ATAU PENDAPATAN")
                    {
                        iniId = 20;
                    }
                    else if (sanitizeIni == "BANTUAN PROGRAM BAKUL MAKANAN BERNILAI RM100 KEPADA 20 RIBU RAKYAT")
                    {
                        iniId = 22;
                    }
                    else if (sanitizeIni == "BAUCAR KFC BERNILAI RM20 DIBERIKAN KEPADA GOLONGAN TERJEJAS SEPERTI MAHASISWA, ANAK YATIM PIATU, WARGA EMAS DAN LAIN-LAIN")
                    {
                        iniId = 24;
                    }
                    else if (sanitizeIni == "BANTUAN BAUCAR MAKANAN BARANGAN RM200 KEPADA 300 RIBU KELUARGA DALAM KATEGORI B40")
                    {
                        iniId = 23;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA 630 KETUA KAMPUNG")
                    {
                        iniId = 21;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA 4,202 PEMEGANG LESEN PREMIS GUNTING RAMBUT ATAU SALUN")
                    {
                        iniId = 14;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM500 KEPADA 2,123 PEMANDU BAS DAN VAN YANG BERDAFTAR")
                    {
                        iniId = 13;
                    }
                    else if (sanitizeIni == "BANTUAN ONE-OFF RM2,000 KEPADA 267 PENGUSAHA HOTEL")
                    {
                        iniId = 11;
                    }
                }
                var initiative = await _dbContext.Initiatives.FirstOrDefaultAsync(x => x.Id == iniId);
                var formIni = await _dbContext.FormInitiatives
                    .Include(x => x.Form)
                    .Include(x => x.Initiative)
                    .FirstOrDefaultAsync(x => x.Form == form && x.Initiative == initiative);

                if (formIni == null)
                {
                    var formInitiative = new FormInitiative();
                    formInitiative.Form = form;
                    formInitiative.Initiative = initiative;
                    formInitiative.FormStatus = FormStatus.Disahkan;

                    await _dbContext.FormInitiatives.AddAsync(formInitiative);

                    var formInitiativeLog = new FormInitiativeLog();
                    formInitiativeLog.FormStatus = FormStatus.Disahkan;
                    formInitiativeLog.FormInitiative = formInitiative;
                    formInitiativeLog.Remarks = "Data dimuat naik";

                    await _dbContext.FormInitiativeLogs.AddAsync(formInitiativeLog);

                    await _dbContext.SaveChangesAsync("DummyJ");
                }
            }

        }

        private async Task<IEnumerable<DummyJDTO>> GetAllFromOrignalDataSourceAsync()
        {

            var list = new List<DummyJDTO>();


            _logger.LogInformation("Getting Connection ...");

            //create instanace of database connection
            var connection = new SqlConnection(_settings.ConnectionString);

            string query = @"SELECT [NAME],[IC],[ADDRESS],[BANDAR],[INITIATIVE] FROM DummyJohor where NO between 279 and 399";

            //define the SqlCommand object
            var command = new SqlCommand(query, connection);

            try
            {
                Console.WriteLine("Openning Connection ...");

                //open connection
                await connection.OpenAsync();

                Console.WriteLine("Connection successful!");

                //execute the SQLCommand
                var reader = await command.ExecuteReaderAsync();
                Console.WriteLine(Environment.NewLine + "Retrieving data from database..." + Environment.NewLine);

                var count = 0;
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        count++;
                        var dto = new DummyJDTO();
                        if (!reader.IsDBNull(0))
                        {
                            dto.Name = reader.GetString(0);
                        }
                        if (!reader.IsDBNull(1))
                        {
                            dto.NO_KAD_PENGENALAN = reader.GetString(1);
                        }
                        if (!reader.IsDBNull(2))
                        {
                            dto.Address = reader.GetString(2);
                        }
                        if (!reader.IsDBNull(3))
                        {
                            dto.Bandar = reader.GetString(3);
                        }
                        if (!reader.IsDBNull(4))
                        {
                            dto.Initiative = reader.GetString(4);
                        }

                        //display retrieved record
                        list.Add(dto);
                        Console.WriteLine(dto);
                    }
                    Console.WriteLine("Retrieved records count: " + count);
                }
                else
                {
                    Console.WriteLine("No data found.");
                }

                await reader.CloseAsync();

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                await connection.CloseAsync();
            }

            return list;
        }
    }
}
