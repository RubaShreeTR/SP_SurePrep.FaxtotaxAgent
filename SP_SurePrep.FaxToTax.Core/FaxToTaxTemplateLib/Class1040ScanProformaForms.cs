using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class Class1040ScanProformaForms : IComparable
    {
        public int EngagementID { get; }
        public int FormTypeID { get; set; }
        public string FormTypeName { get; set; }
        public int EngagementFormID { get; set; }
        public int EngFormID { get; set; }
        public string InputForm { get; set; }
        public int SeqFt { get; set; }
        public bool IsMultiInstance { get; set; }
        public string FormTypeDWPCode { get; set; }
        public int UpdatedFormTypeID { get; set; }
        public int UpdatedTaxFormInstanceNo { get; set; }
        public int UpdatedTaxFormSequenceNo { get; set; }
        public bool IsPwCOrganizer { get; set; }
        public string EntityNumber { get; set; }

        public Class1040ScanProformaForms(
          int engagementID,
          int formTypeID,
          string formTypeName,
          int engagementFormID,
          string inputForm,
          int seqFt,
          bool isMultiInstance,
          string formTypeDWPCode)
        {
            EngagementID = engagementID;
            FormTypeID = formTypeID;
            FormTypeName = formTypeName;
            EngagementFormID = engagementFormID;
            InputForm = inputForm;
            SeqFt = seqFt;
            IsMultiInstance = isMultiInstance;
            FormTypeDWPCode = formTypeDWPCode;
        }

        public int CompareTo(object obj)
        {
            if (obj is Class1040ScanProformaForms other)
            {
                int result = SeqFt.CompareTo(other.SeqFt);
                if (result == 0) result = EngFormID.CompareTo(other.EngFormID);
                return result;
            }
            throw new ArgumentException("Object is not Class1040ScanProformaForms");
        }
    }
}
