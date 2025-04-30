using FaxToTaxLibrary.Classes;
using FaxToTaxLibrary;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static FaxToTaxLibrary.CommonCode;
using Microsoft.Extensions.Configuration;
using FaxToTaxDataValidations;

namespace FaxToTaxLibrary
{

    [StandardModule]
    public static class CommonCode
    {
        public static string ConfigConnectionKey = "ConnectionStrings";
        public enum FileType
        {
            DropDown,
            LSDMT,
            TemplateOrganizer,
            TemplateSourDoc,
            FDT
        }

        public enum TraceType
        {
            InfoLog = 1,
            ErrorLog
        }

        public enum Severity
        {
            Low = 1,
            Medium,
            High
        }



        

        /// <summary>
        /// This function will return Primary dbCommon object
        /// </summary>
        /// <returns></returns>
        public static DbCommon GetEngagementDbCommon()
        {
            return new DbCommon(ConfigConnectionKey);
        }

        /// <summary>
        /// This function will return Secondary dbCommon object
        /// </summary>
        /// <returns></returns>
        public static DbCommon GetEngagementDbCommon(int engagementId)
        {
            return new DbCommon(engagementId, ConfigConnectionKey);
        }

        public static int GetEngagementDBConnectionID(int intEngagementID)
        {
            try
            {
                DataTable dt2;
                
                    SpParameter[] parm = new SpParameter[1]
                    {
                    new SpParameter("@EngagementId", intEngagementID.ToString())
                    };
                    dt2 = CommonCode.GetEngagementDbCommon().GetDataTable("dbo.Proc_GetEngagementDBConnectionID", parm, "", true);
                
                if (dt2.Rows.Count > 0)
                {
                    return Conversions.ToInteger(dt2.Rows[0]["DBConnectionID"]);
                }
                return 0;
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                Exception ex = ex2;
                //SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "DDPAgent", "SPDatabase -> GetEngagementConnecionString - Error Occured :" + ex.Message + "::" + ex.StackTrace, 901, 1, intEngagementID, SurePrepLogger.TraceType.ErrorLog);
                throw ex;
            }
            finally
            {
                DataTable dt2 = null;
            }
        }

        public static bool WriteDBLog(string strMessage, int EventId, int Priority, Severity Severity = Severity.Low, TraceType TraceType = TraceType.InfoLog, int engagementId = 0, int jobId = 0, int stepId = 0, int subStepId = 0, bool ISSPEL = false)
        {
            try
            {
                int isLogSeverity  = Convert.ToInt32( GenModule.configuration.GetSection("LogSeverity").Value); 
                if (stepId > 0)
                {
                    return CommonCode.WriteDdpDBLog(strMessage, engagementId, jobId, stepId, subStepId, ISSPEL);
                }
                bool WriteDBLog5 = default(bool);
                if (isLogSeverity == 1 & Severity == Severity.Low & !Severity.Equals(Severity.Medium) & !Severity.Equals(Severity.High))
                {
                    CommonCode.DBWriteLogs(strMessage, engagementId, EventId, Priority, Severity, TraceType);
                    return WriteDBLog5;
                }
                if (isLogSeverity == 2 & (Severity == Severity.Low | Severity == Severity.Medium) & !Severity.Equals(Severity.High))
                {
                    CommonCode.DBWriteLogs(strMessage, engagementId, EventId, Priority, Severity, TraceType);
                    return WriteDBLog5;
                }
                if (isLogSeverity == 3)
                {
                    CommonCode.DBWriteLogs(strMessage, engagementId, EventId, Priority, Severity, TraceType);
                    return WriteDBLog5;
                }
                return WriteDBLog5;
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                Exception ex = ex2;
                //SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "DDPAgent", "CommonCode -> WriteDBLog - Error Occured : " + ex.Message + " -> " + Conversions.ToString(DateAndTime.Now) + " strMessage = " + strMessage + " ISSPEL = " + Conversions.ToString(ISSPEL), 902, 1, engagementId, SurePrepLogger.TraceType.ErrorLog);
                bool WriteDBLog5 = false;
                ProjectData.ClearProjectError();
                return WriteDBLog5;
            }
        }

        private static object DBWriteLogs(string strMessage, int engagementId, int EventId, int Priority, Severity Severity, TraceType TraceType)
        {
            
            SpParameter[] spParams = new SpParameter[6]
            {
            new SpParameter("EngagementID", engagementId),
            new SpParameter("strMessage", strMessage),
            new SpParameter("EventId", EventId),
            new SpParameter("Priority", Priority),
            new SpParameter("Severity", Severity),
            new SpParameter("TraceType", TraceType)
            };
            return CommonCode.GetEngagementDbCommon(engagementId).GetData("Proc_InsertSPEngagementCurrentStatus", spParams, true);
        }

        public static bool WriteDdpDBLog(string strMessage, int engagementId, int jobId = 0, int stepId = 0, int subStepId = 0, bool ISSPEL = false)
        {
            try
            {
               
                SpParameter[] spParams = new SpParameter[5]
                {
                new SpParameter("EngagementID", engagementId),
                new SpParameter("Comments", strMessage),
                new SpParameter("JobId", jobId),
                new SpParameter("StepId", stepId),
                new SpParameter("SubStepId", subStepId)
                };
                return Conversions.ToBoolean(CommonCode.GetEngagementDbCommon(engagementId).GetData("dbo.Proc_InsertSPEngagementJobSteps", spParams, true));
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                Exception ex = ex2;
                //SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "DDPAgent", "CommonCode -> WriteDdpDBLog - Error Occured : " + ex.Message + " -> " + Conversions.ToString(DateAndTime.Now) + " strMessage = " + strMessage + " ISSPEL = " + Conversions.ToString(ISSPEL), 903, 1, engagementId, SurePrepLogger.TraceType.ErrorLog);
                bool WriteDdpDBLog2 = false;
                ProjectData.ClearProjectError();
                return WriteDdpDBLog2;
            }
        }

