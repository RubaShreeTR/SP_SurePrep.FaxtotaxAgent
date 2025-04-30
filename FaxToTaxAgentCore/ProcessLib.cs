using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FaxToTaxDataValidations;
using FaxToTaxLibrary;
using FaxToTaxLibrary.Classes;
using FaxToTaxTemplateLib;
using FaxToTaxTemplateLib.QueueManagerClasses;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using TextExtractionLib;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static FaxToTaxLibrary.CommonCode;
using MuniBondLogic = FaxToTaxDataValidations.MuniBondLogic;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlDataAdapter = Microsoft.Data.SqlClient.SqlDataAdapter;
using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;

namespace FaxToTaxAgentCore
{
    public class ProcessLib
    {
        public SqlConnection SqlConn, sqlconnmain;
        public string strServerName;
        public string strDbName;
        public string strUserID;
        public bool blnsecondryconnection;
        public string strPassword;
        public string SourceFile;
        public string Constr;
        public string ConMain;
        public bool blncomplete = false;

        public int EngagementTypeID;
        public int TaxYear;
        public int TaxSoftwareID;
        private long EngCnt;
        private string ErrorDescriptions ;
        private string strTime;
        private string WebUrl;
        private string HardCodeString;

        public string lngclientNumber;
        public string domainname;
        public int DomainID;
        public string ProVersion;
        public string JbType = "";
        public int engagementID;
        public int AgentId;
        public Jobs qobj;
        private CookieContainer spwscookie;

        public bool IsMergeBinder;
        private string strWSPath;
        private string IsNewVeritaxAgent;
        private bool IsNewPreprocess = false;

        private DataTable currentStatusTable;
        private DbCommon _dBComm;
        public string IsK1PreRuleValidationOn { get; set; }
        private void GetData( string AuthToken)
        {
            DataTable dt;
            int cntTemp;

            try
            {
                
                    var spParams = new SpParameter[1];
                    spParams[0] = new SpParameter("EngagementID", this.engagementID);
                    dt = CommonCode.GetEngagementDbCommon(engagementID).GetDataTable("Proc_GetspEngagementsDetails", spParams, "SPEngagementsDetails", true);
                

                cntTemp = 0;
                if (dt.Rows.Count > 0)
                {
                    cntTemp++;
                    var row = dt.Rows[0];
                    this.domainname = IsDBNull(row["domainabbreviation"]) ? "NULL" : row["domainabbreviation"].ToString();
                    this.lngclientNumber = IsDBNull(row["clientnumber"]) ? "NULL" : row["clientnumber"].ToString();
                    this.TaxYear = IsDBNull(row["taxyear"]) ? 0 : Convert.ToInt32(row["TAXYEAR"]);
                    this.EngagementTypeID = IsDBNull(row["Engagementtypeid"]) ? 0 : Convert.ToInt32(row["Engagementtypeid"]);
                    this.TaxSoftwareID = IsDBNull(row["taxsoftwareid"]) ? 0 : Convert.ToInt32(row["taxsoftwareid"]);
                    this.DomainID = IsDBNull(row["domainid"]) ? 0 : Convert.ToInt32(row["Domainid"]);
                    this.ProVersion = IsDBNull(row["ProSystemVersion"]) ? "" : row["ProSystemVersion"].ToString();
                    this.IsMergeBinder = IsDBNull(row["MergeBinder"]) ? false : Convert.ToBoolean(row["MergeBinder"]);
                }
                else
                {
                    return;
                }

                dt.Dispose();
            }
            catch (Exception ex)
            {
                ErrorDescriptions = ex.ToString();
            }
        }

        private static bool IsDBNull(object Expression)
        {
            if (Expression is DBNull || Expression == null || string.IsNullOrEmpty(Expression.ToString()))
            {
                return true;
            }
            return false;
        }

