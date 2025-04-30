using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxLibrary.Classes
{
    public class TemplateDefinitions
    {
        public int SPFaxPreRuleFieldId { get; set; }
        public int EngagementTypeID { get; set; }
        public int TaxYear { get; set; }
        public string FaxFieldName { get; set; }
        public int FaxFormID_1 { get; set; }
        public int FaxFormFieldID_Code1 { get; set; }
        public string FaxDWPCode_Code1 { get; set; }
        public string FaxDWPCode_Value1 { get; set; }
        public int FaxFormFieldID_Amount1 { get; set; }
        public string FaxDWPCode_Amount1 { get; set; }
        public int FaxFormID_2 { get; set; }
        public int FaxFormFieldID_Code2 { get; set; }
        public string FaxDWPCode_Code2 { get; set; }
        public string FaxDWPCode_Value2 { get; set; }
        public int FaxFormFieldID_Amount2 { get; set; }
        public string FaxDWPCode_Amount2 { get; set; }
    }

}
