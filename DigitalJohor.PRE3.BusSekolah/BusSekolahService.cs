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

        public async Task CreateNewFormAsync(BusSekolahDTO dto)
        {
            if (string.IsNullOrEmpty(dto.NoKadPengenalan))
            {
                throw new InvalidOperationException("IC is required");
            }

            var sanitizeIC = dto.NoKadPengenalan.Replace(" ", string.Empty).Replace("-", string.Empty);

            if (string.IsNullOrEmpty(sanitizeIC))
            {
                throw new InvalidOperationException("IC is required");
            }

            if (string.IsNullOrEmpty(dto.NamaPenuh))
            {
                throw new InvalidOperationException("Name is required");
            }

            var form = await _dbContext.Forms.FirstOrDefaultAsync(x => x.IdentityNumber == sanitizeIC);
            if (form == null)
            {

                form = new Form();
                form.Name = dto.NamaPenuh;
                form.IdentityNumber = sanitizeIC;


                var phones = dto.NoTel.Split("/", StringSplitOptions.RemoveEmptyEntries);

                for (var i = 0; i < phones.Length; i++)
                {
                    var sanitizePhone = phones[i];

                    if (!string.IsNullOrEmpty(sanitizePhone))
                    {
                        sanitizePhone = sanitizePhone.Replace(" ", string.Empty).Replace("-", string.Empty);

                        if (sanitizePhone.StartsWith("+"))
                        {
                            sanitizePhone = sanitizePhone.Remove(0, 1);
                        }
                        if (sanitizePhone.StartsWith("60"))
                        {
                            sanitizePhone = sanitizePhone.Remove(0, 2);
                        }
                        if (sanitizePhone.StartsWith("0"))
                        {
                            sanitizePhone = sanitizePhone.Remove(0, 1);
                        }
                        sanitizePhone = "+60" + sanitizePhone;
                    }

                    if (!string.IsNullOrEmpty(sanitizePhone))
                    {
                        if (i == 0)
                        {
                            form.Phone = sanitizePhone.Trim();
                        }
                        else
                        {
                            form.Phone2 = sanitizePhone.Trim();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(dto.Emel))
                {
                    form.Email = dto.Emel;
                }

                form.Address = dto.AlamatKediaman;

                if (!string.IsNullOrEmpty(dto.Parlimen))
                {
                    form.District = dto.Parlimen;
                }

                if (!string.IsNullOrEmpty(dto.DUN))
                {
                    form.Mukim = dto.DUN;
                }

                if (!string.IsNullOrEmpty(dto.Negeri))
                {
                    if (dto.Negeri == "Johor")
                    {
                        form.StateId = 2; //Johor
                    }
                    else
                    {
                        form.StateId = 1;
                    }
                }

                form.AccountNumber = dto.NoAkaunBank;
                form.AccountBeneficiary = dto.NamaPenuh;
                form.ApplicationId = 1;

                var sanitizeBank = dto.Bank.Replace(" ", string.Empty).Replace("\t", string.Empty);
                if (!string.IsNullOrEmpty(sanitizeBank))
                {
                    if (sanitizeBank == "AffinBank")
                    {
                        form.BankId = 31;
                    }
                    else if (sanitizeBank == "Agrobank")
                    {
                        form.BankId = 19;
                    }
                    else if (sanitizeBank == "AllianceBank")
                    {
                        form.BankId = 30;
                    }
                    else if (sanitizeBank == "AmBank")
                    {
                        form.BankId = 14;
                    }
                    else if (sanitizeBank == "BankIslamMalaysia")
                    {
                        form.BankId = 10;
                    }
                    else if (sanitizeBank == "BankRakyat")
                    {
                        form.BankId = 11;
                    }
                    else if (sanitizeBank == "BankSimpananNasional(BSN)")
                    {
                        form.BankId = 11;
                    }
                    else if (sanitizeBank == "CIMBBank")
                    {
                        form.BankId = 7;
                    }
                    else if (sanitizeBank == "HongLeongBank")
                    {
                        form.BankId = 25;
                    }
                    else if (sanitizeBank == "Maybank")
                    {
                        form.BankId = 9;
                    }
                    else if (sanitizeBank == "MUAMALAT")
                    {
                        form.BankId = 28;
                    }
                    else if (sanitizeBank == "PublicBankBerhad")
                    {
                        form.BankId = 4;
                    }
                    else if (sanitizeBank == "RHBBank")
                    {
                        form.BankId = 27;
                    }
                    else if (sanitizeBank == "Al-RajhiBank")
                    {
                        form.BankId = 33;
                    }
                    else if (sanitizeBank == "HSBCBankMalaysia")
                    {
                        form.BankId = 34;
                    }
                    else if (sanitizeBank == "OCBCBankMalaysia")
                    {
                        form.BankId = 35;
                    }
                    else if (sanitizeBank == "StandardCharteredBankMalaysia")
                    {
                        form.BankId = 36;
                    }
                    else if (sanitizeBank == "UOBMalaysiaBank")
                    {
                        form.BankId = 37;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Invalid bank id");
                }

                if (!string.IsNullOrEmpty(dto.NamaSyarikat))
                {
                    form.CompanyName = dto.NamaSyarikat;
                }

                if (!string.IsNullOrEmpty(dto.NoPendaftaranSyarikat))
                {
                    form.CompanyRegistrationNumber = dto.NoPendaftaranSyarikat;
                }

                if (!string.IsNullOrEmpty(dto.NoLesenPSV))
                {
                    form.PSVLicenseNumber = dto.NoLesenPSV;
                }

                if (!string.IsNullOrEmpty(dto.TarikhTamatLesenPSV))
                {
                    form.PSVLicenseExpiredDate = DateTime.ParseExact(dto.TarikhTamatLesenPSV, "dd/MM/yyyy", null);
                }

                form.CitizenshipId = 1;
                form.IdentityTypeId = 20; //MyKad
                form.RefNo = "PRE3000000" + form.IdentityNumber.Substring(form.IdentityNumber.Length - 4); ;

                await _dbContext.Forms.AddAsync(form);

                var initiative = await _dbContext.Initiatives.FirstOrDefaultAsync(x => x.Id == 13);

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

                await _dbContext.SaveChangesAsync("import service");
            }
            else
            {
                var initiative = await _dbContext.Initiatives.FirstOrDefaultAsync(x => x.Id == 13);
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

                    await _dbContext.SaveChangesAsync("import service");
                }
            }

        }

        private async Task<IEnumerable<BusSekolahDTO>> GetAllFromOrignalDataSourceAsync()
        {

            var list = new List<BusSekolahDTO>();


            _logger.LogInformation("Getting Connection ...");

            //create instanace of database connection
            var connection = new SqlConnection(_settings.ConnectionString);

            string query = @"SELECT * FROM BasSekolah ";

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
                        var dto = new BusSekolahDTO();
                        if (!reader.IsDBNull(5))
                        {
                            dto.NamaPenuh = reader.GetString(5);
                        }
                        if (!reader.IsDBNull(6))
                        {
                            dto.NoKadPengenalan = reader.GetString(6);
                        }
                        if (!reader.IsDBNull(8))
                        {
                            dto.Emel = reader.GetString(8);
                        }
                        if (!reader.IsDBNull(9))
                        {
                            dto.Warganegara = reader.GetString(9);
                        }
                        if (!reader.IsDBNull(12))
                        {
                            dto.Bangsa = reader.GetString(12);
                        }
                        if (!reader.IsDBNull(16))
                        {
                            dto.AlamatKediaman = reader.GetString(16);
                        }
                        if (!reader.IsDBNull(19))
                        {
                            dto.Negeri = reader.GetString(19);
                        }
                        if (!reader.IsDBNull(20))
                        {
                            dto.DUN = reader.GetString(20);
                        }
                        if (!reader.IsDBNull(21))
                        {
                            dto.Parlimen = reader.GetString(21);
                        }
                        if (!reader.IsDBNull(22))
                        {
                            dto.NoTel = reader.GetString(22);
                        }
                        if (!reader.IsDBNull(23))
                        {
                            dto.Bank = reader.GetString(23);
                        }
                        if (!reader.IsDBNull(24))
                        {
                            dto.NoAkaunBank = reader.GetString(24);
                        }
                        if (!reader.IsDBNull(26))
                        {
                            dto.NoLesenPSV = reader.GetString(26);
                        }
                        if (!reader.IsDBNull(27))
                        {
                            dto.TarikhTamatLesenPSV = reader.GetString(27);
                        }
                        if (!reader.IsDBNull(28))
                        {
                            dto.NoPlatKenderaan = reader.GetString(28);
                        }
                        if (!reader.IsDBNull(29))
                        {
                            dto.NamaSyarikat = reader.GetString(29);
                        }
                        if (!reader.IsDBNull(30))
                        {
                            dto.NoPendaftaranSyarikat = reader.GetString(30);
                        }

                        //display retrieved record
                        list.Add(dto);
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