        public void SetInitialParameter()
        {
            try
            {
                DataTable tmpdt;

                ConMain = GenModule.ConnectionString;
                FillConnectionMain();
                Constr = GenModule.ConnectionString;
                FillConnection();

                
                    var spParams = new SpParameter[1];
                    spParams[0] = new SpParameter("EngagementID", this.engagementID);
                    _dBComm = new DbCommon("ConnectionString");

                    tmpdt = _dBComm.GetDataTable("dbo.Proc_GetEngagementDBConnectionID", spParams, "SecConnection", true);
               

                if (tmpdt.Rows.Count > 0)
                {
                    if (IsDBNull(tmpdt.Rows[0][0]))
                    {
                        blnsecondryconnection = false;
                        Constr = GenModule.ConnectionString;
                    }
                    else
                    {
                        Constr = $"uid={tmpdt.Rows[0]["dblogin"]};pwd={tmpdt.Rows[0]["dbpassword"]};database={tmpdt.Rows[0]["dbname"]};Max Pool Size=100;Connect TimeOut=0;server={tmpdt.Rows[0]["dbserver"]}";
                        blnsecondryconnection = true;
                    }
                }
                else
                {
                    blnsecondryconnection = false;
                    Constr = GenModule.ConnectionString;
                }
            }
            catch (Exception ex)
            {
                Constr = GenModule.ConnectionString;
                blnsecondryconnection = false;
               // ErrorDescriptionns = ex.ToString();
            }

            FillConnection();
            //GetData( AuthToken);
        }
        bool IsDDP = false;
        public void StartAgentProcess(string strWSPath, string OCRValidation, int LogSeverity, int JobId,string IsK1PreRuleValidationOn, bool blnSPELJOB = false)
        {
            bool blnsuc;
            string tmpstarttime;
            EngCnt = GenModule.EngagementCount;
            ErrorDescriptions = "";

            HardCodeString = "";
            GenModule.IniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ini", "Engagement.ini");
            WebUrl = GenModule.ReadValueFromINI("Engagement", "WebURL", GenModule.IniPath);
            IsNewVeritaxAgent = GenModule.ReadValueFromINI("Engagement", "IsNewVeritaxAgent", GenModule.IniPath);
            HardCodeString = GenModule.ReadValueFromINI("Engagement", "HardCode", GenModule.IniPath);
            tmpstarttime = DateTime.Now.ToString();

            FaxToTaxConversion objFax2Tax = new FaxToTaxConversion(GenModule.ConfigConnectionKey,JobId);

            try
            {
                if (blnSPELJOB)
                {
                    bool MergeBinderConversion = ConvertMergeBinderWS(this.lngclientNumber, this.TaxYear, this.DomainID, this.engagementID, this.TaxSoftwareID, this.ProVersion, 7, "N", strWSPath);
                    if (MergeBinderConversion)
                    {
                        // Log success
                    }
                    else
                    {
                        // Log error
                    }
                    UpdateSPELStatus(this.engagementID);
                    return;
                }
                else
                {
                    EvaluateFormula evaluateFormula = new EvaluateFormula();
                    bool isMultiThreadingActive = evaluateFormula.IsMultiThreadingActive();
                    blnsuc = VeriWizard(OCRValidation, LogSeverity,  JobId, isMultiThreadingActive, IsK1PreRuleValidationOn);
try
                    {
                        
                        if (OCRValidation == "Y")
                        {
                            ReviewWizardValidations objReviewWizardValidatons = new ReviewWizardValidations(ConfigConnectionKey);
                            objReviewWizardValidatons.UpdateNullFieldEditedBy122(Convert.ToString(engagementID));
                            //SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", $"UpdateNullFieldEditedBy122 function called for EngagementID : {engagementID}", 11113, 1, this.engagementID, TraceType.InfoLog);
                        }
                    }
                    catch (Exception ex)
                    {
                        //SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", $"Error Occured in StartAgentProcess: {ex.Message} ==>> {DateTime.Now}", 11114, 1, this.engagementID, TraceType.ErrorLog);
                    }
                }

                try
                {
                    ExtractionData exo = new TextExtractionLib.ExtractionData();
                    exo.StartProcess(qobj.Engagementid, string.Empty, string.Empty);
                }
                catch (Exception)
                {
                    // Log error
                }

                try
                {
                    objFax2Tax = new FaxToTaxConversion(GenModule.ConfigConnectionKey, JobId);
                    objFax2Tax.OCRDecimal(this.engagementID);
                }
                catch (Exception)
                {
                    // Log error
                }
                try
                {
                     objFax2Tax = new FaxToTaxConversion(ConfigConnectionKey);
                    objFax2Tax.Update_ApplyDecimalRule(this.engagementID);
                }
                catch (Exception ex)
                {
                    // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Error Occurred in OCRDecimal: " + ex.Message + " ==>> " + DateTime.Now, 11010, 1, this.engagementID, TraceType.ErrorLog);
                }

                if (!IsDDP)
                {
                    // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Mark fields as uncertain", 11011, 1, this.engagementID, TraceType.InfoLog);
                    DataSet dsUncertainFields = objFax2Tax.Update_OCRDecimalUncertain(engagementID);
                    if (dsUncertainFields != null && dsUncertainFields.Tables.Count > 0 && dsUncertainFields.Tables[0].Rows.Count > 0)
                    {
                        objFax2Tax.UpdateEvenPagesUncertainFields(engagementID, dsUncertainFields.Tables[0]);
                    }

                    // Call Function for Bot here.
                    try
                    {
                        objFax2Tax.SkipWorkFlowSteps(this.engagementID);
                    }
                    catch (Exception ex)
                    {
                        // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Error Occurred in SkipWorkFlowSteps: " + ex.Message + " ==>> " + DateTime.Now, 11012, 1, this.engagementID, TraceType.ErrorLog);
                    }
                }

                if (!blnsuc)
                {
                    // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Completed With Error " + "," + DateTime.Now, 11013, 1, this.engagementID, TraceType.InfoLog);
                    // tx.Text = this.AgentId + ", Completed With Error " + "," + DateTime.Now + Environment.NewLine + tx.Text;
                    Console.WriteLine(this.AgentId + ", Completed With Error " + "," + DateTime.Now + Environment.NewLine);
                    if (qobj != null)
                    {
                        qobj.S_ErrorDescription = "Error Occurred: " + ErrorDescriptions;
                        qobj.JobStatus = Jobs.JobStatusenum.INERROR;
                        qobj.AgentResponse = ErrorDescriptions;
                    }
                    return;
                }
                else
                {
                    try
                    {
                        if (qobj != null)
                        {
                            // Commenting Due to SubJobCreated for Preprocess and This will be completed once SubjobCompleted
                            if (!IsNewPreprocess)
                            {
                                JobStatus(JobId, 0, 0, "Completed");
                            }
                            else
                            {
                                qobj.AgentResponse = "Completed with SubJobCreation";
                            }
                        }

                        // tx.Text = this.AgentId + ", Completed Successfully For " + this.engagementID + " From " + tmpstarttime + " To " + DateTime.Now + Environment.NewLine + tx.Text;
                        Console.WriteLine(this.AgentId + ", Completed Successfully For " + this.engagementID + " From " + tmpstarttime + " To " + DateTime.Now + Environment.NewLine);
                        // Application.DoEvents();
                        // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Completed Successfully For " + this.engagementID + " At , " + DateTime.Now, 11014, 1, this.engagementID, TraceType.InfoLog);
                        GenModule.WriteINI("Engagement", "Total", (GenModule.EngagementCount + 1).ToString(), GenModule.IniPath);
                       GenModule.EngagementCount = GenModule.EngagementCount + 1;
                    }
                    catch (Exception ex)
                    {
                        // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Exception Occurred in application : " + ex.ToString() + ", " + DateTime.Now, 11015, 1, this.engagementID, TraceType.ErrorLog);
                    }
                }
                // Application.DoEvents();

            }
            catch (Exception ex)
            {
                ErrorDescriptions = ex.ToString();
                if (qobj != null)
                {
                    qobj.S_ErrorOccured = Common_SPError.ErrorEnum.ERROROCCURED;
                    qobj.S_ErrorDescription = "Error Occured: " + ErrorDescriptions;
                    qobj.JobStatus = Jobs.JobStatusenum.INERROR;
                    qobj.AgentResponse = ErrorDescriptions;
                }
                blncomplete = true;
            }
            try
            {
                // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Start Execute BDP Process" + "," + DateTime.Now + ", 11017, 1, this.engagementID, TraceType.InfoLog");
                objFax2Tax.ExecuteBDPProcedure(this.engagementID);
                // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "End Execute BDP Process" + "," + DateTime.Now + ", 11018, 1, this.engagementID, TraceType.InfoLog");
            }
            catch (Exception ex)
            {
                // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Error in Execute BDP Process" + "," + DateTime.Now + ", 11019, 1, this.engagementID, TraceType.ErrorLog");
            }

            if (!IsNewPreprocess)
            {
                JobStatus(JobId, 0, 0, "Completed");
                RetryJob(JobId);
            }

            try
            {
                objFax2Tax.Update_EngFlags(this.engagementID);
                JobStatus(JobId, 0, 0, "Completed");

            }
            catch (Exception ex)
            {
                // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Error in syncing flags" + "," + DateTime.Now + ", 11021, 1, this.engagementID, TraceType.ErrorLog");
            }

        }
        private void RetryJob(int JobId)
        {
            try
            {
                Reset_InOCRToFaxAndDBProcedureid(this.engagementID);
                
                    SpParameter[] Param = new SpParameter[4];
                    Param[0] = new SpParameter("@EngagementId", this.engagementID);
                    Param[1] = new SpParameter("@JobID", JobId);
                    Param[2] = new SpParameter("@IsMainJob", true);
                    Param[3] = new SpParameter("@RetryConfig", GenModule._jobRetryValue);
                    GetEngagementDbCommon().AddUpdateOrDelete("DBO.Proc_RetryFax2TaxJob", Param, true);
                
            }
            catch (Exception ex)
            {
                //SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", $"Error Occured From RetryJob Procedure At , {DateTime.Now}", 11131, 1, this.engagementID, TraceType.ErrorLog);
            }
        }
        private void Reset_InOCRToFaxAndDBProcedureid(int EngagementID)
        {
            try
            {
                
                    SpParameter[] Param = new SpParameter[1];
                    Param[0] = new SpParameter("@EngagementId", EngagementID);
                    GetEngagementDbCommon().AddUpdateOrDelete("DBO.Proc_ResetInOCRToFaxAndDBProcedureid", Param, true);
                    GetEngagementDbCommon(EngagementID).AddUpdateOrDelete("DBO.Proc_ResetInOCRToFaxAndDBProcedureid", Param, true);
                
            }
            catch (Exception ex)
            {
                //SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", $"Error Occured From RetryJob Reset Procedure At , {DateTime.Now}", 11131, 1, this.engagementID, TraceType.ErrorLog);
            }
        }

