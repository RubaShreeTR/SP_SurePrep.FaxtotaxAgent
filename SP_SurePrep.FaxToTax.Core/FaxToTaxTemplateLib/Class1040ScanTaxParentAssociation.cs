using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    public class Class1040ScanTaxParentAssociation
    {
        public int EngagementID { get; }
        public int FormTypeID { get; set; }
        public string FormTypeName { get; set; }
        public string FormTypeDWPCode { get; set; }
        public int EngagementFormID { get; set; }
        public string InputForm { get; set; }
        public int SeqFt { get; set; }
        public int ParentEngagementFormID { get; set; }
        public string ParentFormDWPCode { get; set; }
        public bool IsMultiInstance { get; set; }
        public int UpdatedEngagementFormID { get; set; }
        public int UpdatedFieldGroupInstance { get; set; }
        public int ProformaDataId { get; set; }
        public List<RWPage> Pages { get; set; }

        public Class1040ScanTaxParentAssociation(
          int engagementID,
          int formTypeID,
          string formTypeName,
          string formTypeDWPCode,
          int engagementFormID,
          string inputForm,
          int seqFt,
          int parentEngagementFormID,
          string parentFormDWPCode,
          bool isMultiInstance,
          int proformaDataId)
        {
            EngagementID = engagementID;
            FormTypeID = formTypeID;
            FormTypeName = formTypeName;
            FormTypeDWPCode = formTypeDWPCode;
            EngagementFormID = engagementFormID;
            InputForm = inputForm;
            SeqFt = seqFt;
            ParentEngagementFormID = parentEngagementFormID;
            ParentFormDWPCode = parentFormDWPCode;
            IsMultiInstance = isMultiInstance;
            ProformaDataId = proformaDataId;
            Pages = new List<RWPage>();
        }

        public void AddPage(RWPage page)
        {
            Pages.Add(page);
        }
    }
}
