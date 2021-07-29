using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalJohor.PRE3.EFCore;
using DigitalJohor.PRE3.EFCore.Entities.Forms;
using DigitalJohor.PRE3.Enumerations;
using DigitalJohor.PRE3.PemberhentianKerja;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DigitalJohor.PRE3.DataImport.PemberhentianKerja
{
    public class PemberhentianKerjaService : IImportService
    {
        private readonly DigitalJohorPRE3DbContext _dbContext;
        private readonly ILogger<PemberhentianKerjaService> _logger;
        private readonly PemberhentianKerjaSettings _settings;

        public PemberhentianKerjaService(
            DigitalJohorPRE3DbContext dbContext,
            ILogger<PemberhentianKerjaService> logger,
            IOptions<PemberhentianKerjaSettings> settingsOptions)
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

        private async Task CreateForm(PemberhentianKerjaDTO dto)
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


                if (!string.IsNullOrEmpty(dto.JANTINA))
                {
                    if (dto.JANTINA == "Lelaki")
                    {
                        form.Gender = Enumerations.Gender.Lelaki;
                    }
                    else if (dto.JANTINA == "Perempuan")
                    {
                        form.Gender = Enumerations.Gender.Perempuan;
                    }
                }

                if (!string.IsNullOrEmpty(dto.ALAMAT))
                {
                    form.Address = dto.ALAMAT;
                }
                if (!string.IsNullOrEmpty(dto.COMPANY_NAME))
                {
                    form.Address = dto.COMPANY_NAME;
                }
                if (!string.IsNullOrEmpty(dto.COMPANY_ADD))
                {
                    form.Address = dto.COMPANY_ADD;
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

                await _dbContext.SaveChangesAsync("PemberhentianKerja");
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

                    await _dbContext.SaveChangesAsync("PemberhentianKerja");
                }
            }

        }

        private async Task<List<PemberhentianKerjaDTO>> GetAllFromOrignalDataSourceAsync()
        {

            var list = new List<PemberhentianKerjaDTO>();


            _logger.LogInformation("Getting Connection ...");

            //create instanace of database connection
            var connection = new SqlConnection(_settings.ConnectionString);

            string query = @"SELECT [Nama_Penuh_Pekerja]
      ,[No_Kad_Pengenalan]
      ,[Jantina]
      ,[Alamat]
      ,[No_Telefon]
      ,[Jawatan_Terakhir]
      ,[Company_Name]
      ,[Company_Address] FROM [PemberhentianKerja]";

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
                        var dto = new PemberhentianKerjaDTO();
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
                            dto.JANTINA = reader.GetString(2);
                        }
                        if (!reader.IsDBNull(3))
                        {
                            dto.ALAMAT = reader.GetString(3);
                        }
                        if (!reader.IsDBNull(4))
                        {
                            dto.NO_TEL = reader.GetString(4);
                        }
                        if (!reader.IsDBNull(5))
                        {
                            dto.JAWATAN_TERAKHIR = reader.GetString(5);
                        }
                        if (!reader.IsDBNull(6))
                        {
                            dto.COMPANY_NAME = reader.GetString(6);
                        }
                        if (!reader.IsDBNull(7))
                        {
                            dto.COMPANY_ADD = reader.GetString(7);
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
