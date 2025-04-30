using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxLibrary.Classes
{
    public class EngFormField
    {
        public int EngagementID { get; set; }
        public int EngagementFormID { get; set; }
        public int EngagementFormFieldID { get; set; }
        public int FormFieldID { get; set; }

        public string FieldDWPCode { get; set; }

        public string FieldValue { get; set; }
        public int DataType { get; set; }

        public int ParentEngagementFormID { get; set; }
    }
}
