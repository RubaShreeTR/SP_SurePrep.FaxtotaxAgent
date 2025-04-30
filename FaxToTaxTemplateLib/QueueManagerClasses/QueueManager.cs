using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
namespace FaxToTaxTemplateLib.QueueManagerClasses
{
    [Serializable]
        public class QueueManager  : Common_SPError
        {
            public enum Actionenum
            {
                SUBMISSION = 1,
                RESUBMISSION = 2,
                PORTING = 3,
                MANUALEXPORT = 4,
                AUTOMATED = 5,
                IMPORTTR = 6,
                PRINT = 7,
                ADDIMAGES = 8,
                SUBMITTOCUSTOMEROUTSOURCED = 9,
                SCANSUBMISSION = 10,
                DASHBOARDPREPROCESSING = 11,
                DASHBOARDOCRRESUBMIT = 12,
                DEO = 13,
                WEBSERVICESUBMISSION = 14,
                WEBSERVICERESUBMISSION = 15,
                SUBMIT_TO_CUSTOMER_OUTSOURCED_GTL = 16,
                GTLPRINT = 17,
                RDC_OCR = 19,
                BUSINESSSUBMISSION = 20,
                BUSINESSRESUBMISSION = 21,
                DOCUMENTINFO = 22,
                SPVALIDATION = 23,
                GTLNOTIFICATION = 24
            }

            public enum AgentLocationenum
            {
                USSERVER = 1,
                MUMBAISERVICECENTER,
                AHMSERVICECENTER,
                ONSHORE
            }

            public enum Cancelenum
            {
                INERROR = -1,
                ALLJOBS,
                PROCESSNOTSTARTED,
                INPROCESS,
                ALLEXCEPTINERROR
            }
        public enum JoblinkWithenum
        {
            SPEXPRESS = 1,
            SCAN1040,
            FORMS,
            OCR,
            AGENT,
            FILEROOM,
            DASHBOARD,
            OTHERS
        }
        private SqlParameter sqlparam;

