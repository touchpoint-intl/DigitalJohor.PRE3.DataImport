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

namespace DigitalJohor.PRE3.DataImport.JTKK3
{
    public class JTKK3Service : IImportService
    {
        private readonly DigitalJohorPRE3DbContext _dbContext;
        private readonly ILogger<JTKK3Service> _logger;
        private readonly JTKK3Settings _settings;

        public JTKK3Service(
            DigitalJohorPRE3DbContext dbContext,
            ILogger<JTKK3Service> logger,
            IOptions<JTKK3Settings> settingsOptions)
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

        public async Task CreateNewFormAsync(JTKKDTO dto)
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

            if (string.IsNullOrEmpty(dto.NamePenerima))
            {
                throw new InvalidOperationException("Name is required");
            }

            var form = await _dbContext.Forms.FirstOrDefaultAsync(x => x.IdentityNumber == sanitizeIC);
            if (form == null)
            {

                form = new Form();
                form.Name = dto.NamePenerima;
                form.IdentityNumber = sanitizeIC;

                if (!string.IsNullOrEmpty(dto.Jantina))
                {
                    if (dto.Jantina == "M")
                    {
                        form.Gender = Enumerations.Gender.Lelaki;
                    }
                    else if (dto.Jantina == "F")
                    {
                        form.Gender = Enumerations.Gender.Perempuan;
                    }
                }
                if (!string.IsNullOrEmpty(dto.Bangsa))
                {
                    if (dto.Bangsa == "Malay")
                    {
                        form.RaceId = 1;
                    }
                    else if (dto.Bangsa == "Chinese")
                    {
                        form.RaceId = 2;
                    }
                    else if (dto.Bangsa == "India")
                    {
                        form.RaceId = 3;
                    }
                    else if (dto.Bangsa == "Bumiputera Sarawak")
                    {
                        form.RaceId = 10;
                    }
                    else if (dto.Bangsa == "Orang Asli (Peninsular)")
                    {
                        form.RaceId = 11;
                    }
                    else if (dto.Bangsa == "Bumiputera Sabah")
                    {
                        form.RaceId = 12;
                    }
                    else if (dto.Bangsa == "Lain-lain")
                    {
                        form.RaceId = 4;
                    }
                }

                var birthYear = Convert.ToInt32($"19{(dto.NoKadPengenalan.Substring(0, 2))}");
                form.Age = DateTime.Now.Year - birthYear;

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

                form.Address = dto.Alamat;

                if (!string.IsNullOrEmpty(dto.Parlimen))
                {
                    form.District = dto.Parlimen;
                }

                if (!string.IsNullOrEmpty(dto.DUN))
                {
                    form.Mukim = dto.DUN;
                }

                form.AccountNumber = dto.NoBankAkaun;
                form.AccountBeneficiary = dto.NamePenerima;
                form.ApplicationId = 1;

                var sanitizeBank = dto.BankName;
                if (!string.IsNullOrEmpty(sanitizeBank))
                {
                    if (sanitizeBank == "AFFIN BANK BERHAD")
                    {
                        form.BankId = 31;
                    }
                    else if (sanitizeBank == "AGRO BANK / BANK PERTANIAN MALAYSIA")
                    {
                        form.BankId = 19;
                    }
                    else if (sanitizeBank == "ALLIANCE BANK BERHAD")
                    {
                        form.BankId = 30;
                    }
                    else if (sanitizeBank == "AMBANK BERHAD")
                    {
                        form.BankId = 14;
                    }
                    else if (sanitizeBank == "BANK ISLAM MALAYSIA")
                    {
                        form.BankId = 10;
                    }
                    else if (sanitizeBank == "BANK RAKYAT MALAYSIA")
                    {
                        form.BankId = 11;
                    }
                    else if (sanitizeBank == "BANK SIMPANAN NASIONAL")
                    {
                        form.BankId = 11;
                    }
                    else if (sanitizeBank == "CIMB BANK BERHAD")
                    {
                        form.BankId = 7;
                    }
                    else if (sanitizeBank == "HONG LEONG BANK")
                    {
                        form.BankId = 25;
                    }
                    else if (sanitizeBank == "MALAYAN BANKING BERHAD")
                    {
                        form.BankId = 9;
                    }
                    else if (sanitizeBank == "BANK MUAMALAT (MALAYSIA)")
                    {
                        form.BankId = 28;
                    }
                    else if (sanitizeBank == "PUBLIC BANK")
                    {
                        form.BankId = 4;
                    }
                    else if (sanitizeBank == "RHB Bank")
                    {
                        form.BankId = 27;
                    }
                    else if (sanitizeBank == "Al-RajhiBank")
                    {
                        form.BankId = 33;
                    }
                    else if (sanitizeBank == "HSBC (M) BERHAD")
                    {
                        form.BankId = 34;
                    }
                    else if (sanitizeBank == "OCBC BANK (M'SIA) BHD")
                    {
                        form.BankId = 35;
                    }
                    else if (sanitizeBank == "STANDARD CHARTERED  ")
                    {
                        form.BankId = 36;
                    }
                    else if (sanitizeBank == "UNITED OVERSEAS BANK")
                    {
                        form.BankId = 37;
                    }
                    else if (sanitizeBank == "IND & COM BANK OF CHINA")
                    {
                        form.BankId = 38;
                    }
                    else if (sanitizeBank == "KUWAIT FINANCE HOUSE")
                    {
                        form.BankId = 39;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Invalid bank id");
                }

                if (!string.IsNullOrEmpty(dto.Majikan))
                {
                    form.CompanyName = dto.Majikan;
                }
                if (!string.IsNullOrEmpty(dto.AlamatMajikan))
                {
                    form.CompanyAddress = dto.AlamatMajikan;
                }
                if (!string.IsNullOrEmpty(dto.SektorMajikan))
                {
                    form.CompanySector = dto.SektorMajikan;
                }
                if (!string.IsNullOrEmpty(dto.PendapatanTerakhir))
                {
                    var pendapatanString = Convert.ToDouble(dto.PendapatanTerakhir.Replace(",", string.Empty));
                    var pendapatan = Convert.ToInt32(Math.Ceiling(pendapatanString));
                    if (pendapatan < 2501)
                    {
                        form.IncomeRangeId = 1;
                    }
                    else if (pendapatan > 2500 && pendapatan < 4001) {
                        form.IncomeRangeId = 3;
                    }
                    else if (pendapatan > 4000 && pendapatan < 5001) {
                        form.IncomeRangeId = 4;
                    }
                    else if (pendapatan > 5000) {
                        form.IncomeRangeId = 5;
                    }
                }

                if (!string.IsNullOrEmpty(dto.TerimaBantuanPerkeso))
                {
                    var dependent = Convert.ToInt32(dto.NoIsiRumahTanggungan);
                    if (dependent == 0)
                    {
                        form.DependentId = 10;
                    }
                    else if (dependent > 0 && dependent < 4)
                    {
                        form.DependentId = 11;
                    }
                    else if (dependent >= 4)
                    {
                        form.DependentId = 12;
                    }
                }

                if (!string.IsNullOrEmpty(dto.TerimaBantuanPerkeso))
                {
                    if (dto.TerimaBantuanPerkeso == "Yes")
                    {
                        form.BantuanPerkesoRecipient = true;
                    }
                    else
                    {
                        form.BantuanPerkesoRecipient = false;
                    }
                }

                if (!string.IsNullOrEmpty(dto.StatusPerkahwinan))
                {
                    if (dto.StatusPerkahwinan == "M")
                    {
                        form.MaritalStatusId = 4;
                    }
                    else if (dto.StatusPerkahwinan == "S")
                    {
                        form.MaritalStatusId = 5;
                    }
                    else if (dto.StatusPerkahwinan == "R")
                    {
                        form.MaritalStatusId = 6;
                    }
                    else if (dto.StatusPerkahwinan == "W")
                    {
                        form.MaritalStatusId = 7;
                    }
                }

                form.CitizenshipId = 1;
                form.IdentityTypeId = 20; //MyKad
                form.RefNo = "PRE3000000" + form.IdentityNumber.Substring(form.IdentityNumber.Length - 4);

                await _dbContext.Forms.AddAsync(form);

                var initiative = await _dbContext.Initiatives.FirstOrDefaultAsync(x => x.Id == 20);

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

                await _dbContext.SaveChangesAsync("JTKK3");
            }
            else
            {
                var initiative = await _dbContext.Initiatives.FirstOrDefaultAsync(x => x.Id == 20);
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

                    await _dbContext.SaveChangesAsync("JTKK3");
                }
            }

        }