        private bool ConvertMergeBinderWS(string strClientNumber, int intTaxYear, int intDomainID, int intEngagementid, int intTaxSoftwareID, string strTaxSoftwareVersion, int intUserID, string strIsTemplate, string strWSPath)
        {
            int intSuccess;
            var cclObject = new SurePrep.MergedBinder.CommunicationLayer.WebServices();
            SurePrep.MergedBinder.CommunicationLayer.CommunicationCommon.Instance.UserName = "TestUser";
            SurePrep.MergedBinder.CommunicationLayer.CommunicationCommon.Instance.Password = "pass@1234";
            var temp = cclObject.InvokeWebMethod(strWSPath, "ISPDatabaseService", "SPELDataConversion", "", strClientNumber, intTaxYear, intDomainID, intEngagementid, intTaxSoftwareID, strTaxSoftwareVersion, intUserID, strIsTemplate);
            intSuccess = (int)temp.Result;
            return intSuccess == 1;
        }

        private void UpdateSPELStatus(int intEngagementid)
        {
            try
            {
                var spParams = new SpParameter[] {
             new SpParameter("@EngagementID", this.engagementID)
         };
                GetEngagementDbCommon().AddUpdateOrDelete("Dbo.SPELStatusupdate", spParams, true);

                // 
                spParams = new SpParameter[] {
              new SpParameter("@EngagementID", this.engagementID)
         };
                GetEngagementDbCommon(this.engagementID).AddUpdateOrDelete("Dbo.SPELStatusupdate", spParams, true);

            }
            catch (Exception)
            {
                // Log error
            }
        }
        private void JobStatus(int JobId, int JobStatus, int StatuCode, string StatuComment)
        {
            var spParams = new SpParameter[] {
         new SpParameter("@JobID", JobId),
         new SpParameter("@JobStatus", JobStatus),
         new SpParameter("@ErrorCode", StatuCode),
         new SpParameter("@AgentComment", StatuComment)
     };
            GetEngagementDbCommon().AddUpdateOrDelete("Dbo.Proc_CompleteJob", spParams, true);
        }

