using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class Class1040Superseded
    {
        public int EngagementID { get; set; }
        public string FaxFormType { get; set; }
        public int EngagementFaxFormID { get; set; }
        public string PrimaryFieldValue { get; set; }
        public string SecondaryFieldValue { get; set; }
        public int EngagementPageID { get; set; }
        public string PageName { get; set; }
        public bool IsSuperceded { get; set; }
        public int ClientPageDPI { get; set; }
        public string FileType { get; set; }
        public List<RWReference> PageReferences { get; set; }

        public Class1040Superseded(
          int engagementID,
          int engagementFaxFormID,
          int engagementPageID,
          string faxFormType,
          string primaryFieldValue,
          string secondaryFieldValue,
          string pageName,
          int clientPageDPI,
          string fileType,
          bool isSuperceded = false)
        {
            EngagementID = engagementID;
            FaxFormType = faxFormType;
            EngagementFaxFormID = engagementFaxFormID;
            PrimaryFieldValue = primaryFieldValue;
            SecondaryFieldValue = secondaryFieldValue;
            EngagementPageID = engagementPageID;
            PageName = pageName;
            IsSuperceded = isSuperceded;
            ClientPageDPI = clientPageDPI;
            FileType = fileType;
            PageReferences = new List<RWReference>();
        }

        public void AddPageReference(RWReference reference)
        {
            PageReferences.Add(reference);
        }
    }
}
