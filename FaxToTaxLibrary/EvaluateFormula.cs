using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FaxToTaxLibrary.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FaxToTaxTemplateLib;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using static FaxToTaxLibrary.CommonCode;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using FaxToTaxDataValidations;

namespace FaxToTaxLibrary
{

    public class EvaluateFormula
    {
        #region Declarations
        private int intCounter = 101;
        public DataSet dsEngFormFields;
        public DataSet dsEngFaxFormFields;
        public DataSet dsEngFaxTaxFormFields;
        private OcrFieldData lstOcrItem;
        private IList<Faxformdata> lstFaxdata;
        private bool blnDoUpdate;
        private Dictionary<int, bool> dicDoUpdate = new Dictionary<int, bool>();
        private string strInsertFFF = "";
        private int engagementid;
        public bool UsePivot;
        public bool UseMultiThreading;
        private object lockObj = new object();
        public DataSet dsFaxTax = new DataSet();
        private bool ExceptionOccured;

        public event EventHandler CancelTask;

        // Declare the CancellationTokenSource and CancellationToken
        private CancellationTokenSource tokenSource2;
        private CancellationToken cancellationToken;


        private Dictionary<int, string> taskStatus = new Dictionary<int, string>();
        #endregion

       // #region Parse Formula

        public string ParseFormula1(int intEngFaxFormID, string strFormula, int intGroupInstance, int intEngagementPageID, string strEvaluateAt, bool blnIsMapping, DataTable dtFaxData, DataTable dtOCRData, string OCRFormName = "", int DataType = 0, string StrPrerRuleDiagInfo = "")
        {
            string strExpResult = "";
            try
            {
                if (string.IsNullOrEmpty(strFormula))
                {
                    return "";
                }

                string[] arrFormula = strFormula.Split(' ');
                if (arrFormula.Length > 1)
                {
                    strExpResult = GetExpResult(arrFormula, intEngFaxFormID, intGroupInstance, intEngagementPageID, strEvaluateAt, blnIsMapping, dtFaxData, dtOCRData, OCRFormName, DataType, StrPrerRuleDiagInfo);
                    return strExpResult;
                }
                else
                {
                    strExpResult = EvaluateExpression(intEngFaxFormID, arrFormula[0], intGroupInstance, intEngagementPageID, strEvaluateAt, blnIsMapping, dtFaxData, dtOCRData, OCRFormName, DataType, true);
                    if (strExpResult.Split(new[] { "~~" }, StringSplitOptions.None).Length > 1)
                    {
                        return strExpResult.Split(new[] { "~~" }, StringSplitOptions.None)[0];
                    }
                    else
                    {
                        return strExpResult;
                    }
                }
            }
            catch (Exception ex)
            {
                if (dtFaxData != null && dtFaxData.Rows.Count > 0)
                {
                    DataRow drwTemp = GetEngagementId(intEngagementPageID, dtFaxData);
                    // LogEntry(Severity.Low, "DDPAgent", $"ParseFormula1 - Error Occured : {ex.Message} -> {DateTime.Now} intEngFaxFormID = {intEngFaxFormID} intGroupInstance = {intGroupInstance} intEngagementPageID = {intEngagementPageID} OCRFormName = {OCRFormName} StrPrerRuleDiagInfo{StrPrerRuleDiagInfo} {ex.Message} -> {DateTime.Now}", 9005, 1, drwTemp["EngagementID"], TraceType.ErrorLog);
                }
                else
                {
                    // LogEntry(Severity.Low, "DDPAgent", $"ParseFormula1 - Error Occured : {ex.Message} -> {DateTime.Now} intEngFaxFormID = {intEngFaxFormID} intGroupInstance = {intGroupInstance} intEngagementPageID = {intEngagementPageID} OCRFormName = {OCRFormName} StrPrerRuleDiagInfo{StrPrerRuleDiagInfo} {ex.Message} -> {DateTime.Now}", 9005, 1, intEngagementPageID, TraceType.ErrorLog);
                }
                throw;
            }
            return strExpResult;
        }

        private  DataRow GetEngagementId(int intEngagementPageID, DataTable dtFaxData)
        {
            DataRow drwTemp = null;
            foreach (DataRow row in dtFaxData.Select($"EngagementPageID = {intEngagementPageID}"))
            {
                drwTemp = dtFaxData.NewRow();
                drwTemp["EngagementID"] = row["EngagementID"];
            }

            return drwTemp;
        }

        private string GetExpResult(string[] arrFormula, int intEngFaxFormID, int intGroupInstance, int intEngagementPageID, string strEvaluateAt, bool blnIsMapping, DataTable dtFaxData, DataTable dtOCRData, string OCRFormName, int DataType, string StrPrerRuleDiagInfo)
        {
            string strExpResult = string.Empty;
            string strCase = arrFormula[1];

            for (int intCount = 2; intCount < arrFormula.Length; intCount++)
            {
                if (arrFormula[intCount] == "When")
                {
                    string strCompareCondition = arrFormula[intCount + 1];
                    string strWhenCondition = arrFormula[intCount + 2];
                    string strThenCondition = GetFormulaThenCaseCondition(arrFormula, ref intCount);

                    if (EvaluateCondition(intEngFaxFormID, strCase, strCompareCondition, strWhenCondition, intGroupInstance, intEngagementPageID, strEvaluateAt, dtFaxData, dtOCRData, OCRFormName, DataType, StrPrerRuleDiagInfo))
                    {
                        if (strThenCondition.ToUpper().Contains("CASE"))
                        {
                            return ParseFormula1(intEngFaxFormID, strThenCondition, intGroupInstance, intEngagementPageID, strEvaluateAt, blnIsMapping, dtFaxData, dtOCRData, OCRFormName, DataType);
                        }
                        else
                        {
                            strExpResult = EvaluateExpression(intEngFaxFormID, strThenCondition, intGroupInstance, intEngagementPageID, strEvaluateAt, blnIsMapping, dtFaxData, dtOCRData, OCRFormName, DataType, true);
                            if (strExpResult.Split(new[] { "~~" }, StringSplitOptions.None).Length > 1)
                            {
                                return strExpResult.Split(new[] { "~~" }, StringSplitOptions.None)[0];
                            }
                            else
                            {
                                return strExpResult;
                            }
                        }
                    }
                }
                else if (arrFormula[intCount] == "Else")
                {
                    string strElse = arrFormula[intCount + 1];

                    strExpResult = EvaluateExpression(intEngFaxFormID, strElse, intGroupInstance, intEngagementPageID, strEvaluateAt, blnIsMapping, dtFaxData, dtOCRData, OCRFormName, DataType, true);
                    if (strExpResult.Split(new[] { "~~" }, StringSplitOptions.None).Length > 1)
                    {
                        return strExpResult.Split(new[] { "~~" }, StringSplitOptions.None)[0];
                    }
                    else
                    {
                        return strExpResult;
                    }
                }
            }

            return strExpResult;
        }

