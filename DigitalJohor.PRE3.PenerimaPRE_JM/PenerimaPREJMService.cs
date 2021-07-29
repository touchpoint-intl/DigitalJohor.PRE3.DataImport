using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalJohor.PRE3.EFCore;
using DigitalJohor.PRE3.EFCore.Entities.Forms;
using DigitalJohor.PRE3.Enumerations;
using DigitalJohor.PRE3.PenerimaPRE_JM;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DigitalJohor.PRE3.DataImport.Penerima_JM
{
    public class PenerimaPREJMService : IImportService
    {
        private readonly DigitalJohorPRE3DbContext _dbContext;
        private readonly ILogger<PenerimaPREJMService> _logger;
        private readonly PenerimaPREJMSettings _settings;

        public PenerimaPREJMService(
            DigitalJohorPRE3DbContext dbContext,
            ILogger<PenerimaPREJMService> logger,
            IOptions<PenerimaPREJMSettings> settingsOptions)
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
                   _logger.LogError(ex, $"PemberhentianKerja: {JsonConvert.SerializeObject(dto)}");
                }
            }
            await _dbContext.SaveChangesAsync("PemberhentianKerja");
        }

        private async Task CreateForm(PenerimaPREJMDTO dto)
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

                if (!string.IsNullOrEmpty(dto.NO_TEL) && dto.NO_TEL != "-")
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
                else
                {
                    form.Phone = null;
                }

                if (!string.IsNullOrEmpty(dto.ALAMAT))
                {
                    form.Address = dto.ALAMAT;
                }
                form.AccountNumber = dto.NO_AKAUN;

                var sanitizeBank = dto.NAMA_BANK;
                if (!string.IsNullOrEmpty(sanitizeBank))
                {
                    if (sanitizeBank == "Bank Islam")
                    {
                        form.BankId = 10;
                    }
                    else if (sanitizeBank == "BANK RAKYAT")
                    {
                        form.BankId = 11;
                    }
                    else if (sanitizeBank == "CIMB" || sanitizeBank == "CIMB BANK")
                    {
                        form.BankId = 7;
                    }
                    else if (sanitizeBank == "Hong Leong Bank")
                    {
                        form.BankId = 25;
                    }
                    else if (sanitizeBank == "MAY BANK" || sanitizeBank == "MAYBANK")
                    {
                        form.BankId = 9;
                    }
                    else if (sanitizeBank == "PUBLIC BANK")
                    {
                        form.BankId = 4;
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

                if (!string.IsNullOrEmpty(dto.DAERAH))
                {
                    form.District = dto.DAERAH;
                }
                if (!string.IsNullOrEmpty(dto.DUN))
                {
                    form.Mukim = dto.DUN;
                }

                form.CitizenshipId = 1;
                form.IdentityTypeId = 20; //MyKad
                form.RefNo = "PRE3000000" + form.IdentityNumber.Substring(form.IdentityNumber.Length - 4); ;

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

                await _dbContext.SaveChangesAsync("PenerimaPREJM");
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

                    await _dbContext.SaveChangesAsync("PenerimaPREJM");
                }
            }

        }

        private async Task<List<PenerimaPREJMDTO>> GetAllFromOrignalDataSourceAsync()
        {

            var list = new List<PenerimaPREJMDTO>();


            _logger.LogInformation("Getting Connection ...");

            //create instanace of database connection
            var connection = new SqlConnection(_settings.ConnectionString);

            string query = @"SELECT [Nama]
      ,[No_Kad_Pengenalan]
      ,[Alamat]
      ,[No_Akaun]
      ,[Nama_Bank]
      ,[No_Telefon]
      ,[DUN]
      ,[Parlimen]
      ,[Daerah] FROM [PemberhentianKerja]";

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
                        var dto = new PenerimaPREJMDTO();
                        if (!reader.IsDBNull(0))
                        {
                            dto.Nama = reader.GetString(0);
                        }
                        if (!reader.IsDBNull(1))
                        {
                            dto.NO_KAD_PENGENALAN = reader.GetString(1);
                        }
                        if (!reader.IsDBNull(2))
                        {
                            dto.ALAMAT = reader.GetString(2);
                        }
                        if (!reader.IsDBNull(3))
                        {
                            dto.NO_AKAUN = reader.GetString(3);
                        }
                        if (!reader.IsDBNull(4))
                        {
                            dto.NAMA_BANK = reader.GetString(4);
                        }
                        if (!reader.IsDBNull(5))
                        {
                            dto.NO_TEL = reader.GetString(5);
                        }
                        if (!reader.IsDBNull(6))
                        {
                            dto.DUN = reader.GetString(6);
                        }
                        if (!reader.IsDBNull(7))
                        {
                            dto.PARLIMEN = reader.GetString(7);
                        }
                        
                        if (!reader.IsDBNull(8))
                        {
                            dto.DAERAH = reader.GetString(8);
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
