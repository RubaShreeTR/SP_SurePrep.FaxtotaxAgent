using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class SpPreRulePages
    {
        public int EngagementFaxFormID { get; }
        public string FileName { get; }

        public SpPreRulePages(int engagementFaxFormID, string fileName)
        {
            EngagementFaxFormID = engagementFaxFormID;
            FileName = fileName;
        }

        public SpPreRulePages(int engagementFaxFormID)
        {
            EngagementFaxFormID = engagementFaxFormID;
        }
    }
}