        public static DataTable GetStepDetail(int intEngID, int jobId)
        {
            try
            {
               
                SpParameter[] parm = new SpParameter[2]
                {
                new SpParameter("@EngagementId", intEngID.ToString()),
                new SpParameter("@JobId", jobId)
                };
                return CommonCode.GetEngagementDbCommon(intEngID).GetDataTable("Dbo.Proc_GetSPEngagementJobSteps", parm, "CurrentStatusTable", true);
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                Exception ex = ex2;
                //SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "DDPAgent", "SPDatabase -> GetEngagementConnecionString - Error Occured : " + ex.Message + " -> " + Conversions.ToString(DateAndTime.Now), 904, 1, intEngID, SurePrepLogger.TraceType.ErrorLog);
                throw ex;
            }
        }

        public static bool CheckStepStatus(int stepId, int subStepId, int engId, ref DataTable dtStep, int jobId)
        {
            try
            {
                if (dtStep == null || dtStep.Select("EngagementId=" + Conversions.ToString(engId)).Length <= 0)
                {
                    dtStep = CommonCode.GetStepDetail(engId, jobId);
                }
                return (byte)((dtStep.Select("EngagementId=" + Conversions.ToString(engId) + "and StepId=" + Conversions.ToString(stepId) + " and SubStepId=" + Conversions.ToString(subStepId)).Length > 0) ? 1 : 0) != 0;
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                Exception ex = ex2;
                bool CheckStepStatus = false;
                ProjectData.ClearProjectError();
                return CheckStepStatus;
            }
        }

        public static string ReplaceSPLCharacters(string strString)
        {
            try
            {
                if (Operators.CompareString(strString, "", false) != 0)
                {
                    strString = Strings.Replace(strString, "<", "", 1, -1, CompareMethod.Binary);
                    strString = Strings.Replace(strString, ">", "", 1, -1, CompareMethod.Binary);
                    strString = Strings.Replace(strString, "=", "", 1, -1, CompareMethod.Binary);
                    strString = Strings.Replace(strString, ";", "", 1, -1, CompareMethod.Binary);
                    strString = Strings.Replace(strString, "--", "", 1, -1, CompareMethod.Binary);
                }
                return strString;
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                Exception ex = ex2;
                int engagementid = default(int);
               // SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "DDPAgent", "CommonCode -> ReplaceSPLCharacters - Error Occured : " + ex.Message + " -> " + Conversions.ToString(DateAndTime.Now) + "strString = " + strString, 905, 1, engagementid, SurePrepLogger.TraceType.ErrorLog);
                throw ex;
            }
        }

        public static void AppendString(string strData, StringBuilder strBuilder, string delimiter = ",")
        {
            if (string.IsNullOrWhiteSpace(strBuilder.ToString()))
            {
                strBuilder.Append(strData);
            }
            else
            {
                strBuilder.Append(delimiter + strData);
            }
        }