            public Response GenerateJobs(Actionenum Action, JoblinkWithenum JobLinked, Engagement objeng)
            {
                SPDatabase sPDatabase = new SPDatabase(SPDatabase.EnumConnection.Product10401041);
                Response response;
                try
                {
                    sPDatabase.Openconnection();
                    sPDatabase.InitializedCommand();
                    sPDatabase.SetProcedure("Proc_CreateEngagementJobs");
                    sqlparam = new SqlParameter("@EngagementID", objeng.EngagementID);
                    sqlparam.SqlDbType = SqlDbType.Int;
                    sPDatabase.AddSqlParameter(sqlparam);
                    sqlparam = new SqlParameter("@ActionID", Action);
                    sqlparam.SqlDbType = SqlDbType.Int;
                    sPDatabase.AddSqlParameter(sqlparam);
                    sqlparam = new SqlParameter("@JobLinkedWith", JobLinked);
                    sqlparam.SqlDbType = SqlDbType.Int;
                    sPDatabase.AddSqlParameter(sqlparam);
                    sqlparam = new SqlParameter("@@JobGroupID", 0);
                    sqlparam.SqlDbType = SqlDbType.Int;
                    sqlparam.Direction = ParameterDirection.Output;
                    sPDatabase.AddSqlParameter(sqlparam);
                    sPDatabase.ExecuteProcedure();
                    response = new Response(CommandStatusEnum.EXECUTEDSUCCESSFULLY);
                    response.JobGroupId = Convert.ToInt32(sPDatabase.GetParameterValue("@@JobGroupID"));
                    //QueueLog.MaintainLog(objeng.EngagementID, 0, 0, "GenerateJobs", "Jobs Generated Successfully", "", objeng.DomainAbbreviation);
                }
                catch (Exception ex)
                {
                    //QueueLog.MaintainErrorLog(objeng.EngagementID, 0, 0, "GenerateJobs", ex);
                    response = new Response(CommandStatusEnum.EXECUTIONFAIL);
                    response.S_ErrorNumber = ex.HResult;
                    response.S_ExceptionObject = ex;
                   
                }
                finally
                {
                    sPDatabase.Closeconnection();
                }

                return response;
            }
        public Response JobCompleted(Jobs objJob)
        {
            //IL_0040: Unknown result type (might be due to invalid IL or missing references)
            //IL_004a: Expected O, but got Unknown
            //IL_0075: Unknown result type (might be due to invalid IL or missing references)
            //IL_007f: Expected O, but got Unknown
            //IL_00aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b4: Expected O, but got Unknown
            //IL_012c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0136: Expected O, but got Unknown
            SPDatabase sPDatabase = new SPDatabase(SPDatabase.EnumConnection.Product10401041);
            DataSet dataSet = new DataSet();
            DataTable dataTable = new DataTable();
            Response response;
            try
            {
                sPDatabase.Openconnection();
                sPDatabase.InitializedCommand();
                sPDatabase.SetProcedure("Proc_CompleteJob");
                sqlparam = new SqlParameter("@JobID", objJob.JobId);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@JobStatus", objJob.JobStatus);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@ErrorCode", objJob.S_ErrorOccured);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                if (Information.IsNothing(objJob.AgentResponse))
                {
                    objJob.AgentResponse = "";
                }
                else if (objJob.AgentResponse.Length > 8000)
                {
                    objJob.AgentResponse = objJob.AgentResponse.Substring(1, 8000);
                }

                sqlparam = new SqlParameter("@AgentComment", objJob.AgentResponse);
                sqlparam.SqlDbType = SqlDbType.VarChar;
                sqlparam.Size = 8000;
                sPDatabase.AddSqlParameter(sqlparam);
                sPDatabase.DataSet_Procedure("CompleteJob");
                response = new Response(CommandStatusEnum.EXECUTEDSUCCESSFULLY);
                Jobs jobs = objJob;
                jobs = null;
            }
            catch (Exception ex)
            {
                Exception ex2 = ex;
                Jobs jobs2 = objJob;
                jobs2 = null;
                response = new Response(CommandStatusEnum.EXECUTIONFAIL);
                response.S_ErrorDescription = Information.Err().Description;
                response.S_ErrorNumber = Information.Err().Number;
                response.S_ExceptionObject = ex2;
                
                
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }

            return response;
        }
        public Response GenerateSubJob(Jobs objJob, int intSubJobID, ExtraData[] ObjExtraData)
        {
            //IL_0044: Unknown result type (might be due to invalid IL or missing references)
            //IL_004e: Expected O, but got Unknown
            //IL_0075: Unknown result type (might be due to invalid IL or missing references)
            //IL_007f: Expected O, but got Unknown
            //IL_00a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b0: Expected O, but got Unknown
            SPDatabase sPDatabase = new SPDatabase(SPDatabase.EnumConnection.Product10401041);
            DataSet dataSet = new DataSet();
            DataTable dataTable = new DataTable();
            Response response = default;
            try
            {
                sPDatabase.Openconnection();
                sPDatabase.InitializedCommand();
                sPDatabase.SetProcedure("Proc_CreateEngagementSubJob");
                sqlparam = new SqlParameter("@JobID", objJob.JobId);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@SubJobID", intSubJobID);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@@SubJobID", intSubJobID);
                sqlparam.Direction = ParameterDirection.Output;
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sPDatabase.ExecuteProcedure();
                intSubJobID = Conversions.ToInteger(sPDatabase.GetParameterValue("@@SubJobID"));
                if (!Information.IsNothing(ObjExtraData))
                {
                    int upperBound = ObjExtraData.GetUpperBound(0);
                    int num = 0;
                    while (true)
                    {
                        int num2 = num;
                        int num3 = upperBound;
                        if (num2 > num3)
                        {
                            break;
                        }

                        response = AddExtraData(0, objJob, intSubJobID, blnMainJob: false, ObjExtraData[num]);
                        if (response.ExecutionStatus == CommandStatusEnum.EXECUTIONFAIL)
                        {
                            break;
                        }

                        num = checked(num + 1);
                    }
                }

                sPDatabase.intExecuteNonQuery("Update spengagementSubjobs set  Active = 'Y' where subjobid= " + Conversions.ToString(intSubJobID));
                if (response.ExecutionStatus == CommandStatusEnum.EXECUTIONFAIL)
                {
                    return response;
                }

                response = new Response(CommandStatusEnum.EXECUTEDSUCCESSFULLY);
                Jobs jobs = objJob;
                jobs = null;
            }
            catch (Exception ex)
            {
                Exception ex2 = ex;
                Jobs jobs2 = objJob;
                jobs2 = null;
                response = new Response(CommandStatusEnum.EXECUTIONFAIL);
                response.S_ErrorDescription = Information.Err().Description;
                response.S_ErrorNumber = Information.Err().Number;
                response.S_ExceptionObject = ex2;
            }


