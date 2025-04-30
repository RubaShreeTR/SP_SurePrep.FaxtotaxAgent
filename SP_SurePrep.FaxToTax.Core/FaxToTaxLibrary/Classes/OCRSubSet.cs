using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxLibrary.Classes
{
    public class OCRSubSet
    {
        public int EngagementPageID { get; set; }
        public int FaxRowNumber { get; set; }
        public string OCRDWPCode { get; set; }
        public string OCRIdentifierWithoutSPLChars { get; set; }
        public string FaxDWPCode { get; set; }
        public short UnCertainChar { get; set; }
        public string FaxFieldInstance { get; set; }
        public string OCRValue { get; set; }
        public int DataType { get; set; }
        // public int EngagementOCRFieldID { get; set; } 
        public string InputForm { get; set; }
        public string FFValue { get; set; }
        public int EngagementFaxFormID { get; set; }
    }

}
