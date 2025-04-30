using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class Class1040ScanCorrectionItems
    {
        public int FiledId { get; set; }
        public string OriginalValue { get; set; }
        public string CorrectedValue { get; set; }
        public bool HasBeenCorrected { get; set; }
        public List<string> Validations { get; set; }
        public List<string> AllowableValues { get; set; }
        public int EngagementID { get; set; }
        public int PageId { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public string PageName { get; set; }
        public string FaxFormType { get; set; }
        public string FaxFieldName { get; set; }
        public List<ListValues> ListValues { get; set; }
        public bool Check { get; set; }
        public string Tip { get; set; }
        public int DeletePageID { get; set; }
        public int OCRFieldDataType { get; set; }
        public int ClientPageDPI { get; set; }
        public string FileType { get; set; }

        //public Class1040ScanCorrectionItems()
        //{
        //    ListValues = new List<ListValues>();
        //}

        //public Class1040ScanCorrectionItems(int engagementID, string correctedValue, bool hasBeenCorrected)
        //{
        //    EngagementID = engagementID;
        //    CorrectedValue = correctedValue;
        //    HasBeenCorrected = hasBeenCorrected;
        //    ListValues = new List<ListValues>();
        //}

        public Class1040ScanCorrectionItems(
          int engagementID,
          int fieldId,
          string originalValue,
          string correctedValue,
          bool hasBeenCorrected,
          List<string> validations,
          List<string> allowableValues,
          int pageId,
          int top,
          int left,
          int right,
          int bottom,
          string pageName,
          string faxFormType,
          string faxFieldName,
          bool check,
          string tip,
          int deletePageID,
          int ocrFieldDataType,
          int clientPageDPI,
          string fileType)
        {
            EngagementID = engagementID;
            FiledId = fieldId;
            OriginalValue = originalValue;
            CorrectedValue = correctedValue;
            HasBeenCorrected = hasBeenCorrected;
            Validations = validations;
            AllowableValues = allowableValues;
            PageId = pageId;
            Top = top;
            Left = left;
            Right = right;
            Bottom = bottom;
            PageName = pageName;
            FaxFormType = faxFormType;
            FaxFieldName = faxFieldName;
            Check = check;
            Tip = tip;
            DeletePageID = deletePageID;
            OCRFieldDataType = ocrFieldDataType;
            ClientPageDPI = clientPageDPI;
            FileType = fileType;
            ListValues = new List<ListValues>();
        }

        public void AddListValues(ListValues listValues)
        {
            ListValues.Add(listValues);
        }
    }
}