            return response;
        }

        public Response AddExtraData(Actionenum Actionid, Jobs objJob, int intSubJobID, bool blnMainJob, ExtraData objExtraData)
        {
            //IL_0034: Unknown result type (might be due to invalid IL or missing references)
            //IL_003e: Expected O, but got Unknown
            //IL_0064: Unknown result type (might be due to invalid IL or missing references)
            //IL_006e: Expected O, but got Unknown
            //IL_0099: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a3: Expected O, but got Unknown
            //IL_00c9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d3: Expected O, but got Unknown
            //IL_00fe: Unknown result type (might be due to invalid IL or missing references)
            //IL_0108: Expected O, but got Unknown
            //IL_012f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0139: Expected O, but got Unknown
            //IL_0172: Unknown result type (might be due to invalid IL or missing references)
            //IL_017c: Expected O, but got Unknown
            //IL_01ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c4: Expected O, but got Unknown
            //IL_01f0: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fa: Expected O, but got Unknown
            //IL_0233: Unknown result type (might be due to invalid IL or missing references)
            //IL_023d: Expected O, but got Unknown
            //IL_0270: Unknown result type (might be due to invalid IL or missing references)
            //IL_027a: Expected O, but got Unknown
            SPDatabase sPDatabase = new SPDatabase(SPDatabase.EnumConnection.Product10401041);
            Response response;
            try
            {
                sPDatabase.Openconnection();
                sPDatabase.InitializedCommand();
                sPDatabase.SetProcedure("Proc_AddExtraData");
                sqlparam = new SqlParameter("@EngID", objJob.Engagementid);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@ActionID", Actionid);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@JobId", objJob.JobId);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@SubJobId", intSubJobID);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@GroupID", objJob.GroupId);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@DataKey", objExtraData.DataKey);
                sqlparam.SqlDbType = SqlDbType.VarChar;
                sqlparam.Size = 500;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@DataValue", objExtraData.DataValue);
                sqlparam.SqlDbType = SqlDbType.VarChar;
                sqlparam.Size = 500;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@DataType", objExtraData.DataType);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@FilterID", objExtraData.DataFilter);
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                string text = !blnMainJob ? "N" : "Y";
                sqlparam = new SqlParameter("@MainJob", text);
                sqlparam.SqlDbType = SqlDbType.VarChar;
                sqlparam.Size = 1;
                sPDatabase.AddSqlParameter(sqlparam);
                sqlparam = new SqlParameter("@@ExtraDataID", "0");
                sqlparam.Direction = ParameterDirection.Output;
                sqlparam.SqlDbType = SqlDbType.Int;
                sPDatabase.AddSqlParameter(sqlparam);
                sPDatabase.ExecuteProcedure();
                response = new Response(CommandStatusEnum.EXECUTEDSUCCESSFULLY);
                Jobs jobs = objJob;
                jobs = null;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                Jobs jobs2 = objJob;
                jobs2 = null;
                response = new Response(CommandStatusEnum.EXECUTIONFAIL);
                response.S_ErrorDescription = Information.Err().Description;
                response.S_ErrorNumber = Information.Err().Number;
                response.S_ExceptionObject = ex2;
                
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }

            return response;
        }
    }
    
}
