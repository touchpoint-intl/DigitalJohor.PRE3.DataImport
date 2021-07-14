using DigitalJohor.PRE3.EFCore;
using DigitalJohor.PRE3.EFCore.Entities.Forms;
using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouchPoint.Claims.Abstractions;

namespace DigitalJohor.PRE3.DataImport.ImportServices
{
    public class KetuaKampungService : IKetuaKampungService
    {
        private readonly DigitalJohorPRE3DbContext _dbContext;
        private readonly IClaimsService _claimsService;

        public KetuaKampungService(DigitalJohorPRE3DbContext dbContext, IClaimsService claimsService)
        {
            _dbContext = dbContext;
            _claimsService = claimsService;
        }

        public async Task<Form> CreateNewForm(KetuaKampungDTO dto)
        {

            //string newRefNo = "PRE3" + account.Id.ToString("D6") + dto.IdentityNumber.Substring(dto.IdentityNumber.Length - 4);
            var form = new Form();
            form.Name = dto.NamaMPKK;
            form.IdentityNumber = dto.NoIc;
            form.Phone = dto.NoTelefon;
            form.Address = "";
            form.Postcode = "";
            form.AccountNumber = dto.NoAcc;
            form.AccountBeneficiary = dto.NamaMPKK;
            //form.PartnerName = dto.PartnerName;
            //form.PartnerIdentityNumber = dto.PartnerIdentityNumber;
            //form.PartnerPhone = dto.PartnerPhone;
            form.Account = 1;
            form.ApplicationId = 1;
            //form.AgeRangeId = dto.AgeRangeId;
            form.BankId = dto.BankId;
            //form.ChannelId = dto.ChannelId;
            form.CitizenshipId = 1;
            form.DependentId = 0;
            form.IdentityTypeId = 20; //MyKad
            form.IncomeRangeId = 0;
            form.MaritalStatusId = 0;
            form.OccupationId = 0;
            form.PartnerIdentityTypeId = 0;
            form.StateId = 2; //Johor
            form.IdentityPhotoFrontId = 0; //To remove after merge with FormSupportingDoc branch
            form.IdentityPhotoBackId = 0; //To remove after merge with FormSupportingDoc branch
            //form.RefNo = newRefNo;

            await _dbContext.Forms.AddAsync(form);
            await _dbContext.SaveChangesAsync(await _claimsService.GetSubAsync());

            return form;
        }
    }
}
