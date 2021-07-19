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

namespace DigitalJohor.PRE3.DataImport.LHDN
{
    public class LhdnService : IImportService
    {
        private readonly DigitalJohorPRE3DbContext _dbContext;
        private readonly ILogger<LhdnService> _logger;
        private readonly LhdnSettings _settings;

        public LhdnService(
            DigitalJohorPRE3DbContext dbContext,
            ILogger<LhdnService> logger,
            IOptions<LhdnSettings> settingsOptions)
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
                    _logger.LogError(ex, $"LHDN: {JsonConvert.SerializeObject(dto)}");
                }
            }
            await _dbContext.SaveChangesAsync("LHDN");
        }

        private async Task CreateForm(LhdnDTO dto)
        {

            if (string.IsNullOrEmpty(dto.IdentityNumber))
            {
                throw new InvalidOperationException("IC is required");
            }

            var sanitizeIC = dto.IdentityNumber.Replace(" ", string.Empty).Replace("-", string.Empty);

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

                if (!string.IsNullOrEmpty(dto.NoTelefon))
                {
                    var phones = dto.NoTelefon.Split("/", StringSplitOptions.RemoveEmptyEntries);

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


                if(!string.IsNullOrEmpty(dto.Jantina))
                {
                    if(dto.Jantina == "L")
                    {
                        form.Gender = Enumerations.Gender.Lelaki;
                    }
                    else if(dto.Jantina == "P")
                    {
                        form.Gender = Enumerations.Gender.Perempuan;
                    }
                }

                if(!string.IsNullOrEmpty(dto.StatusKahwin))
                {
                    if(dto.StatusKahwin == "Bujang")
                    {
                        form.MaritalStatusId = 1;
                    }
                    else if (dto.StatusKahwin == "Ibu Bapa Tunggal/Balu/Janda/Duda Dan Mempunyai Anak")
                    {
                        form.MaritalStatusId = 3;
                    }
                    else if (dto.StatusKahwin == "Kahwin")
                    {
                        form.MaritalStatusId = 2;
                    }
                }

                if (!string.IsNullOrEmpty(dto.Tanggungan))
                {
                    if(int.TryParse(dto.Tanggungan, out var dependent))
                    {
                        if(dependent == 0)
                        {
                            form.DependentId = 10;
                        }
                        else if (dependent > 0 && dependent < 4)
                        {
                            form.DependentId = 11;
                        }
                        else 
                        {
                            form.DependentId = 12;
                        }
                    }
                }

                form.Address = dto.Alamat;
                if (!string.IsNullOrEmpty(dto.BWHRM2500))
                {
                    if(dto.BWHRM2500 == "X")
                    {
                        form.IncomeRangeId = 1;
                    }
                    else if (dto.RM2500RM4000 == "X")
                    {
                        form.IncomeRangeId = 3;
                    }
                    else if (dto.RM4001RM5000 == "X")
                    {
                        form.IncomeRangeId = 4;
                    }
                    else if (dto.ATASRM5000 == "X")
                    {
                        form.IncomeRangeId = 5;
                    }
                }

                form.CitizenshipId = 1;
                form.IdentityTypeId = 20; //MyKad
                form.RefNo = "PRE3000000" + form.IdentityNumber.Substring(form.IdentityNumber.Length - 4); ;

                await _dbContext.Forms.AddAsync(form);
            }
            else
            {

                if (!string.IsNullOrEmpty(dto.Jantina))
                {
                    if (dto.Jantina == "L")
                    {
                        form.Gender = Enumerations.Gender.Lelaki;
                    }
                    else if (dto.Jantina == "P")
                    {
                        form.Gender = Enumerations.Gender.Perempuan;
                    }
                }

                if (!string.IsNullOrEmpty(dto.StatusKahwin))
                {
                    if (dto.StatusKahwin == "Bujang")
                    {
                        form.MaritalStatusId = 1;
                    }
                    else if (dto.StatusKahwin == "Ibu Bapa Tunggal/Balu/Janda/Duda Dan Mempunyai Anak")
                    {
                        form.MaritalStatusId = 3;
                    }
                    else if (dto.StatusKahwin == "Kahwin")
                    {
                        form.MaritalStatusId = 2;
                    }
                }

                if (!string.IsNullOrEmpty(dto.Tanggungan))
                {
                    if (int.TryParse(dto.Tanggungan, out var dependent))
                    {
                        if (dependent == 0)
                        {
                            form.DependentId = 10;
                        }
                        else if (dependent > 0 && dependent < 4)
                        {
                            form.DependentId = 11;
                        }
                        else
                        {
                            form.DependentId = 12;
                        }
                    }
                }

                form.Address = dto.Alamat;
                if (!string.IsNullOrEmpty(dto.BWHRM2500))
                {
                    if (dto.BWHRM2500 == "X")
                    {
                        form.IncomeRangeId = 1;
                    }
                    else if (dto.RM2500RM4000 == "X")
                    {
                        form.IncomeRangeId = 3;
                    }
                    else if (dto.RM4001RM5000 == "X")
                    {
                        form.IncomeRangeId = 4;
                    }
                    else if (dto.ATASRM5000 == "X")
                    {
                        form.IncomeRangeId = 5;
                    }
                }

                _dbContext.Forms.Update(form);
            }

        }

        private async Task<List<LhdnDTO>> GetAllFromOrignalDataSourceAsync()
        {

            var list = new List<LhdnDTO>();


            _logger.LogInformation("Getting Connection ...");

            //create instanace of database connection
            var connection = new SqlConnection(_settings.ConnectionString);

            string query = @"SELECT [nama]
      ,[noicpolistentera]
      ,[Jantina]
      ,[Bangsa]
      ,[Agama]
      ,[StatusKahwin]
      ,[Tanggungan]
      ,[Alamat]
      ,[Adun]
      ,[Parlimen]
      ,[Bandar]
      ,[NoTelefon]
      ,[BWHRM2500]
      ,[RM2500RM4000]
      ,[RM4001RM5000]
      ,[ATASRM5000] FROM LHDN";

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
                        var dto = new LhdnDTO();
                        if (!reader.IsDBNull(0))
                        {
                            dto.Nama = reader.GetString(0);
                        }
                        if (!reader.IsDBNull(1))
                        {
                            dto.IdentityNumber = reader.GetString(1);
                        }
                        if (!reader.IsDBNull(2))
                        {
                            dto.Jantina = reader.GetString(2);
                        }
                        if (!reader.IsDBNull(3))
                        {
                            dto.Bangsa = reader.GetString(3);
                        }
                        if (!reader.IsDBNull(4))
                        {
                            dto.Agama = reader.GetString(4);
                        }
                        if (!reader.IsDBNull(5))
                        {
                            dto.StatusKahwin = reader.GetString(5);
                        }
                        if (!reader.IsDBNull(6))
                        {
                            dto.Tanggungan = reader.GetString(6);
                        }
                        if (!reader.IsDBNull(7))
                        {
                            dto.Alamat = reader.GetString(7);
                        }
                        if (!reader.IsDBNull(8))
                        {
                            dto.Adun = reader.GetString(8);
                        }
                        if (!reader.IsDBNull(9))
                        {
                            dto.Parliment = reader.GetString(9);
                        }
                        if (!reader.IsDBNull(10))
                        {
                            dto.Bandar = reader.GetString(10);
                        }
                        if (!reader.IsDBNull(11))
                        {
                            dto.NoTelefon = reader.GetString(11);
                        }
                        if (!reader.IsDBNull(12))
                        {
                            dto.BWHRM2500 = reader.GetString(12);
                        }
                        if (!reader.IsDBNull(13))
                        {
                            dto.RM2500RM4000 = reader.GetString(13);
                        }
                        if (!reader.IsDBNull(14))
                        {
                            dto.RM4001RM5000 = reader.GetString(14);
                        }
                        if (!reader.IsDBNull(15))
                        {
                            dto.ATASRM5000 = reader.GetString(15);
                        }
                        //if (!reader.IsDBNull(16))
                        //{
                        //    dto.Pendapatan = reader.GetString(16);
                        //}

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
