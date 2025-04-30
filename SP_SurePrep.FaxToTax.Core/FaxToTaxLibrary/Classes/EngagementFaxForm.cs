using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxLibrary.Classes
{
    public class EngagementFaxForm
    {
        private int _EngagementFaxFormId;
        private List<OCRField> _EngagementOCRFieldCollection;

        public int EngagementFaxFormId
        {
            get;
            set;
        }

        public List<OCRField> EngagementOCRFieldCollection
        {
            get;
            set;
        }
    }
}