        private async Task<IEnumerable<JTKKDTO>> GetAllFromOrignalDataSourceAsync()
        {

            var list = new List<JTKKDTO>();


            _logger.LogInformation("Getting Connection ...");

            //create instanace of database connection
            var connection = new SqlConnection(_settings.ConnectionString);

            string query = @"SELECT * FROM JTKK3";

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
                        var dto = new JTKKDTO();
                        if (!reader.IsDBNull(1))
                        {
                            dto.NamePenerima = reader.GetString(1);
                        }
                        if (!reader.IsDBNull(2))
                        {
                            dto.NoKadPengenalan = reader.GetString(2);
                        }
                        if (!reader.IsDBNull(4))
                        {
                            dto.Bangsa = reader.GetString(4);
                        }
                        if (!reader.IsDBNull(3))
                        {
                            dto.Jantina = reader.GetString(3);
                        }
                        if (!reader.IsDBNull(6))
                        {
                            dto.StatusPerkahwinan = reader.GetString(6);
                        }
                        if (!reader.IsDBNull(7))
                        {
                            dto.NoIsiRumahTanggungan = reader.GetString(7);
                        }
                        if (!reader.IsDBNull(8))
                        {
                            dto.Alamat = reader.GetString(8);
                        }
                        if (!reader.IsDBNull(9))
                        {
                            dto.NoTel = reader.GetString(9);
                        }
                        if (!reader.IsDBNull(10))
                        {
                            dto.DUN = reader.GetString(10);
                        }
                        if (!reader.IsDBNull(11))
                        {
                            dto.Parlimen = reader.GetString(11);
                        }
                        if (!reader.IsDBNull(15))
                        {
                            dto.BankName = reader.GetString(15);
                        }
                        if (!reader.IsDBNull(14))
                        {
                            dto.NoBankAkaun = reader.GetString(14);
                        }
                        if (!reader.IsDBNull(16))
                        {
                            dto.TerimaBantuanPerkeso = reader.GetString(16);
                        }
                        if (!reader.IsDBNull(17))
                        {
                            dto.PendapatanTerakhir = reader.GetString(17);
                        }
                        if (!reader.IsDBNull(18))
                        {
                            dto.Majikan = reader.GetString(18);
                        }
                        if (!reader.IsDBNull(19))
                        {
                            dto.AlamatMajikan = reader.GetString(19);
                        }
                        if (!reader.IsDBNull(20))
                        {
                            dto.SektorMajikan = reader.GetString(20);
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
