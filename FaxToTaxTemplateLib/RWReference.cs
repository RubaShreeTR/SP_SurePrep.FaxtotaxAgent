using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class RWReference
    {
        public int EngagementPageID { get; set; }
        public string? FieldValue { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public string? FaxDwpcode { get; set; }
        public int FaxRownumber { get; set; }
        public int DataType { get; set; }
        public int FaxFormid { get; set; }
        public int FaxFormFieldID { get; set; }
        public string? FaxFormName { get; set; }
        public string? FaxFieldName { get; set; }
        public string? EngFormName { get; set; }
        public int EngementFormId { get; set; }
        public int TaxFormInstanceNo { get; set; }
        public int UnCertainCharValue { get; set; }
        public string? DropDownDWPCode { get; set; }
        public int EngagementFaxFormFieldID { get; set; }
        public string? Identifier { get; set; }
        public int IsEditable { get; set; }

        public RWReference() { }

        public RWReference(
          int engagementPageID,
          string fieldValue,
          int left,
          int top,
          int right,
          int bottom,
          string faxDwpcode,
          int faxRownumber,
          int dataType,
          int faxFormid,
          int faxFormFieldID,
          string faxFormName,
          string faxFieldName,
          string engFormName,
          int engFormid,
          int taxFormInstanceNo,
          int unCertainCharValue)
        {
            EngagementPageID = engagementPageID;
            FieldValue = fieldValue;
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
            FaxDwpcode = faxDwpcode;
            FaxRownumber = faxRownumber;
            DataType = dataType;
            FaxFormid = faxFormid;
            FaxFormFieldID = faxFormFieldID;
            FaxFormName = faxFormName;
            FaxFieldName = faxFieldName;
            EngFormName = engFormName;
            EngementFormId = engFormid;
            TaxFormInstanceNo = taxFormInstanceNo;
            UnCertainCharValue = unCertainCharValue;
        }


    }
}
