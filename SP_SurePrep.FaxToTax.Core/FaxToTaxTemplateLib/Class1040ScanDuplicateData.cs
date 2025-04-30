using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class Class1040ScanDuplicateData
    {
        public int EngagementID { get; set; }
        public int SEngagementFaxTaxFormFieldID { get; set; }
        public int OEngagementFaxTaxFormFieldID { get; set; }
        public string SFaxFormFDTName { get; set; }
        public string SInputForm { get; set; }
        public int SFaxFormID { get; set; }
        public string OFaxFormFDTName { get; set; }
        public string OInputForm { get; set; }
        public int OFaxFormID { get; set; }
        public int SPageID { get; set; }
        public string SPageName { get; set; }
        public int OPageID { get; set; }
        public string OPageName { get; set; }
        public string SFFName { get; set; }
        public string SFFValue { get; set; }
        public string OFFName { get; set; }
        public string OFFValue { get; set; }
        public bool IsDuplicate { get; set; }
        public int OClientPageDPI { get; set; }
        public int SClientPageDPI { get; set; }
        public string OFileType { get; set; }
        public string SFileType { get; set; }
        public int SFaxRowNumber { get; set; }
        public int OFaxRowNumber { get; set; }

        public Class1040ScanDuplicateData(
          int engagementID,
          int sEngagementFaxTaxFormFieldID,
          int oEngagementFaxTaxFormFieldID,
          string sFaxFormFDTName,
          string sInputForm,
          int sFaxFormID,
          string oFaxFormFDTName,
          string oInputForm,
          int oFaxFormID,
          int sPageID,
          string sPageName,
          int oPageID,
          string oPageName,
          string sFFName,
          string sFFValue,
          string oFFName,
          string oFFValue,
          bool isDuplicate,
          int oClientPageDPI,
          int sClientPageDPI,
          string oFileType,
          string sFileType,
          int sFaxRowNumber,
          int oFaxRowNumber)
        {
            EngagementID = engagementID;
            SEngagementFaxTaxFormFieldID = sEngagementFaxTaxFormFieldID;
            OEngagementFaxTaxFormFieldID = oEngagementFaxTaxFormFieldID;
            SFaxFormFDTName = sFaxFormFDTName;
            SInputForm = sInputForm;
            SFaxFormID = sFaxFormID;
            OFaxFormFDTName = oFaxFormFDTName;
            OInputForm = oInputForm;
            OFaxFormID = oFaxFormID;
            SPageID = sPageID;
            SPageName = sPageName;
            OPageID = oPageID;
            OPageName = oPageName;
            SFFName = sFFName;
            SFFValue = sFFValue;
            OFFName = oFFName;
            OFFValue = oFFValue;
            IsDuplicate = isDuplicate;
            OClientPageDPI = oClientPageDPI;
            SClientPageDPI = sClientPageDPI;
            OFileType = oFileType;
            SFileType = sFileType;
            SFaxRowNumber = sFaxRowNumber;
            OFaxRowNumber = oFaxRowNumber;
        }
    }
}
