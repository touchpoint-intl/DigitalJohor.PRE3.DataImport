using DigitalJohor.PRE3.EFCore;
using DigitalJohor.PRE3.EFCore.Entities.Forms;
using DigitalJohor.PRE3.Enumerations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalJohor.PRE3.DataImport.KetuaKampung
{
    public class KetuaKampungService : IImportService
    {
        private readonly DigitalJohorPRE3DbContext _dbContext;
        private readonly ILogger<KetuaKampungService> _logger;
        private readonly KetuaKampungSettings _settings;

        public KetuaKampungService(
            DigitalJohorPRE3DbContext dbContext,
            ILogger<KetuaKampungService> logger,
            IOptions<KetuaKampungSettings> settingsOptions)
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

        public async Task CreateNewFormAsync(KetuaKampungDTO dto)
        {
            if (string.IsNullOrEmpty(dto.NoIc))
            {
                throw new InvalidOperationException("IC is required");
            }

            var sanitizeIC = dto.NoIc.Replace(" ", string.Empty).Replace("-", string.Empty);

            if (string.IsNullOrEmpty(sanitizeIC))
            {
                throw new InvalidOperationException("IC is required");
            }

            if (string.IsNullOrEmpty(dto.NamaPengerusi))
            {
                throw new InvalidOperationException("Name is required");
            }

            var form = await _dbContext.Forms.FirstOrDefaultAsync(x => x.IdentityNumber == sanitizeIC);
            if (form == null)
            {

                form = new Form();
                form.Name = dto.NamaPengerusi;
                form.IdentityNumber = sanitizeIC;


                var phones = dto.NoTelefon.Split("/", StringSplitOptions.RemoveEmptyEntries);

                for(var i =0; i < phones.Length; i++)
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
                        if(i == 0)
                        {
                            form.Phone = sanitizePhone.Trim();
                        }
                        else
                        {
                            form.Phone2 = sanitizePhone.Trim();
                        }
                    }
                }

                form.Address = dto.NamaMPKK;

                if (!string.IsNullOrEmpty(dto.Mukim))
                {
                    form.Mukim = dto.Mukim;
                }

                if (!string.IsNullOrEmpty(dto.Daerah))
                {
                    form.District = dto.Daerah;
                }

                form.AccountNumber = dto.NoAcc;
                form.AccountBeneficiary = dto.NamaPengerusi;
                form.ApplicationId = 1;

                if(!string.IsNullOrEmpty(dto.BankId))
                {
                    if(dto.BankId == "AFFIN")
                    {
                        form.BankId = 31;
                    }
                    else if (dto.BankId == "AGRO")
                    {
                        form.BankId = 19;
                    }
                    else if (dto.BankId == "ALLIANCE")
                    {
                        form.BankId = 30;
                    }
                    else if (dto.BankId == "AMBANK")
                    {
                        form.BankId = 14;
                    }
                    else if (dto.BankId == "BISLAM")
                    {
                        form.BankId = 10;
                    }
                    else if (dto.BankId == "BRAKYAT" || dto.BankId == "RAKYAT")
                    {
                        form.BankId = 11;
                    }
                    else if (dto.BankId == "BSN")
                    {
                        form.BankId = 11;
                    }
                    else if (dto.BankId == "CIMB")
                    {
                        form.BankId = 7;
                    }
                    else if (dto.BankId == "HLB")
                    {
                        form.BankId = 25;
                    }
                    else if (dto.BankId == "MAYBANK")
                    {
                        form.BankId = 9;
                    }
                    else if (dto.BankId == "MUAMALAT")
                    {
                        form.BankId = 28;
                    }
                    else if (dto.BankId == "PUBLIC")
                    {
                        form.BankId = 4;
                    }
                    else if (dto.BankId == "RHB")
                    {
                        form.BankId = 27;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Invalid bank id");
                }

                form.CitizenshipId = 1;
                form.IdentityTypeId = 20; //MyKad
                form.StateId = 2; //Johor
                form.RefNo = "PRE3000000" + form.IdentityNumber.Substring(form.IdentityNumber.Length - 4); ;

                await _dbContext.Forms.AddAsync(form);

                var initiative = await _dbContext.Initiatives.FirstOrDefaultAsync(x => x.Id == 21);

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


        private async Task<IEnumerable<KetuaKampungDTO>> GetAllFromOrignalDataSourceAsync()
        {

            var list = new List<KetuaKampungDTO>();


            _logger.LogInformation("Getting Connection ...");

            //create instanace of database connection
            var connection = new SqlConnection(_settings.ConnectionString);

            string query = @"SELECT * FROM KetuaKampung";

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
                        var dto = new KetuaKampungDTO();
                        if (!reader.IsDBNull(0))
                        {
                            dto.NamaMPKK = reader.GetString(0);
                        }
                        if (!reader.IsDBNull(1))
                        {
                            dto.Mukim = reader.GetString(1);
                        }
                        if (!reader.IsDBNull(2))
                        {
                            dto.NamaPengerusi = reader.GetString(2);
                        }
                        if (!reader.IsDBNull(3))
                        {
                            dto.BankId = reader.GetString(3);
                        }
                        if (!reader.IsDBNull(4))
                        {
                            dto.NoAcc = reader.GetString(4);
                        }
                        if (!reader.IsDBNull(5))
                        {
                            dto.NoIc = reader.GetString(5);
                        }
                        if (!reader.IsDBNull(6))
                        {
                            dto.NoTelefon = reader.GetString(6);
                        }
                        if (!reader.IsDBNull(7))
                        {
                            dto.Cacatan = reader.GetString(7);
                        }
                        if (!reader.IsDBNull(8))
                        {
                            dto.Daerah = reader.GetString(8);
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
