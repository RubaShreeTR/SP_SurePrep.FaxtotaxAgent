using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxLibrary.Classes
{
    public class Faxformdata
    {
        public int EngagementFaxFormFieldID { get; set; }
        public int EngagementFaxFormID { get; set; }
        public int EngagementID { get; set; }
        public int EngagementPageID { get; set; }
        public int FFLeft { get; set; }
        public int FFTop { get; set; }
        public int FFRight { get; set; }
        public int FFBottom { get; set; }
        public string FFValue { get; set; }
        public string TFValue { get; set; }
        public int FFX { get; set; }
        public int FFY { get; set; }
        public int FFHeight { get; set; }
        public int FFWidth { get; set; }
        public string FaxDWPCode { get; set; }
        public int FaxRowNumber { get; set; }
        public int FaxFormID { get; set; }
        public int FaxFormFieldID { get; set; }
        public string FaxFormInstance { get; set; }
        public string FaxFieldInstance { get; set; }
        public string Identifier { get; set; }
        public string FieldDWPCode { get; set; }
        public int EngagementFormFieldID { get; set; }
        public int EngagementFormID { get; set; }
        public int FormTypeID { get; set; }
        public int FormFieldID { get; set; }
        public string FieldGroupInstance { get; set; }
        public string InputForm { get; set; }
        public int EngagementOCRFieldID { get; set; }
        public int DataType { get; set; }
        public int ParentEngagementFaxFormID { get; set; }
    }


}