        public static object ReplaceNull(object objData, string replaceBy = "")
        {
            if ((object)objData.GetType() == typeof(string))
            {
                return Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(objData)), replaceBy, RuntimeHelpers.GetObjectValue(objData));
            }
            return Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(objData)), RuntimeHelpers.GetObjectValue(Interaction.IIf(Operators.CompareString(replaceBy, "", false) == 0, 0, replaceBy)), RuntimeHelpers.GetObjectValue(objData));
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

        public static string GetFileStr(int intFileType)
        {
            switch (intFileType)
            {
                case 0:
                    return "DropDown";
                case 1:
                    return "LSDMT";
                case 2:
                    return "Template";
                case 3:
                    return "Template";
                default:
                    return "";
            }
        }

        public static string GetfileName(int SoftwareId, int EngTypeId, int intTaxYear, int intFileType, ArrayList ArrParam)
        {
            string filename = "";
            switch (intFileType)
            {
                case 0:
                    filename = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject("SPData_", ArrParam[5]), "_"), ArrParam[4]), "_"), intTaxYear.ToString()), "_"), ArrParam[3]), ".bin"));
                    break;
                case 1:
                    filename = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject("SPData_", ArrParam[5]), "_"), ArrParam[4]), "_"), intTaxYear.ToString()), ".bin"));
                    break;
                default:
                    if (SoftwareId == 0)
                    {
                        if (intFileType == 3)
                        {
                            filename = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject("SPData_", ArrParam[5]), "_"), ArrParam[4]), "_"), intTaxYear.ToString()), "_"), ArrParam[3]));
                        }
                    }
                    else if (intFileType == 2)
                    {
                        filename = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject("SPData_", ArrParam[5]), "_"), ArrParam[4]), "_"), intTaxYear.ToString()), "_"), ArrParam[3]));
                    }
                    break;
            }
            return filename;
        }

        public static string GetDataToBeStoredAs(string TempArea)
        {
            int tildPosition = checked(Strings.InStr(1, TempArea, "~", CompareMethod.Binary) + 1);
            return Strings.Mid(TempArea, tildPosition);
        }

        public static string GetFolderPath(string TempArea)
        {
            int tildPosition = checked(Strings.InStr(1, TempArea, "~", CompareMethod.Binary) - 1);
            return Strings.Mid(TempArea, 1, tildPosition);
        }

        public static DataTable GetCustomizeDataTable(string[] strValues, string[] columnNames)
        {
            DataTable dt = new DataTable();
            foreach (string col in columnNames)
            {
                dt.Columns.Add(col);
            }
            bool IsRowsAdded = false;
            checked
            {
                int num = columnNames.Count() - 1;
                for (int intCol = 0; intCol <= num; intCol++)
                {
                    if (strValues[intCol] != null)
                    {
                        string[] values = strValues[intCol].Replace("'", "").Split(',');
                        int intRow = Information.UBound(values, 1);
                        int intCounter = 0;
                        string[] array = values;
                        foreach (string value in array)
                        {
                            DataRow dr = (intCounter > intRow || IsRowsAdded) ? dt.Rows[intCounter] : dt.NewRow();
                            dr[columnNames[intCol]] = value;
                            if (!IsRowsAdded)
                            {
                                dt.Rows.Add(dr);
                            }
                            intCounter++;
                        }
                        IsRowsAdded = true;
                    }
                }
                return dt;
            }
        }

        public static DataTable GetCustomizeDataTable_List(List<string>[] strValues, string[] columnNames)
        {
            DataTable dt = new DataTable();
            foreach (string col in columnNames)
            {
                dt.Columns.Add(col);
            }
            bool IsRowsAdded = false;
            checked
            {
                int num = columnNames.Count() - 1;
                for (int intCol = 0; intCol <= num; intCol++)
                {
                    if (strValues[intCol] != null)
                    {
                        List<string> values2 = strValues[intCol];
                        string[] values = values2.ToArray();
                        int intRow = Information.UBound(values, 1);
                        int intCounter = 0;
                        string[] array = values;
                        foreach (string value in array)
                        {
                            DataRow dr = (intCounter > intRow || IsRowsAdded) ? dt.Rows[intCounter] : dt.NewRow();
                            dr[columnNames[intCol]] = value;
                            if (!IsRowsAdded)
                            {
                                dt.Rows.Add(dr);
                            }
                            intCounter++;
                        }
                        IsRowsAdded = true;
                    }
                }
                return dt;
            }
        }

        private static DataTable GetFaxFormUDT()
        {
            DataTable dt = new DataTable();
            DataColumn dc34 = new DataColumn("EngagementFaxFormID", typeof(int));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("EngagementID", typeof(int));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("EngagementPageID", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FFValue", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("FFX", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FFY", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FFHeight", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FFWidth", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FFLeft", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FFTop", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FaxDWPCode", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("InputForm", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("FaxRowNumber", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FaxFormID", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FaxFormFieldID", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FaxFormInstance", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("FaxFieldInstance", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("Identifier", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("SheetNo", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("FaxFormType", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("Verified", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("AutoVerified", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("EngagementOCRFieldID", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("CreatedBy", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("CreatedOn", typeof(DateTime));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("DuplicateOcrFieldID", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("IsAutoDuplicate", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("IsAutoUnchecked", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("OcrDuplicateFieldIDs", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("DuplicatePageIDs", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("MuniLogicApplied", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("EngagementFormFieldID", typeof(int));
            dt.Columns.Add(dc34);
            dc34.AllowDBNull = true;
            dc34 = new DataColumn("Currency", typeof(string));
            dt.Columns.Add(dc34);
            dc34 = new DataColumn("SubGroupNo", typeof(int));
            dt.Columns.Add(dc34);
            return dt;
        }

        public static DataTable GetCustomizeDataTable(string[] strValues, string[] columnNames, ref DataTable CustomizeDataTable)
        {
            if (CustomizeDataTable == null)
            {
                CustomizeDataTable = CommonCode.GetFaxFormUDT();
            }
            DataRow dr = CustomizeDataTable.NewRow();
            int indexFFvalue = CustomizeDataTable.Columns.IndexOf("FFValue");
            checked
            {
                int num = columnNames.Count() - 1;
                for (int intCol = 0; intCol <= num; intCol++)
                {
                    dr[intCol] = RuntimeHelpers.GetObjectValue(string.IsNullOrWhiteSpace(strValues[intCol]) ? Interaction.IIf(indexFFvalue == intCol, string.Empty, DBNull.Value) : strValues[intCol]);
                }
                CustomizeDataTable.Rows.Add(dr);
                return CustomizeDataTable;
            }
        }

        public static List<OcrFieldData> GetPagePreCorrectionData(DataTable dtPreCorrectionData, int EngPageId, int faxRowNumber)
        {
            if (EngPageId > 0 & faxRowNumber > 0)
            {
                return (from Ocr in dtPreCorrectionData.AsEnumerable()
                        where Conversions.ToDouble(Ocr["EngagementPageID"].ToString()) == (double)EngPageId && Conversions.ToDouble(Ocr["FaxRowNumber"].ToString()) == (double)faxRowNumber
                        select Ocr).Select((delegate (DataRow Ocr)
                        {
                            OcrFieldData ocrFieldData2 = new OcrFieldData();
                            ocrFieldData2.EngagementID = Conversions.ToInteger(Ocr["EngagementID"]);
                            ocrFieldData2.EngagementPageID = Conversions.ToInteger(CommonCode.validateinteger(RuntimeHelpers.GetObjectValue(Ocr["EngagementPageID"])));
                            ocrFieldData2.FileName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["FileName"])), "", RuntimeHelpers.GetObjectValue(Ocr["FileName"])));
                            ocrFieldData2.TaxSoftwareID = Conversions.ToInteger(Ocr["TaxSoftwareID"]);
                            ocrFieldData2.EngagementTypeID = Conversions.ToInteger(Ocr["EngagementTypeID"]);
                            ocrFieldData2.TaxYear = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["TaxYear"])), 0, RuntimeHelpers.GetObjectValue(Ocr["TaxYear"])));
                            ocrFieldData2.InSPVerification = Conversions.ToString(Ocr["InSPVerification"]);
                            ocrFieldData2.EngagementOCRFieldID = Conversions.ToInteger(Ocr["EngagementOCRFieldID"]);
                            ocrFieldData2.OCRTemplateName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateName"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateName"])));
                            ocrFieldData2.OCRTableName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTableName"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRTableName"])));
                            ocrFieldData2.OCRFieldName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRFieldName"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRFieldName"])));
                            ocrFieldData2.OCRRowNo = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRowNo"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRowNo"])));
                            ocrFieldData2.OCRPageNo = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPageNo"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRPageNo"])));
                            ocrFieldData2.OCRLeft = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRLeft"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRLeft"])));
                            ocrFieldData2.OCRTop = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTop"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRTop"])));
                            ocrFieldData2.OCRRight = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRight"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRight"])));
                            ocrFieldData2.OCRBottom = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRBottom"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRBottom"])));
                            ocrFieldData2.SPVOCRLeft = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SPVOCRLeft"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SPVOCRLeft"])));
                            ocrFieldData2.SPVOCRTop = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SPVOCRTop"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SPVOCRTop"])));
                            ocrFieldData2.SPVOCRRight = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SPVOCRRight"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SPVOCRRight"])));
                            ocrFieldData2.SPVOCRBottom = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SPVOCRBottom"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SPVOCRBottom"])));
                            ocrFieldData2.OCRValue = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRValue"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRValue"])));
                            ocrFieldData2.OCRTemplateID = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateID"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateID"])));
                            ocrFieldData2.OCRTemplateFieldID = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateFieldID"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateFieldID"])));
                            ocrFieldData2.SheetNo = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SheetNo"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SheetNo"])));
                            ocrFieldData2.FaxDWPCode = Conversions.ToString(Ocr["FaxDWPCode"]);
                            ocrFieldData2.FaxRowNumber = Conversions.ToInteger(Ocr["FaxRowNumber"]);
                            ocrFieldData2.FaxFormID = Conversions.ToInteger(Ocr["FaxFormID"]);
                            ocrFieldData2.FaxFormFieldID = Conversions.ToInteger(Ocr["FaxFormFieldID"]);
                            ocrFieldData2.ForCorrection = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["ForCorrection"])), "", RuntimeHelpers.GetObjectValue(Ocr["ForCorrection"])));
                            ocrFieldData2.OCRRuleType1 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType1"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType1"])));
                            ocrFieldData2.OCRRule1 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule1"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule1"])));
                            ocrFieldData2.OCRRuleType2 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType2"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType2"])));
                            ocrFieldData2.OCRRule2 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule2"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule2"])));
                            ocrFieldData2.OCRRuleType3 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType3"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType3"])));
                            ocrFieldData2.OCRRule3 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule3"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule3"])));
                            ocrFieldData2.OCRRuleType4 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType4"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType4"])));
                            ocrFieldData2.OCRRule4 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule4"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule4"])));
                            ocrFieldData2.OCRRuleType5 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType5"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType5"])));
                            ocrFieldData2.OCRRule5 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule5"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule5"])));
                            ocrFieldData2.FaxFormInstance = Conversions.ToString(Ocr["FaxFormInstance"]);
                            ocrFieldData2.FaxFieldInstance = Conversions.ToString(Ocr["FaxFieldInstance"]);
                            ocrFieldData2.FaxFormType = Conversions.ToInteger(Ocr["FaxFormType"]);
                            ocrFieldData2.DisplayOrder = Conversions.ToInteger(Ocr["DisplayOrder"]);
                            ocrFieldData2.Identifier = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["Identifier"])), "", RuntimeHelpers.GetObjectValue(Ocr["Identifier"])));
                            ocrFieldData2.IsConverted = Conversions.ToString(Ocr["IsConverted"]);
                            ocrFieldData2.FaxFormDWPCode = Conversions.ToString(Ocr["FaxFormDWPCode"]);
                            ocrFieldData2.FaxFieldName = Conversions.ToString(Ocr["FaxFieldName"]);
                            ocrFieldData2.FaxFormName = Conversions.ToString(Ocr["FaxFormName"]);
                            ocrFieldData2.FaxFormIdentifier = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["FaxFormIdentifier"])), "", RuntimeHelpers.GetObjectValue(Ocr["FaxFormIdentifier"])));
                            ocrFieldData2.OCRIdentifier = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRIdentifier"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRIdentifier"])));
                            ocrFieldData2.OCRDWPCode = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRDWPCode"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRDWPCode"])));
                            ocrFieldData2.Verified = Conversions.ToString(Ocr["Verified"]);
                            ocrFieldData2.DataType = Conversions.ToInteger(Ocr["DataType"]);
                            ocrFieldData2.OCRVerified = Conversions.ToString(Ocr["OCRVerified"]);
                            ocrFieldData2.OCRAutoVerified = Conversions.ToString(Ocr["OCRAutoVerified"]);
                            ocrFieldData2.EngagementFormFieldID = Conversions.ToInteger(Ocr["EngagementFormFieldID"]);
                            ocrFieldData2.VirtualRotation = Conversions.ToInteger(Ocr["VirtualRotation"]);
                            ocrFieldData2.ApplyDecimalRule = Conversions.ToString(Ocr["ApplyDecimalRule"]);
                            ocrFieldData2.OCRPreRuleType1 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType1"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType1"])));
                            ocrFieldData2.OCRPreRule1 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule1"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule1"])));
                            ocrFieldData2.PreRuleAutoVerify1 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify1"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify1"])));
                            ocrFieldData2.OCRPreRuleType2 = Conversions.ToInteger(Interaction.IIf(string.IsNullOrEmpty(Conversions.ToString(Ocr["OCRPreRuleType2"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType2"])));
                            ocrFieldData2.OCRPreRule2 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule2"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule2"])));
                            ocrFieldData2.PreRuleAutoVerify2 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify2"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify2"])));
                            ocrFieldData2.OCRPreRuleType3 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify2"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify2"])));
                            ocrFieldData2.OCRPreRule3 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule3"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule3"])));
                            ocrFieldData2.PreRuleAutoVerify3 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify3"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify3"])));
                            ocrFieldData2.OCRPreRuleType4 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType4"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType4"])));
                            ocrFieldData2.OCRPreRule4 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule4"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule4"])));
                            ocrFieldData2.PreRuleAutoVerify4 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify4"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify4"])));
                            ocrFieldData2.OCRPreRuleType5 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType5"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType5"])));
                            ocrFieldData2.OCRPreRule5 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule5"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule5"])));
                            ocrFieldData2.PreRuleAutoVerify5 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify5"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify5"])));
                            ocrFieldData2.OCRRuleType6 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType6"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType6"])));
                            ocrFieldData2.OCRRule6 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule6"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule6"])));
                            ocrFieldData2.OCRRuleType7 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType7"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType7"])));
                            ocrFieldData2.OCRRule7 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule7"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule7"])));
                            ocrFieldData2.OCRRuleType8 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType8"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType8"])));
                            ocrFieldData2.OCRRule8 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule8"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule8"])));
                            ocrFieldData2.OCRRuleType9 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType9"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType9"])));
                            ocrFieldData2.OCRRule9 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule9"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule9"])));
                            ocrFieldData2.OCRRuleType10 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType10"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType10"])));
                            ocrFieldData2.OCRRule10 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule10"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule10"])));
                            ocrFieldData2.OCRRuleTip1 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip1"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip1"])));
                            ocrFieldData2.OCRRuleTip2 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip2"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip2"])));
                            ocrFieldData2.OCRRuleTip3 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip3"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip3"])));
                            ocrFieldData2.OCRRuleTip4 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip4"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip4"])));
                            ocrFieldData2.OCRRuleTip5 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip5"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip5"])));
                            ocrFieldData2.OCRRuleTip6 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip6"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip6"])));
                            ocrFieldData2.OCRRuleTip7 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip7"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip7"])));
                            ocrFieldData2.OCRRuleTip8 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip8"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip8"])));
                            ocrFieldData2.OCRRuleTip9 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip9"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip9"])));
                            ocrFieldData2.OCRRuleTip10 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip10"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip10"])));
                            ocrFieldData2.PreOCRFormName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreOCRFormName"])), "", RuntimeHelpers.GetObjectValue(Ocr["PreOCRFormName"])));
                            ocrFieldData2.AllowZeroValue = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["AllowZeroValue"])), "", RuntimeHelpers.GetObjectValue(Ocr["AllowZeroValue"])));
                            ocrFieldData2.In1040ScanVerification = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["In1040ScanVerification"])), "", RuntimeHelpers.GetObjectValue(Ocr["In1040ScanVerification"])));
                            ocrFieldData2.ClientPageDPI = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["ClientPageDPI"])), 0, RuntimeHelpers.GetObjectValue(Ocr["ClientPageDPI"])));
                            ocrFieldData2.FileType = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["FileType"])), "", RuntimeHelpers.GetObjectValue(Ocr["FileType"])));
                            ocrFieldData2.SkewAngle = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SkewAngle"])), "", RuntimeHelpers.GetObjectValue(Ocr["SkewAngle"])));
                            ocrFieldData2.OCRIdentifierWithoutSPLChars = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRIdentifierWithoutSPLChars"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRIdentifierWithoutSPLChars"])));
                            ocrFieldData2.UnCertainChar = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["UnCertainChar"])), 0, RuntimeHelpers.GetObjectValue(Ocr["UnCertainChar"])));
                            ocrFieldData2.DuplicateOcrFieldID = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["DuplicateOcrFieldID"])), 0, RuntimeHelpers.GetObjectValue(Ocr["DuplicateOcrFieldID"])));
                            ocrFieldData2.IsAutoDuplicate = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["IsAutoDuplicate"])), 0, RuntimeHelpers.GetObjectValue(Ocr["IsAutoDuplicate"])));
                            ocrFieldData2.IsAutoUnchecked = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["IsAutoUnchecked"])), 0, RuntimeHelpers.GetObjectValue(Ocr["IsAutoUnchecked"])));
                            ocrFieldData2.OcrDuplicateFieldIds = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OcrDuplicateFieldIds"])), "", RuntimeHelpers.GetObjectValue(Ocr["OcrDuplicateFieldIds"])));
                            ocrFieldData2.DuplicatePageIds = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["DuplicatePageIds"])), "", RuntimeHelpers.GetObjectValue(Ocr["DuplicatePageIds"])));
                            ocrFieldData2.MuniLogicApplied = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["MuniLogicApplied"])), 0, RuntimeHelpers.GetObjectValue(Ocr["MuniLogicApplied"])));
                            ocrFieldData2.IsPWCOrganizer = Conversions.ToBoolean(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["IsPWCOrganizer"])), 0, RuntimeHelpers.GetObjectValue(Ocr["IsPWCOrganizer"])));
                            ocrFieldData2.Currency = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["Currency"])), "", RuntimeHelpers.GetObjectValue(Ocr["Currency"])));
                            ocrFieldData2.SubGroupNo = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SubGroupNo"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SubGroupNo"])));
                            ocrFieldData2.IsNewProcess = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["IsNewProcess"])), "", RuntimeHelpers.GetObjectValue(Ocr["IsNewProcess"])));
                            ocrFieldData2.OCROriginalValue = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCROriginalValue"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCROriginalValue"])));
                            ocrFieldData2.TLValue = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["TLValue"])), "", RuntimeHelpers.GetObjectValue(Ocr["TLValue"])));
                            ocrFieldData2.PageType = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PageType"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PageType"])));
                            return ocrFieldData2;
                        }) ).ToList();
            }
            return dtPreCorrectionData.AsEnumerable().Select((delegate (DataRow Ocr)
            {
                OcrFieldData ocrFieldData = new OcrFieldData();
                ocrFieldData.EngagementID = Conversions.ToInteger(Ocr["EngagementID"]);
                ocrFieldData.EngagementPageID = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["EngagementPageID"])), 0, RuntimeHelpers.GetObjectValue(Ocr["EngagementPageID"])));
                ocrFieldData.FileName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["FileName"])), "", RuntimeHelpers.GetObjectValue(Ocr["FileName"])));
                ocrFieldData.TaxSoftwareID = Conversions.ToInteger(Ocr["TaxSoftwareID"]);
                ocrFieldData.EngagementTypeID = Conversions.ToInteger(Ocr["EngagementTypeID"]);
                ocrFieldData.TaxYear = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["TaxYear"])), 0, RuntimeHelpers.GetObjectValue(Ocr["TaxYear"])));
                ocrFieldData.InSPVerification = Conversions.ToString(Ocr["InSPVerification"]);
                ocrFieldData.EngagementOCRFieldID = Conversions.ToInteger(Ocr["EngagementOCRFieldID"]);
                ocrFieldData.OCRTemplateName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateName"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateName"])));
                ocrFieldData.OCRTableName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTableName"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRTableName"])));
                ocrFieldData.OCRFieldName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRFieldName"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRFieldName"])));
                ocrFieldData.OCRRowNo = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRowNo"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRowNo"])));
                ocrFieldData.OCRPageNo = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPageNo"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRPageNo"])));
                ocrFieldData.OCRLeft = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRLeft"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRLeft"])));
                ocrFieldData.OCRTop = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTop"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRTop"])));
                ocrFieldData.OCRRight = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRight"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRight"])));
                ocrFieldData.OCRBottom = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRBottom"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRBottom"])));
                ocrFieldData.SPVOCRLeft = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SPVOCRLeft"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SPVOCRLeft"])));
                ocrFieldData.SPVOCRTop = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SPVOCRTop"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SPVOCRTop"])));
                ocrFieldData.SPVOCRRight = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SPVOCRRight"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SPVOCRRight"])));
                ocrFieldData.SPVOCRBottom = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SPVOCRBottom"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SPVOCRBottom"])));
                ocrFieldData.OCRValue = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRValue"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRValue"])));
                ocrFieldData.OCRTemplateID = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateID"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateID"])));
                ocrFieldData.OCRTemplateFieldID = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateFieldID"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRTemplateFieldID"])));
                ocrFieldData.SheetNo = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SheetNo"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SheetNo"])));
                ocrFieldData.FaxDWPCode = Conversions.ToString(Ocr["FaxDWPCode"]);
                ocrFieldData.FaxRowNumber = Conversions.ToInteger(Ocr["FaxRowNumber"]);
                ocrFieldData.FaxFormID = Conversions.ToInteger(Ocr["FaxFormID"]);
                ocrFieldData.FaxFormFieldID = Conversions.ToInteger(Ocr["FaxFormFieldID"]);
                ocrFieldData.ForCorrection = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["ForCorrection"])), "", RuntimeHelpers.GetObjectValue(Ocr["ForCorrection"])));
                ocrFieldData.OCRRuleType1 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType1"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType1"])));
                ocrFieldData.OCRRule1 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule1"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule1"])));
                ocrFieldData.OCRRuleType2 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType2"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType2"])));
                ocrFieldData.OCRRule2 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule2"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule2"])));
                ocrFieldData.OCRRuleType3 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType3"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType3"])));
                ocrFieldData.OCRRule3 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule3"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule3"])));
                ocrFieldData.OCRRuleType4 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType4"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType4"])));
                ocrFieldData.OCRRule4 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule4"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule4"])));
                ocrFieldData.OCRRuleType5 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType5"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType5"])));
                ocrFieldData.OCRRule5 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule5"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule5"])));
                ocrFieldData.FaxFormInstance = Conversions.ToString(Ocr["FaxFormInstance"]);
                ocrFieldData.FaxFieldInstance = Conversions.ToString(Ocr["FaxFieldInstance"]);
                ocrFieldData.FaxFormType = Conversions.ToInteger(Ocr["FaxFormType"]);
                ocrFieldData.DisplayOrder = Conversions.ToInteger(Ocr["DisplayOrder"]);
                ocrFieldData.Identifier = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["Identifier"])), "", RuntimeHelpers.GetObjectValue(Ocr["Identifier"])));
                ocrFieldData.IsConverted = Conversions.ToString(Ocr["IsConverted"]);
                ocrFieldData.FaxFormDWPCode = Conversions.ToString(Ocr["FaxFormDWPCode"]);
                ocrFieldData.FaxFieldName = Conversions.ToString(Ocr["FaxFieldName"]);
                ocrFieldData.FaxFormName = Conversions.ToString(Ocr["FaxFormName"]);
                ocrFieldData.FaxFormIdentifier = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["FaxFormIdentifier"])), "", RuntimeHelpers.GetObjectValue(Ocr["FaxFormIdentifier"])));
                ocrFieldData.OCRIdentifier = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRIdentifier"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRIdentifier"])));
                ocrFieldData.OCRDWPCode = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRDWPCode"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRDWPCode"])));
                ocrFieldData.Verified = Conversions.ToString(Ocr["Verified"]);
                ocrFieldData.DataType = Conversions.ToInteger(Ocr["DataType"]);
                ocrFieldData.OCRVerified = Conversions.ToString(Ocr["OCRVerified"]);
                ocrFieldData.OCRAutoVerified = Conversions.ToString(Ocr["OCRAutoVerified"]);
                ocrFieldData.EngagementFormFieldID = Conversions.ToInteger(Ocr["EngagementFormFieldID"]);
                ocrFieldData.VirtualRotation = Conversions.ToInteger(Ocr["VirtualRotation"]);
                ocrFieldData.ApplyDecimalRule = Conversions.ToString(Ocr["ApplyDecimalRule"]);
                ocrFieldData.OCRPreRuleType1 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType1"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType1"])));
                ocrFieldData.OCRPreRule1 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule1"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule1"])));
                ocrFieldData.PreRuleAutoVerify1 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify1"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify1"])));
                ocrFieldData.OCRPreRuleType2 = Conversions.ToInteger(CommonCode.validateinteger(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType2"])));
                ocrFieldData.OCRPreRule2 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule2"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule2"])));
                ocrFieldData.PreRuleAutoVerify2 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify2"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify2"])));
                ocrFieldData.OCRPreRuleType3 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify2"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify2"])));
                ocrFieldData.OCRPreRule3 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule3"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule3"])));
                ocrFieldData.PreRuleAutoVerify3 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify3"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify3"])));
                ocrFieldData.OCRPreRuleType4 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType4"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType4"])));
                ocrFieldData.OCRPreRule4 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule4"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule4"])));
                ocrFieldData.PreRuleAutoVerify4 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify4"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify4"])));
                ocrFieldData.OCRPreRuleType5 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType5"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRPreRuleType5"])));
                ocrFieldData.OCRPreRule5 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule5"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRPreRule5"])));
                ocrFieldData.PreRuleAutoVerify5 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify5"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PreRuleAutoVerify5"])));
                ocrFieldData.OCRRuleType6 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType6"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType6"])));
                ocrFieldData.OCRRule6 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule6"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule6"])));
                ocrFieldData.OCRRuleType7 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType7"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType7"])));
                ocrFieldData.OCRRule7 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule7"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule7"])));
                ocrFieldData.OCRRuleType8 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType8"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType8"])));
                ocrFieldData.OCRRule8 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule8"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule8"])));
                ocrFieldData.OCRRuleType9 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType9"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType9"])));
                ocrFieldData.OCRRule9 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule9"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule9"])));
                ocrFieldData.OCRRuleType10 = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType10"])), 0, RuntimeHelpers.GetObjectValue(Ocr["OCRRuleType10"])));
                ocrFieldData.OCRRule10 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRule10"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRule10"])));
                ocrFieldData.OCRRuleTip1 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip1"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip1"])));
                ocrFieldData.OCRRuleTip2 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip2"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip2"])));
                ocrFieldData.OCRRuleTip3 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip3"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip3"])));
                ocrFieldData.OCRRuleTip4 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip4"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip4"])));
                ocrFieldData.OCRRuleTip5 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip5"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip5"])));
                ocrFieldData.OCRRuleTip6 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip6"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip6"])));
                ocrFieldData.OCRRuleTip7 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip7"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip7"])));
                ocrFieldData.OCRRuleTip8 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip8"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip8"])));
                ocrFieldData.OCRRuleTip9 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip9"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip9"])));
                ocrFieldData.OCRRuleTip10 = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip10"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRRuleTip10"])));
                ocrFieldData.PreOCRFormName = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PreOCRFormName"])), "", RuntimeHelpers.GetObjectValue(Ocr["PreOCRFormName"])));
                ocrFieldData.AllowZeroValue = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["AllowZeroValue"])), "", RuntimeHelpers.GetObjectValue(Ocr["AllowZeroValue"])));
                ocrFieldData.In1040ScanVerification = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["In1040ScanVerification"])), "", RuntimeHelpers.GetObjectValue(Ocr["In1040ScanVerification"])));
                ocrFieldData.ClientPageDPI = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["ClientPageDPI"])), 0, RuntimeHelpers.GetObjectValue(Ocr["ClientPageDPI"])));
                ocrFieldData.FileType = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["FileType"])), "", RuntimeHelpers.GetObjectValue(Ocr["FileType"])));
                ocrFieldData.SkewAngle = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SkewAngle"])), "", RuntimeHelpers.GetObjectValue(Ocr["SkewAngle"])));
                ocrFieldData.OCRIdentifierWithoutSPLChars = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCRIdentifierWithoutSPLChars"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCRIdentifierWithoutSPLChars"])));
                ocrFieldData.UnCertainChar = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["UnCertainChar"])), 0, RuntimeHelpers.GetObjectValue(Ocr["UnCertainChar"])));
                ocrFieldData.DuplicateOcrFieldID = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["DuplicateOcrFieldID"])), 0, RuntimeHelpers.GetObjectValue(Ocr["DuplicateOcrFieldID"])));
                ocrFieldData.IsAutoDuplicate = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["IsAutoDuplicate"])), 0, RuntimeHelpers.GetObjectValue(Ocr["IsAutoDuplicate"])));
                ocrFieldData.IsAutoUnchecked = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["IsAutoUnchecked"])), 0, RuntimeHelpers.GetObjectValue(Ocr["IsAutoUnchecked"])));
                ocrFieldData.OcrDuplicateFieldIds = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OcrDuplicateFieldIds"])), "", RuntimeHelpers.GetObjectValue(Ocr["OcrDuplicateFieldIds"])));
                ocrFieldData.DuplicatePageIds = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["DuplicatePageIds"])), "", RuntimeHelpers.GetObjectValue(Ocr["DuplicatePageIds"])));
                ocrFieldData.MuniLogicApplied = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["MuniLogicApplied"])), 0, RuntimeHelpers.GetObjectValue(Ocr["MuniLogicApplied"])));
                ocrFieldData.IsPWCOrganizer = Conversions.ToBoolean(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["IsPWCOrganizer"])), 0, RuntimeHelpers.GetObjectValue(Ocr["IsPWCOrganizer"])));
                ocrFieldData.Currency = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["Currency"])), "", RuntimeHelpers.GetObjectValue(Ocr["Currency"])));
                ocrFieldData.SubGroupNo = Conversions.ToInteger(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["SubGroupNo"])), 0, RuntimeHelpers.GetObjectValue(Ocr["SubGroupNo"])));
                ocrFieldData.IsNewProcess = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["IsNewProcess"])), "", RuntimeHelpers.GetObjectValue(Ocr["IsNewProcess"])));
                ocrFieldData.OCROriginalValue = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["OCROriginalValue"])), "", RuntimeHelpers.GetObjectValue(Ocr["OCROriginalValue"])));
                ocrFieldData.TLValue = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["TLValue"])), "", RuntimeHelpers.GetObjectValue(Ocr["TLValue"])));
                ocrFieldData.PageType = Conversions.ToString(Interaction.IIf(CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(Ocr["PageType"])), 0, RuntimeHelpers.GetObjectValue(Ocr["PageType"])));
                return ocrFieldData;
            }) ).ToList();
        }

        public static List<Faxformdata> GetFaxformList(DataTable dtFaxData, int EngPageId)
        {
            return null;
        }

        public static List<FormFieldData> GetFormfieldList(DataTable dtEngFormFieldData, int EngPageId)
        {
            return dtEngFormFieldData.AsEnumerable().Select(( ((DataRow frmfield) => new FormFieldData
            {
                EngagementFormFieldID = Conversions.ToInteger(frmfield["EngagementFormFieldID"]),
                FieldValue = Conversions.ToString(frmfield["FieldValue"]),
                FieldDWPCode = Conversions.ToString(frmfield["FieldDWPCode"]),
                DataType = Conversions.ToInteger(frmfield["DataType"])
            })) ).ToList();
        }

        public static List<DropdownData> GetDropdownList(DataTable dtDropDown)
        {
            return dtDropDown.AsEnumerable().Select(( ((DataRow DD) => new DropdownData
            {
                ParameterID = Conversions.ToString(DD["ParameterID"]),
                ParameterDetailValue = Conversions.ToString(DD["ParameterDetailValue"]),
                ParameterDetailName = Conversions.ToString(DD["ParameterDetailName"]),
                ParameterDisplayName = Conversions.ToString(DD["ParameterDisplayName"]),
                DisplaySequence = Conversions.ToInteger(DD["DisplaySequence"]),
                PWCDisplayName = Conversions.ToString(DD["PWCDisplayName"]),
                ParameterDetailValueStateAbbrivation = Conversions.ToString(DD["ParameterDetailValueStateAbbrivation"])
            })) ).ToList();
        }

        public static string GetOCRRule(int iSequence, OcrFieldData Ocritem)
        {
            switch (iSequence)
            {
                case 1:
                    return Ocritem.OCRRule1;
                case 2:
                    return Ocritem.OCRRule2;
                case 3:
                    return Ocritem.OCRRule3;
                case 4:
                    return Ocritem.OCRRule4;
                case 5:
                    return Ocritem.OCRRule5;
                case 6:
                    return Ocritem.OCRRule6;
                case 7:
                    return Ocritem.OCRRule7;
                case 8:
                    return Ocritem.OCRRule8;
                case 9:
                    return Ocritem.OCRRule9;
                case 10:
                    return Ocritem.OCRRule10;
                default:
                    {
                        string GetOCRRule = default(string);
                        return GetOCRRule;
                    }
            }
        }

        public static string GetOCRPreRuleType(int iSequence, OcrFieldData Ocritem)
        {
            switch (iSequence)
            {
                case 1:
                    return Conversions.ToString(Ocritem.OCRRuleType1);
                case 2:
                    return Conversions.ToString(Ocritem.OCRRuleType2);
                case 3:
                    return Conversions.ToString(Ocritem.OCRRuleType3);
                case 4:
                    return Conversions.ToString(Ocritem.OCRRuleType4);
                case 5:
                    return Conversions.ToString(Ocritem.OCRRuleType5);
                default:
                    {
                        string GetOCRPreRuleType = default(string);
                        return GetOCRPreRuleType;
                    }
            }
        }

        public static int GetPreRuleAutoVerify(int iSequence, OcrFieldData Ocritem)
        {
            switch (iSequence)
            {
                case 1:
                    return Ocritem.PreRuleAutoVerify1;
                case 2:
                    return Ocritem.PreRuleAutoVerify2;
                case 3:
                    return Ocritem.PreRuleAutoVerify3;
                case 4:
                    return Ocritem.PreRuleAutoVerify4;
                case 5:
                    return Ocritem.PreRuleAutoVerify5;
                default:
                    {
                        int GetPreRuleAutoVerify = default(int);
                        return GetPreRuleAutoVerify;
                    }
            }
        }

        public static List<OCRSubSet> GetPageFaxData(DataTable dtfaxdata, int faxRowNumber)
        {
            if (faxRowNumber > 0)
            {
                return (from Ocr in dtfaxdata.AsEnumerable()
                        where Conversions.ToDouble(Ocr["FaxRowNumber"].ToString()) == (double)faxRowNumber
                        select Ocr).Select(( ((DataRow Ocr) => new OCRSubSet
                        {
                            InputForm = Conversions.ToString(Ocr["InputForm"]),
                            FaxRowNumber = Conversions.ToInteger(Ocr["FaxRowNumber"]),
                            FFValue = Conversions.ToString(Ocr["FFValue"]),
                            FaxDWPCode = Conversions.ToString(Ocr["FaxDWPCode"]),
                            DataType = Conversions.ToInteger(Ocr["DataType"]),
                            EngagementFaxFormID = Conversions.ToInteger(Ocr["EngagementFaxFormID"]),
                            EngagementPageID = Conversions.ToInteger(Ocr["EngagementPageID"])
                        })) ).ToList();
            }
            return dtfaxdata.AsEnumerable().Select(( ((DataRow Ocr) => new OCRSubSet
            {
                InputForm = Conversions.ToString(Ocr["InputForm"]),
                FaxRowNumber = Conversions.ToInteger(Ocr["FaxRowNumber"]),
                FFValue = Conversions.ToString(Ocr["FFValue"]),
                FaxDWPCode = Conversions.ToString(Ocr["FaxDWPCode"]),
                DataType = Conversions.ToInteger(Ocr["DataType"]),
                EngagementFaxFormID = Conversions.ToInteger(Ocr["EngagementFaxFormID"]),
                EngagementPageID = Conversions.ToInteger(Ocr["EngagementPageID"])
            }))).ToList();
        }

        public static List<EngFormField> GetEngFormFields(DataTable dtFormFields)
        {
            return dtFormFields.AsEnumerable().Select((((DataRow fld) => new EngFormField
            {
                FormFieldID = Conversions.ToInteger(fld["FormFieldID"]),
                DataType = Conversions.ToInteger(fld["DataType"]),
                EngagementFormFieldID = Conversions.ToInteger(fld["EngagementFormFieldID"]),
                EngagementFormID = Conversions.ToInteger(fld["EngagementFormID"]),
                EngagementID = Conversions.ToInteger(fld["EngagementID"]),
                FieldDWPCode = Conversions.ToString(fld["FieldDWPCode"]),
                FieldValue = Conversions.ToString(fld["FieldValue"]),
                ParentEngagementFormID = Conversions.ToInteger(fld["ParentEngagementFormID"])
            })) ).ToList();
        }

        private static object validateinteger(object val)
        {
            if (CommonCode.IsDBNull(RuntimeHelpers.GetObjectValue(val)) || Operators.ConditionalCompareObjectEqual(val, "", false))
            {
                return 0;
            }
            return Convert.ToInt32(RuntimeHelpers.GetObjectValue(val));
        }
    }

}
