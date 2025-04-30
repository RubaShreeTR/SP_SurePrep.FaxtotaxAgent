namespace WSSPELiteSupport
{
    using System;
    using System.Data.Common;
    using System.Data;
    using FaxToTaxDataValidations;
    using FaxToTaxLibrary;
    using System.Reflection;
    using System.Runtime.Intrinsics.X86;
    using FaxToTaxTemplateLib.QueueManagerClasses;

    public class SPELLightSupport
    {
        public int SPELDataConversion(int intEngagementid, int intUserID)
        {
            int intSPEliteEngagementid;
            int NoOfPages = 0;
            DataSet ds;
            int SubmissionType = 0;

            ds = GetEngagementInfo(intEngagementid);
            if (ds.Tables[3].Rows.Count > 0)
                SubmissionType = Convert.ToInt32(ds.Tables[3].Rows[0]["SubmissionType"]);
            else
                SubmissionType = 0;

            // IF Condition Added By Ajay On 25-12-2018 FOR MB-34089'
            WriteDBLog("Convert Binder SPELDataConversion step:1. Submissiontype: " + SubmissionType, intEngagementid);
            if (SubmissionType == 2)
            {
                // WriteDBLog("Convert Binder IsCBDBinder. Submissiontype: " + SubmissionType, intEngagementid);
                if (IsCBDBinder(intEngagementid))
                    InvokeJobforWithOutLeadAPIBinders(intEngagementid);
                return 1;
            }
            else
            {
                WriteDBLog("Convert Binder SPELDataConversion Else Condition step1. Submissiontype: " + SubmissionType, intEngagementid);
                intSPEliteEngagementid = CreateSPELiteEngagement(intEngagementid, intUserID);
                Console.WriteLine("Convert Binder SPELDataConversion Condition step 1 Completed");
                WriteDBLog("Convert Binder SPELDataConversion Else Condition step2. Submissiontype: " + SubmissionType, intEngagementid);
                UpdateCommonProcedure(intSPEliteEngagementid, intEngagementid, intUserID, ref NoOfPages);
                Console.WriteLine("Convert Binder SPELDataConversion Condition step 2 Completed");
                WriteDBLog("Convert Binder SPELDataConversion Else Condition step3. Submissiontype: " + SubmissionType, intEngagementid);
                UpdateStatusCommonProcedure(intSPEliteEngagementid, intEngagementid, NoOfPages, SubmissionType);    // 'Added for Faxtotax,etc. Flag update in Primary
                Console.WriteLine("Convert Binder SPELDataConversion Condition step 3 Completed");
                return intSPEliteEngagementid;
            }
        }

        internal int SPELDataConversion_CBD(string strClientNumber, int intTaxYear, int intDomainID, int intEngagementid, int intTaxSoftwareID, string strTaxsoftwareVersion, int intUserID, string strIsTemplate)
        {
            int intSPEliteEngagementid;
            int NoOfPages = 0;
            int SubmissionType = 0;

            WriteDBLog("Convert Binder SPELDataConversion Else Condition step1. Submissiontype: " + SubmissionType, intEngagementid);
            intSPEliteEngagementid = CreateSPELiteEngagement(intEngagementid, intUserID);
            WriteDBLog("Convert Binder SPELDataConversion Else Condition step2. Submissiontype: " + SubmissionType, intEngagementid);
            UpdateCommonProcedure(intSPEliteEngagementid, intEngagementid, intUserID, ref NoOfPages);
            WriteDBLog("Convert Binder SPELDataConversion Else Condition step3. Submissiontype: " + SubmissionType, intEngagementid);
            UpdateStatusCommonProcedure(intSPEliteEngagementid, intEngagementid, NoOfPages, SubmissionType);    // 'Added for Faxtotax,etc. Flag update in Primary
            return intSPEliteEngagementid;
        }


        internal int CreateSPELiteEngagement(int intEngagementid, int intUserID)
        {
            int intSPEliteEngagementid;
            DataSet ds;
            DataSet dsSPUser; // '  

            var spParams = new SpParameter[]
            {
                new SpParameter("@EngagementId", intEngagementid)
            };
            ds = CommonCode.GetEngagementDbCommon(intEngagementid).GetDataSet("dbo.Proc_CreateSPELiteEngagement", spParams, true);


            int intDomainID, intTaxYear, intOwnerID, intTaxSoftwareID, intDBConnectionID, intEngagementTypeID, intShowOrgPagesInVW, intShowUnidentifiedPagesInVW;
            string strClientNumber, strClientName, strDomainabbreviation, strClientFirstName, strClientlastname, strProsystemVersion, strUnlockpassword;
            int intScanPrintPageReOrder, intScanPrintOption, intAnywhereAccess, intDomainLocationID, intSPBinder, intDeleteOrganizerData;
            string strIsTemplate, strTemplateStatus, strTemplateName, IsGlobalfx, strGlobalfxVersion;
            string strUseCase = "";
            int intSurePrepSPVUserID = 0; // 'Added 'IsSSGDomain' for SSG requirement - Jyoti on 31-Jan-13
            int intOriginalOwnerID = 0;  // 'Added 'IsSSGDomain' for SSG requirement - Jyoti on 31-Jan-13 

            intDomainID = ds.Tables[0].Rows[0]["DomainID"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["DomainID"]);
            intTaxYear = ds.Tables[0].Rows[0]["TaxYear"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["TaxYear"]);
            strClientNumber = ds.Tables[0].Rows[0]["ClientNumber"] != DBNull.Value ? "" : ds.Tables[0].Rows[0]["ClientNumber"].ToString();
            intTaxSoftwareID = ds.Tables[0].Rows[0]["TaxSoftwareID"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["TaxSoftwareID"]);
            intDBConnectionID = ds.Tables[0].Rows[0]["DBConnectionID"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["DBConnectionID"]);
            intEngagementTypeID = ds.Tables[0].Rows[0]["EngagementTypeID"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["EngagementTypeID"]);
            strClientName = ds.Tables[0].Rows[0]["ClientName"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["ClientName"].ToString();
            strDomainabbreviation = ds.Tables[0].Rows[0]["Domainabbreviation"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["Domainabbreviation"].ToString();
            intOwnerID = ds.Tables[0].Rows[0]["OwnerID"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["OwnerID"]);
            strClientFirstName = ds.Tables[0].Rows[0]["ClientFirstName"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["ClientFirstName"].ToString();
            strClientlastname = ds.Tables[0].Rows[0]["Clientlastname"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["Clientlastname"].ToString();
            intScanPrintPageReOrder = ds.Tables[0].Rows[0]["ScanPrintPageReOrder"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["ScanPrintPageReOrder"]);
            intScanPrintOption = ds.Tables[0].Rows[0]["ScanPrintOption"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["ScanPrintOption"]);
            strProsystemVersion = ds.Tables[0].Rows[0]["ProsystemVersion"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["ProsystemVersion"].ToString();
            intAnywhereAccess = ds.Tables[0].Rows[0]["AnywhereAccess"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["AnywhereAccess"]);
            strUnlockpassword = ds.Tables[0].Rows[0]["Usepasswordforunlock"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["Usepasswordforunlock"].ToString();
            intDomainLocationID = ds.Tables[0].Rows[0]["DomainLocationID"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["DomainLocationID"]);
            intSPBinder = ds.Tables[0].Rows[0]["SPBinder"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["SPBinder"]);
            strIsTemplate = ds.Tables[0].Rows[0]["IsTemplate"] == DBNull.Value ? "N" : ds.Tables[0].Rows[0]["IsTemplate"].ToString();
            strTemplateStatus = ds.Tables[0].Rows[0]["TemplateStatus"] == DBNull.Value ? "N" : ds.Tables[0].Rows[0]["TemplateStatus"].ToString();
            strTemplateName = ds.Tables[0].Rows[0]["TemplateName"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["TemplateName"].ToString();
            strGlobalfxVersion = ds.Tables[0].Rows[0]["GlobalfxVersion"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["GlobalfxVersion"].ToString();
            IsGlobalfx = ds.Tables[0].Rows[0]["IsGlobalFx"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["IsGlobalFx"].ToString();
            strUseCase = ds.Tables[0].Rows[0]["UseCase"] == DBNull.Value ? "" : ds.Tables[0].Rows[0]["UseCase"].ToString();
            intDeleteOrganizerData = ds.Tables[0].Rows[0]["DeleteOrganizerData"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["DeleteOrganizerData"]);
            intShowOrgPagesInVW = ds.Tables[0].Rows[0]["ShowOrgPagesInVW"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["ShowOrgPagesInVW"]);

            if ((bool)ds.Tables[0].Rows[0]["ShowUnidentifiedPagesInVW"])
                intShowUnidentifiedPagesInVW = 1;
            else if ((bool)ds.Tables[0].Rows[0]["ShowUnidentifiedPagesInVW"])
                intShowUnidentifiedPagesInVW = 1;
            else
                intShowUnidentifiedPagesInVW = 0;


            // Insert Into Primary 
            strClientName = strClientName.Replace("'", "''").Replace("/", "' + Char(47) + '");
            strClientNumber = strClientNumber.Replace("'", "''").Replace("/", "' + Char(47) + '");
            strClientFirstName = strClientFirstName.Replace("'", "''").Replace("/", "' + Char(47) + '");
            strClientlastname = strClientlastname.Replace("'", "''").Replace("/", "' + Char(47) + '");

            int intLsMajorVersion = 1;
            if (intTaxYear == 2008 | intTaxYear == 2011)
                intLsMajorVersion = 2;

            intSPEliteEngagementid = InsertEngDetails(intEngagementid);

            intOriginalOwnerID = ds.Tables[0].Rows[0]["OriginalOwnerID"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[0]["OriginalOwnerID"]);  // 'Added for SSG requirement - Jyoti on 31-Jan-13


            spParams = new SpParameter[]
            {
                new SpParameter("@EngagementID", intEngagementid) 
            };           
            dsSPUser = CommonCode.GetEngagementDbCommon().GetDataSet("dbo.Proc_GetPlayerFromEngagement", spParams, true);

            intSurePrepSPVUserID = dsSPUser.Tables[0].Rows[0]["PlayerID"] == DBNull.Value ? 0 : Convert.ToInt32(dsSPUser.Tables[0].Rows[0]["PlayerID"]);

            if (intSurePrepSPVUserID > 0 && intOriginalOwnerID > 0)
            {
                spParams = new SpParameter[]
                {
                    new SpParameter("@Engagementid", intEngagementid),
                    new SpParameter("@OwnerID", intOriginalOwnerID)
                };
                CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("Proc_UpdateOwnerID", spParams, true);
            }
            dsSPUser.Clear();
            dsSPUser.Dispose();

            ds.Clear();
            ds.Dispose();

            return intSPEliteEngagementid;
        }

        public int InsertEngDetails(int intEngagementid)
        {
            int EngagementIDN = 0;
            var spParams = new SpParameter[]
            {
                new SpParameter("@EngagementIDO", intEngagementid),
                new SpParameter("@EngagementIDN", EngagementIDN,ParameterDirection.Output)
            };

            CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("MBApp.Proc_MBInsertBinderEngagement_V02", spParams, true,false,true);
            EngagementIDN=(int)spParams[1].ArgValue; 
            return EngagementIDN;
        }

        public bool UpdateCommonProcedure(int intSPELiteEngagementID, int intEngagementid, int intUserID, ref int NoOfPages)
        {

            var spParams = new SpParameter[]
            {
                new SpParameter("@EngagementID", intEngagementid),
                new SpParameter("@EngagementNewID", intSPELiteEngagementID),
                new SpParameter("@FinalNoOfPages", NoOfPages,ParameterDirection.Output)
            };
            CommonCode.GetEngagementDbCommon(intSPELiteEngagementID).AddUpdateOrDelete("MBApp.Proc_Convert1040ToSPBinder", spParams, true,false,true);
            NoOfPages = (int)spParams[2].ArgValue;

            spParams = new SpParameter[]
            {
                new SpParameter("@EngagementID", intEngagementid),
                new SpParameter("@EngagementNewID", intSPELiteEngagementID),
                new SpParameter("@FinalNoOfPages", NoOfPages,ParameterDirection.Output)
            };
            CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("MBApp.Proc_Convert1040ToSPBinder_Primary", spParams, true,false,true);

            return true;
        }


        public bool UpdateStatusCommonProcedure(int intSPELiteEngagementID, int intEngagementid, int NoOfPages, int SubmissionType = 0)
        {
            var spParams = new SpParameter[]
            {
                new SpParameter("@FromEngagementID", intEngagementid),
                new SpParameter("@EngagementID", intSPELiteEngagementID),
                new SpParameter("@FinalNoOfPages", NoOfPages)
            };
            CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("MBApp.Proc_MBUpdateEngagement", spParams, true);

            InvokeJobforWithOutLeadAPIBinders(intEngagementid);    // 'Added to invoke TDO Agent job : 171 - for API Binders after Binder generation for WithoutLead Eng (Pass Parent: EngType :8) - API-156 - Jyoti on 28-OCT-15--- (Pass Child: EngType :5) - 29-OCT-15
                                                                   // Code For Tax Chat Print Added By Ajay 10-01-2018 For JIRA : API-1163'
            if (IsTaxChatPrintEnabled(intEngagementid) & SubmissionType != 2)
            {
                // WriteDBLog("TaxChatPrint Job Triggered. Submissiontype: " + SubmissionType, intEngagementid);
                // GenerateJobsForTaxChatPrint(intEngagementid);
            }
            return true;
        }

        // 'Seperate function called for Agent Job : 171 invok call for API - WithOutLead Binder ---on 28-OCT-15
        public bool InvokeJobforWithOutLeadAPIBinders(int intEngagementID)
        {
            int toinvoke;
            var spParams = new SpParameter[]
            {
                new SpParameter("@EngagementID", intEngagementID)
            };
            toinvoke = CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("Select dbo.Func_IsWithOutLeadAPIBinder_V01(@engagementid)", spParams, false,true);
            if (Convert.ToBoolean(toinvoke) == true)
            {
                // WriteDBLog("171 job triggered", intEngagementID);
                GenerateJobs(intEngagementID);
            }
            return true;
        }

        public void GenerateJobs(int binderId)
        {
            QueueManager QM = new QueueManager();
            Response objResponse;
            Engagement engagement = new FaxToTaxTemplateLib.QueueManagerClasses.Engagement(binderId);
            objResponse = QM.GenerateJobs((QueueManager.Actionenum)35, (QueueManager.JoblinkWithenum)5, engagement);
        } 

        private bool IsTaxChatPrintEnabled(int EngagementId)
        {
            DataSet ds;
            bool isEnabled = false;

            try
            {
                var spParams = new SpParameter[]
                {
                    new SpParameter("@EngagementID", EngagementId)
                };
                ds = CommonCode.GetEngagementDbCommon().GetDataSet("dbo.Proc_GetTaxChatPrintFlag", spParams, true);

                if ((Convert.ToBoolean(ds.Tables[0].Rows[0]["AutomatePrint"])))
                {
                    isEnabled = true;
                }
            }
            catch (Exception ex)
            {
                isEnabled = false;
            }
            return isEnabled;
        }

        private void GenerateJobsForTaxChatPrint(int binderId)
        {
           QueueManager QM = new QueueManager();
            Response objResponse;
            try
            {
                Engagement engagement = new FaxToTaxTemplateLib.QueueManagerClasses.Engagement(binderId);
                objResponse = QM.GenerateJobs((QueueManager.Actionenum)52, (QueueManager.JoblinkWithenum) 5, engagement);
            }
            finally
            {
                QM = null/* TODO Change to default(_) if this is not a reference type */;
            }

        }

        private bool IsCBDBinder(int EngagementId)
        {
            int subType = 0;
            DataSet ds;
            bool result = false;

            var spParams = new SpParameter[]
            {
                new SpParameter("@EngagementID", EngagementId)
            };

            ds = CommonCode.GetEngagementDbCommon().GetDataSet("dbo.Proc_IsValidForCBDPrinting", spParams, true);
            if (ds != null)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {
                    bool isCBDCalled = Convert.ToBoolean(ds.Tables[0].Rows[0]["IsCBDCalled"]);
                    if (isCBDCalled == true)
                        return result;
                    subType = Convert.ToInt32(ds.Tables[0].Rows[0]["SubmissionType"]);
                    if (subType == 2)
                        result = true;
                }
            }
            return result;
        }


        public void WriteDBLog(string strMessage, int intEngagementID)
        {

            var spParams = new SpParameter[]
            {
                new SpParameter("@EngagementID", intEngagementID),
                new SpParameter("@CurrentStatus", strMessage)
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("MBRW.Proc_MBWriteDBLog", spParams, true);
        }

        public DataSet GetEngagementInfo(int intEngId)
        {
            DataSet ds;
            var spParams = new SpParameter[]
            {
                new SpParameter("@EngagementID", intEngId)
            };
            ds = CommonCode.GetEngagementDbCommon().GetDataSet("MBAPP.Proc_MBGetEngagementInfo_V02", spParams, true);
            ds.Tables[0].TableName = "PopulateEngagement";
            return ds;
        }
    }
}
