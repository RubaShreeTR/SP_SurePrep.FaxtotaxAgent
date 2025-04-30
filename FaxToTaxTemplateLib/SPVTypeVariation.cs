using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class SPVTypeVariation
    {
        public int ID { get; }
        public string OCRTemplateName { get; }
        public int OCRTemplateID { get; }
        public string BrokerName { get; }

        public SPVTypeVariation(int id, int ocrTemplateID, string ocrTemplateName, string brokerName)
        {
            ID = id;
            OCRTemplateID = ocrTemplateID;
            OCRTemplateName = ocrTemplateName;
            BrokerName = brokerName;
        }
    }
}
