using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxLibrary.Classes
{
    public class FormFieldData
    {
        public int EngagementFormFieldID { get; set; }
        public string FieldValue { get; set; }
        public string FieldDWPCode { get; set; }
        public int DataType { get; set; }
    }

}
