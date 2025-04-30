using FaxToTaxLibrary;
using FaxToTaxLibrary.Classes;
using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using static FaxToTaxLibrary.OCRToFax;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using FaxToTaxDataValidations;

namespace FaxToTaxLibrary
{
    public class OCRToFax
    {

        private int intCounter;

        private bool blnDoUpdate;

        private float sngConversionRatio;

        private StringBuilder strInsertFFF;

        private StringBuilder strUpdateOF;

        private DataTable _dtFaxFormField;

        public OCRToFax(string configConnectionKey)
        {
            this.intCounter = 101;
            this.sngConversionRatio = 1f;
            this.strInsertFFF = new StringBuilder();
            this.strUpdateOF = new StringBuilder();
            CommonCode.ConfigConnectionKey = configConnectionKey;
        }

        public bool ConvertOCRToFax(DataSet dsOCR)
        {
            string strParentTable = "FaxForm";
            string strChildTable = "FaxFormField";
            string strFaxFormName = "Vitesh Chheda";
            string strFaxFormDWPCode = "";
            string strEngID2 = "";
            DataTable dtOCR = dsOCR.Tables[0];
            DataView dvOCR = new DataView(dtOCR);
            dvOCR.Sort = "FaxFormID Asc, OCRIdentifierWithoutSPLChars Asc, FaxRowNumber Asc";
            checked
            {
                if (dvOCR.Count > 0)
                {
                    strEngID2 = Conversions.ToString(dsOCR.Tables[0].Rows[0]["EngagementID"]);
                    //SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "DDPAgent", "Converting Start Webservice Process for OCR to Fax... ", 714, 1, Conversions.ToInteger(strEngID2), SurePrepLogger.TraceType.InfoLog);
                    CommonCode.WriteDBLog("Converting Start Webservice Process for OCR to Fax...", 714, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, Conversions.ToInteger(strEngID2), 0, 0, 0, false);
                    Console.WriteLine("Converting Start Webservice Process for OCR to Fax... " + strEngID2);
                    DataTable dtFaxForm;
                    DataTable dtFaxFormField;
                    
                        SpParameter[] SqlParentparam = new SpParameter[1]
                        {
                        new SpParameter("@EngagementID", strEngID2)
                        };
                        dtFaxForm = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngID2)).GetDataTable("dbo.Proc_GetSPEngagementFaxForm", SqlParentparam, strParentTable, true);
                        SpParameter[] SqlChildparam = new SpParameter[1]
                        {
                        new SpParameter("@EngagementID", strEngID2)
                        };
                        dtFaxFormField = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngID2)).GetDataTable("dbo.Proc_GetSPEngagementFaxFormField", SqlChildparam, strChildTable, true);
                    
