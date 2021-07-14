using DigitalJohor.PRE3.EFCore.Entities.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalJohor.PRE3.DataImport.ImportServices
{
    public interface IKetuaKampungService
    {
        Task<Form> CreateNewForm(KetuaKampungDTO dto);
    }
}
