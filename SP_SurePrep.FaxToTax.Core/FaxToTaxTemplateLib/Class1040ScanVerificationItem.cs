using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class Class1040ScanVerificationItem
    {
        public int EngagementID { get; set; }
        public int FieldID { get; set; }
        public int PageID { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public string? OriginalValue { get; set; }
        public string? CorrectedValue { get; set; }
        public bool HasBeenVerified { get; set; }
        public List<string>? UnCertainChars { get; set; }
        public string? PageName { get; set; }
        public string? FaxFormType { get; set; }
        public string? FaxFieldName { get; set; }
        public bool Check { get; set; }
        public int DeletePageID { get; set; }
        public int OCRFieldDataType { get; set; }
        public int ClientPageDPI { get; set; }
        public string? FileType { get; set; }
        public int OCRTemplateID { get; set; }
        public string? FaxDWPCode { get; set; }
        public List<RWReference>? PageReferences { get; set; }

        public Class1040ScanVerificationItem()
        {
            PageReferences = new List<RWReference>();
        }

        public Class1040ScanVerificationItem(int engagementID)
        {
            EngagementID = engagementID;
            PageReferences = new List<RWReference>();
        }

        public Class1040ScanVerificationItem(
          int engagementID,
          int fieldID,
          int pageID,
          int top,
          int left,
          int right,
          int bottom,
          string originalValue,
          string correctedValue,
          bool hasBeenVerified,
          List<string> unCertainChars,
          string pageName,
          string faxFormType,
          string faxFieldName,
          bool check,
          int deletePageID,
          int ocrFieldDataType,
          int clientPageDPI,
          string fileType
          )
        {
            EngagementID = engagementID;
            FieldID = fieldID;
            PageID = pageID;
            Top = top;
            Left = left;
            Right = right;
            Bottom = bottom;
            OriginalValue = originalValue;
            CorrectedValue = correctedValue;
            HasBeenVerified = hasBeenVerified;
            UnCertainChars = unCertainChars;
            PageName = pageName;
            FaxFormType = faxFormType;
            FaxFieldName = faxFieldName;
            Check = check;
            DeletePageID = deletePageID;
            OCRFieldDataType = ocrFieldDataType;
            ClientPageDPI = clientPageDPI;
            FileType = fileType;

            PageReferences = new List<RWReference>();
        }

        public void AddPageReference(RWReference reference)
        {
            if (PageReferences != null) PageReferences.Add(reference);
        }
    }
}
