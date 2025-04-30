using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxDataValidations
{
    public interface IOCRReference
    {
        int LengthAfterDecimal { get; set; }
        int LengthBeforeDecimal { get; set; }
        int MaxLength { get; set; }
        bool AcceptVarious { get; set; }
        BinderEnums.DataType DataType { get; set; }
        string OCRValue { get; set; }
        string OCRValueVerifiedvalue { get; set; }
        string EngagementOCRFieldID { get; set; }
        string FaxDWPCode { get; set; }
        bool IsUpdateRequried { get; set; }
        string ValueToUpdate { get; set; }
        string F2tComment { get; set; }
    }
}
