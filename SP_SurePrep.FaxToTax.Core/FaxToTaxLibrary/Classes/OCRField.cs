using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxLibrary.Classes
{
    public class OCRField
    {
        public int EngagementOCRFieldID { get; set; }
        public int EngagementID { get; set; }
        public int EngagementFaxFormID { get; set; }
        public int EngagementPageID { get; set; }
        public int FaxFormID { get; set; }
        public int FaxFormFieldID { get; set; }
        public string OCRValue { get; set; }
        public string OCRVerifiedValue { get; set; }
        public string UnCertainChar { get; set; }
        public string AutoVerified { get; set; }
        public int FaxRowNumber { get; set; }
    }

}
