using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalJohor.PRE3.EFCore;
using DigitalJohor.PRE3.EFCore.Entities.Forms;
using DigitalJohor.PRE3.Enumerations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DigitalJohor.PRE3.DataImport.PBT
{
    public class PBTService : IImportService
    {
        private readonly DigitalJohorPRE3DbContext _dbContext;
        private readonly ILogger<PBTService> _logger;
        private readonly PBTSettings _settings;

        public PBTService(
            DigitalJohorPRE3DbContext dbContext,
            ILogger<PBTService> logger,
            IOptions<PBTSettings> settingsOptions)
        {
            _dbContext = dbContext;
            _logger = logger;
            _settings = settingsOptions.Value;
        }

        public async Task ImportAsync()
        {
            var dtos = await GetAllFromOrignalDataSourceAsync();

            for(var i=0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                try
                {
                    await CreateForm(dto);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, $"PBT: {JsonConvert.SerializeObject(dto)}");
                }
            }
            await _dbContext.SaveChangesAsync("PBT");
        }

        private async Task CreateForm(PBTDTO dto)
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

            if (string.IsNullOrEmpty(dto.Nama))
            {
                throw new InvalidOperationException("Name is required");
            }

            var form = await _dbContext.Forms
                                       .Include(a => a.FormInitiatives)
                                       .FirstOrDefaultAsync(x => x.IdentityNumber == sanitizeIC);

            if(form == null)
            {
                form = new Form();
                form.IdentityNumber = sanitizeIC;
                form.Name = dto.Nama;
                form.ApplicationId = 1;

                var birthYear = Convert.ToInt32($"19{(dto.NO_KAD_PENGENALAN.Substring(0, 2))}");
                form.Age = DateTime.Now.Year - birthYear;

                if (!string.IsNullOrEmpty(dto.NO_TEL))
                {
                    var phones = dto.NO_TEL.Split("/", StringSplitOptions.RemoveEmptyEntries);

                    for (var i = 0; i < phones.Length; i++)
                    {
                        var sanitizePhone = phones[i];
                        if (!int.TryParse(sanitizePhone, out var iphone)){
                            continue;
                        }

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

                }

                if (!string.IsNullOrEmpty(dto.BANGSA))
                {
                    if (dto.BANGSA == "MEALYU" || dto.BANGSA == "Melayu")
                    {
                        form.RaceId = 1;
                    }
                    else if (dto.BANGSA == "CINA" || dto.BANGSA == "CHINA")
                    {
                        form.RaceId = 2;
                    }
                    else if (dto.BANGSA == "India")
                    {
                        form.RaceId = 3;
                    }
                    else if (dto.BANGSA == "Bumiputera Sarawak" || dto.BANGSA == "IBAN")
                    {
                        form.RaceId = 10;
                    }
                    else if (dto.BANGSA == "Orang Asli (Peninsular)" || dto.BANGSA == "Peribumi")
                    {
                        form.RaceId = 11;
                    }
                    else if (dto.BANGSA == "Bumiputera Sabah" || dto.BANGSA == "KADAZAN")
                    {
                        form.RaceId = 12;
                    }
                    else if (dto.BANGSA == "Lain-lain")
                    {
                        form.RaceId = 4;
                    }
                }

                var sanitizeBank = dto.NAMA_BANK;
                if (!string.IsNullOrEmpty(dto.NAMA_BANK))
                {
                    var str = sanitizeBank.Substring(0, 2);
                    bool isNumber = int.TryParse(str, out int num);
                    if (!isNumber)
                    {
                        sanitizeBank = dto.NAMA_BANK;
                    }
                    else
                    {
                        sanitizeBank = dto.NO_AKAUN_BANK;
                    }

                    sanitizeBank = sanitizeBank.ToLower();
                    if (!string.IsNullOrEmpty(sanitizeBank))
                    {
                        if (sanitizeBank.Contains("affin"))
                        {
                            form.BankId = 31;
                        }
                        else if (sanitizeBank.Contains("agro"))
                        {
                            form.BankId = 19;
                        }
                        else if (sanitizeBank.Contains("alliance"))
                        {
                            form.BankId = 30;
                        }
                        else if (sanitizeBank.Contains("ambank"))
                        {
                            form.BankId = 14;
                        }
                        else if ((sanitizeBank.Contains("bank") && sanitizeBank.Contains("islam")) || sanitizeBank.Contains("bank islam"))
                        {
                            form.BankId = 10;
                        }
                        else if (sanitizeBank.Contains("rakyat"))
                        {
                            form.BankId = 11;
                        }
                        else if (sanitizeBank.Contains("simpanan") || sanitizeBank.Contains("bsn"))
                        {
                            form.BankId = 11;
                        }
                        else if (sanitizeBank.Contains("cim"))
                        {
                            form.BankId = 7;
                        }
                        else if (sanitizeBank.Contains("leong"))
                        {
                            form.BankId = 25;
                        }
                        else if (sanitizeBank.Contains("may"))
                        {
                            form.BankId = 9;
                        }
                        else if (sanitizeBank.Contains("muamalat"))
                        {
                            form.BankId = 28;
                        }
                        else if (sanitizeBank.Contains("public"))
                        {
                            form.BankId = 4;
                        }
                        else if (sanitizeBank.Contains("rhb"))
                        {
                            form.BankId = 27;
                        }
                        else if (sanitizeBank == "Al-RajhiBank")
                        {
                            form.BankId = 33;
                        }
                        else if (sanitizeBank.Contains("hsbc"))
                        {
                            form.BankId = 34;
                        }
                        else if (sanitizeBank.Contains("cbc"))
                        {
                            form.BankId = 35;
                        }
                        else if (sanitizeBank.Contains("standard"))
                        {
                            form.BankId = 36;
                        }
                        else if (sanitizeBank.Contains("uob") || sanitizeBank.Contains("united"))
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
                    else if (sanitizeBank == null)
                    {
                        form.BankId = null;
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid bank id");
                    }
                }
                if (!string.IsNullOrEmpty(dto.NO_AKAUN_BANK))
                {
                    bool containsInt = dto.NO_AKAUN_BANK.Any(char.IsDigit);
                    if (containsInt)
                    {
                        form.AccountNumber = dto.NO_AKAUN_BANK;
                    }
                    else
                    {
                        form.AccountNumber = dto.NAMA_BANK;
                    }
                }

                


                if (!string.IsNullOrEmpty(dto.JENIS))
                {
                    form.CompanyName = dto.JENIS;
                }

                if (!string.IsNullOrEmpty(dto.PBT))
                {
                    if (dto.PBT == "FAMA ENDAU")
                    {
                        form.PihakBerkuasaTempatanId = 1;
                    }
                    else if (dto.PBT == "MDM")
                    {
                        form.PihakBerkuasaTempatanId = 2;
                    }
                    else if (dto.PBT == "MPM")
                    {
                        form.PihakBerkuasaTempatanId = 3;
                    }
                    else if (dto.PBT == "MPKU")
                    {
                        form.PihakBerkuasaTempatanId = 4;
                    }
                    else if (dto.PBT == "MPS")
                    {
                        form.PihakBerkuasaTempatanId = 5;
                    }
                    else if (dto.PBT == "FAMA SEGAMAT")
                    {
                        form.PihakBerkuasaTempatanId = 6;
                    }
                    else if (dto.PBT == "FAMA KOTA TINGGI")
                    {
                        form.PihakBerkuasaTempatanId = 7;
                    }
                    else if (dto.PBT == "MBIP")
                    {
                        form.PihakBerkuasaTempatanId = 8;
                    }
                    else if (dto.PBT == "MDYP")
                    {
                        form.PihakBerkuasaTempatanId = 9;
                    }
                    else if (dto.PBT == "MBPG")
                    {
                        form.PihakBerkuasaTempatanId = 10;
                    }
                    else if (dto.PBT == "MDT")
                    {
                        form.PihakBerkuasaTempatanId = 11;
                    }
                    else if (dto.PBT == "FAMA MERSING")
                    {
                        form.PihakBerkuasaTempatanId = 12;
                    }
                    else if (dto.PBT == "MDP")
                    {
                        form.PihakBerkuasaTempatanId = 13;
                    }
                    else if (dto.PBT == "MBJB")
                    {
                        form.PihakBerkuasaTempatanId = 14;
                    }
                    else if (dto.PBT == "MPP")
                    {
                        form.PihakBerkuasaTempatanId = 15;
                    }
                    else if (dto.PBT == "FAMA MUAR")
                    {
                        form.PihakBerkuasaTempatanId = 16;
                    }
                    else if (dto.PBT == "FAMA KULAI")
                    {
                        form.PihakBerkuasaTempatanId = 17;
                    }
                    else if (dto.PBT == "MPBP")
                    {
                        form.PihakBerkuasaTempatanId = 18;
                    }
                    else if (dto.PBT == "MDSR")
                    {
                        form.PihakBerkuasaTempatanId = 19;
                    }
                    else if (dto.PBT == "FAMA BATU PAHAT")
                    {
                        form.PihakBerkuasaTempatanId = 20;
                    }
                    else if (dto.PBT == "FAMA KLUANG")
                    {
                        form.PihakBerkuasaTempatanId = 21;
                    }
                    else if (dto.PBT == "FAMA JB")
                    {
                        form.PihakBerkuasaTempatanId = 22;
                    }
                    else if (dto.PBT == "FAMA PONTIAN")
                    {
                        form.PihakBerkuasaTempatanId = 23;
                    }
                    else if (dto.PBT == "MDKT")
                    {
                        form.PihakBerkuasaTempatanId = 24;
                    }
                    else if (dto.PBT == "MPK")
                    {
                        form.PihakBerkuasaTempatanId = 25;
                    }
                    else if (dto.PBT == "FAMA TANGKAK")
                    {
                        form.PihakBerkuasaTempatanId = 26;
                    }
                }
                else if (string.IsNullOrEmpty(dto.PBT))
                {
                    form.PihakBerkuasaTempatanId = 0;
                }
                else
                {
                    throw new InvalidOperationException("Invalid Pihak Berkuasa Tempatan id");
                }

                if (!string.IsNullOrEmpty(dto.PARLIMEN))
                {
                    form.District = dto.PARLIMEN;
                }

                if (!string.IsNullOrEmpty(dto.DUN))
                {
                    form.Mukim = dto.DUN;
                }

                form.CitizenshipId = 1;
                form.IdentityTypeId = 20; //MyKad
                form.RefNo = "PRE3000000" + form.IdentityNumber.Substring(form.IdentityNumber.Length - 4); ;

                await _dbContext.Forms.AddAsync(form);

                var initiative = await _dbContext.Initiatives.FirstOrDefaultAsync(x => x.Id == 27);

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

                await _dbContext.SaveChangesAsync("PBT");
            }
            else
            {

                if (!string.IsNullOrEmpty(dto.PBT))
                {
                    if (dto.PBT == "FAMA ENDAU")
                    {
                        form.PihakBerkuasaTempatanId = 1;
                    }
                    else if (dto.PBT == "MDM")
                    {
                        form.PihakBerkuasaTempatanId = 2;
                    }
                    else if (dto.PBT == "MPM")
                    {
                        form.PihakBerkuasaTempatanId = 3;
                    }
                    else if (dto.PBT == "MPKU")
                    {
                        form.PihakBerkuasaTempatanId = 4;
                    }
                    else if (dto.PBT == "MPS")
                    {
                        form.PihakBerkuasaTempatanId = 5;
                    }
                    else if (dto.PBT == "FAMA SEGAMAT")
                    {
                        form.PihakBerkuasaTempatanId = 6;
                    }
                    else if (dto.PBT == "FAMA KOTA TINGGI")
                    {
                        form.PihakBerkuasaTempatanId = 7;
                    }
                    else if (dto.PBT == "MBIP")
                    {
                        form.PihakBerkuasaTempatanId = 8;
                    }
                    else if (dto.PBT == "MDYP")
                    {
                        form.PihakBerkuasaTempatanId = 9;
                    }
                    else if (dto.PBT == "MBPG")
                    {
                        form.PihakBerkuasaTempatanId = 10;
                    }
                    else if (dto.PBT == "MDT")
                    {
                        form.PihakBerkuasaTempatanId = 11;
                    }
                    else if (dto.PBT == "FAMA MERSING")
                    {
                        form.PihakBerkuasaTempatanId = 12;
                    }
                    else if (dto.PBT == "MDP")
                    {
                        form.PihakBerkuasaTempatanId = 13;
                    }
                    else if (dto.PBT == "MBJB")
                    {
                        form.PihakBerkuasaTempatanId = 14;
                    }
                    else if (dto.PBT == "MPP")
                    {
                        form.PihakBerkuasaTempatanId = 15;
                    }
                    else if (dto.PBT == "FAMA MUAR")
                    {
                        form.PihakBerkuasaTempatanId = 16;
                    }
                    else if (dto.PBT == "FAMA KULAI")
                    {
                        form.PihakBerkuasaTempatanId = 17;
                    }
                    else if (dto.PBT == "MPBP")
                    {
                        form.PihakBerkuasaTempatanId = 18;
                    }
                    else if (dto.PBT == "MDSR")
                    {
                        form.PihakBerkuasaTempatanId = 19;
                    }
                    else if (dto.PBT == "FAMA BATU PAHAT")
                    {
                        form.PihakBerkuasaTempatanId = 20;
                    }
                    else if (dto.PBT == "FAMA KLUANG")
                    {
                        form.PihakBerkuasaTempatanId = 21;
                    }
                    else if (dto.PBT == "FAMA JB")
                    {
                        form.PihakBerkuasaTempatanId = 22;
                    }
                    else if (dto.PBT == "FAMA PONTIAN")
                    {
                        form.PihakBerkuasaTempatanId = 23;
                    }
                    else if (dto.PBT == "MDKT")
                    {
                        form.PihakBerkuasaTempatanId = 24;
                    }
                    else if (dto.PBT == "MPK")
                    {
                        form.PihakBerkuasaTempatanId = 25;
                    }
                    else if (dto.PBT == "FAMA TANGKAK")
                    {
                        form.PihakBerkuasaTempatanId = 26;
                    }
                }
                else if (string.IsNullOrEmpty(dto.PBT))
                {
                    form.PihakBerkuasaTempatanId = 0;
                }
                else
                {
                    throw new InvalidOperationException("Invalid Pihak Berkuasa Tempatan id");
                }

                _dbContext.Forms.Update(form);

                var initiative = await _dbContext.Initiatives.FirstOrDefaultAsync(x => x.Id == 27);
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

                    await _dbContext.SaveChangesAsync("PBT");
                }
            }

        }

        private async Task<List<PBTDTO>> GetAllFromOrignalDataSourceAsync()
        {

            var list = new List<PBTDTO>();


            _logger.LogInformation("Getting Connection ...");

            //create instanace of database connection
            var connection = new SqlConnection(_settings.ConnectionString);

            string query = @"SELECT [PBT]
      ,[NAMA]
      ,[NO_KAD_PENGENALAN]
      ,[NO_TELEFON]
      ,[NAMA_BANK]
      ,[NO_AKAUN_BANK]
      ,[JENIS]
      ,[DUN]
      ,[PARLIMEN]
      ,[DAERAH] FROM PBT where NO > 7792";

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
                        var dto = new PBTDTO();
                        if (!reader.IsDBNull(0))
                        {
                            dto.PBT = reader.GetString(0);
                        }
                        if (!reader.IsDBNull(1))
                        {
                            dto.Nama = reader.GetString(1);
                        }
                        if (!reader.IsDBNull(2))
                        {
                            dto.NO_KAD_PENGENALAN = reader.GetString(2);
                        }
                        if (!reader.IsDBNull(3))
                        {
                            dto.NO_TEL = reader.GetString(3);
                        }
                        if (!reader.IsDBNull(4))
                        {
                            dto.NAMA_BANK = reader.GetString(4);
                        }
                        if (!reader.IsDBNull(5))
                        {
                            dto.NO_AKAUN_BANK = reader.GetString(5);
                        }
                        if (!reader.IsDBNull(6))
                        {
                            dto.JENIS = reader.GetString(6);
                        }
                        if (!reader.IsDBNull(7))
                        {
                            dto.DUN = reader.GetString(7);
                        }
                        if (!reader.IsDBNull(8))
                        {
                            dto.PARLIMEN = reader.GetString(8);
                        }
                        if (!reader.IsDBNull(9))
                        {
                            dto.DAERAH = reader.GetString(9);
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