        private bool EvaluateCondition(int intEngFaxFormID, string strCaseCode, string strCompare, string strWhenCode, int intGroupInstance, int intEngagementPageID, string strEvaluateAt, DataTable dtFaxData, DataTable dtOCRData, string OCRFormName = "", int DataType = 0, string StrPrerRuleDiagInfo = "")
        {
            string strCase;
            string strWhen;

            string strCaseDataType = "";
            int intCasePercentage = 0;

            string strWhenDataType = "";
            int intWhenPercentage = 0;
            int intRes = 0;
            int intp = 0;
            int intper = 0;
            int intMaxlength = 0;
            bool CaseNumeric = false;
            bool WhenNumeric = false;

            if (strCaseCode.ToUpper() == "PARENT")
            {
                if (dsEngFaxFormFields == null)
                {
                    dsEngFaxFormFields = dtFaxData.DataSet;
                }
                strWhenCode = strWhenCode.Replace("{", "").Replace("}", "");
                strCase = EvaluateParent(intEngFaxFormID);
                if (string.IsNullOrEmpty(strCase))
                {
                    strCase = "-1";
                }
            }
            else
            {
                string strCaseResult = EvaluateExpression(intEngFaxFormID, strCaseCode, intGroupInstance, intEngagementPageID, strEvaluateAt, false, dtFaxData, dtOCRData, OCRFormName, DataType, false);
                if (strCaseResult.Split(new[] { "~~" }, StringSplitOptions.None).Length > 1)
                {
                    strCase = strCaseResult.Split(new[] { "~~" }, StringSplitOptions.None)[0];
                    strCaseDataType = strCaseResult.Split(new[] { "~~" }, StringSplitOptions.None)[1];
                    intCasePercentage = int.Parse(strCaseResult.Split(new[] { "~~" }, StringSplitOptions.None)[2]);
                }
                else
                {
                    strCase = strCaseResult;
                }
            }

            string strWhenResult = EvaluateExpression(intEngFaxFormID, strWhenCode, intGroupInstance, intEngagementPageID, strEvaluateAt, false, dtFaxData, dtOCRData, OCRFormName, DataType, false);
            if (strWhenResult.Split(new[] { "~~" }, StringSplitOptions.None).Length > 1)
            {
                strWhen = strWhenResult.Split(new[] { "~~" }, StringSplitOptions.None)[0];
                strWhenDataType = strWhenResult.Split(new[] { "~~" }, StringSplitOptions.None)[1];
                intWhenPercentage = int.Parse(strWhenResult.Split(new[] { "~~" }, StringSplitOptions.None)[2]);
            }
            else
            {
                strWhen = strWhenResult;
            }

            if (DataType == 1 && !(strCaseDataType == "1" && strWhenDataType == "1"))
            {
                DataType = 4;
            }
            else if (!(strCaseDataType == "1" && strWhenDataType == "1"))
            {
                DataType = 4;
            }

            if (strCase == ".")
            {
                strCase = "0";
            }

            if (!string.IsNullOrEmpty(strCase) && !string.IsNullOrEmpty(strWhen))
            {
                intRes = GetCalEvaluateResult(DataType, strCase, ref CaseNumeric, strWhen, ref WhenNumeric, strCompare, intCasePercentage, ref intMaxlength, ref intp, ref intper);
                if (intRes == 1)
                {
                    return true;
                }
            }
            return false;
        }
        private int GetCalEvaluateResult(int DataType, string strCase, ref bool CaseNumeric, string strWhen, ref bool WhenNumeric, string strCompare, int intCasePercentage, ref int intMaxlength, ref int intp, ref int intper)
        {
            int intRes = 0;
            try
            {
                switch (DataType)
                {
                    case 1:
                        CaseNumeric = double.TryParse(strCase, out _);
                        if (!CaseNumeric)
                        {
                            strCase = GetAsciiValue(strCase.ToUpper());
                        }
                        WhenNumeric = double.TryParse(strWhen, out _);
                        if (!WhenNumeric)
                        {
                            strWhen = GetAsciiValue(strWhen.ToUpper());
                        }

                        switch (strCompare)
                        {
                            case "=":
                                if (CaseNumeric)
                                {
                                    if (WhenNumeric)
                                    {
                                        intRes = Math.Abs(Calc.Evaluate(strCase) - Calc.Evaluate(strWhen)) < 1 ? 1 : 0;
                                    }
                                    else
                                    {
                                        intRes = (int)Calc.Evaluate($"{Calc.Evaluate(strCase)}{strCompare}{Calc.Evaluate(strWhen)}");
                                    }
                                }
                                else
                                {
                                    intRes = (int)Calc.Evaluate($"{Calc.Evaluate(strCase)}{strCompare}{Calc.Evaluate(strWhen)}");
                                }
                                break;
                            default:
                                intRes = (int)Calc.Evaluate($"{Calc.Evaluate(strCase)}{strCompare}{Calc.Evaluate(strWhen)}");
                                break;
                        }
                        break;

                    default:
                        if (strCompare == "=")
                        {
                            if (DataType == 4)
                            {
                                if (intCasePercentage > 0)
                                {
                                    intMaxlength = Math.Max(strCase.Replace(" ", "").Length, strWhen.Replace(" ", "").Length);
                                    intp = Levenshtein_distance(strCase.ToUpper().Replace(" ", ""), strWhen.ToUpper().Replace(" ", ""));
                                    intper = Convert.ToInt32(((double)(intMaxlength - intp) / intMaxlength) * 100);
                                    intRes = intper >= intCasePercentage ? 1 : 0;
                                }
                                else if (strWhen.Contains("##"))
                                {
                                    string strCheck = $"##{strWhen}##";
                                    intRes = strCheck.Contains($"##{strCase}##", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                                }
                                else if (strCase.Contains("##"))
                                {
                                    string strCheck = $"##{strCase}##";
                                    intRes = strCheck.Contains($"##{strWhen}##", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                                }
                                else
                                {
                                    intRes = string.Equals(strCase.ToUpper(), strWhen.ToUpper(), StringComparison.Ordinal) ? 1 : 0;
                                }
                            }
                            else
                            {
                                intRes = string.Equals(strCase.ToUpper(), strWhen.ToUpper(), StringComparison.Ordinal) ? 1 : 0;
                            }
                        }
                        else if (strCompare == "#")
                        {
                            if (DataType == 4)
                            {
                                if (intCasePercentage > 0)
                                {
                                    intMaxlength = Math.Max(strCase.Replace(" ", "").Length, strWhen.Replace(" ", "").Length);
                                    intp = Levenshtein_distance(strCase.ToUpper().Replace(" ", ""), strWhen.ToUpper().Replace(" ", ""));
                                    intper = Convert.ToInt32(((double)(intMaxlength - intp) / intMaxlength) * 100);
                                    intRes = intper >= intCasePercentage ? 0 : 1;
                                }
                                else
                                {
                                    intRes = string.Equals(strCase.ToUpper(), strWhen.ToUpper(), StringComparison.Ordinal) ? 0 : 1;
                                }
                            }
                            else
                            {
                                intRes = string.Equals(strCase.ToUpper(), strWhen.ToUpper(), StringComparison.Ordinal) ? 0 : 1;
                            }
                        }
                        else
                        {
                            CaseNumeric = double.TryParse(strCase, out _);
                            if (!CaseNumeric)
                            {
                                strCase = GetAsciiValue(strCase.ToUpper());
                            }
                            WhenNumeric = double.TryParse(strWhen, out _);
                            if (!WhenNumeric)
                            {
                                strWhen = GetAsciiValue(strWhen.ToUpper());
                            }

                            intRes = (int)Calc.Evaluate($"{Calc.Evaluate(strCase)}{strCompare}{Calc.Evaluate(strWhen)}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                // LogEntry(Severity.Low, "DDPAgent", $"GetCalEvaluateResult - Error Occurred: {ex.Message} -> {DateTime.Now} strCompare = {strCompare}", 2, 1, engagementid, TraceType.ErrorLog);
            }

            return intRes;
        }

        private string GetFormulaThenCaseCondition(string[] arrFormula, ref int intCount)
        {
            int tmpcase = 0, tmpend = 0;
            StringBuilder strThenCondition = new StringBuilder();
            strThenCondition.Append(arrFormula[intCount + 4]);

            if (strThenCondition.ToString().ToUpper() == "CASE")
            {
                intCount += 5;
                for (int intCount1 = intCount; intCount1 <= arrFormula.Length - 1; intCount1++)
                {
                    strThenCondition.Append(" ");
                    strThenCondition.Append(arrFormula[intCount1]);
                    intCount++;
                    if (arrFormula[intCount1].ToUpper() == "END")
                    {
                        if (tmpcase == tmpend)
                        {
                            intCount--;
                            break;
                        }
                        else
                        {
                            tmpend++;
                        }
                    }
                    else if (arrFormula[intCount1].ToUpper() == "CASE")
                    {
                        tmpcase++;
                    }
                }
            }

            return strThenCondition.ToString();
        }
        private string GetValue(int intEngFaxFormID, string strCode, int intGroupInstance, int intEngagementPageID, string strEvaluateAt, bool blnStopReplace = true)
        {
            int intTempDataType = 0;
            int intPercentage = 0;
            string strResultValue = string.Empty;
            try
            {
                strCode = strCode.Replace("{", "").Replace("}", "");
                string FuncName = "";
                if (strCode.IndexOf("[") >= 0)
                {
                    FuncName = strCode.Substring(strCode.IndexOf("[") + 1, strCode.IndexOf("]") - strCode.IndexOf("[") - 1);
                    strCode = strCode.Substring(strCode.IndexOf("[" + FuncName + "]") + FuncName.Length + 2);
                }

                if (strCode.Length < 3)
                {
                    return string.Empty;
                }

                if (strCode.Substring(0, 3).ToUpper() == "FAX")
                {
                    strResultValue = GetValueFax(intEngFaxFormID, strCode, intGroupInstance, intEngagementPageID, strEvaluateAt, blnStopReplace, ref intTempDataType, FuncName, ref intPercentage);
                }
                else
                {
                    strResultValue = GetValueNonFax(intEngFaxFormID, strCode, blnStopReplace, ref intTempDataType, FuncName, intGroupInstance);
                }

                return (strResultValue == "" ? "0" : strResultValue) + "~~" + intTempDataType + "~~" + intPercentage;
            }
            catch (Exception ex)
            {
                //LogEntry(Severity.Low, "DDPAgent", "GetValue - Error Occured : " + ex.Message + " -> " + DateTime.Now + " intEngFaxFormID = " + intEngFaxFormID + " strCode =" + strCode + " intGroupInstance =" + intGroupInstance + " intEngagementPageID = " + intEngagementPageID + " strEvaluateAt =" + strEvaluateAt, 3, 1, engagementid, TraceType.ErrorLog);
                throw;
            }

            return string.Empty;
        }
        private string GetValueFax(int intEngFaxFormID, string strCode, int intGroupInstance, int intEngagementPageID, string strEvaluateAt, bool blnStopReplace, ref int intTempDataType, string FuncName, ref int intPercentage)
        {
            string strResultValue = string.Empty;

            List<OCRSubSet> faxSubList = null;
            if (strEvaluateAt == "G" && intGroupInstance > 0)
            {
                faxSubList = GetPageFaxData(dsEngFaxFormFields.Tables[0], intGroupInstance);
            }
            else
            {
                faxSubList = GetPageFaxData(dsEngFaxFormFields.Tables[0], 0);
            }

            List<OCRSubSet> filtrFaxData = null;

            if (FuncName.Contains("AF"))
            {
                filtrFaxData = faxSubList.Where(x => x.FaxDWPCode == strCode).ToList();
            }
            else if (strCode.Substring(0, 7).ToUpper() == "FAX.CCN")
            {
                filtrFaxData = faxSubList.Where(x => x.FaxDWPCode == strCode && x.EngagementFaxFormID == intEngFaxFormID && x.EngagementPageID == intEngagementPageID).ToList();
            }
            else
            {
                filtrFaxData = faxSubList.Where(x => x.FaxDWPCode == strCode && x.EngagementFaxFormID == intEngFaxFormID).ToList();
            }

            foreach (OCRSubSet faxData in filtrFaxData)
            {
                if (strEvaluateAt == "G" && faxData.FaxRowNumber > 0)
                {
                    if (faxData.FaxDWPCode == strCode && faxData.FaxRowNumber == intGroupInstance && faxData.EngagementPageID == intEngagementPageID)
                    {
                        strResultValue = faxData.FFValue == "" ? "0" : faxData.FFValue;
                        ProcessFunction(ref strResultValue, FuncName, ref intTempDataType, blnStopReplace, faxData, filtrFaxData, ref intPercentage);
                        break;
                    }
                    else if (faxData.FaxDWPCode == strCode)
                    {
                        ProcessFunction(ref strResultValue, FuncName, ref intTempDataType, blnStopReplace, faxData, filtrFaxData, ref intPercentage);
                    }
                }
                else if (strEvaluateAt == "S")
                {
                    if (faxData.FaxDWPCode == strCode && faxData.EngagementPageID == intEngagementPageID)
                    {
                        strResultValue = faxData.FFValue == "" ? "0" : faxData.FFValue;
                        ProcessFunction(ref strResultValue, FuncName, ref intTempDataType, blnStopReplace, faxData, filtrFaxData, ref intPercentage);
                        break;
                    }
                }
                else
                {
                    if (faxData.FaxDWPCode == strCode)
                    {
                        if (IsSpecialCode(faxData.FaxDWPCode))
                        {
                            strResultValue += "'" + (faxData.FFValue == "" ? "0" : faxData.FFValue).Replace(" ", "") + "',";
                            intTempDataType = faxData.DataType;
                        }
                        else if (IsMatchSpecialCode(faxData.FaxDWPCode))
                        {

                            bool blnEvaluate = false;
                            DataRow[] drRowsTemp = dsEngFaxFormFields.Tables[0].Select("FaxDWPCode ='FAX.CON.218' and EngagementPageID=" + intEngagementPageID + " and FaxRowNumber=" + intGroupInstance);

                            if (drRowsTemp.Length > 0)
                            {

                                if (Convert.ToInt32(drRowsTemp[0]["FFValue"].ToString().Replace(" ", "")) > 0)

                                {
                                    blnEvaluate = true;
                                }
                            }
                            else
                            {
                                blnEvaluate = true;
                            }

                            drRowsTemp = null;


                            if (blnEvaluate && faxData.FaxRowNumber == intGroupInstance && faxData.EngagementPageID == intEngagementPageID)
                            {
                                strResultValue = (faxData.FFValue == "" ? "0" : faxData.FFValue).Replace(" ", "");
                                intTempDataType = faxData.DataType;
                                switch (faxData.DataType)
                                {
                                    case 7:
                                    case 14:
                                        if (!blnStopReplace)
                                        {
                                            strResultValue = strResultValue.Replace("-", "");
                                        }
                                        break;
                                }
                                break;
                            }
                        }
                        else
                        {
                            ProcessFunction(ref strResultValue, FuncName, ref intTempDataType, blnStopReplace, faxData, filtrFaxData, ref intPercentage);
                            if (strEvaluateAt == "F") break;
                        }
                    }
                }
            }

            if (strResultValue.EndsWith("',") && (strCode == "FAX.CON.096" || strCode == "FAX.CON.149" || strCode == "FAX.CON.153" || strCode == "FAX.CON.161" || strCode == "FAX.CON.163" || strCode == "FAX.CON.508" || strCode == "FAX.CON.291" || strCode == "FAX.CON.292"))
            {
                strResultValue = strResultValue.Substring(0, strResultValue.Length - 1);
            }

            return strResultValue;
        }

        private  bool IsSpecialCode(string strCode)
        {
            return strCode == "FAX.CON.096" || strCode == "FAX.CON.149" || strCode == "FAX.CON.153" ||
                   strCode == "FAX.CON.161" || strCode == "FAX.CON.163" || strCode == "FAX.CON.508" ||
                   strCode == "FAX.CON.291" || strCode == "FAX.CON.292";
        }

        private  bool IsMatchSpecialCode(string strCode)
        {
            return strCode == "FAX.CON.084" || strCode == "FAX.CON.086" || strCode == "FAX.CON.088" ||
                   strCode == "FAX.CON.090" || strCode == "FAX.CON.092" || strCode == "FAX.CON.094" ||
                   strCode == "FAX.CON.165" || strCode == "FAX.CON.173" || strCode == "FAX.CON.248" ||
                   strCode == "FAX.CON.250" || strCode == "FAX.CON.261" || strCode == "FAX.CON.509" ||
                   strCode == "FAX.CON.262" || strCode == "FAX.CON.174" || strCode == "FAX.CON.251" ||
                   strCode == "FAX.CON.166" || strCode == "FAX.CON.248" || strCode == "FAX.CON.249" ||
                   strCode == "FAX.CON.242" || strCode == "FAX.CON.243" || strCode == "FAX.CON.284" ||
                   strCode == "FAX.CON.285";
        }

        private  void ProcessFunction(ref string strResultValue, string funcName, ref int intDataType, bool blnStopReplace, OCRSubSet faxData, List<OCRSubSet> filtrFaxData, ref int intPercentage)
        {
            switch (funcName.Trim())
            {
                case "":
                    strResultValue = string.IsNullOrEmpty(faxData.FFValue) ? "0" : faxData.FFValue;
                    break;
                case "SUM":
                    strResultValue = "0";
                    strResultValue = filtrFaxData.Where(t => double.TryParse(t.FFValue, out _))
                                                 .Sum(t => double.Parse(t.FFValue.Replace(",", "")))
                                                 .ToString();
                    intDataType = 1;
                    return;
                case "AF":
                    strResultValue = string.IsNullOrEmpty(faxData.FFValue) ? "0" : faxData.FFValue;
                    break;
                case "DM":
                    strResultValue = string.Join("##", filtrFaxData.Select(j => j.FFValue));
                    return;
                default:
                    if (int.TryParse(funcName.Trim(), out int percentage))
                    {
                        intPercentage = percentage;
                        strResultValue = string.IsNullOrEmpty(faxData.FFValue) ? "0" : faxData.FFValue;
                    }
                    break;
            }

            intDataType = faxData.DataType;
            if ((faxData.DataType == 7 || faxData.DataType == 14) && !blnStopReplace)
            {
                strResultValue = strResultValue.Replace("-", "");
            }
        }

        private  string GetValueNonFax(int intEngFaxFormID, string strCode, bool blnStopReplace, ref int intTempDataType, string funcName, int intGroupInstance)
        {
            string strResultValue = string.Empty;
            int intEngFormID;
           List<EngFormField> engFormFieldList = GetEngFormFields(dsEngFormFields.Tables[0]);
            List<EngFormField> filtrFormField = null;

            switch (funcName.Trim())
            {
                case "":
                    filtrFormField = engFormFieldList.Where(x => x.FieldDWPCode == strCode).ToList();
                    break;
                case "EP":
                    intEngFormID = EvaluateProformaParent(intEngFaxFormID);
                    filtrFormField = engFormFieldList.Where(x => x.FieldDWPCode == strCode && x.EngagementFormID == intEngFormID).ToList();
                    break;
                case "EPDM":
                    intEngFormID = EvaluateProformaParent(intEngFaxFormID);
                    filtrFormField = engFormFieldList.Where(x => x.FieldDWPCode == strCode && x.ParentEngagementFormID == intEngFormID).ToList();
                    break;
            }

            foreach (var formFieldData in filtrFormField)
            {
                if (formFieldData.FieldDWPCode == "DEP.004")
                {
                    strResultValue += $"'{(string.IsNullOrEmpty(formFieldData.FieldValue) ? "0" : formFieldData.FieldValue).Replace(" ", "")}',";
                    intTempDataType = formFieldData.DataType;

                    if ((formFieldData.DataType == 7 || formFieldData.DataType == 14) && !blnStopReplace)
                    {
                        strResultValue = strResultValue.Replace("-", "");
                    }
                }
                else
                {
                    switch (funcName.Trim())
                    {
                        case "":
                        case "EP":
                            strResultValue = formFieldData.FieldValue ?? string.Empty;
                            intTempDataType = formFieldData.DataType;

                            if ((formFieldData.DataType == 7 || formFieldData.DataType == 14) && !blnStopReplace)
                            {
                                strResultValue = strResultValue.Replace("-", "");
                            }
                            return strResultValue;
                        case "EPDM":
                            strResultValue = string.Join("##", filtrFormField.Select(j => j.FieldValue));
                            break;
                    }
                }
            }

            if (strCode == "DEP.004" && strResultValue.EndsWith("',"))
            {
                strResultValue = strResultValue.Substring(0, strResultValue.Length - 1);
            }

            return strResultValue;
        }
        private string GetValue(string strCode, DataTable dtFaxData, DataTable dtOCRData, int intPageID = 0, int intGroupInstance = 0, string OCRFormName = "", bool blnStopReplace = true)
        {
            string strValue = string.Empty;
            DataRow[] drRows = null;
            int intTempDataType = 0;
            int intPercentage = 0;
            string FuncName = "";

            if (strCode.Contains("["))
            {
                FuncName = strCode.Substring(strCode.IndexOf("[") + 1, strCode.IndexOf("]") - strCode.IndexOf("[") - 1);
                strCode = strCode.Substring(strCode.IndexOf("[" + FuncName + "]") + FuncName.Length + 2);
            }

            if (strCode.Substring(0, 3).ToUpper() == "FAX")
            {
                switch (FuncName.Trim())
                {
                    case "":
                        if (!string.IsNullOrEmpty(OCRFormName))
                        {
                            if (intGroupInstance > 0)
                            {
                                drRows = dtFaxData.Select($"FaxDWPCode='{strCode}' And InputForm='{OCRFormName.Replace("'", "''")}' and FaxRowNumber={intGroupInstance}");
                            }
                            else
                            {
                                drRows = dtFaxData.Select($"FaxDWPCode='{strCode}' And InputForm='{OCRFormName.Replace("'", "''")}'");
                            }
                        }
                        else
                        {
                            drRows = dtFaxData.Select($"FaxDWPCode='{strCode}'");
                        }
                        break;

                    default:
                        drRows = dtFaxData.Select($"FaxDWPCode='{strCode}' And InputForm='{OCRFormName.Replace("'", "''")}'");
                        break;
                }

                if (drRows.Length > 0)
                {
                    strValue = "0";
                    switch (FuncName.Trim().ToUpper())
                    {
                        case "SUM":
                            foreach (var row in drRows)
                            {
                                try
                                {
                                    strValue = (Math.Round(Convert.ToDouble(strValue), 2) + Math.Round(Convert.ToDouble(row["FFValue"]), 2)).ToString();
                                }
                                catch (Exception ex)
                                {
                                    if (dtFaxData != null && dtFaxData.Rows.Count > 0)
                                    {
                                        DataRow drwTemp = GetEngagementId(intPageID, dtFaxData);
                                        // LogEntry(Severity.Low, "DDPAgent", $"GetValue - Error Occurred: {ex.Message} -> strCode = {strCode} intPageID = {intPageID} intGroupInstance = {intGroupInstance}", 912, 1, drwTemp["EngagementID"], TraceType.ErrorLog);
                                    }
                                    else
                                    {
                                        // LogEntry(Severity.Low, "DDPAgent", $"GetValue - Error Occurred: {ex.Message} -> strCode = {strCode} intPageID = {intPageID} intGroupInstance = {intGroupInstance}", 912, 1, intPageID, TraceType.ErrorLog);
                                    }
                                    throw;
                                }
                            }
                            intTempDataType = 1;
                            break;

                        case "AF":
                            strValue = string.IsNullOrEmpty(drRows[0]["FFValue"].ToString()) ? "0" : drRows[0]["FFValue"].ToString();
                            break;

                        case "":
                            strValue = drRows[0]["FFValue"].ToString();
                            intTempDataType = Convert.ToInt32(drRows[0]["DataType"]);
                            if (intTempDataType == 7 || intTempDataType == 14)
                            {
                                if (!blnStopReplace)
                                {
                                    strValue = strValue.Replace("-", "");
                                }
                            }
                            break;
                    }
                }
                else
                {
                    strValue = "0";
                }

                Array.Clear(drRows, 0, drRows.Length);
                return $"{strValue}~~{intTempDataType}~~{intPercentage}";
            }
            else if (strCode.Substring(0, 3).ToUpper() == "OCR")
            {
                if (intGroupInstance > 0)
                {
                    drRows = dtOCRData.Select($"OCRDWPCode='{strCode}' and EngagementPageID={intPageID} and FaxRowNumber={intGroupInstance}");
                    if (drRows.Length == 0)
                    {
                        drRows = dtOCRData.Select($"OCRDWPCode='{strCode}'");
                        if (drRows.Length > 0)
                        {
                            if (drRows[0]["FaxFieldInstance"].ToString() == "S")
                            {
                                drRows = dtOCRData.Select($"OCRDWPCode='{strCode}' and EngagementPageID={intPageID}");
                            }
                            else
                            {
                                drRows = dtOCRData.Select($"OCRDWPCode='{strCode}' and EngagementPageID={intPageID} and FaxRowNumber={intGroupInstance}");
                            }
                        }
                    }
                }
                else
                {
                    drRows = dtOCRData.Select($"OCRDWPCode='{strCode}' and EngagementPageID={intPageID}");
                }

                if (drRows.Length > 0)
                {
                    strValue = string.IsNullOrEmpty(drRows[0]["OCRValue"].ToString()) ? "0" : drRows[0]["OCRValue"].ToString();
                    intTempDataType = Convert.ToInt32(drRows[0]["DataType"]);
                    if (intTempDataType == 7 || intTempDataType == 14)
                    {
                        if (!blnStopReplace)
                        {
                            strValue = strValue.Replace("-", "");
                        }
                    }
                }
                else
                {
                    strValue = "0";
                }

                Array.Clear(drRows, 0, drRows.Length);
                return $"{strValue}~~{intTempDataType}~~{intPercentage}";
            }
            else
            {
                drRows = dsEngFormFields.Tables[0].Select($"FieldDWPCode ='{strCode}'");
                foreach (var row in drRows)
                {
                    if (row["FieldDWPCode"].ToString() == "DEP.004")
                    {
                        strValue += $"'{row["FieldValue"].ToString().Replace(" ", "")}',";
                        intTempDataType = Convert.ToInt32(row["DataType"]);
                        if (intTempDataType == 7 || intTempDataType == 14)
                        {
                            if (!blnStopReplace)
                            {
                                strValue = strValue.Replace("-", "");
                            }
                        }
                    }
                    else
                    {
                        strValue = row["FieldValue"] == DBNull.Value ? "" : row["FieldValue"].ToString();
                        intTempDataType = Convert.ToInt32(row["DataType"]);
                        if (intTempDataType == 7 || intTempDataType == 14)
                        {
                            if (!blnStopReplace)
                            {
                                strValue = strValue.Replace("-", "");
                            }
                        }
                        break;
                    }
                }

                if (strCode == "DEP.004" && strValue.EndsWith("',"))
                {
                    strValue = strValue.Substring(0, strValue.Length - 1);
                }

                return $"{(string.IsNullOrEmpty(strValue) ? "0" : strValue)}~~{intTempDataType}~~{intPercentage}";
            }
        }
        private  string GetValue(string strCode, List<OCRSubSet> ocrSubList, List<OCRSubSet> faxSubList, int intPageID = 0, int intGroupInstance = 0, string OCRFormName = "", bool blnStopReplace = true)
        {
            string strValue = string.Empty;
            string FuncName = string.Empty;

            if (strCode.IndexOf("[") >= 0)
            {
                FuncName = strCode.Substring(strCode.IndexOf("[") + 1, strCode.IndexOf("]") - strCode.IndexOf("[") - 1);
                strCode = strCode.Substring(strCode.IndexOf("[" + FuncName + "]") + FuncName.Length + 2);
            }

            if (strCode.Substring(0, 3).ToUpper() == "FAX")
            {
                return GetFaxValueString(faxSubList, OCRFormName, strCode, intGroupInstance, blnStopReplace, FuncName);
            }
            else if (strCode.Substring(0, 3).ToUpper() == "OCR")
            {
                return GetOCRValueString(ocrSubList, strCode, intGroupInstance, intPageID, blnStopReplace);
            }
            else
            {
                return GetFormFieldFormFieldValue(strCode, blnStopReplace);
            }
        }

        private  string GetFaxValueString(List<OCRSubSet> faxSubList, string OCRFormName, string strCode, int intGroupInstance, bool blnStopReplace, string FuncName)
        {
            int intTempDataType = 0;
            int intPercentage = 0;
            string strValue = string.Empty;

            List<OCRSubSet> fltrFaxData = null;

            if (string.IsNullOrWhiteSpace(FuncName))
            {
                if (!string.IsNullOrEmpty(OCRFormName))
                {
                    if (intGroupInstance > 0)
                    {
                        fltrFaxData = faxSubList.Where(f => f.FaxDWPCode == strCode && f.InputForm == OCRFormName && f.FaxRowNumber == intGroupInstance).ToList();
                    }
                    else
                    {
                        fltrFaxData = faxSubList.Where(f => f.FaxDWPCode == strCode && f.InputForm == OCRFormName).ToList();
                    }
                }
                else
                {
                    fltrFaxData = faxSubList.Where(f => f.FaxDWPCode == strCode).ToList();
                }
            }
            else
            {
                fltrFaxData = faxSubList.Where(f => f.FaxDWPCode == strCode && f.InputForm == OCRFormName).ToList();
            }

            if (fltrFaxData != null && fltrFaxData.Count > 0)
            {
                strValue = "0";
                switch (FuncName.Trim().ToUpper())
                {
                    case "SUM":
                        try
                        {
                            strValue = fltrFaxData.Where(t => double.TryParse(t.FFValue, out _))
                                                  .Sum(t => double.Parse(t.FFValue.Replace(",", "")))
                                                  .ToString();
                        }
                        catch (Exception ex)
                        {
                            // LogEntry(Severity.Low, "DDPAgent", $"GetFaxValueString - Error Occurred: {ex.Message} -> strCode = {strCode} intGroupInstance = {intGroupInstance}", 4, 1, engagementid, TraceType.ErrorLog);
                            throw;
                        }
                        intTempDataType = 1;
                        break;

                    case "AF":
                        strValue = string.IsNullOrWhiteSpace(fltrFaxData[0].FFValue) ? "0" : fltrFaxData[0].FFValue;
                        break;

                    case "":
                        strValue = fltrFaxData[0].FFValue;
                        intTempDataType = fltrFaxData[0].DataType;

                        if ((intTempDataType == 7 || intTempDataType == 14) && !blnStopReplace)
                        {
                            strValue = strValue.Replace("-", "");
                        }
                        break;
                }
            }
            else
            {
                strValue = "0";
            }

            return $"{strValue}~~{intTempDataType}~~{intPercentage}";
        }

        private  string GetOCRValueString(List<OCRSubSet> ocrSubList, string strCode, int intGroupInstance, int intPageID, bool blnStopReplace)
        {
            int intTempDataType = 0;
            int intPercentage = 0;
            string strValue = string.Empty;

            List<OCRSubSet> fltrOcrList = null;

            if (intGroupInstance > 0)
            {
                if (!ocrSubList.Any(x => x.OCRDWPCode == strCode && x.EngagementPageID == intPageID))
                {
                    fltrOcrList = ocrSubList.Where(x => x.OCRDWPCode == strCode).ToList();
                    if (fltrOcrList != null && fltrOcrList.Count > 0)
                    {
                        if (fltrOcrList[0].FaxFieldInstance == "S")
                        {
                            fltrOcrList = ocrSubList.Where(x => x.OCRDWPCode == strCode && x.EngagementPageID == intPageID).ToList();
                        }
                        else
                        {
                            fltrOcrList = ocrSubList.Where(x => x.OCRDWPCode == strCode && x.EngagementPageID == intPageID && x.FaxRowNumber == intGroupInstance).ToList();
                        }
                    }
                }
            }
            else
            {
                fltrOcrList = ocrSubList.Where(x => x.OCRDWPCode == strCode && x.EngagementPageID == intPageID).ToList();
            }

            if (fltrOcrList != null && fltrOcrList.Count > 0)
            {
                strValue = string.IsNullOrEmpty(fltrOcrList[0].OCRValue) ? "0" : fltrOcrList[0].OCRValue;
                intTempDataType = fltrOcrList[0].DataType;

                if ((fltrOcrList[0].DataType == 7 || fltrOcrList[0].DataType == 14) && !blnStopReplace)
                {
                    strValue = strValue.Replace("-", "");
                }
            }
            else
            {
                strValue = "0";
            }

            return $"{strValue}~~{intTempDataType}~~{intPercentage}";
        }

        private  string GetFormFieldFormFieldValue(string strCode, bool blnStopReplace)
        {
            int intTempDataType = 0;
            int intPercentage = 0;
            string strValue = string.Empty;

            List<EngFormField> engFormFieldList = GetEngFormFields(dsEngFormFields.Tables[0]);
            List<EngFormField> filtrFormField = engFormFieldList.Where(x => x.FieldDWPCode == strCode).ToList();

            foreach (var formFieldData in filtrFormField)
            {
                if (formFieldData.FieldDWPCode == "DEP.004")
                {
                    strValue += $"'{(string.IsNullOrEmpty(formFieldData.FieldValue) ? "0" : formFieldData.FieldValue).Replace(" ", "")}',";
                    intTempDataType = formFieldData.DataType;

                    if ((formFieldData.DataType == 7 || formFieldData.DataType == 14) && !blnStopReplace)
                    {
                        strValue = strValue.Replace("-", "");
                    }
                }
                else
                {
                    strValue = formFieldData.FieldValue ?? string.Empty;
                    intTempDataType = formFieldData.DataType;

                    if ((formFieldData.DataType == 7 || formFieldData.DataType == 14) && !blnStopReplace)
                    {
                        strValue = strValue.Replace("-", "");
                    }
                    break;
                }
            }

            if (strCode == "DEP.004" && strValue.EndsWith("',"))
            {
                strValue = strValue.Substring(0, strValue.Length - 1);
            }

            return $"{(string.IsNullOrEmpty(strValue) ? "0" : strValue)}~~{intTempDataType}~~{intPercentage}";
        }

        private string EvaluateParent(int intEngFaxFormID)
        {
            try
            {
                DataRow[] drrows;
                int intParentFaxFormID = 0;
                string strValue = string.Empty;

                drrows = dsEngFaxFormFields.Tables[0].Select("EngagementFaxFormID = " + intEngFaxFormID);

                if (drrows.Length > 0)
                {
                    intParentFaxFormID = Convert.IsDBNull(drrows[0]["ParentEngagementFaxFormID"]) ? -1 : Convert.ToInt32(drrows[0]["ParentEngagementFaxFormID"]);
                }
                drrows = new DataRow[1];
                if (intParentFaxFormID > 0)
                {
                    drrows = dsEngFaxFormFields.Tables[0].Select("EngagementFaxFormID = " + intParentFaxFormID);
                    if (drrows.Length > 0)
                    {
                        strValue = Convert.IsDBNull(drrows[0]["FaxDWPCode"]) ? "" : drrows[0]["FaxDWPCode"].ToString();
                        if (strValue.LastIndexOf('.') > 0)
                        {
                            strValue = strValue.Substring(0, strValue.LastIndexOf('.'));
                        }
                    }
                }
                return strValue;
            }
            catch (Exception ex)
            {
                // LogEntry(Severity.Low, "DDPAgent", "EvaluateParent - Error Occured : " + ex.Message + " -> " + DateTime.Now + " intEngFaxFormID = " + intEngFaxFormID, 5, 1, engagementid, TraceType.ErrorLog);
                throw;
            }
        }

        private string EvaluateExpression(int intEngFaxFormID, string strExpression, int intGroupInstance, int intEngagementPageID, string strEvaluateAt, bool blnIsMapping, DataTable dtFaxData, DataTable dtOCRData, string OCRFormName = "", int DataType = 0, bool blnStopReplace = true)
        {
            string strCode = "";
            bool blnIsCode = false;
            string strValue = "";
            char[] arrData;
            bool blnExpression = false;
            string strTempResult;
            int intTempDataType = 0;
            int intTempPercentage = 0;

            arrData = strExpression.ToCharArray();

            for (int intCount = 0; intCount < arrData.Length; intCount++)
            {
                if (arrData[intCount] == '{')
                {
                    strCode = "";
                    blnIsCode = true;
                }
                else if (arrData[intCount] == '}')
                {
                    if (blnIsMapping)
                    {
                        strValue += strCode;
                    }
                    else
                    {
                        if (intEngFaxFormID == 0)
                        {
                            strTempResult = GetValue(strCode, dtFaxData, dtOCRData, intEngagementPageID, intGroupInstance, OCRFormName, blnStopReplace);
                            intTempDataType = Convert.ToInt32(strTempResult.Split("~~")[1]);
                            strValue += strTempResult.Split("~~")[0];
                            intTempPercentage = Convert.ToInt32(strTempResult.Split("~~")[2]);
                        }
                        else
                        {
                            strTempResult = GetValue(intEngFaxFormID, strCode, intGroupInstance, intEngagementPageID, strEvaluateAt, blnStopReplace);
                            intTempDataType = Convert.ToInt32(strTempResult.Split("~~")[1]);
                            strValue += strTempResult.Split("~~")[0];
                            intTempPercentage = Convert.ToInt32(strTempResult.Split("~~")[2]);
                        }
                    }
                    blnIsCode = false;
                }
                else
                {
                    if (blnIsCode)
                    {
                        strCode += arrData[intCount];
                    }
                    else
                    {
                        strValue += arrData[intCount];
                        blnExpression = true;
                    }
                }
            }

            if (Regex.IsMatch(ReplaceFunctions(strValue), "[a-zA-Z]") || !blnExpression)
            {
                if (strValue.Contains("+"))
                {
                    strValue = Regex.Replace(strValue, "@", "");
                    strValue = strValue.Replace("+", "").Replace("space", " ");

                    if (intTempDataType != 0)
                    {
                        return strValue + "~~" + intTempDataType + "~~" + intTempPercentage;
                    }
                    else
                    {
                        return strValue;
                    }
                }
                else
                {
                    if (blnIsMapping)
                    {
                        strValue = Regex.Replace(strValue, @"\s", "");
                    }
                    strValue = Regex.Replace(strValue, "~", " ");
                    strValue = Regex.Replace(strValue, "@", "");
                    strValue = strValue + "~~" + intTempDataType + "~~" + intTempPercentage;
                    return strValue;
                }
            }
            else
            {
                strValue = Regex.Replace(strValue, @"\s", "");
                if (strValue != "")
                {
                    try
                    {
                        switch (DataType)
                        {
                            case 1:
                                if (strValue.IndexOf("=") >= 0)
                                {
                                    string[] tmpar = strValue.Split(" ");
                                    if (Math.Abs(Convert.ToInt32(tmpar[0]) - Convert.ToInt32(tmpar[2])) <= 1)
                                    {
                                        return "1";
                                    }
                                    else
                                    {
                                        return "0";
                                    }
                                }
                                else
                                {
                                    return Calc.Evaluate(strValue).ToString() + "~~1" + "~~" + intTempPercentage;
                                }
                            default:
                                if (strValue == "99/99/9999")
                                {
                                    return strValue;
                                }
                                else
                                {
                                    return Calc.Evaluate(strValue).ToString();
                                }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (dtFaxData != null && dtFaxData.Rows.Count > 0)
                        {
                            DataRow drwTemp = GetEngagementId(intEngagementPageID, dtFaxData);
                            // LogEntry(Severity.Low, "DDPAgent", "EvaluateExpression - Error Occured : " + ex.Message + " -> " + DateTime.Now + " intEngFaxFormID = " + intEngFaxFormID + " intGroupInstance = " + intGroupInstance + " intEngagementPageID = " + intEngagementPageID + " OCRFormName = " + OCRFormName, 6, 1, Convert.ToInt32(drwTemp["EngagementID"]), TraceType.ErrorLog);
                        }
                        else
                        {
                            // LogEntry(Severity.Low, "DDPAgent", "EvaluateExpression - Error Occured : " + ex.Message + " -> " + DateTime.Now + " intEngFaxFormID = " + intEngFaxFormID + " intGroupInstance = " + intGroupInstance + " intEngagementPageID = " + intEngagementPageID + " OCRFormName = " + OCRFormName, 6, 1, intEngagementPageID, TraceType.ErrorLog);
                        }
                        return "0";
                    }
                }
            }
            return "0";
        }

        private string GetAsciiValue(string strVal)
        {
            try
            {
                int intResult = 0;
                strVal = strVal.ToUpper();

                for (int intCount = 1; intCount <= strVal.Length; intCount++)
                {
                    //intResult += (Asc(strVal.Substring(intCount - 1, 1)) * intCount) * intCount;

                    intResult += (Convert.ToInt32(strVal.Substring(intCount - 1, 1)[0]) * intCount) * intCount;

                }
                return intResult.ToString();
            }
            catch (Exception ex)
            {
                // LogEntry(Severity.Low, "DDPAgent", "GetAsciiValue - Error Occured : " + ex.Message + " -> " + DateTime.Now + " strVal =" + strVal, 7, 1, engagementid, TraceType.ErrorLog);
                throw;
            }
        }

        private string ReplaceFunctions(string strValue)
        {
            try
            {
                if (!string.IsNullOrEmpty(strValue))
                {
                    return strValue.ToUpper().Replace("ROUND", "");
                }
                return strValue;
            }
            catch (Exception ex)
            {
                // LogEntry(Severity.Low, "DDPAgent", "ReplaceFunctions - Error Occured : " + ex.Message + " -> " + DateTime.Now + " strValue =" + strValue, 913, 1, engagementid, TraceType.ErrorLog);
                throw;
            }
        }
        public void EvaluateFaxToTaxFormula(int intEngagementID, bool isMultiThreadingActive )
        {
            PopulateDSEngFormFields(intEngagementID);
            PopulateDSEngFaxFormFields(intEngagementID);
            PopulateDSEngFaxTaxFormFields(intEngagementID);

            if (IsMultiThreadingActive())
            {
                ParseMappingAndFormulaViaMultiTasking(intEngagementID);
            }
            else
            {
                ParseMappingAndFormula(intEngagementID);
                UpdateEvaluationCompleted(intEngagementID);
            }
        }

        public DataSet PublicPopulateDSEngFormFields(int intEngagementID)
        {
            PopulateDSEngFormFields(intEngagementID);
            return dsEngFormFields;
        }

        public DataSet PublicPopulateDSEngFaxTaxFormFields(int intEngagementID)
        {
            PopulateDSEngFaxTaxFormFields(intEngagementID);
            return dsEngFaxTaxFormFields;
        }

        public DataSet PublicParseMappingAndFormulaViaMultiThreading(DataSet dsEngFaxFormFieldsParam, DataSet dsEngFaxTaxFormFieldsParam, int intEngagementID)
        {
            dsEngFaxFormFields = dsEngFaxFormFieldsParam;
            dsEngFaxTaxFormFields = dsEngFaxTaxFormFieldsParam;
            ParseMappingAndFormulaViaMultiTasking(intEngagementID);
            return dsFaxTax;
        }

        public DataSet PublicParsemappingAndFormula(DataSet dsEngFaxFormFieldsParam, DataSet dsEngFaxTaxFormFieldsParam, int intEngagementID)
        {
            dsEngFaxFormFields = dsEngFaxFormFieldsParam;
            dsEngFaxTaxFormFields = dsEngFaxTaxFormFieldsParam;
            ParseMappingAndFormulaViaMultiTasking(intEngagementID);
            return dsFaxTax;
        }

        public DataSet PublicPopulateDSEngFaxFormFields(int intEngagementID)
        {
            PopulateDSEngFaxFormFields(intEngagementID);
            return dsEngFaxFormFields;
        }

        public bool PopulateDSEngFormFields(int intEngagementID)
        {
            try
            {
               
                    SpParameter[] spParams = new SpParameter[1];
                    spParams[0] = new SpParameter("EngagementID", intEngagementID);
                    dsEngFormFields = GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_PopulateDSEngFormFields", spParams, true);
                
                WriteDBLog("Function PopulateDSEngFormFields called...", 701, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                Console.WriteLine("Function PopulateDSEngFormFields called..." + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", "Function PopulateDSEngFormFields called -> " + DateTime.Now, 914, 1, intEngagementID, TraceType.InfoLog);
            }
            catch (Exception ex)
            {
                WriteDBLog($"PopulateDSEngFormFields>>Error occurred:{ex.Message}", 701, 1, Severity.Low, TraceType.ErrorLog, intEngagementID);
                Console.WriteLine($"PopulateDSEngFormFields>>Error occurred:{ex.Message}, called ->" + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", $"PopulateDSEngFormFields>>Error occurred:{ex.Message}, called ->" + DateTime.Now, 914, 1, intEngagementID, TraceType.ErrorLog);
            }
            return true;
        }

        public bool IsMultiThreadingActive()
        {
            try
            {
                string result = "";
                result = (string.IsNullOrWhiteSpace(GenModule.configuration.GetSection("UseMutlithreadedFormulaEvaluation").Value) ? "N" : GenModule.configuration.GetSection("UseMutlithreadedFormulaEvaluation").Value);
                if(result == "Y")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private void PopulateDSEngFaxFormFields(int intEngagementID)
        {
            try
            {
                
                    DataSet Localds;
                    SpParameter[] sqlParam = new SpParameter[1];
                    sqlParam[0] = new SpParameter("@EngagementID", intEngagementID);

                    Localds = GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetEngFaxFormFieldWithMapping", sqlParam, true);

                    dsEngFaxFormFields = Localds;
                
                dsEngFaxFormFields.Tables[0].TableName = "FaxFormData";
                dsEngFaxFormFields.Tables[0].Columns.Add("Threadid");
                WriteDBLog("Function PopulateDSEngFaxFormFields called...", 702, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                Console.WriteLine("Function PopulateDSEngFaxFormFields called..." + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", "Function PopulateDSEngFaxFormFields called -> " + DateTime.Now, 915, 1, intEngagementID, TraceType.InfoLog);
            }
            catch (Exception ex)
            {
                WriteDBLog($"PopulateDSEngFaxFormFields>>Error occurred:{ex.Message}", 702, 1, Severity.Low, TraceType.ErrorLog, intEngagementID);
                Console.WriteLine($"PopulateDSEngFaxFormFields>>Error occurred:{ex.Message}, called ->" + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", $"PopulateDSEngFaxFormFields>>Error occurred:{ex.Message}, called ->" + DateTime.Now, 915, 1, intEngagementID, TraceType.ErrorLog);
            }
        }

        private DataTable Pivot_Unpivot(DataTable Dtpivot)
        {
            List<string> columns = new List<string> { "Mapping_1", "Mapping_2", "Mapping_3", "Mapping_4", "Mapping_5", "Mapping_6", "Mapping_7", "Mapping_8", "Mapping_9", "Mapping_10" };
            var cartesian = from row in Dtpivot.AsEnumerable()
                            from col in columns
                            select new
                            {
                                EngagementPageID = row.Field<int>("EngagementPageID"),
                                FaxFieldType = row.Field<string>("FaxFieldType"),
                                FormStatus = row.Field<string>("FormStatus"),
                                EngagementFaxFormFieldID = row.Field<int?>("EngagementFaxFormFieldID"),
                                EngagementFaxFormID = row.Field<int>("EngagementFaxFormID"),
                                EngagementID = row.Field<int>("EngagementID"),
                                FFValue = row.Field<string>("FFValue"),
                                FaxDWPCode = row.Field<string>("FaxDWPCode"),
                                FaxRowNumber = row.Field<int>("FaxRowNumber"),
                                FaxFormID = row.Field<int>("FaxFormID"),
                                ParentEngagementFaxFormID = row.Field<int?>("ParentEngagementFaxFormID"),
                                FaxFormFieldID = row.Field<int?>("FaxFormFieldID"),
                                FaxFormType = row.Field<string>("FaxFormType"),
                                FaxFormInstance = row.Field<string>("FaxFormInstance"),
                                FaxFieldInstance = row.Field<string>("FaxFieldInstance"),
                                Identifier = row.Field<string>("Identifier"),
                                FieldDWPCode = row.Field<string>("FieldDWPCode"),
                                InputForm = row.Field<string>("InputForm"),
                                SheetNo = row.Field<int>("SheetNo"),
                                EvaluateAt = row.Field<string>("EvaluateAt"),
                                DataType = row.Field<string>("DataType"),
                                ConvertCount = row.Field<int>("ConvertCount"),
                                DuplicateOcrFieldID = row.Field<int>("DuplicateOcrFieldID"),
                                IsAutoDuplicate = row.Field<int>("IsAutoDuplicate"),
                                IsAutoUnchecked = row.Field<int>("IsAutoUnchecked"),
                                OcrDuplicateFieldIds = row.Field<string>("OcrDuplicateFieldIds"),
                                DuplicatePageIds = row.Field<string>("DuplicatePageIds"),
                                EngagementFormID = row.Field<int>("EngagementFormID"),
                                DoNotEvaluateMapping = row.Field<bool>("DoNotEvaluateMapping"),
                                Currency = row.Field<string>("Currency"),
                                Mapping_ID = col == "Mapping_1" ? 1 : col == "Mapping_2" ? 2 : col == "Mapping_3" ? 3 : col == "Mapping_4" ? 4 : col == "Mapping_5" ? 5 : col == "Mapping_6" ? 6 : col == "Mapping_7" ? 7 : col == "Mapping_8" ? 8 : col == "Mapping_9" ? 9 : 10,
                                Mapping = row.Field<string>(col),
                                Formula = col == "Mapping_1" ? row.Field<string>("Formula_1") : col == "Mapping_2" ? row.Field<string>("Formula_2") : col == "Mapping_3" ? row.Field<string>("Formula_3") : col == "Mapping_4" ? row.Field<string>("Formula_4") : col == "Mapping_5" ? row.Field<string>("Formula_5") : col == "Mapping_6" ? row.Field<string>("Formula_6") : col == "Mapping_7" ? row.Field<string>("Formula_7") : col == "Mapping_8" ? row.Field<string>("Formula_8") : col == "Mapping_9" ? row.Field<string>("Formula_9") : row.Field<string>("Formula_10")
                            };

            var qry = from docs in cartesian
                      where docs.Mapping.Length > 0
                      select docs;

            DataTable DtunPivot = CreateUnpivotTable();
            foreach (var item in qry)
            {
                DtunPivot.Rows.Add(
                    item.EngagementPageID,
                    item.FaxFieldType,
                    item.FormStatus,
                    item.EngagementFaxFormFieldID,
                    item.EngagementFaxFormID,
                    item.EngagementID,
                    item.FFValue,
                    item.FaxDWPCode,
                    item.FaxRowNumber,
                    item.FaxFormID,
                    item.ParentEngagementFaxFormID,
                    item.FaxFormFieldID,
                    item.FaxFormType,
                    item.FaxFormInstance,
                    item.FaxFieldInstance,
                    item.Identifier,
                    item.FieldDWPCode,
                    item.InputForm,
                    item.SheetNo,
                    item.EvaluateAt,
                    item.DataType,
                    item.ConvertCount,
                    item.DuplicateOcrFieldID,
                    item.IsAutoDuplicate,
                    item.IsAutoUnchecked,
                    item.OcrDuplicateFieldIds,
                    item.DuplicatePageIds,
                    item.EngagementFormID,
                    item.DoNotEvaluateMapping,
                    item.Currency,
                    item.Mapping_ID,
                    item.Mapping,
                    item.Formula
                );
            }

            return DtunPivot;
        }

        private DataTable CreateUnpivotTable()
        {
            DataTable Unpivot = new DataTable("Unpivoted");
            Unpivot.Columns.Add("EngagementPageID", typeof(int));
            Unpivot.Columns.Add("FaxFieldType", typeof(string));
            Unpivot.Columns.Add("FormStatus", typeof(string));
            Unpivot.Columns.Add("EngagementFaxFormFieldID", typeof(int)).AllowDBNull = true;
            Unpivot.Columns.Add("EngagementFaxFormID", typeof(int));
            Unpivot.Columns.Add("EngagementID", typeof(int));
            Unpivot.Columns.Add("FFValue", typeof(string));
            Unpivot.Columns.Add("FaxDWPCode", typeof(string));
            Unpivot.Columns.Add("FaxRowNumber", typeof(int));
            Unpivot.Columns.Add("FaxFormID", typeof(int));
            Unpivot.Columns.Add("ParentEngagementFaxFormID", typeof(int)).AllowDBNull = true;
            Unpivot.Columns.Add("FaxFormFieldID", typeof(int)).AllowDBNull = true;
            Unpivot.Columns.Add("FaxFormType", typeof(string)).AllowDBNull = true;
            Unpivot.Columns.Add("FaxFormInstance", typeof(string));
            Unpivot.Columns.Add("FaxFieldInstance", typeof(string));
            Unpivot.Columns.Add("Identifier", typeof(string));
            Unpivot.Columns.Add("FieldDWPCode", typeof(string));
            Unpivot.Columns.Add("InputForm", typeof(string));
            Unpivot.Columns.Add("SheetNo", typeof(int));
            Unpivot.Columns.Add("EvaluateAt", typeof(string));
            Unpivot.Columns.Add("DataType", typeof(string));
            Unpivot.Columns.Add("ConvertCount", typeof(int));
            Unpivot.Columns.Add("DuplicateOcrFieldID", typeof(int));
            Unpivot.Columns.Add("IsAutoDuplicate", typeof(int));
            Unpivot.Columns.Add("IsAutoUnchecked", typeof(int));
            Unpivot.Columns.Add("OcrDuplicateFieldIds", typeof(string));
            Unpivot.Columns.Add("DuplicatePageIds", typeof(string));
            Unpivot.Columns.Add("EngagementFormID", typeof(int));
            Unpivot.Columns.Add("DoNotEvaluateMapping", typeof(bool));
            Unpivot.Columns.Add("Currency", typeof(string));
            Unpivot.Columns.Add("Mapping_ID", typeof(string));
            Unpivot.Columns.Add("Mapping", typeof(string));
            Unpivot.Columns.Add("Formula", typeof(string));
            return Unpivot;
        }
        private bool PopulateDSEngFaxTaxFormFields(int intEngagementID)
        {
            try
            {
               
                    SpParameter[] spParams = new SpParameter[1];
                    spParams[0] = new SpParameter("EngagementID", intEngagementID);
                    dsEngFaxTaxFormFields = GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_PopulateDSEngFaxTaxFormFields", spParams, true);
                
                dsEngFaxTaxFormFields.Tables[0].Columns.Add("Threadid", typeof(int));

                WriteDBLog("Function PopulateDSEngFaxTaxFormFields called...", 703, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                Console.WriteLine("Function PopulateDSEngFaxTaxFormFields called..." + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", "Function PopulateDSEngFaxTaxFormFields called -> " + DateTime.Now, 916, 1, intEngagementID, TraceType.InfoLog);
            }
            catch (Exception ex)
            {
                WriteDBLog($"PopulateDSEngFaxTaxFormFields>>Error occurred:{ex.Message}", 703, 1, Severity.Low, TraceType.ErrorLog, intEngagementID);
                Console.WriteLine($"PopulateDSEngFaxTaxFormFields>>Error occurred:{ex.Message}, called ->" + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", $"PopulateDSEngFaxTaxFormFields>>Error occurred:{ex.Message}, called ->" + DateTime.Now, 916, 1, intEngagementID, TraceType.ErrorLog);
                return false;
            }
            return true;
        }

        private void ParseMappingAndFormula(int intEngagementID)
        {
            try
            {
                DataTable dt = dsEngFaxFormFields.Tables[0];
                DataTable dtFaxTax = dsEngFaxTaxFormFields.Tables[0];

                for (int intCount = 0; intCount < dt.Rows.Count; intCount++)
                {
                    if (intCount == dt.Rows.Count - 1)
                    {
                        // LogEntry(Severity.Low, "DDPAgent", "ParseMappingAndFormula......Total Records Processed-" + intCount.ToString(), 70004, 1, intEngagementID, TraceType.InfoLog);
                    }

                    if (NeedsEvaluation(dt.Rows[intCount]))
                    {
                        for (int i = 10; i >= 1; i--)
                        {
                            EvalMapping("Mapping_" + i.ToString(), "Formula_" + i.ToString(), dt.Rows[intCount], dtFaxTax,Convert.ToInt32( i.ToString()));
                        }
                    }

                    if (intCount % 10000 == 0 && intCount > 0)
                    {
                        WriteDBLog("ParseMappingAndFormula......Total Records Processed-" + intCount.ToString(), 704, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                        Console.WriteLine("ParseMappingAndFormula......Total Records Processed-" + intCount.ToString() + " -> " + DateTime.Now);
                        // LogEntry(Severity.Low, "DDPAgent", "ParseMappingAndFormula......Total Records Processed-" + intCount.ToString(), 704, 1, intEngagementID, TraceType.InfoLog);
                    }
                }

                if (blnDoUpdate)
                {
                    UpdateFaxFormData(ref dtFaxTax, 0);
                }

                dsFaxTax.Tables.Add(dtFaxTax.Copy());
                WriteDBLog("Function ParseMappingAndFormula called...", 705, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                Console.WriteLine("Function ParseMappingAndFormula called..." + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", "Function ParseMappingAndFormula called -> " + DateTime.Now, 706, 1, intEngagementID, TraceType.InfoLog);
            }
            catch (Exception ex)
            {
                WriteDBLog($"ParseMappingAndFormula>>Error occurred:{ex.Message}", 705, 1, Severity.Low, TraceType.ErrorLog, intEngagementID);
                Console.WriteLine($"ParseMappingAndFormula>>Error occurred:{ex.Message}, called ->" + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", $"ParseMappingAndFormula>>Error occurred:{ex.Message}, called ->" + DateTime.Now, 706, 1, intEngagementID, TraceType.ErrorLog);
            }
        }



        //#region Multi-Threading Code to Perform Evaluation


        protected virtual void OnCancelTask(EventArgs e)
        {
            CancelTask?.Invoke(this, e);
        }

        // Example method to trigger the event
        public void TriggerCancelTask()
        {
            OnCancelTask(EventArgs.Empty);
        }


        private void HandleCancelTask()
        {
            tokenSource2.Cancel();
        }

        private void ParseMappingAndFormulaViaMultiTasking(int intEngagementID)
        {
            DataTable dt = dsEngFaxFormFields.Tables[0];
            int totalNumOfRecords = dt.Rows.Count;
            DataTable dtFaxTaxTable = dsEngFaxTaxFormFields.Tables[0];
            int numOfTasks = 0;
            int capValue = 0;
            DataTable taskTable = CreateTaskTable();
            ExceptionOccured = false;

            if (totalNumOfRecords > 0)
            {
                int remainingRecords = 0;
                GetNumOfThreadAndCapValue(totalNumOfRecords,ref numOfTasks,ref capValue);

                decimal perTaskRecords = Math.Ceiling((decimal)totalNumOfRecords / numOfTasks);

                int startRecord = 0;
                int endRecord = startRecord + (int)perTaskRecords - 1;
                List<Task> listOfTasks = new List<Task>();

                WriteDBLog("ParseMappingAndFormula Multi-Tasking started..." + " Total Number of Records:" + totalNumOfRecords.ToString() + " Total Tasks are: " + numOfTasks.ToString(),
                 705, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                Console.WriteLine("ParseMappingAndFormula Multi-Tasking started..." + " Total Number of Records:" + totalNumOfRecords.ToString() + " Total Tasks are: " + numOfTasks.ToString() + " -> " + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", "ParseMappingAndFormula Multi-Tasking started..." + " Total Number of Records:" + totalNumOfRecords.ToString() + " Total Task are: " + numOfTasks.ToString(), 705, 1, intEngagementID, TraceType.InfoLog);

                for (int i = 1; i <= numOfTasks; i++)
                {
                    int taskNum = i;

                    CreateRows(taskTable, ref startRecord,ref endRecord, taskNum);

                    if (i < numOfTasks)
                    {
                        startRecord = endRecord + 1;
                        endRecord = endRecord + (int)perTaskRecords;

                        if (endRecord >= totalNumOfRecords)
                        {
                            int extraRecords = endRecord - totalNumOfRecords;
                            endRecord = (endRecord - extraRecords) - 1;
                        }
                    }
                }
                dicDoUpdate.Clear();
                CallPharseMappingFormula(intEngagementID, dt, dtFaxTaxTable, capValue, taskTable, listOfTasks);

                Task.WaitAll(listOfTasks.ToArray(), cancellationToken);

                WriteDBLog("ParseMappingAndFormula Multi-Tasking Completed...", 705, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                Console.WriteLine("ParseMappingAndFormula Multi-Tasking Completed..." + " Total Number of Records:" + totalNumOfRecords.ToString() + " Total Tasks are: " + numOfTasks.ToString() + " -> " + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", "Function ParseMappingAndFormula via Multi-Tasking Completed...", 705, 1, intEngagementID, TraceType.InfoLog);
            }
            else
            {
                WriteDBLog("ParseMappingAndFormula MultiTasking........" + " No Records found to proceed", 705, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                Console.WriteLine("ParseMappingAndFormula MultiTasking........" + " No Records found to proceed" + " -> " + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", "ParseMappingAndFormulaViaMultiTasking........" + "No Records found to proceed", 705, 1, intEngagementID, TraceType.InfoLog);

                if (!ExceptionOccured)
                {
                    UpdateEvaluationCompleted(intEngagementID);
                }
            }

            dsFaxTax.Merge(dtFaxTaxTable.Copy());
        }
        private void CallPharseMappingFormula(int intEngagementID, DataTable dt, DataTable dtFaxTaxTable, int capValue, DataTable taskTable, List<Task> listOfTasks)
        {
            try
            {
                int rowIndex = 0;
                while (rowIndex < taskTable.Rows.Count)
                {
                    DataRow row = taskTable.Rows[rowIndex];
                    DataTable tempFaxTaxTable = dtFaxTaxTable.Clone();
                    DataTable tempFaxTaxTable1 = dtFaxTaxTable.Clone();
                    DataTable tempFaxTaxTable2 = dtFaxTaxTable.Copy();
                    if (!taskStatus.ContainsKey((int)row["TaskNum"]))
                    {
                        taskStatus.Add((int)row["TaskNum"], row["ReferenceCount"].ToString());
                    }
                    WriteDBLog("ParseMappingFormula... Task " + row["TaskNum"].ToString() + " Ref Proceeding from : " + row["StartRecord"].ToString() + "-" + row["EndRecord"].ToString(), 705, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                    Console.WriteLine("ParseMappingFormula... Task " + row["TaskNum"].ToString() + " Ref Proceeding from : " + row["StartRecord"].ToString() + "-" + row["EndRecord"].ToString());
                    listOfTasks.Add(Task.Run(() => ParseMappingAndFormula(intEngagementID, dt, (int)row["StartRecord"], (int)row["EndRecord"], (int)row["TaskNum"], tempFaxTaxTable, capValue)));
                    rowIndex++;
                }
                rowIndex--;
            }
            catch (Exception ex)
            {
                // LogEntry(Severity.Low, "DDPAgent", "CallPharseMappingFormula - Error Occured:" + ex.Message + "::" + ex.StackTrace, 147, 1, intEngagementID, TraceType.ErrorLog);
            }
        }

        private void CreateRows(DataTable taskTable, ref int startRecord, ref int endRecord, int taskNum)
        {
            DataRow Row1 = taskTable.NewRow();

            Row1["TaskNum"] = taskNum;
            Row1["StartRecord"] = startRecord;
            Row1["EndRecord"] = endRecord;
            Row1["ReferenceCount"] = startRecord.ToString() + "-" + endRecord.ToString();
            taskTable.Rows.Add(Row1);
        }

        private DataTable CreateTaskTable()
        {
            DataTable taskTable = new DataTable("TreadTable");
            DataColumn taskNumCl = new DataColumn("TaskNum");
            taskNumCl.DataType = typeof(int);

            DataColumn startRecordCl = new DataColumn("StartRecord");
            startRecordCl.DataType = typeof(int);
            DataColumn endRecordCl = new DataColumn("EndRecord");
            endRecordCl.DataType = typeof(int);
            DataColumn referenceCountCl = new DataColumn("ReferenceCount");
            referenceCountCl.DataType = typeof(string);
            taskTable.Columns.Add(taskNumCl);
            taskTable.Columns.Add(startRecordCl);
            taskTable.Columns.Add(endRecordCl);
            taskTable.Columns.Add(referenceCountCl);
            return taskTable;
        }

        private bool ParseMappingAndFormula(int intEngagementID, DataTable dt, double startIndex, double endIndex, int taskNum, DataTable tempFaxTaxTable, int capValue)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                WriteDBLog("ParseMappingAndFormula Milti-Tasking Cancelled...", 705, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                Console.WriteLine("ParseMappingAndFormula Milti-Tasking Cancelled..." + " Task : " + taskNum.ToString() + " -> " + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", "Cancellation of the ParseMappingAndFormula is requested. Task Cancelled" + intEngagementID + "Task Information: " + taskNum + ", " + "Records: " + taskStatus[taskNum], 705, 1, intEngagementID, TraceType.ErrorLog);
                throw new OperationCanceledException();
            }

            DataTable dtSPEngagementTempTable = CreateTempSPEngagementTable();
            DataTable dtMappingTable = CreateTempTableForStatusUpdate();
            int intCount = 0 ;
            int mappingFormula = 0;
            int modCounter = 0;
            try
            {
                for (intCount = (int)startIndex; intCount <= (int)endIndex; intCount++)
                {
                    modCounter++;
                    if (NeedsEvaluation(dt.Rows[intCount]))
                    {
                        for (int i = 10; i >= 1; i--)
                        {
                            mappingFormula = i;
                            EvalMapping("Mapping_" + i.ToString(), "Formula_" + i.ToString(), dt.Rows[intCount], tempFaxTaxTable,Convert.ToInt32(i.ToString()), taskNum);
                        }

                        AddTempRowToUpdateMappingStatus(intEngagementID,ref dtSPEngagementTempTable, dt.Rows[intCount]);
                    }

                    AddTempRowToUpdateMappingStatus(intEngagementID,ref dtSPEngagementTempTable, dt.Rows[intCount]);
                    if (modCounter % capValue == 0 && modCounter > 0)
                    {
                        if (dicDoUpdate.ContainsKey(taskNum) && dicDoUpdate[taskNum])
                        {
                            if (tempFaxTaxTable.Rows.Count > 0)
                            {
                                UpdateFaxFormData(ref tempFaxTaxTable, taskNum);
                                blnDoUpdate = false;
                                dicDoUpdate[taskNum] = false;
                                WriteDBLog("ParseMappingAndFormula... Task " + taskNum.ToString() + ": " + modCounter.ToString() + " (" + startIndex.ToString() + "-" + endIndex.ToString() + ")", 704, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                                Console.WriteLine("ParseMappingAndFormula... Task " + taskNum.ToString() + ": " + modCounter.ToString() + " (" + startIndex.ToString() + "-" + endIndex.ToString() + ")");
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ExceptionOccured = true;
                string exceptionString = "Excpetion: " + ex.ToString() + "Task Information: " + taskNum.ToString() + ", " + "Records: " + taskStatus[taskNum].ToString();
                AddTempRowToUpdateExecutionStatus(intEngagementID,ref dtMappingTable, dt.Rows[intCount], "-" + mappingFormula.ToString(), -1, exceptionString + " " + "FaxFormFieldID: " + dt.Rows[intCount]["FaxFormFieldID"].ToString());
                UpdateExecutionStatusForFaxFormData(intEngagementID, dtMappingTable);

                WriteDBLog("ParseMappingAndFormula Failed..." + "Exception: " + ex.ToString(), 705, 1, Severity.Low, TraceType.ErrorLog, intEngagementID);
                Console.WriteLine("ParseMappingAndFormula Failed..." + "Exception: " + ex.ToString() + " Task : " + taskNum.ToString() + " -> " + DateTime.Now);
                // LogEntry(Severity.Low, "DDPAgent", "Error occured while evaluation formula for engagementid " + intEngagementID + exceptionString, 705, 1, intEngagementID, TraceType.ErrorLog);

                return false;
            }
            finally
            {
                lock (lockObj)
                {
                    if (tempFaxTaxTable.Rows.Count > 0)
                    {
                        if (dicDoUpdate.ContainsKey(taskNum) && dicDoUpdate[taskNum])
                        {
                            UpdateFaxFormData(ref tempFaxTaxTable, taskNum);
                            dicDoUpdate[taskNum] = false;
                            WriteDBLog("ParseMappingAndFormula... Task " + taskNum.ToString() + ": " + modCounter.ToString() + " (" + startIndex.ToString() + "-" + endIndex.ToString() + ")", 704, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                            Console.WriteLine("ParseMappingAndFormula... Task " + taskNum.ToString() + ": " + modCounter.ToString() + " (" + startIndex.ToString() + "-" + endIndex.ToString() + ")");
                        }

                        dsFaxTax.Merge(tempFaxTaxTable);
                    }

                    UpdateEvaluationCompleted(intEngagementID, ref dtSPEngagementTempTable);
                    WriteDBLog("ParseMappingAndFormula Completed..." + " Task : " + taskNum.ToString(), 705, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                    Console.WriteLine("ParseMappingAndFormula Completed..." + " Task : " + taskNum.ToString() + " -> " + DateTime.Now);
                }
            }
        }
        private DataTable CreateTempSPEngagementTable()
        {
            DataTable dtTempTable = new DataTable();
            dtTempTable.Columns.Add("EngagementID", typeof(int));
            dtTempTable.Columns.Add("EngagementFaxFormFieldID", typeof(int));

            return dtTempTable;
        }

        private DataTable CreateTempTableForStatusUpdate()
        {
            DataTable dtMappingTable = new DataTable();
            dtMappingTable.Columns.Add("EngagementFaxFormFieldID", typeof(string));
            dtMappingTable.Columns.Add("EngagementID", typeof(int));
            dtMappingTable.Columns.Add("StatusCode", typeof(int));
            dtMappingTable.Columns.Add("StatusMessage", typeof(string));
            dtMappingTable.Columns.Add("FormulaMapping", typeof(int));
            dtMappingTable.Columns.Add("FaxFormFieldID", typeof(string));

            return dtMappingTable;
        }

        private void UpdateExecutionStatusForFaxFormData(int intEngagementID, DataTable dtMappingTable)
        {
            try
            {
                if (dtMappingTable.Rows.Count > 0)
                {
                    
                        SpParameter[] spParams = new SpParameter[2];
                        spParams[0] = new SpParameter("EngagementID", intEngagementID);
                        spParams[1] = new SpParameter("tblTypeFaxFormField", dtMappingTable);
                        GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("[dbo].[Proc_UpdateStatusForFaxFormData]", spParams, true);
                    
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void AddTempRowToUpdateMappingStatus(int engagementId, ref DataTable tempTable, DataRow dataRow)
        {
            DataRow drTemp = tempTable.NewRow();
            drTemp["EngagementID"] = engagementId;
            
                drTemp["EngagementFaxFormFieldID"] = dataRow["EngagementFaxFormFieldID"];
                tempTable.Rows.Add(drTemp);
                intCounter++;
            
        }

        private void AddTempRowToUpdateExecutionStatus(int intEngagementID, ref DataTable tempTable, DataRow dataRow, string strMappingValue, int statusCode, string statusMsg)
        {
            DataRow drTemp = tempTable.NewRow();

            drTemp["EngagementFaxFormFieldID"] = dataRow["EngagementFaxFormFieldID"];
            drTemp["EngagementID"] = intEngagementID;
            drTemp["StatusCode"] = statusCode;
            drTemp["StatusMessage"] = statusMsg;
            drTemp["FormulaMapping"] = Convert.ToInt32(strMappingValue);
            drTemp["FaxFormFieldID"] = dataRow["FaxFormFieldID"];
            tempTable.Rows.Add(drTemp);
            intCounter++;
        }

        private void UpdateEvaluationCompleted(int intEngagementID, ref DataTable dtTempTable)
        {
            try
            {
                if (dtTempTable.Rows.Count > 0)
                {
                   
                        SpParameter[] spParams = new SpParameter[2];
                        spParams[0] = new SpParameter("EngagementID", intEngagementID);
                        spParams[1] = new SpParameter("tblTypeFaxFormField", dtTempTable);
                        GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("[dbo].[Proc_UpdateSPEngagementFaxFormFieldMapping_V1]", spParams, true);
                   
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        //protected void GetNumOfThreadAndCapValue(int numOfRecords, ref int numOfThreads, ref int capValue)
        //{
        //    DataTable dt = SetupThreadRecordMatrix();

        //    // In case, number of records exceeded than the Max value present in the Data Table, i.e. 10000000, below queries will return Nothing
        //    numOfThreads = dt.AsEnumerable().Where(rt => numOfRecords >= rt.Field<int>("MinValue") && numOfRecords <= rt.Field<int>("MaxValue"))
        //                                     .Select(rt => rt.Field<int>("NumberOfThreads")).LastOrDefault();
        //    capValue = dt.AsEnumerable().Where(rt => numOfRecords >= rt.Field<int>("MinValue") && numOfRecords <= rt.Field<int>("MaxValue"))
        //                                 .Select(rt => rt.Field<int>("Threshold")).LastOrDefault();

        //    // To handle the above mentioned case, we will have a fallback which first get the maxValue from the table and use the last values return for numOfThreads and CapValue 
        //    if (numOfThreads == 0)
        //    {
        //        int maxValue = dt.AsEnumerable().Max(row => row.Field<int>("MaxValue"));

        //        if (numOfRecords > maxValue)
        //        {
        //            numOfThreads = dt.AsEnumerable().OrderBy(row => row.Field<int>("NumberOfThreads")).Select(row => row.Field<int>("NumberOfThreads")).LastOrDefault();
        //            capValue = dt.AsEnumerable().OrderBy(row => row.Field<int>("Threshold")).Select(row => row.Field<int>("Threshold")).LastOrDefault();
        //        }
        //    }

        //    // Recaclculate CapX value, to get the correct modulation
        //    capValue = (int)Math.Ceiling((double)numOfRecords / numOfThreads);
        //    capValue = (int)(capValue / Math.Ceiling((double)numOfRecords / numOfThreads / capValue));
        //    capValue = (int)Math.Ceiling((double)capValue);
        //}
        
protected void GetNumOfThreadAndCapValue(int numOfRecords, ref int numOfThreads, ref int capValue)
        {
            DataTable dt = SetupThreadRecordMatrix();

            // In case, number of records exceeded the Max value present in the Data Table, i.e. 10000000, below queries will return Nothing
            numOfThreads = dt.AsEnumerable().Where(rt => numOfRecords >= rt.Field<int>("MinValue") && numOfRecords <= rt.Field<int>("MaxValue"))
                                             .Select(rt => rt.Field<int>("NumberOfThreads")).LastOrDefault();
            capValue = dt.AsEnumerable().Where(rt => numOfRecords >= rt.Field<int>("MinValue") && numOfRecords <= rt.Field<int>("MaxValue"))
                                         .Select(rt => rt.Field<int>("Threshold")).LastOrDefault();

            // To handle the above mentioned case, we will have a fallback which first gets the maxValue from the table and uses the last values returned for numOfThreads and capValue
            if (numOfThreads == 0)
            {
                int maxValue = dt.AsEnumerable().Max(row => row.Field<int>("MaxValue"));

                if (numOfRecords > maxValue)
                {
                    numOfThreads = dt.AsEnumerable().OrderBy(row => row.Field<int>("NumberOfThreads")).Select(row => row.Field<int>("NumberOfThreads")).LastOrDefault();
                    capValue = dt.AsEnumerable().OrderBy(row => row.Field<int>("Threshold")).Select(row => row.Field<int>("Threshold")).LastOrDefault();
                }
            }

            // Recalculate capValue to get the correct modulation
            capValue = (int)Math.Ceiling((double)numOfRecords / numOfThreads);
            capValue = (int)(capValue / Math.Ceiling((double)numOfRecords / numOfThreads / capValue));
            capValue = (int)Math.Ceiling((double)capValue);
        }
        private DataTable SetupThreadRecordMatrix()
        {
            List<int> listOfRange = new List<int>();
            int startRange = 0, endRange = 0;

            DataTable dt = new DataTable("RecordThreadMatrix");
            dt.Columns.Add("Range", typeof(string));
            dt.Columns.Add("MinValue", typeof(int));
            dt.Columns.Add("MaxValue", typeof(int));
            dt.Columns.Add("Threshold", typeof(int));
            dt.Columns.Add("NumberOfThreads", typeof(int));

            string startupPath = AppDomain.CurrentDomain.BaseDirectory;
            XDocument document = XDocument.Load(startupPath + "\\ThreadMatrix.xml");

            var recordsRanges = document.Descendants("Range").Select(r => r.Value);
            var minValue = document.Descendants("MinValue").Select(th => th.Value);
            var maxValue = document.Descendants("MaxValue").Select(th => th.Value);
            var threshold = document.Descendants("Threshold").Select(th => th.Value);
            var threadValue = document.Descendants("Thread").Select(t => t.Value);

            for (int intCount = 0; intCount < recordsRanges.Count(); intCount++)
            {
                AddRowsToMatrixTable(recordsRanges.ElementAt(intCount), int.Parse(minValue.ElementAt(intCount)), int.Parse(maxValue.ElementAt(intCount)), int.Parse(threshold.ElementAt(intCount)), int.Parse(threadValue.ElementAt(intCount)), dt);
            }

            return dt;
        }

        private void AddRowsToMatrixTable(string recordRange, int minValue, int maxValue, int threshold, int threadValue, DataTable dt)
        {
            DataRow drTemp = dt.NewRow();
            drTemp["Range"] = recordRange;
            drTemp["MinValue"] = minValue;
            drTemp["MaxValue"] = maxValue;
            drTemp["Threshold"] = threshold;
            drTemp["NumberOfThreads"] = threadValue;
            dt.Rows.Add(drTemp);
            intCounter++;
        }

        private bool NeedsEvaluation(DataRow drw)
        {
            // "U" 'Parent unassociated fields
            // "S" 'Superceded fields
            if (drw["FormStatus"].ToString() == "U" || drw["FaxFieldType"].ToString() == "S" || Convert.ToBoolean(drw["DoNotEvaluateMapping"]))
            {
                return false;
            }
            int intCount = 0;
            for (int i = 1; i <= 5; i++)
            {
                if (drw["Mapping_" + i.ToString()].ToString().Length > 0)
                {
                    intCount++;
                }
            }
            if (intCount > Convert.ToInt32(drw["ConvertCount"]))
            {
                return true;
            }

            return false;
        }

        public void EvalMapping(string strMapping, string strFormula, DataRow drwFaxFieldRow, DataTable dtFaxField, int MappingID, int tasknum = 0)
        {
            string strMappingValue;
            string strFormulaValue = string.Empty;

            if (drwFaxFieldRow[strMapping].ToString().Length > 0)
            {
                strMappingValue = ParseFormula1(Convert.ToInt32(drwFaxFieldRow["EngagementFaxFormID"]), drwFaxFieldRow[strMapping].ToString(), Convert.ToInt32(drwFaxFieldRow["FaxRowNumber"]), Convert.ToInt32(drwFaxFieldRow["EngagementPageID"]), drwFaxFieldRow["EvaluateAt"].ToString(), true, null, null, null, Convert.ToInt32(drwFaxFieldRow["DataType"].ToString()));

                if (drwFaxFieldRow[strFormula].ToString().Length > 0)
                {
                    if (strMappingValue.Length > 0)
                    {
                        strFormulaValue = ParseFormula1(Convert.ToInt32(drwFaxFieldRow["EngagementFaxFormID"]), drwFaxFieldRow[strFormula].ToString(), Convert.ToInt32(drwFaxFieldRow["FaxRowNumber"]), Convert.ToInt32(drwFaxFieldRow["EngagementPageID"]), drwFaxFieldRow["EvaluateAt"].ToString(), false, null, null, null,Convert.ToInt32( drwFaxFieldRow["DataType"].ToString()));
                    }
                }
                else
                {
                    if (strMappingValue.Length > 0)
                    {
                        if (drwFaxFieldRow["FaxDWPCode"].ToString().ToUpper() == "FAX.CON.166" || drwFaxFieldRow["FaxDWPCode"].ToString().ToUpper() == "FAX.CON.174")
                        {
                            try
                            {
                                strFormulaValue = (Convert.ToInt32(drwFaxFieldRow["FFValue"]) * (-1)).ToString();
                            }
                            catch (Exception ex)
                            {
                                // ignore
                            }
                        }
                        else
                        {
                            strFormulaValue = drwFaxFieldRow["FFValue"].ToString();
                        }
                    }
                }

                if (strMappingValue.Length > 0)
                {
                    AddFaxTaxFieldRow(ref dtFaxField, Convert.ToInt32(drwFaxFieldRow["EngagementID"]), Convert.ToInt32(drwFaxFieldRow["EngagementFaxFormFieldID"]), strMappingValue, strFormulaValue, MappingID.ToString().Substring(MappingID.ToString().Length - 1, 1), Convert.ToInt32(drwFaxFieldRow["DuplicateOcrFieldID"]), Convert.ToInt32(drwFaxFieldRow["IsAutoDuplicate"]), Convert.ToInt32(drwFaxFieldRow["IsAutoUnchecked"]), drwFaxFieldRow["OcrDuplicateFieldIds"].ToString(), drwFaxFieldRow["DuplicatePageIds"].ToString(), tasknum);
                    blnDoUpdate = true;
                    if (!dicDoUpdate.ContainsKey(tasknum))
                    {
                        dicDoUpdate.Add(tasknum, true);
                    }
                }
            }
        }
        private void AddFaxTaxFieldRow(ref DataTable dtFaxTaxFormField, int intEngagementID, int intEngagementFaxFormFieldID, string strFieldDWPCode, string strTFValue, string strMappings, int IntDuplicateOcrFieldID, int IntIsAutoDuplicate, int IntIsAutoUnchecked, string StrOcrDuplicateFieldIds, string StrDuplicatePageIds, int taskNum)
        {
            try
            {
                DataRow drwTemp = dtFaxTaxFormField.NewRow();
                drwTemp["EngagementFaxTaxFormFieldID"] = intCounter;
                drwTemp["EngagementFaxFormFieldID"] = intEngagementFaxFormFieldID;
                drwTemp["EngagementID"] = intEngagementID;
                drwTemp["FieldDWPCode"] = strFieldDWPCode;
                drwTemp["TFValue"] = strTFValue;
                drwTemp["Mappings"] = strMappings;
                drwTemp["Operation"] =  1;
                drwTemp["DuplicateOcrFieldID"] = IntDuplicateOcrFieldID;
                drwTemp["IsAutoDuplicate"] = IntIsAutoDuplicate;
                drwTemp["IsAutoUnchecked"] = IntIsAutoUnchecked;
                drwTemp["OcrDuplicateFieldIds"] = StrOcrDuplicateFieldIds;
                drwTemp["DuplicatePageIds"] = StrDuplicatePageIds;
                drwTemp["Threadid"] = taskNum;
                dtFaxTaxFormField.Rows.Add(drwTemp);
                intCounter++;
            }
            catch (Exception ex)
            {
                // LogEntry(Severity.Low, "DDPAgent", "AddFaxTaxFieldRow - Error Occured : " + ex.Message + " -> " + DateTime.Now + " intEngagementID =" + intEngagementID + " intEngagementFaxFormFieldID =" + intEngagementFaxFormFieldID + " IntDuplicateOcrFieldID =" + IntDuplicateOcrFieldID + " IntIsAutoDuplicate = " + IntIsAutoDuplicate + " IntIsAutoUnchecked =" + IntIsAutoUnchecked + " StrOcrDuplicateFieldIds =" + StrOcrDuplicateFieldIds + " StrDuplicatePageIds =" + StrDuplicatePageIds, 917, 1, intEngagementID, TraceType.InfoLog);
                // Throw
                // commented throw to prevent going the job in error
            }
        }

        private void UpdateFaxFormData(ref DataTable dtFaxFormField, int taskNum)
        {
            if (dtFaxFormField.Rows.Count > 0)
            {
                // Filter data before passing
                DataTable tempDt;
                DataView dv = new DataView(dtFaxFormField);
                dv.RowFilter = "Operation = 1 and Threadid = " + taskNum.ToString();
                tempDt = dv.ToTable();
                tempDt.Columns.Remove("Threadid");

                // Bulk insert function to insert data bulk using DT and UDDT
                if (dtFaxFormField.Rows.Count > 0)
                {
                    BulkInsert_SPEngagementFaxTaxFormField(tempDt, Convert.ToInt32(dtFaxFormField.Rows[0]["EngagementID"]));

                    // Update the Operation value to 2, so that in the next go, it shouldn't update the same records...
                   
                        var rowsToUpdate = dtFaxFormField.AsEnumerable().Where(row => row.Field<int>("Operation") == 1);
                        rowsToUpdate.ToList().ForEach(row => row["Operation"] = 2);
                    
                }
            }
        }

        private int BulkInsert_SPEngagementFaxTaxFormField(DataTable dtFaxFormField, int intEngagementID)
        {
            try
            {
                
                
                    SpParameter[] spParams = new SpParameter[1];
                    spParams[0] = new SpParameter("tblTypeFaxTaxFormField", dtFaxFormField);
                    return GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("[dbo].[Proc_InsertBulkFaxTaxFormField]", spParams, true);
                
            }
            catch (Exception ex)
            {
                // LogEntry(Severity.Low, "DDPAgent", "Insert_SPEngagementFaxTaxFormField - Error Occured : " + ex.Message + " -> " + DateTime.Now + " intEngagementID = " + intEngagementID + "- StackTrace :" + ex.StackTrace, 918, 1, intEngagementID, TraceType.ErrorLog);
                throw;
            }
        }

        // This is for Unit Testing ONLY, DONT DELETE MODIFY OR USE
        private int BulkInsert_SPEngagementFaxTaxFormField_UnitTest(int intEngagementID)
        {
            try
            {
                DataTable dtFaxFormField;
               
                    SpParameter[] spParams = new SpParameter[1];
                    spParams[0] = new SpParameter("EngagementID", intEngagementID);
                    dtFaxFormField = GetEngagementDbCommon(intEngagementID).GetDataTable("dbo.Proc_BulkInsert_SPEngagementFaxTaxFormField_UnitTest", spParams, "FaxTaxFormField", true);
                

                return BulkInsert_SPEngagementFaxTaxFormField(dtFaxFormField, intEngagementID);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private bool UpdateEvaluationCompleted(int intEngagementID)
        {
            try
            {
               
                    SpParameter[] spParams = new SpParameter[1];
                    spParams[0] = new SpParameter("EngagementID", intEngagementID);
                    GetEngagementDbCommon(intEngagementID).GetData("Proc_UpdateSPEngagementFaxFormFieldMapping", spParams, true);
               

                WriteDBLog("Function UpdateEvaluationCompleted called...", 707, 1, Severity.Low, TraceType.InfoLog, intEngagementID);
                Console.WriteLine("Function UpdateEvaluationCompleted called..." + " " + DateTime.Now.ToString() + " intEngagementID = " + intEngagementID);
                // LogEntry(Severity.Low, "DDPAgent", "Function UpdateEvaluationCompleted called... ", 707, 1, intEngagementID, TraceType.InfoLog);
            }
            catch (Exception ex)
            {
                WriteDBLog($"UpdateEvaluationCompleted>>Error occurred:{ex.Message}", 707, 1, Severity.Low, TraceType.ErrorLog, intEngagementID);
                Console.WriteLine("UpdateEvaluationCompleted>>Error occurred:" + ex.Message + " " + DateTime.Now.ToString() + " intEngagementID = " + intEngagementID);
                // LogEntry(Severity.Low, "DDPAgent", $"UpdateEvaluationCompleted>>Error occurred:{ex.Message}, called ->" + DateTime.Now, 707, 1, intEngagementID, TraceType.ErrorLog);
                return false;
            }
            return true;
        }
        public void AssignDataset(DataSet dsEngFaxFormField, DataSet dsEngFormField, OcrFieldData OcrItem, IList<Faxformdata> Faxdata)
        {
            dsEngFaxFormFields = dsEngFaxFormField;
            dsEngFormFields = dsEngFormField;
            lstOcrItem = OcrItem;
            lstFaxdata = Faxdata;
        }

        #region Common Functions

        public int Levenshtein_distance(string s, string t)
        {
            int i; // iterates through s
            int j; // iterates through t
            string s_i; // ith character of s
            string t_j; // jth character of t
            int cost; // cost
                      // Step 1
            int n = s.Length; // length of s
            int m = t.Length; // length of t
            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {
                return n;
            }
            int[,] d = new int[n + 1, m + 1];
            // Step 2
            for (i = 0; i <= n; i++)
            {
                d[i, 0] = i;
            }
            for (j = 0; j <= m; j++)
            {
                d[0, j] = j;
            }
            // Step 3
            for (i = 1; i <= n; i++)
            {
                s_i = s.Substring(i - 1, 1);
                // Step 4
                for (j = 1; j <= m; j++)
                {
                    t_j = t.Substring(j - 1, 1);
                    // Step 5
                    cost = s_i == t_j ? 0 : 1;
                    // Step 6
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        #endregion

        #region Events

        //protected override void Finalize()
        //{
        //    base.Finalize();
        //}
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern
        protected virtual void Dispose(bool disposing)
        {

            if (!disposed)
            {
                if (disposing)
                {
                    // Free managed resources here
                }

                // Free unmanaged resources here

                disposed = true;
            }
        }


        #endregion

        #region Property


        public DataSet EngFormFieldDataset
        {
            get { return dsEngFormFields; }
            set { dsEngFormFields = value; }
        }



        #endregion

        #region For Tax Exempt

        public DataTable EvaluateFaxToTaxFormula_TaxExempt(int intEngagementID)
        {
            PopulateDSEngFormFields_TaxExempt(intEngagementID);
            PopulateDSEngFaxFormFields_TaxExempt(intEngagementID);
            PopulateDSEngFaxTaxFormFields_TaxExempt(intEngagementID);
            return ParseMappingAndFormula_TaxExempt();
        }

        private bool PopulateDSEngFormFields_TaxExempt(int intEngagementID)
        {
            
                SpParameter[] spParams = new SpParameter[1];
                spParams[0] = new SpParameter("EngagementID", intEngagementID);
                dsEngFormFields = GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_PopulateDSEngFormFields_TaxExempt", spParams, true);
            

            return true;
        }
        private void PopulateDSEngFaxFormFields_TaxExempt(int intEngagementID)
        {
           
                SpParameter[] sqlParam = new SpParameter[1];
                sqlParam[0] = new SpParameter("@EngagementID", intEngagementID);
                dsEngFaxFormFields = GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetEngFaxFormFieldWithMappingTaxExempt", sqlParam, true);
            

            dsEngFaxFormFields.Tables[0].TableName = "FaxFormData";
        }

        private void PopulateDSEngFaxTaxFormFields_TaxExempt(int intEngagementID)
        {
            
                SpParameter[] sqlParam = new SpParameter[1];
                sqlParam[0] = new SpParameter("@EngagementID", intEngagementID);
                dsEngFaxTaxFormFields = GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_PopulateDSEngFaxTaxFormFields_TaxExempt", sqlParam, true);
            
        }

        public DataTable ParseMappingAndFormula_TaxExempt()
        {
            DataTable dt = dsEngFaxFormFields.Tables[0];
            DataTable dtFaxTax = dsEngFaxTaxFormFields.Tables[0];
            for (int intCount = 0; intCount < dt.Rows.Count; intCount++)
            {
                if (NeedsEvaluation(dt.Rows[intCount]))
                {
                    for (int i = 10; i >= 1; i--)
                    {
                        EvalMapping("Mapping_" + i.ToString(), "Formula_" + i.ToString(), dt.Rows[intCount], dtFaxTax,Convert.ToInt32( i.ToString()));
                    }
                }
            }
            return dtFaxTax;
        }
        #endregion
        private int EvaluateProformaParent(int intEngFaxFormID)
        {
            DataRow[] drrows;
            int intParentFaxFormID = 0;
            int EngagementFormID = 0;

            drrows = dsEngFaxFormFields.Tables[0].Select("EngagementFaxFormID = " + intEngFaxFormID);
            if (drrows.Length > 0)
            {
                intParentFaxFormID = IsDBNull(drrows[0]["ParentEngagementFaxFormID"]) ? -1 : Convert.ToInt32(drrows[0]["ParentEngagementFaxFormID"]);
            }
            if (intParentFaxFormID > 0)
            {
                drrows = dsEngFaxFormFields.Tables[0].Select("EngagementFaxFormID = " + intParentFaxFormID);

                if (drrows.Length > 0)
                {
                    EngagementFormID = IsDBNull(drrows[0]["EngagementFormID"]) ? 0 : Convert.ToInt32(drrows[0]["EngagementFormID"]);
                }
            }
            return EngagementFormID;
        }

        private  bool IsDBNull(object Expression)
        {
            if (Expression is DBNull)
            {
                return true;
            }

            if (Expression == null || string.IsNullOrEmpty(Expression.ToString()) || Expression.Equals(""))
            {
                return true;
            }

            return false;
        }


        public EvaluateFormula()
        {
            ConfigConnectionKey = "ConnectionString";
            tokenSource2 = new CancellationTokenSource();
            cancellationToken = tokenSource2.Token;

        }


    }
}