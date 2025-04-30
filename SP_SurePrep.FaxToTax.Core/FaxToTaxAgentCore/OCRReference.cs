using FaxToTaxDataValidations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxAgentCore
{
    public class OCRReference : SPDataTypeValidation, IOCRReference
    {

        public bool AcceptVarious { get; set; }
    public BinderEnums.DataType DataType { get; set; }
    public string EngagementOCRFieldID { get; set; }
    public string FaxDWPCode { get; set; }
    public int LengthAfterDecimal { get; set; }
    public int LengthBeforeDecimal { get; set; }
    public int MaxLength { get; set; }
    public string OCRValue { get; set; }
    public string OCRValueVerifiedvalue { get; set; }
    public bool IsUpdateRequried { get; set; }
    public string ValueToUpdate { get; set; }
    public string F2tComment { get; set; }

}
}
