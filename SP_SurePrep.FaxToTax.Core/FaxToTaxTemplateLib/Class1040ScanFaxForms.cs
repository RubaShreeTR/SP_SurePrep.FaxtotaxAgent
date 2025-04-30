using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class Class1040ScanFaxForms : IComparable
    {
        public int EngagementID { get; }
        public int FormTypeID { get; set; }
        public string FormTypeName { get; set; }
        public string InputForm { get; set; }
        public int SeqFt { get; set; }
        public int TaxFormSequenceNo { get; set; }
        public int TaxFormInstanceNo { get; set; }
        public bool IsMultiInstance { get; set; }
        public string FormTypeDWPCode { get; set; }
        public int EngagementFormID { get; set; }
        public string AutoPageMatched { get; set; }
        public int EngagementFaxFormID { get; set; }
        public int EngagementPageID { get; set; }
        public int EngagementFaxTaxFormFieldID { get; set; }
        public int FieldGroupID { get; set; }
        public int FaxRowNumber { get; set; }
        public int EngagementFieldGroupID { get; set; }
        public string EntityNumber { get; set; }
        public int FromEngagementID { get; set; }
        public List<RWPage> Pages { get; set; }

        public Class1040ScanFaxForms(
          int engagementID,
          int formTypeID,
          string formTypeName,
          string inputForm,
          int seqFt,
          int taxFormSequenceNo,
          int taxFormInstanceNo,
          bool isMultiInstance,
          string formTypeDWPCode,
          string pageMatched)
        {
            EngagementID = engagementID;
            FormTypeID = formTypeID;
            FormTypeName = formTypeName;
            InputForm = inputForm;
            SeqFt = seqFt;
            TaxFormSequenceNo = taxFormSequenceNo;
            TaxFormInstanceNo = taxFormInstanceNo;
            IsMultiInstance = isMultiInstance;
            FormTypeDWPCode = formTypeDWPCode;
            AutoPageMatched = pageMatched;
            Pages = new List<RWPage>();
        }

        public void AddPage(RWPage page)
        {
            Pages.Add(page);
        }

        public int CompareTo(object obj)
        {
            if (obj is Class1040ScanFaxForms other)
            {
                int result = SeqFt.CompareTo(other.SeqFt);
                if (result == 0) result = EngagementFaxFormID.CompareTo(other.EngagementFaxFormID);
                if (result == 0) result = FaxRowNumber.CompareTo(other.FaxRowNumber);
                return result;
            }
            throw new ArgumentException("Object is not Class1040ScanFaxForms");
        }
    }
}