                    DataView dvFaxForm = new DataView(dtFaxForm);
                    this.blnDoUpdate = false;
                    int num = dtFaxForm.Rows.Count - 1;
                    for (int j = 0; j <= num; j++)
                    {
                        if (Operators.ConditionalCompareObjectLessEqual(this.intCounter, dtFaxForm.Rows[j]["EngagementFaxFormID"], false))
                        {
                            this.intCounter = Conversions.ToInteger(Operators.AddObject(dtFaxForm.Rows[j]["EngagementFaxFormID"], 1));
                        }
                    }
                    int num2 = dvOCR.Count - 1;
                    for (int i = 0; i <= num2; i++)
                    {
                        if (this.NeedsConversion(dvOCR[i].Row))
                        {
                            string strInputForm = this.GetFaxFormName(dvOCR, i, false);
                            string strInputFormWithoutSPLChars = this.GetFaxFormName(dvOCR, i, true);
                            if (Strings.Len(strInputForm) > 0)
                            {
                                DataRow row;
                                DataRow drwParent = default(DataRow);
                                if (Conversions.ToBoolean(Operators.AndObject(Operators.CompareString(Strings.UCase(strFaxFormName), Strings.UCase(strInputFormWithoutSPLChars), false) == 0 && Strings.Len(strFaxFormName) > 0, Operators.CompareObjectEqual(strFaxFormDWPCode, dvOCR[i]["FaxFormDWPCode"], false))))
                                {
                                    int intEngagementID = Conversions.ToInteger(dvOCR[i]["EngagementID"]);
                                    int intEngagementPageID = Conversions.ToInteger(dvOCR[i]["EngagementPageID"]);
                                    string strFFValue = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["OCRValue"]), ""));
                                    int intFFX = Conversions.ToInteger(dvOCR[i]["OCRRight"]);
                                    int intFFY = Conversions.ToInteger(dvOCR[i]["OCRTop"]);
                                    int intFFHeight = Conversions.ToInteger(Operators.SubtractObject(dvOCR[i]["OCRBottom"], dvOCR[i]["OCRTop"]));
                                    int intFFWidth = Conversions.ToInteger(Operators.SubtractObject(dvOCR[i]["OCRRight"], dvOCR[i]["OCRLeft"]));
                                    int intFFLeft = Conversions.ToInteger(dvOCR[i]["OCRLeft"]);
                                    int intFFTop = Conversions.ToInteger(dvOCR[i]["OCRTop"]);
                                    string strFaxDWPCode = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["FaxDWPCode"]), ""));
                                    string strInputForm2 = Conversions.ToString(dvOCR[i]["OCRIdentifier"]);
                                    int intFaxRowNumber = Conversions.ToInteger(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["FaxRowNumber"]), ""));
                                    int intFaxFormID = Conversions.ToInteger(dvOCR[i]["FaxFormID"]);
                                    int intFaxFormFieldID = Conversions.ToInteger(dvOCR[i]["FaxFormFieldID"]);
                                    string strFaxFormInstance = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["FaxFormInstance"]), ""));
                                    string strFaxFieldInstance = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["FaxFieldInstance"]), ""));
                                    string strIdentifier = Conversions.ToString(Interaction.IIf(OCRToFax.IsDBNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["Identifier"])), "", RuntimeHelpers.GetObjectValue(dvOCR[i]["Identifier"])));
                                    int intEngagementOCRFieldID = Conversions.ToInteger(dvOCR[i]["EngagementOCRFieldID"]);
                                    int intSheetNo = Conversions.ToInteger(dvOCR[i]["OCRPageNo"]);
                                    string strFaxFormType = Conversions.ToString(dvOCR[i]["FaxFormType"]);
                                    string strVerified = Conversions.ToString(dvOCR[i]["OCRVerified"]);
                                    string strAutoVerified = Conversions.ToString(dvOCR[i]["OCRAutoVerified"]);
                                    int intDuplicateOcrFieldID = Conversions.ToInteger(dvOCR[i]["DuplicateOcrFieldID"]);
                                    int intIsAutoDuplicate = Conversions.ToInteger(dvOCR[i]["IsAutoDuplicate"]);
                                    int intIsAutoUnchecked = Conversions.ToInteger(dvOCR[i]["IsAutoUnchecked"]);
                                    string strOcrDuplicateFieldIds = Conversions.ToString(dvOCR[i]["OcrDuplicateFieldIds"]);
                                    string strDuplicatePageIds = Conversions.ToString(dvOCR[i]["DuplicatePageIds"]);
                                    int intMuniLogicApplied = Conversions.ToInteger(dvOCR[i]["MuniLogicApplied"]);
                                    int intEngagementFormFieldID = Conversions.ToInteger(dvOCR[i]["EngagementFormFieldID"]);
                                    string strCurrency = Conversions.ToString(dvOCR[i]["Currency"]);
                                    row = dvOCR[i].Row;
                                    this.AddChildRow(ref drwParent, ref dtFaxFormField, intEngagementID, intEngagementPageID, strFFValue, intFFX, intFFY, intFFHeight, intFFWidth, intFFLeft, intFFTop, strFaxDWPCode, strInputForm2, intFaxRowNumber, intFaxFormID, intFaxFormFieldID, strFaxFormInstance, strFaxFieldInstance, strIdentifier, intEngagementOCRFieldID, intSheetNo, strFaxFormType, strVerified, strAutoVerified, intDuplicateOcrFieldID, intIsAutoDuplicate, intIsAutoUnchecked, strOcrDuplicateFieldIds, strDuplicatePageIds, intMuniLogicApplied, intEngagementFormFieldID, strCurrency, ref row);
                                    this.blnDoUpdate = true;
                                }
                                else
                                {
                                    strFaxFormName = strInputFormWithoutSPLChars;
                                    strFaxFormDWPCode = Conversions.ToString(dvOCR[i]["FaxFormDWPCode"]);
                                    dvFaxForm.RowFilter = "InputForm = '" + Strings.Replace(strInputForm, "'", "''", 1, -1, CompareMethod.Binary) + "' And FaxDWPCode = '" + strFaxFormDWPCode + "'";
                                    if (dvFaxForm.Count <= 0)
                                    {
                                        drwParent = this.AddParentRow(ref dtFaxForm, Conversions.ToInteger(dvOCR[i]["FaxFormID"]), strInputForm, Conversions.ToInteger(dvOCR[i]["EngagementID"]), Conversions.ToInteger(Interaction.IIf(Conversions.ToBoolean(OCRToFax.IsDBNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["PageType"])).ToString()), 0, RuntimeHelpers.GetObjectValue(Interaction.IIf(Operators.CompareString(dvOCR[i]["PageType"].ToString(), "S", false) == 0, 1, 0)))));
                                        this.blnDoUpdate = true;
                                    }
                                    else
                                    {
                                        drwParent = dvFaxForm[0].Row;
                                    }
                                    int intEngagementID2 = Conversions.ToInteger(dvOCR[i]["EngagementID"]);
                                    int intEngagementPageID2 = Conversions.ToInteger(dvOCR[i]["EngagementPageID"]);
                                    string strFFValue2 = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["OCRValue"]), ""));
                                    int intFFX2 = Conversions.ToInteger(dvOCR[i]["OCRRight"]);
                                    int intFFY2 = Conversions.ToInteger(dvOCR[i]["OCRTop"]);
                                    int intFFHeight2 = Conversions.ToInteger(Operators.SubtractObject(dvOCR[i]["OCRBottom"], dvOCR[i]["OCRTop"]));
                                    int intFFWidth2 = Conversions.ToInteger(Operators.SubtractObject(dvOCR[i]["OCRRight"], dvOCR[i]["OCRLeft"]));
                                    int intFFLeft2 = Conversions.ToInteger(dvOCR[i]["OCRLeft"]);
                                    int intFFTop2 = Conversions.ToInteger(dvOCR[i]["OCRTop"]);
                                    string strFaxDWPCode2 = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["FaxDWPCode"]), ""));
                                    string strInputForm3 = Conversions.ToString(dvOCR[i]["OCRIdentifier"]);
                                    int intFaxRowNumber2 = Conversions.ToInteger(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["FaxRowNumber"]), ""));
                                    int intFaxFormID2 = Conversions.ToInteger(dvOCR[i]["FaxFormID"]);
                                    int intFaxFormFieldID2 = Conversions.ToInteger(dvOCR[i]["FaxFormFieldID"]);
                                    string strFaxFormInstance2 = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["FaxFormInstance"]), ""));
                                    string strFaxFieldInstance2 = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["FaxFieldInstance"]), ""));
                                    string strIdentifier2 = Conversions.ToString(Interaction.IIf(OCRToFax.IsDBNull(RuntimeHelpers.GetObjectValue(dvOCR[i]["Identifier"])), "", RuntimeHelpers.GetObjectValue(dvOCR[i]["Identifier"])));
                                    int intEngagementOCRFieldID2 = Conversions.ToInteger(dvOCR[i]["EngagementOCRFieldID"]);
                                    int intSheetNo2 = Conversions.ToInteger(dvOCR[i]["OCRPageNo"]);
                                    string strFaxFormType2 = Conversions.ToString(dvOCR[i]["FaxFormType"]);
                                    string strVerified2 = Conversions.ToString(dvOCR[i]["OCRVerified"]);
                                    string strAutoVerified2 = Conversions.ToString(dvOCR[i]["OCRAutoVerified"]);
                                    int intDuplicateOcrFieldID2 = Conversions.ToInteger(dvOCR[i]["DuplicateOcrFieldID"]);
                                    int intIsAutoDuplicate2 = Conversions.ToInteger(dvOCR[i]["IsAutoDuplicate"]);
                                    int intIsAutoUnchecked2 = Conversions.ToInteger(dvOCR[i]["IsAutoUnchecked"]);
                                    string strOcrDuplicateFieldIds2 = Conversions.ToString(dvOCR[i]["OcrDuplicateFieldIds"]);
                                    string strDuplicatePageIds2 = Conversions.ToString(dvOCR[i]["DuplicatePageIds"]);
                                    int intMuniLogicApplied2 = Conversions.ToInteger(dvOCR[i]["MuniLogicApplied"]);
                                    int intEngagementFormFieldID2 = Conversions.ToInteger(dvOCR[i]["EngagementFormFieldID"]);
                                    string strCurrency2 = Conversions.ToString(dvOCR[i]["Currency"]);
                                    row = dvOCR[i].Row;
                                    this.AddChildRow(ref drwParent, ref dtFaxFormField, intEngagementID2, intEngagementPageID2, strFFValue2, intFFX2, intFFY2, intFFHeight2, intFFWidth2, intFFLeft2, intFFTop2, strFaxDWPCode2, strInputForm3, intFaxRowNumber2, intFaxFormID2, intFaxFormFieldID2, strFaxFormInstance2, strFaxFieldInstance2, strIdentifier2, intEngagementOCRFieldID2, intSheetNo2, strFaxFormType2, strVerified2, strAutoVerified2, intDuplicateOcrFieldID2, intIsAutoDuplicate2, intIsAutoUnchecked2, strOcrDuplicateFieldIds2, strDuplicatePageIds2, intMuniLogicApplied2, intEngagementFormFieldID2, strCurrency2, ref row);
                                    this.blnDoUpdate = true;
                                }
                            }
                        }
                    }
                    if (this.blnDoUpdate)
                    {
                        this.UpdateFaxFormData(dtFaxForm, dtFaxFormField);
                    }
                    //SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "Converting Completed Webservice Process for OCR to Fax", "Converting Start Webservice Process for OCR to Fax... ", 715, 1, Conversions.ToInteger(strEngID2), SurePrepLogger.TraceType.InfoLog);
                    CommonCode.WriteDBLog("Converting Completed Webservice Process for OCR to Fax", 715, 1, CommonCode.Severity.Low, CommonCode.TraceType.ErrorLog, Convert.ToInt32(strEngID2), 0, 0, 0, false);
                    Console.WriteLine("Converting Completed Webservice Process for OCR to Fax... " + strEngID2);
                    if (dvOCR.Count > 0)
                    {
                        this.UpdateAfterOCRToFax(Conversions.ToInteger(dvOCR[0]["EngagementID"]));
                    }
                    return true;
                }
                return true;
            }
        }

        private static bool IsDBNull(object Expression)
        {
            if (Expression is DBNull)
            {
                return true;
            }
            if (Expression == null | string.IsNullOrEmpty(Conversions.ToString(Expression)) | Expression.Equals(""))
            {
                return true;
            }
            return false;
        }

        public bool ConvertXMPToFax(DataSet dsXMP)
        {
            string strParentTable = "FaxForm";
            string strChildTable = "FaxFormField";
            string strFaxFormName = "Vitesh Chheda";
            string strFaxFormDWPCode = "";
            DataTable dtXMP = dsXMP.Tables[0];
            DataView dvXMP = new DataView(dtXMP);
            string strEngID2 = "";
            checked
            {
                if (dvXMP.Count > 0)
                {
                    strEngID2 = Conversions.ToString(dtXMP.Rows[0]["EngagementID"]);
                    DataTable dtFaxForm;
                    DataTable dtFaxFormField;
                   
                        SpParameter[] SqlParentparam = new SpParameter[1]
                        {
                        new SpParameter("@EngagementID", strEngID2)
                        };
                        dtFaxForm = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngID2)).GetDataTable("dbo.Proc_GetSPEngagementFaxForm", SqlParentparam, strParentTable, true);
                        SpParameter[] SqlChildparam = new SpParameter[1]
                        {
                        new SpParameter("@EngagementID", strEngID2)
                        };
                        dtFaxFormField = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngID2)).GetDataTable("dbo.Proc_GetSPEngagementFaxFormField", SqlChildparam, strChildTable, true);
                    
                    DataView dvFaxForm = new DataView(dtFaxForm);
                    this.blnDoUpdate = false;
                    int num = dtFaxForm.Rows.Count - 1;
                    for (int j = 0; j <= num; j++)
                    {
                        if (Operators.ConditionalCompareObjectLessEqual(this.intCounter, dtFaxForm.Rows[j]["EngagementFaxFormID"], false))
                        {
                            this.intCounter = Conversions.ToInteger(Operators.AddObject(dtFaxForm.Rows[j]["EngagementFaxFormID"], 1));
                        }
                    }
                    int num2 = dvXMP.Count - 1;
                    for (int i = 0; i <= num2; i++)
                    {
                        if (this.NeedsConversion(dvXMP[i].Row))
                        {
                            string strInputForm = this.GetFaxFormName(dvXMP, i, false);
                            string strInputFormWithoutSPLChars = this.GetFaxFormName(dvXMP, i, true);
                            if (Strings.Len(strInputForm) > 0)
                            {
                                DataRow row;
                                DataRow drwParent = default(DataRow);
                                if (Conversions.ToBoolean(Operators.AndObject(Operators.CompareString(Strings.UCase(strFaxFormName), Strings.UCase(strInputFormWithoutSPLChars), false) == 0 && Strings.Len(strFaxFormName) > 0, Operators.CompareObjectEqual(strFaxFormDWPCode, dvXMP[i]["FaxFormDWPCode"], false))))
                                {
                                    int intEngagementID = Conversions.ToInteger(dvXMP[i]["EngagementID"]);
                                    int intEngagementPageID = Conversions.ToInteger(dvXMP[i]["EngagementPageID"]);
                                    string strFFValue = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["OCRValue"]), ""));
                                    int intFFX = Conversions.ToInteger(dvXMP[i]["OCRRight"]);
                                    int intFFY = Conversions.ToInteger(dvXMP[i]["OCRTop"]);
                                    int intFFHeight = Conversions.ToInteger(Operators.SubtractObject(dvXMP[i]["OCRBottom"], dvXMP[i]["OCRTop"]));
                                    int intFFWidth = Conversions.ToInteger(Operators.SubtractObject(dvXMP[i]["OCRRight"], dvXMP[i]["OCRLeft"]));
                                    int intFFLeft = Conversions.ToInteger(dvXMP[i]["OCRLeft"]);
                                    int intFFTop = Conversions.ToInteger(dvXMP[i]["OCRTop"]);
                                    string strFaxDWPCode = Conversions.ToString(dvXMP[i]["FaxDWPCode"]);
                                    string strInputForm2 = Conversions.ToString(dvXMP[i]["OCRIdentifier"]);
                                    int intFaxRowNumber = Conversions.ToInteger(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["FaxRowNumber"]), ""));
                                    int intFaxFormID = Conversions.ToInteger(dvXMP[i]["FaxFormID"]);
                                    int intFaxFormFieldID = Conversions.ToInteger(dvXMP[i]["FaxFormFieldID"]);
                                    string strFaxFormInstance = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["FaxFormInstance"]), ""));
                                    string strFaxFieldInstance = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["FaxFieldInstance"]), ""));
                                    string strIdentifier = Conversions.ToString(Interaction.IIf(OCRToFax.IsDBNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["Identifier"])), "", RuntimeHelpers.GetObjectValue(dvXMP[i]["Identifier"])));
                                    int intEngagementOCRFieldID = Conversions.ToInteger(dvXMP[i]["EngagementOCRFieldID"]);
                                    int intSheetNo = Conversions.ToInteger(dvXMP[i]["OCRPageNo"]);
                                    string strFaxFormType = Conversions.ToString(dvXMP[i]["FaxFormType"]);
                                    string strVerified = Conversions.ToString(dvXMP[i]["OCRVerified"]);
                                    string strAutoVerified = Conversions.ToString(dvXMP[i]["OCRAutoVerified"]);
                                    int intDuplicateOcrFieldID = Conversions.ToInteger(dvXMP[i]["DuplicateOcrFieldID"]);
                                    int intIsAutoDuplicate = Conversions.ToInteger(dvXMP[i]["IsAutoDuplicate"]);
                                    int intIsAutoUnchecked = Conversions.ToInteger(dvXMP[i]["IsAutoUnchecked"]);
                                    string strOcrDuplicateFieldIds = Conversions.ToString(dvXMP[i]["OcrDuplicateFieldIds"]);
                                    string strDuplicatePageIds = Conversions.ToString(dvXMP[i]["DuplicatePageIds"]);
                                    int intMuniLogicApplied = Conversions.ToInteger(dvXMP[i]["MuniLogicApplied"]);
                                    int intEngagementFormFieldID = Conversions.ToInteger(dvXMP[i]["EngagementFormFieldID"]);
                                    string strCurrency = Conversions.ToString(dvXMP[i]["Currency"]);
                                    row = dvXMP[i].Row;
                                    this.AddChildRow(ref drwParent, ref dtFaxFormField, intEngagementID, intEngagementPageID, strFFValue, intFFX, intFFY, intFFHeight, intFFWidth, intFFLeft, intFFTop, strFaxDWPCode, strInputForm2, intFaxRowNumber, intFaxFormID, intFaxFormFieldID, strFaxFormInstance, strFaxFieldInstance, strIdentifier, intEngagementOCRFieldID, intSheetNo, strFaxFormType, strVerified, strAutoVerified, intDuplicateOcrFieldID, intIsAutoDuplicate, intIsAutoUnchecked, strOcrDuplicateFieldIds, strDuplicatePageIds, intMuniLogicApplied, intEngagementFormFieldID, strCurrency, ref row);
                                    this.blnDoUpdate = true;
                                }
                                else
                                {
                                    strFaxFormName = strInputFormWithoutSPLChars;
                                    strFaxFormDWPCode = Conversions.ToString(dvXMP[i]["FaxFormDWPCode"]);
                                    dvFaxForm.RowFilter = "InputForm = '" + Strings.Replace(strInputForm, "'", "''", 1, -1, CompareMethod.Binary) + "' And FaxDWPCode = '" + strFaxFormDWPCode + "' And Used=0";
                                    if (dvFaxForm.Count <= 0)
                                    {
                                        drwParent = this.AddParentRow(ref dtFaxForm, Conversions.ToInteger(dvXMP[i]["FaxFormID"]), strInputForm, Conversions.ToInteger(dvXMP[i]["EngagementID"]), 0);
                                        this.blnDoUpdate = true;
                                    }
                                    else
                                    {
                                        drwParent = dvFaxForm[0].Row;
                                        drwParent["Used"] = 1;
                                    }
                                    int intEngagementID2 = Conversions.ToInteger(dvXMP[i]["EngagementID"]);
                                    int intEngagementPageID2 = Conversions.ToInteger(dvXMP[i]["EngagementPageID"]);
                                    string strFFValue2 = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["OCRValue"]), ""));
                                    int intFFX2 = Conversions.ToInteger(dvXMP[i]["OCRRight"]);
                                    int intFFY2 = Conversions.ToInteger(dvXMP[i]["OCRTop"]);
                                    int intFFHeight2 = Conversions.ToInteger(Operators.SubtractObject(dvXMP[i]["OCRBottom"], dvXMP[i]["OCRTop"]));
                                    int intFFWidth2 = Conversions.ToInteger(Operators.SubtractObject(dvXMP[i]["OCRRight"], dvXMP[i]["OCRLeft"]));
                                    int intFFLeft2 = Conversions.ToInteger(dvXMP[i]["OCRLeft"]);
                                    int intFFTop2 = Conversions.ToInteger(dvXMP[i]["OCRTop"]);
                                    string strFaxDWPCode2 = Conversions.ToString(dvXMP[i]["FaxDWPCode"]);
                                    string strInputForm3 = Conversions.ToString(dvXMP[i]["OCRIdentifier"]);
                                    int intFaxRowNumber2 = Conversions.ToInteger(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["FaxRowNumber"]), ""));
                                    int intFaxFormID2 = Conversions.ToInteger(dvXMP[i]["FaxFormID"]);
                                    int intFaxFormFieldID2 = Conversions.ToInteger(dvXMP[i]["FaxFormFieldID"]);
                                    string strFaxFormInstance2 = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["FaxFormInstance"]), ""));
                                    string strFaxFieldInstance2 = Conversions.ToString(CommonCode.ReplaceNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["FaxFieldInstance"]), ""));
                                    string strIdentifier2 = Conversions.ToString(Interaction.IIf(OCRToFax.IsDBNull(RuntimeHelpers.GetObjectValue(dvXMP[i]["Identifier"])), "", RuntimeHelpers.GetObjectValue(dvXMP[i]["Identifier"])));
                                    int intEngagementOCRFieldID2 = Conversions.ToInteger(dvXMP[i]["EngagementOCRFieldID"]);
                                    int intSheetNo2 = Conversions.ToInteger(dvXMP[i]["OCRPageNo"]);
                                    string strFaxFormType2 = Conversions.ToString(dvXMP[i]["FaxFormType"]);
                                    string strVerified2 = Conversions.ToString(dvXMP[i]["OCRVerified"]);
                                    string strAutoVerified2 = Conversions.ToString(dvXMP[i]["OCRAutoVerified"]);
                                    int intDuplicateOcrFieldID2 = Conversions.ToInteger(dvXMP[i]["DuplicateOcrFieldID"]);
                                    int intIsAutoDuplicate2 = Conversions.ToInteger(dvXMP[i]["IsAutoDuplicate"]);
                                    int intIsAutoUnchecked2 = Conversions.ToInteger(dvXMP[i]["IsAutoUnchecked"]);
                                    string strOcrDuplicateFieldIds2 = Conversions.ToString(dvXMP[i]["OcrDuplicateFieldIds"]);
                                    string strDuplicatePageIds2 = Conversions.ToString(dvXMP[i]["DuplicatePageIds"]);
                                    int intMuniLogicApplied2 = Conversions.ToInteger(dvXMP[i]["MuniLogicApplied"]);
                                    int intEngagementFormFieldID2 = Conversions.ToInteger(dvXMP[i]["EngagementFormFieldID"]);
                                    string strCurrency2 = Conversions.ToString(dvXMP[i]["Currency"]);
                                    row = dvXMP[i].Row;
                                    this.AddChildRow(ref drwParent, ref dtFaxFormField, intEngagementID2, intEngagementPageID2, strFFValue2, intFFX2, intFFY2, intFFHeight2, intFFWidth2, intFFLeft2, intFFTop2, strFaxDWPCode2, strInputForm3, intFaxRowNumber2, intFaxFormID2, intFaxFormFieldID2, strFaxFormInstance2, strFaxFieldInstance2, strIdentifier2, intEngagementOCRFieldID2, intSheetNo2, strFaxFormType2, strVerified2, strAutoVerified2, intDuplicateOcrFieldID2, intIsAutoDuplicate2, intIsAutoUnchecked2, strOcrDuplicateFieldIds2, strDuplicatePageIds2, intMuniLogicApplied2, intEngagementFormFieldID2, strCurrency2, ref row);
                                    this.blnDoUpdate = true;
                                }
                            }
                        }
                    }
                    if (this.blnDoUpdate)
                    {
                        this.UpdateFaxFormData(dtFaxForm, dtFaxFormField);
                    }
                    return true;
                }
                return true;
            }
        }

        private bool NeedsConversion(DataRow drw)
        {
            object left = Operators.CompareObjectEqual(drw["IsConverted"], "Y", false);
            Type typeFromHandle = typeof(Strings);
            DataRow dataRow;
            object[] obj = new object[1]
            {
            (dataRow = drw)["OCRValue"]
            };
            object[] array = obj;
            bool[] obj2 = new bool[1]
            {
            true
            };
            bool[] array2 = obj2;
            object left2 = NewLateBinding.LateGet(null, typeFromHandle, "UCase", obj, null, null, obj2);
            if (array2[0])
            {
                dataRow["OCRValue"] = RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(array[0]));
            }
            if (Conversions.ToBoolean(Operators.OrObject(Operators.OrObject(left, Operators.CompareObjectEqual(left2, "FALSE", false)), Operators.AndObject(Operators.AndObject(Operators.OrObject(Operators.OrObject(Operators.CompareObjectEqual(drw["DataType"], 1, false), Operators.CompareObjectEqual(drw["DataType"], 2, false)), Operators.CompareObjectEqual(drw["DataType"], 3, false)), Operators.CompareObjectEqual(drw["OCRValue"], "0", false)), Operators.CompareObjectEqual(drw["AllowZeroValue"], "N", false)))))
            {
                return false;
            }
            return true;
        }

        private  DataRow AddParentRow(ref DataTable dtFaxForm, int intFaxFormID, string strInputForm, int intEngagementID, int intIsSuperseded)
        {
            DataRow drwParent = dtFaxForm.NewRow();
            drwParent["EngagementFaxFormID"] = this.intCounter;
            drwParent["FaxFormID"] = intFaxFormID;
            drwParent["InputForm"] = strInputForm;
            drwParent["EngagementID"] = intEngagementID;
            drwParent["Operation"] = 1;
            drwParent["Used"] = 1;
            drwParent["IsSuperseded"] = intIsSuperseded;
            dtFaxForm.Rows.Add(drwParent);
            intCounter += 1;
            return drwParent;
        }

        private void AddChildRow(ref DataRow drwParentRow, ref DataTable dtFaxFormField, int intEngagementID, int intEngagementPageID, string strFFValue, int intFFX, int intFFY, int intFFHeight, int intFFWidth, int intFFLeft, int intFFTop, string strFaxDWPCode, string strInputForm, int intFaxRowNumber, int intFaxFormID, int intFaxFormFieldID, string strFaxFormInstance, string strFaxFieldInstance, string strIdentifier, int intEngagementOCRFieldID, int intSheetNo, string strFaxFormType, string strVerified, string strAutoVerified, int IntDuplicateOcrFieldID, int IntIsAutoDuplicate, int IntIsAutoUnchecked, string StrOcrDuplicateFieldIds, string StrDuplicatePageIds, int intMuniLogicApplied, int intEngagementFormFieldID, string strCurrency, ref DataRow drOCRData)
        {
            DataRow drwChild = dtFaxFormField.NewRow();
            if(!(drwParentRow == null || drwParentRow["EngagementFaxFormID"] == DBNull.Value || drwParentRow["EngagementFaxFormID"] == null))
            {
                drwChild["EngagementFaxFormID"] = drwParentRow["EngagementFaxFormID"];
            }
            drwChild["EngagementID"] = intEngagementID;
            drwChild["EngagementPageID"] = intEngagementPageID;
            drwChild["FFValue"] = strFFValue;
            drwChild["FFX"] = (float)intFFX / this.sngConversionRatio;
            drwChild["FFY"] = (float)intFFY / this.sngConversionRatio;
            drwChild["FFHeight"] = (float)intFFHeight / this.sngConversionRatio;
            drwChild["FFWidth"] = (float)intFFWidth / this.sngConversionRatio;
            drwChild["FFLeft"] = (float)intFFLeft / this.sngConversionRatio;
            drwChild["FFTop"] = (float)intFFTop / this.sngConversionRatio;
            drwChild["FaxDWPCode"] = strFaxDWPCode;
            drwChild["InputForm"] = strInputForm;
            drwChild["FaxRowNumber"] = intFaxRowNumber;
            drwChild["FaxFormID"] = intFaxFormID;
            drwChild["FaxFormFieldID"] = intFaxFormFieldID;
            drwChild["FaxFormInstance"] = strFaxFormInstance;
            drwChild["FaxFieldInstance"] = strFaxFieldInstance;
            drwChild["Identifier"] = strIdentifier;
            drwChild["Operation"] = 1;
            drwChild["EngagementOCRFieldID"] = intEngagementOCRFieldID;
            drwChild["SheetNo"] = intSheetNo;
            drwChild["FaxFormType"] = strFaxFormType;
            drwChild["Verified"] = strVerified;
            drwChild["AutoVerified"] = strAutoVerified;
            drwChild["DuplicateOcrFieldID"] = IntDuplicateOcrFieldID;
            drwChild["IsAutoDuplicate"] = IntIsAutoDuplicate;
            drwChild["IsAutoUnchecked"] = IntIsAutoUnchecked;
            drwChild["OcrDuplicateFieldIds"] = StrOcrDuplicateFieldIds;
            drwChild["DuplicatePageIds"] = StrDuplicatePageIds;
            drwChild["MuniLogicApplied"] = intMuniLogicApplied;
            drwChild["EngagementFormFieldID"] = intEngagementFormFieldID;
            drwChild["Currency"] = strCurrency;
            drwChild["SubGroupNo"] = RuntimeHelpers.GetObjectValue(drOCRData["SubGroupNo"]);
            drwChild.SetParentRow(drwParentRow);
            dtFaxFormField.Rows.Add(drwChild);
        }

        private string GetFaxFormName(DataView dv, int intIndex, bool blnRemoveSpecialChars)
        {
            string strFormInstance = Conversions.ToString(dv[intIndex]["FaxFormInstance"]);
            if (Operators.CompareString(strFormInstance, "S", false) == 0)
            {
                return Conversions.ToString(dv[intIndex]["FaxFormName"]);
            }
            if (blnRemoveSpecialChars)
            {
                return Conversions.ToString(dv[intIndex]["OCRIdentifierWithoutSPLChars"]);
            }
            return Conversions.ToString(dv[intIndex]["OCRIdentifier"]);
        }

        private void UpdateFaxFormData(DataTable dtFaxForm, DataTable dtFaxFormField)
        {
            checked
            {
                int num = dtFaxForm.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    int intEngagementFaxFormID = (!Operators.ConditionalCompareObjectEqual(dtFaxForm.Rows[i]["Operation"], 1, false)) ? Conversions.ToInteger(dtFaxForm.Rows[i]["EngagementFaxFormID"]) : this.InsertFaxForm(dtFaxForm.Rows[i]);
                    DataView dv = new DataView(dtFaxFormField);
                    dv.RowFilter = Conversions.ToString(Operators.ConcatenateObject("EngagementFaxFormID = ", dtFaxForm.Rows[i]["EngagementFaxFormID"]));
                    int num2 = dv.Count - 1;
                    for (int j = 0; j <= num2; j++)
                    {
                        if (Operators.ConditionalCompareObjectEqual(dv[j]["Operation"], 1, false))
                        {
                            this.InsertFaxFormField(dv[j].Row, intEngagementFaxFormID);
                        }
                    }
                    if (this._dtFaxFormField != null && this._dtFaxFormField.Rows.Count > 0)
                    {
                        this.InsertFFF(Conversions.ToInteger(dtFaxForm.Rows[i]["EngagementID"]), this._dtFaxFormField, this.strUpdateOF.ToString());
                        this.strInsertFFF.Clear();
                        this.strUpdateOF.Clear();
                        this._dtFaxFormField.Clear();
                    }
                }
            }
        }
        private int InsertFaxForm(DataRow drwFaxFormRow)
        {
           
            SpParameter[] sqlParam = new SpParameter[8]
            {
        new SpParameter("@EngagementID", RuntimeHelpers.GetObjectValue(drwFaxFormRow["EngagementID"])),
        new SpParameter("@FaxFormID", RuntimeHelpers.GetObjectValue(drwFaxFormRow["FaxFormID"])),
        new SpParameter("@InputForm", RuntimeHelpers.GetObjectValue(drwFaxFormRow["InputForm"])),
        new SpParameter("@AddedByUser", "0"),
            new SpParameter("@ParentFaxFormID", 0),
            new SpParameter("@ParentEngagementFaxFormID", 0),
            new SpParameter("@EngagementFaxFormID", 0, ParameterDirection.Output),
            new SpParameter("@IsSuperseded", RuntimeHelpers.GetObjectValue(drwFaxFormRow["IsSuperseded"]))
            };
            CommonCode.GetEngagementDbCommon(Convert.ToInt32(RuntimeHelpers.GetObjectValue(drwFaxFormRow["EngagementID"]))).AddUpdateOrDelete("Proc_InsertEngagementFaxForm", sqlParam, true, false, true);
            return Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlParam[6].ArgValue));
        }

        private object InsertFaxFormField(DataRow drwFaxFormFieldRow, int intEngagementFaxFormID)
        {
            SpParameter[] parm = new SpParameter[34]
            {
            new SpParameter("@EngagementFaxFormID", intEngagementFaxFormID),
            new SpParameter("@EngagementID", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["EngagementID"])),
        new SpParameter("@EngagementPageID", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["EngagementPageID"])),
        new SpParameter("@FFValue", this.ReplaceStringAsNull(Conversions.ToString(drwFaxFormFieldRow["FFValue"]))),
            new SpParameter("@FFX", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(Conversions.ToString(drwFaxFormFieldRow["FFX"])))),
            new SpParameter("@FFY", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(Conversions.ToString(drwFaxFormFieldRow["FFY"])))),
            new SpParameter("@FFHeight", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(Conversions.ToString(drwFaxFormFieldRow["FFHeight"])))),
            new SpParameter("@FFWidth", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(Conversions.ToString(drwFaxFormFieldRow["FFWidth"])))),
            new SpParameter("@FFLeft", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(Conversions.ToString(drwFaxFormFieldRow["FFLeft"])))),
            new SpParameter("@FFTop", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(Conversions.ToString(drwFaxFormFieldRow["FFTop"])))),
        new SpParameter("@FaxDWPCode", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["FaxDWPCode"])),
            new SpParameter("@InputForm", this.ReplaceStringAsNull(Conversions.ToString(drwFaxFormFieldRow["InputForm"]))),
            new SpParameter("@FaxRowNumber", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(drwFaxFormFieldRow["FaxRowNumber"].ToString()))),
        new SpParameter("@FaxFormID", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["FaxFormID"])),
            new SpParameter("@FaxFormFieldID", this.ReplaceStringAsNull(Conversions.ToString(drwFaxFormFieldRow["FaxFormFieldID"]))),
            new SpParameter("@FaxFormInstance", this.ReplaceStringAsNull(Conversions.ToString(drwFaxFormFieldRow["FaxFormInstance"]))),
            new SpParameter("@FaxFieldInstance", this.ReplaceStringAsNull(Conversions.ToString(drwFaxFormFieldRow["FaxFieldInstance"]))),
            new SpParameter("@Identifier", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["Identifier"])),
            new SpParameter("@SheetNo", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["SheetNo"])),
            new SpParameter("@FaxFormType", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["FaxFormType"])),
            new SpParameter("@Verified", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["Verified"])),
            new SpParameter("@AutoVerified", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["AutoVerified"])),
            new SpParameter("@EngagementOCRFieldID", RuntimeHelpers.GetObjectValue(drwFaxFormFieldRow["EngagementOCRFieldID"])),
            new SpParameter("@No", 7),
            new SpParameter("@CreatedOn", DateAndTime.Now),
            new SpParameter("@DuplicateOcrFieldID", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(drwFaxFormFieldRow["DuplicateOcrFieldID"].ToString()))),
            new SpParameter("@IsAutoDuplicate", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(Conversions.ToString(drwFaxFormFieldRow["IsAutoDuplicate"])))),
            new SpParameter("@IsAutoUnchecked", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(Conversions.ToString(drwFaxFormFieldRow["IsAutoUnchecked"])))),
            new SpParameter("@OcrDuplicateFieldIDs", this.ReplaceStringAsNull(Conversions.ToString(drwFaxFormFieldRow["OcrDuplicateFieldIDs"]))),
            new SpParameter("@DuplicatePageIDs", this.ReplaceStringAsNull(Conversions.ToString(drwFaxFormFieldRow["DuplicatePageIDs"]))),
            new SpParameter("@MuniLogicApplied", this.ReplaceStringAsNull(Conversions.ToString(drwFaxFormFieldRow["MuniLogicApplied"]))),
            new SpParameter("@EngagementFormFieldID", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(drwFaxFormFieldRow["EngagementFormFieldID"].ToString()))),
            new SpParameter("@Currency", this.ReplaceStringAsNull(Conversions.ToString(drwFaxFormFieldRow["Currency"]))),
            new SpParameter("@SubGroupNo", RuntimeHelpers.GetObjectValue(this.ReplaceNumberAsNull(drwFaxFormFieldRow["SubGroupNo"].ToString())))
            };
            string[] filterColumn = parm.Select(( ((SpParameter item) => item.Arg)) ).ToArray();
            string[] filterData = parm.Select(( ((SpParameter item) => Convert.ToString(RuntimeHelpers.GetObjectValue(item.ArgValue)))) ).ToArray();
            CommonCode.GetCustomizeDataTable(filterData, filterColumn, ref this._dtFaxFormField);
            CommonCode.AppendString(Conversions.ToString(drwFaxFormFieldRow["EngagementOCRFieldID"]), this.strUpdateOF, ",");
            if (this._dtFaxFormField != null && this._dtFaxFormField.Rows.Count > 500)
            {
                this.InsertFFF(Conversions.ToInteger(drwFaxFormFieldRow["EngagementID"]), this._dtFaxFormField, this.strUpdateOF.ToString());
                this.strUpdateOF.Clear();
                this._dtFaxFormField.Clear();
            }
            return true;
        }

        private bool InsertFFF(int intEngagementID, DataTable dt, string strUpdate)
        {
            try
            {
                SpParameter[] spParams = new SpParameter[2];
                int count;
                if (!string.IsNullOrWhiteSpace(strUpdate))
                {
                    string[] columns = new string[1]
                    {
                    "EngagementOCRFieldID"
                    };
                    DataTable dtEngagementOCRFieldID = CommonCode.GetCustomizeDataTable(new string[1]
                    {
                    Strings.Mid(strUpdate, 1, strUpdate.Length)
                    }, columns);
                   
                        spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                        spParams[1] = new SpParameter("@UDT_EngagementOCRFieldID", dtEngagementOCRFieldID);
                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_MarkEngagementOcrFieldIsConvertedBulk", spParams, true, false, false);
                    
                }
                if (dt != null)
                {
                    
                        spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                        spParams[1] = new SpParameter("@UDT_FaxFormField", dt);
                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("[dbo].[Proc_InsertBulkFaxFormField]", spParams, true, false, false);
                    
                }
                return true;
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                Exception ex = ex2;
                bool InsertFFF = false;
                ProjectData.ClearProjectError();
                return InsertFFF;
            }
        }

        public void UpdateAfterOCRToFax(int intEngagementID)
        {
            //SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "DDPAgent", "Converting Start Webservice Process for OCR to Fax... ", 716, 1, intEngagementID, SurePrepLogger.TraceType.InfoLog);
            CommonCode.WriteDBLog("Update Start for After OCR to Fax...", 716, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID, 0, 0, 0, false);
           Console.WriteLine("Update Start for After OCR to Fax...");
            SpParameter[] SqlParam = new SpParameter[1]
                {
                new SpParameter("@EngagementID", intEngagementID)
                };
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateAfterOCRToFax", SqlParam, true, false, false);
            
           // SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "DDPAgent", "Update Completed for After OCR to Fax... ", 717, 1, intEngagementID, SurePrepLogger.TraceType.InfoLog);
            CommonCode.WriteDBLog("Update Completed for After OCR to Fax...", 717, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID, 0, 0, 0, false);
            Console.WriteLine("Update Completed for After OCR to Fax...");
        }

        private string ReplaceStringAsNull(string strData)
        {
            return Conversions.ToString(Interaction.IIf(Operators.CompareString(strData, "", false) == 0, "", strData));
        }

        private object ReplaceNumberAsNull(string strData)
        {
            return Interaction.IIf(Operators.CompareString(strData, "0", false) == 0, DBNull.Value, strData);
        }

        //protected override void Finalize()
        //{
        //    base.Finalize();
        //}
    }

}