        private bool VeriWizard( string OCRValidation, int LogSeverity, int JobId, bool isMultiThreadingActive,string IsK1PreRuleValidationOn)
        {
            bool NewPreRule = false;
            try
            {
                currentStatusTable = GetStepDetail(this.engagementID, JobId);
                WriteDBLog($"VeriWizard-Job Started At , {DateTime.Now}", this.engagementID, 51, LogSeverity, 1, Severity.Low, TraceType.InfoLog);
                Console.WriteLine($"VeriWizard-Job Started At , {DateTime.Now}", this.engagementID, 51, LogSeverity, 1, Severity.Low, TraceType.InfoLog);

                if (blnsecondryconnection)
                {
                    if (!CheckStepStatus(1, 0, this.engagementID,ref currentStatusTable, JobId))
                    {
                       
                            var spParams = new SpParameter[1];
                            spParams[0] = new SpParameter("@EngagementID", this.engagementID);
                            GetEngagementDbCommon(this.engagementID).AddUpdateOrDelete("Dbo.Proc_UpdateSpengagementpages", spParams, true);
                        

                        WriteDBLogJobId($"Calling Update SPengagementPages At , {DateTime.Now}", this.engagementID, JobId, 1);
                        Console.WriteLine($"Calling Update SPengagementPages At , {DateTime.Now}", this.engagementID, JobId, 1);
                    }

                    if (!CheckStepStatus(2, 0, this.engagementID, ref currentStatusTable, JobId))
                    {
                        bool ExecuteUpdateEngagementOCRFieldAfterOCR;
                        
                            var SqlArt = new SqlParameter[1];
                            SqlArt[0] = new SqlParameter("@EngagementID", this.engagementID);
                            ExecuteUpdateEngagementOCRFieldAfterOCR = ExecuteDBSQLParameter("dbo.Proc_UpdateEngagementOCRFieldAfterOCR", true, SqlArt);
                        

                        if (ExecuteUpdateEngagementOCRFieldAfterOCR)
                        {
                            WriteDBLogJobId($"Calling UpdateEngagementOCRFieldAfterOCR At , {DateTime.Now}", this.engagementID, JobId, 2);
                            Console.WriteLine($"Calling UpdateEngagementOCRFieldAfterOCR At , {DateTime.Now}", this.engagementID, JobId, 2);
                        }
                        else
                        {
                           JobStatus(JobId,-1,11,"InError");
                            RetryJob(JobId);
                        }
                    }

                    if (!CheckStepStatus(3, 0, this.engagementID, ref currentStatusTable, JobId))
                    {
                        bool ExecuteAccountNoLogicUpdate;
                       
                            var sqlpara = new SqlParameter[1];
                            sqlpara[0] = new SqlParameter("@EngagementId", this.engagementID);
                            ExecuteAccountNoLogicUpdate = ExecuteDBSQLParameter("dbo.Proc_AccountNoLogicUpdate", true, sqlpara);
                        

                        if (ExecuteAccountNoLogicUpdate)
                        {
                            WriteDBLogJobId($"Calling procedure dbo.Proc_AccountNoLogicUpdate At , {DateTime.Now}", this.engagementID, JobId, 3);
                            Console.WriteLine($"Calling procedure dbo.Proc_AccountNoLogicUpdate At , {DateTime.Now}", this.engagementID, JobId, 3);
                        }
                        else
                        {
                            JobStatus(JobId, -1, 11, "InError");
                            RetryJob(JobId);
                        }
                    }
                }

                if (!CheckStepStatus(4, 0, this.engagementID, ref currentStatusTable, JobId))
                {
                    MuniBondLogic objTEW_MuniBond = new MuniBondLogic(this.engagementID);
                    objTEW_MuniBond.ProcessEngagementForMuniBond();
                    WriteDBLogJobId($"Calling Munibond Object At , {DateTime.Now}", this.engagementID, JobId, 4);
                    Console.WriteLine($"Calling Munibond Object At , {DateTime.Now}", this.engagementID, JobId, 4);
                }

                if (!CheckStepStatus(5, 0, this.engagementID, ref currentStatusTable, JobId))
                {
                    SetPriorData();
                    try
                    {
                        OcrValidateForWithoutLead(OCRValidation);
                    }
                    catch (Exception ex)
                    {
                        // Log exception
                    }
                    WriteDBLogJobId($"Calling OcrValidateForWithoutLead At , {DateTime.Now}", this.engagementID, JobId, 5);
                    Console.WriteLine($"Calling OcrValidateForWithoutLead At , {DateTime.Now}", this.engagementID, JobId, 5);
                }
                Class1040ScanChecker objchecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.PreVerification);

                FaxToTaxConversion objFaxToTaxConversion = new FaxToTaxConversion(ConfigConnectionKey, JobId);
                objchecker = objFaxToTaxConversion.GetData(HardCodeString, engagementID, isMultiThreadingActive,IsK1PreRuleValidationOn);

                Console.WriteLine( $"Checking for new Pre-Processing method At , {DateTime.Now}", 11211, 1, this.engagementID, TraceType.InfoLog);
                if (objchecker != null && objchecker.DDPNewPreprocess)
                {
                    IsDDP = true;
                    if (!CheckStepStatus(6, 0, this.engagementID, ref currentStatusTable, this.qobj.JobId))
                    {
                        //SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", $"Creating Single job for new Pre-Processing method At , {DateTime.Now}", 11212, 1, this.engagementID, TraceType.InfoLog);
                        QueueManager qm = new QueueManager();
                        qm.GenerateSubJob(qobj, 0, null);
                        qm = null;
                        //SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", $"SubJob Created for new Pre-Processing method At , {DateTime.Now}", 11213, 1, this.engagementID, TraceType.InfoLog);
                        //CommonCode.WriteDBLog($"SubJob Created for new Pre-Processing method At , {DateTime.Now}", this.engagementID, qobj.JobId); // Step-6
                    }
                    IsNewPreprocess = objchecker.DDPNewPreprocess;
                }
                //SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", $"Job Completed Successfully At , {DateTime.Now}", 11214, 1, this.engagementID, TraceType.InfoLog);
                return true;

                //if (objchecker != null && objchecker.DDPNewPreprocess)
                //{
                //    QueueManager qm = new QueueManager();
                //    Response response = qm.GenerateSubJob(qobj, 0, null);
                //    IsNewPreprocess = objchecker.DDPNewPreprocess;
                //}

                WriteDBLog($"VeriWizard-Job Completed Successfully At , {DateTime.Now}", this.engagementID, 57, LogSeverity, 1, Severity.Low, TraceType.InfoLog);
                Console.WriteLine($"VeriWizard-Job Completed Successfully At , {DateTime.Now}", this.engagementID, 57, LogSeverity, 1, Severity.Low, TraceType.InfoLog);
                return true;
            }
            catch (Exception ex)
            {
                JobStatus(JobId, -1, 11, "InError");
                RetryJob(JobId);
                return false;
            }
        }

      
private void SetPriorData()
        {
            string tmpstr;
            DataTable tmpdt;
            string tmpsql, tmpmainquery;

            
                SpParameter[] spParams = new SpParameter[1];
                spParams[0] = new SpParameter("EngagementID", this.engagementID);
                _dBComm = new DbCommon("ConnectionString");
                tmpdt = _dBComm.GetDataTable("dbo.Proc_GetFuncPriorYearReturn", spParams, "PriorYearReturn", true);
            

            tmpmainquery = "";
            tmpsql = "";

            switch (tmpdt.Rows.Count)
            {
                case > 0:
                    switch (Convert.IsDBNull(tmpdt.Rows[0].ItemArray[0]) ? 0 : tmpdt.Rows[0].ItemArray[0])
                    {
                        case 0:
                            break;
                        default:
                            string tmpconstr, tmpmessage;
                            int tmpengid;
                            SqlConnection tmpsqlcon;

                            for (int i = 0; i < tmpdt.Rows.Count; i++)
                            {
                                tmpconstr = $"uid={tmpdt.Rows[i]["dblogin"]};pwd={tmpdt.Rows[i]["dbpassword"]};database={tmpdt.Rows[i]["dbname"]};Connect TimeOut=0;server={tmpdt.Rows[i]["dbserver"]}";
                                tmpengid = (int)tmpdt.Rows[i]["EngagementID"];
                                tmpmessage = $"Prior Connection For {this.engagementID} : {i + 1} - {tmpdt.Rows[i]["dbserver"]} - {tmpdt.Rows[i]["dbname"]} - {tmpengid} , {DateTime.Now}";
                               

                                DataTable tmpdt1;

                                try
                                {

                                    tmpsqlcon = new SqlConnection(tmpconstr);

                                    tmpdt1 = GetRecords(tmpsqlcon, "dbo.Proc_GetPriorYearFaxFormData", tmpengid);

                                    SqlParameter[] sqlpara = new SqlParameter[2];
                                    sqlpara[0] = new SqlParameter("@EngagementID", this.engagementID);
                                    sqlpara[0].SqlDbType = SqlDbType.Int;
                                    sqlpara[1] = new SqlParameter("@tblTypeSPEngagementPYFaxFormData", tmpdt1);
                                    ExecuteDBSQLParameter("dbo.Proc_InsertBulkSPEngagementPYFaxFormData", true, sqlpara);

                                    tmpdt1 = null;
                                    tmpdt1 = GetRecords(tmpsqlcon, "dbo.Proc_GetPriorYearTaxFormData", tmpengid);

                                    sqlpara[1] = new SqlParameter();
                                    sqlpara[0] = new SqlParameter("@EngagementID", this.engagementID);
                                    sqlpara[0].SqlDbType = SqlDbType.Int;
                                    sqlpara[1] = new SqlParameter("@tblTypeSPEngagementPYTaxFormData", tmpdt1);

                                    ExecuteDBSQLParameter("dbo.Proc_InsertBulkSPEngagementPYTaxFormData", true, sqlpara);
                                    tmpdt1 = null;

                                }
                                catch (Exception ex)
                                {
                                   // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", $"Error Occured From PreVeriFication : {ex.Message} -> {ex} , {DateTime.Now}", 11051, 1, this.engagementID, TraceType.ErrorLog);
                                    ErrorDescriptions = ex.ToString();
                                }
                            }

                            bool ExecuteUpdatePriorYearFaxTaxFormData;
                          
                                SqlParameter[] SqlArt = new SqlParameter[1];
                                SqlArt[0] = new SqlParameter("@EngagementID", this.engagementID);
                            _dBComm = new DbCommon("ConnectionString");
                            ExecuteUpdatePriorYearFaxTaxFormData =  ExecuteDBSQLParameter("dbo.Proc_UpdPriorYearFaxTaxFormData",true, SqlArt);
                            

                            tmpmessage = ExecuteUpdatePriorYearFaxTaxFormData ? $"Update Prior FaxTaxFormData Successfully For {this.engagementID} , {DateTime.Now}" : $"Error Occured From Prior FaxTaxFormData Update Records For {this.engagementID} , {DateTime.Now}";
                            Console.WriteLine(tmpmessage);
                            tmpsqlcon = null;
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
       
public bool ExecuteDBSQLParameter(string Qrystr, bool IsProcedure = true, SqlParameter[] arrpara = null, bool Maincon = false)
        {
            switch (Maincon)
            {
                case true:
                    if (sqlconnmain.State == ConnectionState.Closed) sqlconnmain.Open();
                    break;
                default:
                    if (SqlConn.State == ConnectionState.Closed) SqlConn.Open();
                    break;
            }

            SqlCommand sqlcmd = new SqlCommand();

            try
            {
                switch (Maincon)
                {
                    case true:
                        sqlcmd.Connection = sqlconnmain;
                        break;
                    default:
                        sqlcmd.Connection = SqlConn;
                        break;
                }

                sqlcmd.CommandType = IsProcedure ? CommandType.StoredProcedure : CommandType.Text;
                sqlcmd.CommandTimeout = 0;
                sqlcmd.CommandText = Qrystr;

                if (arrpara != null)
                {
                    foreach (var param in arrpara)
                    {
                        sqlcmd.Parameters.Add(param);
                    }
                }

                try
                {
                    sqlcmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    ErrorDescriptions = ex.ToString();
                    throw;
                }

                return true;
            }
            catch (Exception ex)
            {
                ErrorDescriptions = ex.ToString();
                return false;
            }
            finally
            {
                switch (Maincon)
                {
                    case true:
                        sqlconnmain.Close();
                        break;
                    default:
                        SqlConn.Close();
                        break;
                }
                sqlcmd.Dispose();
            }
        }
        private DataTable GetRecords(SqlConnection objcon, string procname, int intEngagementID)
        {
            using (var command = new SqlCommand(procname, objcon))
            {
                try
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@EngagementID", SqlDbType.Int) { Value = intEngagementID });
                    command.CommandTimeout = 0;

                    var adapter = new SqlDataAdapter(command);
                    var dataset = new DataSet();
                    adapter.Fill(dataset);

                    return dataset.Tables[0];
                }
                catch (Exception ex)
                {
                    ErrorDescriptions = ex.ToString();
                    return null;
                }
                finally
                {
                    if (objcon.State == ConnectionState.Open)
                    {
                        objcon.Close();
                    }
                }
            }
        }

        public bool ExecuteDbQuery(string QryStr, bool maincon = false)
        {
            SqlConnection connection = maincon ? sqlconnmain : SqlConn;

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            using (var sqlcmd = new SqlCommand(QryStr, connection))
            {
                try
                {
                    sqlcmd.CommandTimeout = 0;
                    sqlcmd.CommandType = CommandType.Text;
                    sqlcmd.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    ErrorDescriptions = ex.ToString();
                    return false;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private void FillConnectionMain()
        {
            try
            {
                sqlconnmain = new SqlConnection();
                sqlconnmain.ConnectionString = ConMain;
            }
            catch (Exception ex)
            {
               // ErrorDescriptionns = ex.ToString();
            }
        }

        public void FillConnection()
        {
            try
            {
                SqlConn = new SqlConnection();
                SqlConn.ConnectionString = Constr;
            }
            catch (Exception ex)
            {
               // ErrorDescriptionns = ex.ToString();
            }
        }

        public ProcessLib()
        {
            strTime = DateTime.Now.ToString();
        }

        public ProcessLib(string _strWSPath)
        {
            strTime = DateTime.Now.ToString();
            strWSPath = _strWSPath;
        }

        public DataTable GetNewDataTable(string Qrystr, string Tablename, bool maincon = false)
        {
            DataTable dt = new DataTable();
            SqlDataAdapter da;

            try
            {
                if (maincon)
                {
                    if (sqlconnmain.State == ConnectionState.Closed) sqlconnmain.Open();
                    da = new SqlDataAdapter(Qrystr, sqlconnmain);
                }
                else
                {
                    if (SqlConn.State == ConnectionState.Closed) SqlConn.Open();
                    da = new SqlDataAdapter(Qrystr, SqlConn);
                }

                da.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
               // ErrorDescriptionns = ex.ToString();
            }
            finally
            {
                if (maincon)
                    sqlconnmain.Close();
                else
                    SqlConn.Close();
            }

            return dt;
        }

        public void OcrValidateForWithoutLead(string OCRValidation)
        {
            try
            {

                if (OCRValidation == "Y")
                {
                    var objReviewWizardValidations = new ReviewWizardValidations(ConfigConnectionKey);
                    objReviewWizardValidations.OcrValidationForWithoutLead(Convert.ToString(engagementID));
                }
            }
            catch (Exception ex)
            {
                // Log exception
            }
        }

        public void SetConfigValues(string Connection)
        {
            GenModule.ConnectionString = Connection;
        }

        public void WriteDBLogJobId(string strMessage, int engagementId, int jobId = 0, int stepId = 0, int subStepId = 0, bool ISSPEL = false)
        {
            try
            {
                if (stepId > 0)
                {
                    WriteDdpDBLog(strMessage, engagementId, jobId, stepId, subStepId, ISSPEL);
                    return;
                }

                
                    var spParams = new SpParameter[2];
                    spParams[0] = new SpParameter("EngagementID", engagementId);
                    spParams[1] = new SpParameter("strMessage", strMessage);
                    GetEngagementDbCommon(engagementId).GetData("Proc_InsertSPEngagementCurrentStatus", spParams, true);
               
            }
            catch (Exception ex)
            {
                // Log exception
            }
        }

        public void WriteDBLog(string strMessage, int engagementId, int EventId, int LogSeverity, int Priority, Severity Severity, TraceType TraceType, int jobId = 0, int stepId = 0, int subStepId = 0, bool ISSPEL = false)
        {
            try
            {
                if (stepId > 0)
                {
                    WriteDdpDBLog(strMessage, engagementId, jobId, stepId, subStepId, ISSPEL);
                    return;
                }

                if (LogSeverity == 1 && Severity == Severity.Low)
                {
                    DBWriteLogs(strMessage, engagementId, EventId, Priority, Severity, TraceType);
                }
                else if (LogSeverity == 2 && (Severity == Severity.Low || Severity == Severity.Medium))
                {
                    DBWriteLogs(strMessage, engagementId, EventId, Priority, Severity, TraceType);
                }
                else if (LogSeverity == 3)
                {
                    DBWriteLogs(strMessage, engagementId, EventId, Priority, Severity, TraceType);
                }
            }
            catch (Exception ex)
            {
                // Log exception
            }
        }

        private bool DBWriteLogs(string strMessage, int engagementId, int EventId, int Priority, Severity Severity, TraceType TraceType)
        {
           
                var spParams = new SpParameter[6];
                spParams[0] = new SpParameter("EngagementID", engagementId);
                spParams[1] = new SpParameter("strMessage", strMessage);
                spParams[2] = new SpParameter("EventId", EventId);
                spParams[3] = new SpParameter("Priority", Priority);
                spParams[4] = new SpParameter("Severity", Severity);
                spParams[5] = new SpParameter("TraceType", TraceType);
                return GetEngagementDbCommon(engagementId).GetData("Proc_InsertSPEngagementCurrentStatus", spParams, true) != null;
           
        }

        public bool WriteDdpDBLog(string strMessage, int engagementId, int jobId = 0, int stepId = 0, int subStepId = 0, bool ISSPEL = false)
        {
            try
            {
                    var spParams = new SpParameter[5];
                    spParams[0] = new SpParameter("EngagementID", engagementId);
                    spParams[1] = new SpParameter("Comments", strMessage);
                    spParams[2] = new SpParameter("JobId", jobId);
                    spParams[3] = new SpParameter("StepId", stepId);
                    spParams[4] = new SpParameter("SubStepId", subStepId);
                    return GetEngagementDbCommon(engagementId).GetData("dbo.Proc_InsertSPEngagementJobSteps", spParams, true) != null;
               
            }
            catch (Exception ex)
            {
                // Log exception
                return false;
            }
        }

        public DbCommon GetEngagementDbCommon(int engagementId)
        {
            return new DbCommon(engagementId, ConfigConnectionKey);
        }

        public DbCommon GetEngagementDbCommon()
        {
            return new DbCommon(ConfigConnectionKey);
        }

        public bool IgnoreCertificateErrorHandler(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public void PreDDP(int Engagementid)
        {
            FaxToTaxConversion objFaxToTaxConversion = new FaxToTaxConversion("ConnectionString", GenModule.JobId);
            objFaxToTaxConversion.GetPreCorrectionItem("", Engagementid, IsK1PreRuleValidationOn, true);
        }

        public void EvaluteFormula(int Engagementid)
        {
            // Logic for evaluating formula
        }

        public void Decimal_val(int engagementID)
        {
            try
            {
                var objFax2Tax = new FaxToTaxConversion(ConfigConnectionKey, GenModule.JobId);
                objFax2Tax.OCRDecimal(engagementID);
            }
            catch (Exception ex)
            {
                // Log exception
            }
        }
    }

}