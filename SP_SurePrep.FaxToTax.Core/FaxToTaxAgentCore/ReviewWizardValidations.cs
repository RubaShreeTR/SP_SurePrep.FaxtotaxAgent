using FaxToTaxDataValidations;
using FaxToTaxLibrary;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxAgentCore
{
    public class ReviewWizardValidations
    {

        public ReviewWizardValidations(string configConnectionKey)
        {

            GenModule.ConfigConnectionKey = configConnectionKey;
        }

        public ReviewWizardValidations(string strEnvironment, string configConnectionKey)
        {
            GenModule.ConfigConnectionKey = configConnectionKey;
            GenModule.configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            //GenModule.UseAPI = GenModule.configuration.GetSection("UseAPI").Value;
            //UesAPI.UesAPIs = Convert.ToBoolean(GenModule.UseAPI);
            //GetToken();
            switch (strEnvironment)
            {
                case "DEV":
                    GenModule.ConnectionString = GenModule.configuration.GetConnectionString("DefaultConnection");
                    break;
                case "QA":
                    GenModule.ConnectionString = GenModule.configuration.GetConnectionString("DefaultConnection");
                    break;
                case "STAGE":
                    GenModule.ConnectionString = GenModule.configuration.GetConnectionString("DefaultConnection");
                    break;
                case "LIVE":
                    GenModule.ConnectionString = GenModule.configuration.GetConnectionString("DefaultConnection");
                    break;
                default:
                    GenModule.ConnectionString = GenModule.configuration.GetConnectionString("DefaultConnection");
                    break;
            }
            SetConfigVariables();
        }
        public void SetConfigVariables()
        {
            try
            {
                string getOCRDataAttempts = GenModule.configuration.GetSection("GetOCRDataAttempts").Value;
                string waitTime = GenModule.configuration.GetSection("WaitTime").Value;
                string cleanAtEnd = GenModule.configuration.GetSection("CleanAtEnd").Value;
                string truncateAmount = GenModule.configuration.GetSection("TruncateAmount").Value;

                string agentAuthKey = GenModule.configuration.GetSection("AgentAuthKey").Value;
                string baseUrl = GenModule.configuration.GetSection("baseUrl").Value;
                string agentAPIbaseUrl = GenModule.configuration.GetSection("AgentAPIbaseUrl").Value;
                string AllowedHosts = GenModule.configuration.GetSection("AllowedHosts").Value;
                GenModule.GetJunkDataAttempts = Convert.ToInt32(getOCRDataAttempts);
                GenModule.WaitTime = Convert.ToInt32(waitTime);
            }
            catch (Exception)
            {
                // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Error Occured in SetConfigVariables : " + ex.Message + " ==>> " + DateTime.Now, 11066, 1, strEngagement, TraceType.InfoLog);
            }
        }

       

        public enum TaxSoftware
        {
            GoSystem = 1,
            Lacerte = 2,
            ProSystem = 3,
            UltraTax = 4,
            ProSeries = 5,
            GlobalFx = 6
        }

        public enum DataType
        {
            Amount = 1,
            Percent = 2,
            AorP = 3,
            Description = 4,
            DropDown = 5,
            DateType = 6,
            SSN = 7,
            EIN = 8,
            MonthYear = 9,
            IntegerType = 10,
            MonthYY = 11,
            MonthDay = 12,
            Quantity = 13,
            SSNorEIN = 14,
            Telephone = 15
        }

        private IOCRReference GetOCRReference(DataRow dtOCRData, int taxSoftwareId, int taxYear)
        {
            try
            {
                var ocrRef = new OCRReference
                {
                    OCRValue = IsDBNull(dtOCRData["OCRValue"]) ? "" : dtOCRData["OCRValue"].ToString(),
                    OCRValueVerifiedvalue = IsDBNull(dtOCRData["OCRVerifiedValue"]) ? "" : dtOCRData["OCRVerifiedValue"].ToString(),
                    EngagementOCRFieldID = IsDBNull(dtOCRData["EngagementOCRFieldID"]) ? "" : dtOCRData["EngagementOCRFieldID"].ToString(),
                    DataType = (BinderEnums.DataType)(IsDBNull(dtOCRData["DataType"]) ? 0 : Convert.ToInt32(dtOCRData["DataType"])),
                    FaxDWPCode = IsDBNull(dtOCRData["FaxDWPCode"]) ? "" : dtOCRData["FaxDWPCode"].ToString(),
                    AcceptVarious = IsDBNull(dtOCRData["AcceptVarious"]) ? false : Convert.ToBoolean(dtOCRData["AcceptVarious"]),
                    MaxLength = IsDBNull(dtOCRData["MaxLength"]) ? 0 : Convert.ToInt32(dtOCRData["MaxLength"]),
                    LengthAfterDecimal = IsDBNull(dtOCRData["LengthAfterDecimal"]) ? 0 : Convert.ToInt32(dtOCRData["LengthAfterDecimal"]),
                    LengthBeforeDecimal = IsDBNull(dtOCRData["LengthBeforeDecimal"]) ? 0 : Convert.ToInt32(dtOCRData["LengthBeforeDecimal"]),
                    TaxSoftwareId = (BinderEnums.TaxSoftware)taxSoftwareId,
                    TaxYear = taxYear
                };
                return ocrRef;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsDBNull(object expression)
        {
            return expression is DBNull || expression == null || string.IsNullOrEmpty(expression.ToString());
        }

        public string ValidateEngOCRFieldData(DataTable dtOCRData, int taxSoftwareID, int taxYear, string strEngagementID = "", string strFieldsToExclude = "")
        {
            string strEngOCRFieldIDsToUpdate = string.Empty;
            try
            {
                strEngOCRFieldIDsToUpdate = ValidateEngOCRFieldData_UpdateData(dtOCRData, taxSoftwareID, taxYear, strEngagementID, strFieldsToExclude);
                if (!string.IsNullOrEmpty(strEngOCRFieldIDsToUpdate))
                {
                    UpdateNullForJunkValues(strEngagementID, strEngOCRFieldIDsToUpdate);
                }
                return strEngOCRFieldIDsToUpdate;
            }
            catch (Exception)
            {
                return strEngOCRFieldIDsToUpdate;
            }
        }

        public string ValidateEngOCRFieldData_UpdateData(DataTable dtOCRData, int TaxSoftwareID, int TaxYear, string StrEngagementID = "", string StrFieldsToExclude = "")
        {
            string strEngOCRFieldIDsToUpdate = "";
            try
            {
                if (dtOCRData.Rows.Count > 0)
                {
                    for (int intCount = 0; intCount < dtOCRData.Rows.Count; intCount++)
                    {
                        OCRReference ocrRef = (OCRReference)GetOCRReference(dtOCRData.Rows[intCount], TaxSoftwareID, TaxYear);
                        string orgValue = ocrRef.OCRValue;
                        string errorMessage = ocrRef.Validate(ocrRef, true);
                        ocrRef.IsUpdateRequried = !string.IsNullOrWhiteSpace(ocrRef.ValueToUpdate) && (ocrRef.ValueToUpdate != orgValue);
                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            strEngOCRFieldIDsToUpdate = AppendEngOCRValue(strEngOCRFieldIDsToUpdate, ocrRef.EngagementOCRFieldID);
                        }
                        else if (ocrRef.IsUpdateRequried)
                        {
                            UpdateValidateValue(ocrRef, Convert.ToInt32(StrEngagementID));
                        }
                    }
                }
                dtOCRData.Dispose();
                dtOCRData = null;
                return strEngOCRFieldIDsToUpdate;
            }
            catch (Exception)
            {
                return strEngOCRFieldIDsToUpdate;
            }
        }

        private string AppendEngOCRValue(string strAllEngagementOCRFieldIds, string strEngagementOCRFieldId)
        {
            if (string.IsNullOrEmpty(strAllEngagementOCRFieldIds))
            {
                strAllEngagementOCRFieldIds = strEngagementOCRFieldId;
            }
            else
            {
                strAllEngagementOCRFieldIds = strAllEngagementOCRFieldIds + "," + strEngagementOCRFieldId;
            }
            return strAllEngagementOCRFieldIds;
        }

        private bool UpdateValidateValue(IOCRReference ocrRef, int intEngagementID)
        {
            try
            {

                if (!string.IsNullOrEmpty(ocrRef.F2tComment))
                {
                    var spParams = new SpParameter[4];
                    spParams[0] = new SpParameter("EngagementID", intEngagementID);
                    spParams[1] = new SpParameter("OCRValue", ocrRef.ValueToUpdate);
                    spParams[2] = new SpParameter("EngagementOCRFieldID", ocrRef.EngagementOCRFieldID);
                    spParams[3] = new SpParameter("F2TComments", ocrRef.F2tComment);
                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementOCRField_V1", spParams, true);
                }
                else
                {
                    var spParams = new SpParameter[3];
                    spParams[0] = new SpParameter("EngagementID", intEngagementID);
                    spParams[1] = new SpParameter("OCRValue", ocrRef.ValueToUpdate);
                    spParams[2] = new SpParameter("EngagementOCRFieldID", ocrRef.EngagementOCRFieldID);
                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementOCRFieldWithoutComments", spParams, true);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void OCRFieldValidations(string strEngagementId, int intTaxSoftwareID, int intTaxYear, int intEngagementTypeID)
        {
            try
            {
                SetConfigVariables();
                GenModule.strEngagement = strEngagementId;
                GenModule.TaxYear = intTaxYear;
                GenModule.TaxSoftwareID = intTaxSoftwareID;

                if (!GenModule.blnRepeatValidation)
                {
                    UpdateOcrFieldDataTypeFaxForm(strEngagementId, intTaxYear, intEngagementTypeID);
                }

                CorrectJunkOCRData(strEngagementId);

                DataTable dtEngagementOcrField = GetOCRDataTovalidate(strEngagementId, intTaxYear, intEngagementTypeID);

                if (GenModule.GetJunkDataAttempts > 0 && GenModule.blnRepeatValidation)
                {
                    if (dtEngagementOcrField == null || dtEngagementOcrField.Rows.Count == 0)
                    {
                        int OcrDataCount = GetOCRData(strEngagementId);
                        if (OcrDataCount > 0)
                        {
                            for (int intAttemptGetData = 0; intAttemptGetData < GenModule.GetJunkDataAttempts; intAttemptGetData++)
                            {
                                UpdateOcrFieldDataTypeFaxForm(strEngagementId, intTaxYear, intEngagementTypeID);
                                CorrectJunkOCRData(strEngagementId);
                                dtEngagementOcrField = GetOCRDataTovalidate(strEngagementId, intTaxYear, intEngagementTypeID);
                                if (dtEngagementOcrField != null && dtEngagementOcrField.Rows.Count > 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                if (dtEngagementOcrField != null && dtEngagementOcrField.Rows.Count > 0)
                {
                    DataTable dtEngOcrFieldToExclude = GetOCRFieldToExclude(strEngagementId);
                    ValidateEngOCRFieldData(dtEngagementOcrField, intTaxSoftwareID, GenModule.TaxYear, strEngagementId, "");
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                GenModule.blnRepeatValidation = false;
            }
        }

        public DataTable GetOCRDataTovalidate(string strEngagementId, int intTaxYear, int intEngagementTypeID)
        {
            DataTable dtEngagementOcrFieldData = new DataTable();
            try
            {

                var SqlArt = new SpParameter[3];
                SqlArt[0] = new SpParameter("@EngagementID", strEngagementId);
                SqlArt[1] = new SpParameter("@TaxYear", intTaxYear);
                SqlArt[2] = new SpParameter("@EngagementTypeID", intEngagementTypeID);
                dtEngagementOcrFieldData = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).GetDataTable("[dbo].[Proc_GetOCRFieldDataToValidate]", SqlArt, "", true);

            }
            catch (Exception)
            {
            }
            return dtEngagementOcrFieldData;
        }

        public DataTable GetOCRFieldToExclude(string strEngagementId, string strDataType = "")
        {
            DataTable dtOCRFieldToExclude = new DataTable();
            try
            {

                var SqlArt = new SpParameter[1];
                if (string.IsNullOrEmpty(strDataType))
                {
                    SqlArt[0] = new SpParameter("@DataType", DBNull.Value);
                }
                else
                {
                    SqlArt[0] = new SpParameter("@DataType", strDataType);
                }
                dtOCRFieldToExclude = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).GetDataTable("[Proc_GetJunkExcludeOCRField]", SqlArt, "", true);

            }
            catch (Exception)
            {
            }
            return dtOCRFieldToExclude;
        }

        public DataTable GetOCRDataTovalidateTestHarness(string strEngagementId, int intTaxYear, int intEngagementTypeID)
        {
            DataTable dtEngagementOcrFieldData = new DataTable();
            try
            {

                var SqlArt = new SpParameter[3];
                SqlArt[0] = new SpParameter("@EngagementID", strEngagementId);
                SqlArt[1] = new SpParameter("@TaxYear", intTaxYear);
                SqlArt[2] = new SpParameter("@EngagementTypeID", intEngagementTypeID);
                dtEngagementOcrFieldData = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).GetDataTable("[dbo].[Proc_GetOCRFieldDataToValidate_TestHarness]", SqlArt, "", true);

            }
            catch (Exception)
            {
            }
            return dtEngagementOcrFieldData;
        }

        private void UpdateOcrFieldDataTypeFaxForm(string strEngagementId, int intTaxYear, int intEngagementTypeID)
        {
            try
            {
                bool UpdateOcrFieldDataTypeFaxForm;

                var SqlArt = new SpParameter[3];
                SqlArt[0] = new SpParameter("@EngagementID", strEngagementId);
                SqlArt[1] = new SpParameter("@TaxYear", intTaxYear);
                SqlArt[2] = new SpParameter("@EngagementTypeID", intEngagementTypeID);
                UpdateOcrFieldDataTypeFaxForm = Convert.ToBoolean(CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).AddUpdateOrDelete("[dbo].[Proc_UpdateOCRFieldDataTypeFaxFormField]", SqlArt, true));

            }
            catch (Exception)
            {
            }
        }

        public void CorrectJunkOCRData(string strEngagementId)
        {
            try
            {
                bool CorrectJunkOCRData;

                var SqlArt = new SpParameter[1];
                SqlArt[0] = new SpParameter("@EngagementID", strEngagementId);
                CorrectJunkOCRData = Convert.ToBoolean(CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).AddUpdateOrDelete("[dbo].[Proc_CorrectJunkOCRData]", SqlArt, true));

            }
            catch (Exception)
            {
            }
        }

        public void UpdateNullForJunkValues(string strEngagementId, string strAllEngOCRFieldIDsToUpdate)
        {
            try
            {
                bool rtnValue;

                var spParams = new SpParameter[2];
                spParams[0] = new SpParameter("EngagementID", strEngagementId);
                spParams[1] = new SpParameter("EngOCRFieldIDsToUpdate", strAllEngOCRFieldIDsToUpdate);
                rtnValue = Convert.ToBoolean(CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).AddUpdateOrDelete("[dbo].[Proc_UpdateEngagementJunkDataOCRField]", spParams, true));

            }
            catch (Exception)
            {
            }
        }
        public bool SendBackBinderToVerification(string strEngagementId)
        {
            bool blnSuccessMain = false;
            bool blnSuccess = false;
            try
            {

                SpParameter[] SqlArt = new SpParameter[2];
                SqlArt[0] = new SpParameter("@EngagementID", strEngagementId);
                SqlArt[1] = new SpParameter("@KeepVerifiedValue", "N");
                blnSuccessMain = CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("[dbo].[proc_sendbacktoverification_net]", SqlArt, true) > 0;
                if (blnSuccessMain)
                {
                    SpParameter[] SpParam = new SpParameter[1];
                    SpParam[0] = new SpParameter("@EngagementID", strEngagementId);
                    blnSuccess = CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("dbo.Proc_SendBackBinderToVerificationWithJobId", SpParam, true, true) > 0;
                }

            }
            catch (Exception ex)
            {
                // Log exception
            }
            return blnSuccess;
        }

        public bool CleanUPData(string strEngagementId)
        {
            bool blnSuccess = false;
            try
            {

                SpParameter[] SpParam = new SpParameter[1];
                SpParam[0] = new SpParameter("@EngagementID", strEngagementId);
                if (CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).AddUpdateOrDelete("dbo.Proc_CleanUPData", SpParam, true, true) > 0)
                {
                    blnSuccess = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).AddUpdateOrDelete("dbo.Proc_UpdateCleanUPData", SpParam, true, true) > 0;
                }

            }
            catch (Exception ex)
            {
                // Log exception
            }
            return blnSuccess;
        }

        public int GetOCRData(string strEngagementId)
        {
            DataTable dtEngOCRField = new DataTable();
            int intRowCount = 0;
            try
            {

                SpParameter[] spParams = new SpParameter[1];
                spParams[0] = new SpParameter("EngagementID", strEngagementId);
                dtEngOCRField = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).GetDataTable("dbo.Proc_GetOCRDataWithEngId", spParams, "test", true);


                if (dtEngOCRField != null)
                {
                    intRowCount = dtEngOCRField.Rows.Count;
                }
                return intRowCount;
            }
            catch (Exception ex)
            {
                // Log exception
                return intRowCount;
            }
        }

        public DataTable GetOCRDataToValidateQuery(string strEngagementId, string strTaxYear, string strEngagementTypeID)
        {
            DataTable dtEngOCRField = new DataTable();
            try
            {

                SpParameter[] spParams = new SpParameter[3];
                spParams[0] = new SpParameter("EngagementID", strEngagementId);
                spParams[1] = new SpParameter("TaxYear", strTaxYear);
                spParams[2] = new SpParameter("EngagementTypeID", strEngagementTypeID);
                dtEngOCRField = CommonCode.GetEngagementDbCommon(Convert.ToInt32(strEngagementId)).GetDataTable("Proc_OCRDataBeforeF2t", spParams, "Test", true);


                if (dtEngOCRField != null)
                {
                    int intRowCount = dtEngOCRField.Rows.Count;
                }
            }
            catch (Exception ex)
            {
                // Log exception
            }
            return dtEngOCRField;
        }

        public void UpdateNullFieldEditedBy122(string strEngagementId)
        {
            try
            {
                GenModule.CleanAtEnd = GenModule.configuration.GetSection("OCRValidation").Value;

                if (GenModule.CleanAtEnd == "Y")
                {
                    GenModule.blnRepeatValidation = true;
                    OCRFieldValidations(strEngagementId, GenModule.TaxSoftwareID, GenModule.TaxYear, GenModule.EngagementTypeID);
                }
            }
            catch (Exception ex)
            {
                // Log exception
            }
            finally
            {
                GenModule.blnRepeatValidation = false;
            }
        }

        public void OcrValidationForWithoutLead(string strEngagementId)
        {
            try
            {
                GenModule.blnRepeatValidation = true;
                OCRFieldValidations(strEngagementId, GenModule.TaxSoftwareID, GenModule.TaxYear, GenModule.EngagementTypeID);
            }
            catch (Exception ex)
            {
                // Log exception
            }
            finally
            {
                GenModule.blnRepeatValidation = false;
            }
        }
    }
}
