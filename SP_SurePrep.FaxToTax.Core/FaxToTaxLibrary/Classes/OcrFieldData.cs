using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxLibrary.Classes
{
    public class OcrFieldData
    {
        public int EngagementID { get; set; }
        public int EngagementPageID { get; set; }
        public string FileName { get; set; }
        public int TaxSoftwareID { get; set; }
        public int EngagementTypeID { get; set; }
        public int TaxYear { get; set; }
        public string InSPVerification { get; set; }
        public int EngagementOCRFieldID { get; set; }
        public string OCRTemplateName { get; set; }
        public string OCRTableName { get; set; }
        public string OCRFieldName { get; set; }
        public int OCRRowNo { get; set; }
        public int OCRPageNo { get; set; }
        public int OCRLeft { get; set; }
        public int OCRTop { get; set; }
        public int OCRRight { get; set; }
        public int OCRBottom { get; set; }
        public int SPVOCRLeft { get; set; }
        public int SPVOCRTop { get; set; }
        public int SPVOCRRight { get; set; }
        public int SPVOCRBottom { get; set; }
        public string OCRValue { get; set; }
        public int OCRTemplateID { get; set; }
        public int OCRTemplateFieldID { get; set; }
        public int SheetNo { get; set; }
        public string FaxDWPCode { get; set; }
        public int FaxRowNumber { get; set; }
        public int FaxFormID { get; set; }
        public int FaxFormFieldID { get; set; }
        public string ForCorrection { get; set; }
        public int OCRRuleType1 { get; set; }
        public string OCRRule1 { get; set; }
        public int OCRRuleType2 { get; set; }
        public string OCRRule2 { get; set; }
        public int OCRRuleType3 { get; set; }
        public string OCRRule3 { get; set; }
        public int OCRRuleType4 { get; set; }
        public string OCRRule4 { get; set; }
        public int OCRRuleType5 { get; set; }
        public string OCRRule5 { get; set; }
        public string FaxFormInstance { get; set; }
        public string FaxFieldInstance { get; set; }
        public int FaxFormType { get; set; }
        public int DisplayOrder { get; set; }
        public string Identifier { get; set; }
        public string IsConverted { get; set; }
        public string FaxFormDWPCode { get; set; }
        public string FaxFieldName { get; set; }
        public string FaxFormName { get; set; }
        public string FaxFormIdentifier { get; set; }
        public string OCRIdentifier { get; set; }
        public string OCRDWPCode { get; set; }
        public string Verified { get; set; }
        public int DataType { get; set; }
        public string OCRVerified { get; set; }
        public string OCRAutoVerified { get; set; }
        public int EngagementFormFieldID { get; set; }
        public int VirtualRotation { get; set; }
        public string ApplyDecimalRule { get; set; }
        public string OCRPreRule1 { get; set; }
        public int PreRuleAutoVerify1 { get; set; }
        public string OCRPreRule2 { get; set; }
        public int PreRuleAutoVerify2 { get; set; }
        public string OCRPreRule3 { get; set; }
        public int PreRuleAutoVerify3 { get; set; }
        public string OCRPreRule4 { get; set; }
        public int PreRuleAutoVerify4 { get; set; }
        public string OCRPreRule5 { get; set; }
        public int PreRuleAutoVerify5 { get; set; }
        public int OCRRuleType6 { get; set; }
        public string OCRRule6 { get; set; }
        public int OCRRuleType7 { get; set; }
        public string OCRRule7 { get; set; }
        public int OCRRuleType8 { get; set; }
        public string OCRRule8 { get; set; }
        public int OCRRuleType9 { get; set; }
        public string OCRRule9 { get; set; }
        public int OCRRuleType10 { get; set; }
        public string OCRRule10 { get; set; }
        public string OCRRuleTip1 { get; set; }
        public string OCRRuleTip2 { get; set; }
        public string OCRRuleTip3 { get; set; }
        public string OCRRuleTip4 { get; set; }
        public string OCRRuleTip5 { get; set; }
        public string OCRRuleTip6 { get; set; }
        public string OCRRuleTip7 { get; set; }
        public string OCRRuleTip8 { get; set; }
        public string OCRRuleTip9 { get; set; }
        public string OCRRuleTip10 { get; set; }
        public string PreOCRFormName { get; set; }
        public string AllowZeroValue { get; set; }
        public string In1040ScanVerification { get; set; }
        public int ClientPageDPI { get; set; }
        public string FileType { get; set; }
        public string SkewAngle { get; set; }
        public string OCRIdentifierWithoutSPLChars { get; set; }
        public int UnCertainChar { get; set; }
        public int DuplicateOcrFieldID { get; set; }
        public int IsAutoDuplicate { get; set; }
        public int IsAutoUnchecked { get; set; }
        public string OcrDuplicateFieldIds { get; set; }
        public string DuplicatePageIds { get; set; }
        public int MuniLogicApplied { get; set; }
        public bool IsPWCOrganizer { get; set; }
        public string Currency { get; set; }
        public int SubGroupNo { get; set; }
        public int OCRPreRuleType1 { get; set; }
        public int OCRPreRuleType2 { get; set; }
        public int OCRPreRuleType3 { get; set; }
        public int OCRPreRuleType4 { get; set; }
        public int OCRPreRuleType5 { get; set; }
        public string IsNewProcess { get; set; }
        public string OCROriginalValue { get; set; }
        public string TLValue { get; set; }
        public string PageType { get; set; }
    }

}
