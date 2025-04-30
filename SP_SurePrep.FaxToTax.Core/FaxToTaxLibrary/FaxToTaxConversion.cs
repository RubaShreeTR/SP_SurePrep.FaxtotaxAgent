using Azure;
using FaxToTaxLibrary.Classes;
using FaxToTaxTemplateLib;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using FaxToTaxDataValidations;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System;
using System.Data.SqlTypes;
using Microsoft.Extensions.Configuration;
using FaxToTaxTemplateLib.QueueManagerClasses;
using static FaxToTaxTemplateLib.QueueManagerClasses.Common_SPError;


namespace FaxToTaxLibrary
{
    public class FaxToTaxConversion
    {
        //public FaxToTaxConversion(int domain_Id, int user_Id, string is_K1PreRuleValidationOn, int assignToBOT_User)
        //{
        //    DomainId = domain_Id;
        //    UserId = user_Id;
        //    IsK1PreRuleValidationOn = is_K1PreRuleValidationOn;
        //    AssignToBOTUser = assignToBOT_User;
        //}

        //public FaxToTaxConversion()
        //{
        //}

        private Collection OCRSuccArr = new Collection();
        private Collection OCRFailARR = new Collection();
        private bool blnIsRuleDefined;
        private string strTempSucc, strTempFail;
        private string strClientIP;
        private DataTable _dtBusinessDiagnostic;
        private int jobId;
        private int EngId;
        private DataTable currentStatusTable;

        private int DomainId { get; set; }
        private int UserId { get; set; }
       
        private int AssignToBOTUser { get; set; }
        DbCommon _dBComm;

        #region Enum

        public enum Operations
        {
            Unaffected = 1,
            Inserted = 2,
            Modified = 3,
            Deleted = 4
        }

        #endregion


        public Class1040ScanChecker GetData(string strGuid, int intEngagementID, bool isMultiThreadingActive,string IsK1PreRuleValidationOn)
        {
            Class1040ScanChecker.ActivityType enmPrevActivity = default(Class1040ScanChecker.ActivityType);
            Class1040ScanChecker.ActivityType enmActivity;
            int intLoop = 0;
            Class1040ScanChecker objChecker = null;
            int intRepeat = 0;

            DataRow[] drStepOption = null;

            DataSet DSWizardSteps = GetEngagementWizardSteps(strGuid, intEngagementID);

            while (intLoop < 20)
            {
                bool BlnWizardOption = true;
                enmActivity = (Class1040ScanChecker.ActivityType)CheckSteps(strGuid, intEngagementID);

                if (enmActivity == enmPrevActivity)
                {
                    intRepeat++;
                }
                else
                {
                    intRepeat = 0;
                }

                if (enmActivity != Class1040ScanChecker.ActivityType.PreVerification &&
                    enmActivity != Class1040ScanChecker.ActivityType.ProformaMaching
                    && enmPrevActivity == Class1040ScanChecker.ActivityType.ProformaMaching)
                {
                    DeleteSteplog(intEngagementID);
                }

                enmPrevActivity = enmActivity;

                if (intRepeat > 1)
                {
                    enmActivity = GetNextStep(enmActivity);
                }

                CommonCode.WriteDBLog($"CheckStep Value...{enmActivity}", 718, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine($"CheckStep Value...{enmActivity}");
                currentStatusTable = CommonCode.GetStepDetail(intEngagementID, jobId);

                switch (enmActivity)
                {
                    case Class1040ScanChecker.ActivityType.ProformaMaching:
                        UpdateProformadID(intEngagementID, isMultiThreadingActive);
                        break;

                    case Class1040ScanChecker.ActivityType.PreVerification:
                        objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.PreVerification);
                        objChecker = GetPreCorrectionItem(strGuid, intEngagementID, IsK1PreRuleValidationOn);
                        if (objChecker != null)
                        {
                            GetEngagementInfo(intEngagementID, ref objChecker);
                            DeleteSteplog(intEngagementID);
                            return objChecker;
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.PrimaryVerification:
                        objChecker = GetVerificationItem(intEngagementID, true);
                        if (objChecker.CountVerificationItems > 0)
                        {
                            GetEngagementInfo(intEngagementID, ref objChecker);
                            return objChecker;
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.NewVerification:
                        if (DSWizardSteps.Tables[0] != null && DSWizardSteps.Tables[0].Rows.Count > 0 &&
                            Convert.ToInt32(DSWizardSteps.Tables[0].Rows[0]["TaxYear"]) >= 2010)
                        {
                            drStepOption  = DSWizardSteps.Tables[0].Select($"StepID = " + (int) enmActivity);
                            if (drStepOption.Length > 0)
                                BlnWizardOption = drStepOption[0]["Enabled"] != DBNull.Value && Convert.ToBoolean(drStepOption[0]["Enabled"]);
                        }

                        if (!BlnWizardOption)
                        {
                            MarkUnCertainFields_Skip(intEngagementID);
                            EvaluateVerificationItem(intEngagementID);
                            UpdateVerification_Skip(intEngagementID);
                            CommonCode.WriteDBLog("Verification Wizard is skipped by user...", 719, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                            Console.WriteLine("Verification Wizard is skipped by user...");
                        }
                        else
                        {
                            objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.NewVerification);
                            GetEngagementInfo(intEngagementID, ref objChecker);
                            return objChecker;
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.Verficiation:
                        objChecker = GetVerificationItem(intEngagementID, false);
                        GetEngagementInfo(intEngagementID, ref objChecker);
                        return objChecker;

                    case Class1040ScanChecker.ActivityType.Correction:
                        objChecker = GetCorrectionItem(strGuid, intEngagementID);
                        if (objChecker.CountCorrectionItems > 0)
                        {
                            GetEngagementInfo(intEngagementID, ref objChecker);
                            return objChecker;
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.DeleteProforma:
                        UpdateProformadCompleted(intEngagementID);
                        break;

                    case Class1040ScanChecker.ActivityType.OCRToFax:
                        OCRToFax(intEngagementID);
                        break;

                    case Class1040ScanChecker.ActivityType.Diagnostic:
                        GenerateDiagnostics(intEngagementID);
                        break;

                    case Class1040ScanChecker.ActivityType.ParentAssociation:
                        if (DSWizardSteps.Tables[0] != null && DSWizardSteps.Tables[0].Rows.Count > 0 &&
                            Convert.ToInt32(DSWizardSteps.Tables[0].Rows[0]["TaxYear"]) >= 2010)
                        {
                            drStepOption = DSWizardSteps.Tables[0].Select($"StepID = " + (int) enmActivity);
                            if (drStepOption.Length > 0)
                                BlnWizardOption = drStepOption[0]["Enabled"] != DBNull.Value && Convert.ToBoolean(drStepOption[0]["Enabled"]);

                            if (!BlnWizardOption)
                            {
                                objChecker = GetParentAssociationItem_2011(intEngagementID);
                                if (objChecker.GetCollection().Count > 0 && objChecker.GetParentFormsCollection().Count > 0)
                                {
                                    GetEngagementInfo(intEngagementID, ref objChecker);
                                    UpdateParentAssociationItem_2011(strGuid, objChecker, true, isMultiThreadingActive,IsK1PreRuleValidationOn);
                                }
                                CommonCode.WriteDBLog("CFA Wizard is skipped by user...", 720, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                                Console.WriteLine("CFA Wizard is skipped by user...");
                            }
                            else
                            {
                                objChecker = GetParentAssociationItem_2011(intEngagementID);
                                if (objChecker.GetCollection().Count > 0 && objChecker.GetParentFormsCollection().Count > 0)
                                {
                                    GetEngagementInfo(intEngagementID, ref objChecker);
                                    return objChecker;
                                }
                            }
                        }
                        else
                        {
                            objChecker = GetParentAssociationItem(intEngagementID);
                            if (objChecker.GetCollection().Count > 0 && objChecker.GetParentFormsCollection().Count > 0)
                            {
                                GetEngagementInfo(intEngagementID, ref objChecker);
                                return objChecker;
                            }
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.SupercededDocuments:
                        if (DSWizardSteps != null && DSWizardSteps.Tables[0] != null &&
                            Convert.ToInt32(DSWizardSteps.Tables[0].Rows[0]["TaxYear"]) >= 2010 &&
                            DSWizardSteps.Tables[0].Rows.Count > 1)
                        {
                            drStepOption = DSWizardSteps.Tables[0].Select($"StepID = " + (int) enmActivity);
                            if (drStepOption.Length > 0)
                                BlnWizardOption = drStepOption[0]["Enabled"] != DBNull.Value && Convert.ToBoolean(drStepOption[0]["Enabled"]);
                        }

                        objChecker = GetSupercededDocuments(intEngagementID);
                        if (BlnWizardOption && objChecker.GetCollection().Count > 0)
                        {
                            GetEngagementInfo(intEngagementID, ref objChecker);
                            return objChecker;
                        }
                        else
                        {
                            if (objChecker.GetCollection().Count > 0)
                            {
                                for (int i = 0; i < objChecker.GetCollection().Count; i++)
                                {


                                    var spParams = new SpParameter[3];
                                    spParams[0] = new SpParameter("@EngagementID", objChecker.GetSupercededItem(i).EngagementID);
                                    spParams[1] = new SpParameter("@EngagementPageID", objChecker.GetSupercededItem(i).EngagementPageID);
                                    spParams[2] = new SpParameter("@EngagementFaxFormID", objChecker.GetSupercededItem(i).EngagementFaxFormID);
                                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_CreateSupercededForm", spParams, true, false);

                                }
                            }
                            UpdateSupercededCompleted(intEngagementID);
                            CommonCode.WriteDBLog("Superseded Wizard skipped by user..", 721, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                            Console.WriteLine("Superseded Wizard skipped by user..", intEngagementID);
                        }
                        break;
                    case Class1040ScanChecker.ActivityType.TaxExempt:
                        // LogEntry(CommonCode.Severity.Low, "DDPAgent", "TaxExempt", 941, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                        if (DSWizardSteps != null && DSWizardSteps.Tables[0] != null &&
                            Convert.ToInt32(DSWizardSteps.Tables[0].Rows[0]["TaxYear"]) >= 2010 &&
                            DSWizardSteps.Tables[0].Rows.Count > 1)
                        {
                            drStepOption = DSWizardSteps.Tables[0].Select($"StepID = " + (int)enmActivity);
                            if (drStepOption.Length > 0)
                                BlnWizardOption = drStepOption[0]["Enabled"] != DBNull.Value && Convert.ToBoolean(drStepOption[0]["Enabled"]);
                        }

                        if (BlnWizardOption)
                        {
                            if (!ResidentState(intEngagementID))
                            {
                                // Do nothing if found resident state
                                if (Convert.ToInt32(EngagementTaxYear(intEngagementID)) >= 2009)
                                {
                                    objChecker = GetTaxExemptData(intEngagementID);
                                }
                                else
                                {
                                    objChecker = GetTaxExemptInterestData(intEngagementID);
                                }

                                if (objChecker.DuplicateDataSet != null && objChecker.DuplicateDataSet.Tables[0].Rows.Count > 0)
                                {
                                    GetEngagementInfo(intEngagementID, ref objChecker);
                                    return objChecker;
                                }
                            }
                        }
                        else
                        {
                            objChecker = GetTaxExemptData(intEngagementID);
                            if (objChecker.DuplicateDataSet != null && objChecker.DuplicateDataSet.Tables[0].Rows.Count > 0)
                            {
                                UpdateTaxExemptData(objChecker.DuplicateDataSet, intEngagementID, true, 8);
                            }
                            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "TaxExempt Wizard skipped by user..", 722, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                            CommonCode.WriteDBLog("TaxExempt Wizard skipped by user..", 722, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                            Console.WriteLine("TaxExempt Wizard skipped by user..");
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.EvaluateFaxToTaxFormula:
                        // LogEntry(CommonCode.Severity.Low, "DDPAgent", "DDP Post verification EvaluateFaxToTaxFormula...", 723, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                        CommonCode.WriteDBLog("DDP Post verification EvaluateFaxToTaxFormula...", 724, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                        Console.WriteLine("DDP Post verification EvaluateFaxToTaxFormula...");
                        objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.EvaluateFaxToTaxFormula);

                        if (GetPreProcessDomainSetting(intEngagementID))
                        {
                            CommonCode.WriteDBLog("DDP Post verification job creation start...", 1036, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                            Console.WriteLine("DDP Post verification job creation start...");
                            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "DDP Post verification job creation start...", 1036, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                            var qm = new QueueManager();
                            var res = new FaxToTaxTemplateLib.QueueManagerClasses.Response(CommandStatusEnum.EXECUTEDSUCCESSFULLY);

                            Engagement engagement = new FaxToTaxTemplateLib.QueueManagerClasses.Engagement(intEngagementID);

                            res = qm.GenerateJobs((QueueManager.Actionenum)42, (QueueManager.JoblinkWithenum)1, engagement);
                            qm = null;

                            if (res.JobGroupId > 0)
                            {
                                GetEngagementInfo(intEngagementID, ref objChecker);
                                return objChecker;
                            }

                            CommonCode.WriteDBLog("DDP Post verification job created...", 725, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                              Console.WriteLine("DDP Post verification job created...");
                            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "DDP Post verification job created...", 725, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                        }
                        else
                        {
                            CommonCode.WriteDBLog("Process by old method...", 726, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                            Console.WriteLine("Process by old method...");
                            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "Process by old method...", 726, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                            EvaluateFaxToTaxFormula(intEngagementID, isMultiThreadingActive);
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.Duplicate:
                        // LogEntry(CommonCode.Severity.Low, "DDPAgent", "Duplicate", 942, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                        objChecker = GetDuplicateItem(intEngagementID, 0, Class1040ScanChecker.ActivityType.Duplicate, BlnWizardOption);
                        if (objChecker.GetCollection().Count > 0)
                        {
                            GetEngagementInfo(intEngagementID, ref objChecker);
                            return objChecker;
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.NewDuplicate:
                        // LogEntry(CommonCode.Severity.Low, "DDPAgent", "NewDuplicate", 943, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                        if (DSWizardSteps != null && DSWizardSteps.Tables[0] != null &&
                            Convert.ToInt32(DSWizardSteps.Tables[0].Rows[0]["TaxYear"]) >= 2010 &&
                            DSWizardSteps.Tables[0].Rows.Count > 1)
                        {
                            drStepOption = DSWizardSteps.Tables[0].Select($"StepID =" + (int)enmActivity);
                            if (drStepOption.Length > 0)
                                BlnWizardOption = drStepOption[0]["Enabled"] != DBNull.Value && Convert.ToBoolean(drStepOption[0]["Enabled"]);
                        }

                        objChecker = GetDuplicateItem(intEngagementID, 0, Class1040ScanChecker.ActivityType.NewDuplicate, BlnWizardOption);

                        if (BlnWizardOption &&
                            objChecker.DuplicateDataSet.Tables[0].Rows.Count > 0 &&
                            objChecker.DuplicateDataSet.Tables[1].Rows.Count > 0 &&
                            objChecker.DuplicateDataSet.Tables[2].Rows.Count > 0)
                        {
                            GetEngagementInfo(intEngagementID, ref objChecker);
                            return objChecker;
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.Fax2Tax:
                        // LogEntry(CommonCode.Severity.Low, "DDPAgent", "Fax2Tax", 944, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                        Fax2Tax1(intEngagementID);
                        break;

                    case Class1040ScanChecker.ActivityType.ProformaFormAssociation:
                        // LogEntry(CommonCode.Severity.Low, "DDPAgent", "ProformaFormAssociation", 945, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                        if (DSWizardSteps != null && DSWizardSteps.Tables[0] != null &&
                            Convert.ToInt32(DSWizardSteps.Tables[0].Rows[0]["TaxYear"]) >= 2010 &&
                            DSWizardSteps.Tables[0].Rows.Count > 1)
                        {
                            drStepOption = DSWizardSteps.Tables[0].Select($"StepID = " + (int)enmActivity);
                            if (drStepOption.Length > 0)
                                BlnWizardOption = drStepOption[0]["Enabled"] != DBNull.Value && Convert.ToBoolean(drStepOption[0]["Enabled"]);
                        }

                        if (BlnWizardOption)
                        {
                            objChecker = GetProformaAssociationItem(intEngagementID);
                            var strFaxFormID = new StringBuilder();
                            var strFormTypeID = new StringBuilder();
                            Array arrFormTypeID;
                            Array arrFaxFormID;
                            bool blnSkip = false;
                            bool blnFieldGroupExist = false;
                            bool blnPriorYearDocExist = false;
                            bool blnAutoMatchDocExist = false;
                            bool blnPriorYearMatchDocExist = false;
                            IEnumerable<Class1040ScanFaxForms> filteredData;

                            if (objChecker.GetCollection().Count > 0 && objChecker.GetProformaFormsCollection().Count > 0)
                            {
                                filteredData = objChecker.GetCollection()
                          .OfType<Class1040ScanFaxForms>()
                          .Where(item => item.EngagementFormID <= 0 ||
                             (!item.IsMultiInstance && item.EngagementFieldGroupID == 0));

                                foreach (Class1040ScanFaxForms fForm in filteredData)
                                {
                                    CommonCode.AppendString(Convert.ToString(fForm.FormTypeID), strFormTypeID);
                                }

                                if (!string.IsNullOrWhiteSpace(strFormTypeID.ToString()))
                                {
                                    arrFormTypeID = strFormTypeID.ToString().Split(',');
                                    for (int intCount = 0; intCount < arrFormTypeID.Length; intCount++)
                                    {
                                        int index = intCount;
                                        IEnumerable<Class1040ScanProformaForms> filteredProformaForm = objChecker.GetProformaFormsCollection().Where(item => item.FormTypeID ==
                                        (float)arrFormTypeID.GetValue(index) && item.EngagementFormID > 0);


                                        foreach (Class1040ScanProformaForms pForm in filteredProformaForm)
                                        {
                                            CommonCode.AppendString(Convert.ToString(pForm.EngagementFormID), strFaxFormID);
                                        }
                                    }

                                    if (!string.IsNullOrWhiteSpace(strFaxFormID.ToString()))
                                    {
                                        arrFaxFormID = strFaxFormID.ToString().Split(',');

                                        for (int intCount = 0; intCount < arrFaxFormID.Length; intCount++)
                                        {
                                            blnFieldGroupExist = objChecker.GetCollection()
                                                .OfType<Class1040ScanFaxForms>()
                                                .Where(item => item.IsMultiInstance && item.EngagementFieldGroupID == 0)
                                                .FirstOrDefault() != null;
                                        }

                                        if (string.IsNullOrEmpty(strFormTypeID.ToString()) && !blnFieldGroupExist)
                                        {
                                            blnSkip = true;
                                        }
                                        else
                                        {
                                            blnSkip = false;
                                        }
                                    }
                                    else
                                    {
                                        if (!blnFieldGroupExist)
                                        {
                                            blnSkip = true;
                                        }

                                    }

                                }
                                else
                                {
                                    if (!blnFieldGroupExist)
                                        blnSkip = true;
                                }
                            }

                            filteredData = objChecker.GetCollection()
   .OfType<Class1040ScanFaxForms>()
   .Where(item => item.AutoPageMatched == "Y" && item.EngagementFaxFormID > 0);

                            blnAutoMatchDocExist = filteredData.Any();

                            blnPriorYearDocExist = objChecker.GetCollection()
                               .OfType<Class1040ScanFaxForms>()
                               .Where(item => item.EngagementFaxFormID == 0)
                               .Any();

                            if (blnAutoMatchDocExist)
                            {
                                var filteredData1 = filteredData;

                                blnPriorYearMatchDocExist = filteredData
                                   .SelectMany(item => filteredData1, (item, item1) => new { item, item1 })
                                   .Where(pair => pair.item.EngagementFaxFormID == 0 && pair.item.FormTypeID == pair.item1.FormTypeID)
                                   .Any();
                            }


                            if (blnAutoMatchDocExist && !blnPriorYearMatchDocExist)
                            {
                                blnPriorYearDocExist = false;
                            }

                            if (!blnSkip || blnPriorYearDocExist)
                            {
                                if (objChecker.GetCollection().Count > 0 && objChecker.GetProformaFormsCollection().Count > 0)
                                {
                                    GetEngagementInfo(intEngagementID, ref objChecker);
                                    return objChecker;
                                }
                            }
                            else
                            {
                                UpdateProformaFormCompleted(intEngagementID);
                            }
                        }
                        else
                        {
                            UpdateProformaFormCompleted(intEngagementID);
                            CommonCode.WriteDBLog("NFR Wizard skipped by user..", 727, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                            Console.WriteLine("NFR Wizard skipped by user..");
                        }
                        break;

                    case Class1040ScanChecker.ActivityType.Fax2Tax2:
                        // LogEntry(CommonCode.Severity.Low, "DDPAgent", "Fax2Tax2", 947, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                        Fax2Tax2(intEngagementID);
                        break;

                    case Class1040ScanChecker.ActivityType.TaxParentAssociation:
                        // LogEntry(CommonCode.Severity.Low, "DDPAgent", "TaxParentAssociation", 948, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                        objChecker = GetTaxParentAssociationItem(intEngagementID);
                        if (objChecker.GetCollection().Count > 0 && objChecker.GetTaxParentCollection().Count > 0)
                        {
                            GetEngagementInfo(intEngagementID, ref objChecker);
                            return objChecker;
                        }
                        break;

                    default:
                        UpdateOnCompletion(intEngagementID);
                        objChecker = new Class1040ScanChecker();
                        GetEngagementInfo(intEngagementID, ref objChecker);
                        return objChecker;
                }

                objChecker = null;
                intLoop++;
            }

            if (objChecker == null)
            {
                objChecker = new Class1040ScanChecker();
            }
            GetEngagementInfo(intEngagementID, ref objChecker);
            //if (objChecker != null)
            //{
            //    if (objChecker.GetCollection().Count > 0)
            //    {
            //        GetEngagementInfo(intEngagementID, ref objChecker);
            //    }
            //}
            return objChecker;
        }
        public DataTable dtDecimalTable;
        public DataTable dtUncertainFields;

        public FaxToTaxConversion(string configConnectionKey)
        {
            CommonCode.ConfigConnectionKey = configConnectionKey;
        }

        public FaxToTaxConversion(string configConnectionKey, int jobId)
        {


            CommonCode.ConfigConnectionKey = configConnectionKey;
            //GenModule.configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            //CommonCode.ConfigConnectionKey = new DbCommon(configConnectionKey);
            this.jobId = jobId;
        }

        private static bool IsDBNull(object expression)
        {
            if (expression is DBNull)
                return true;

            if (expression == null || string.IsNullOrEmpty(expression.ToString()) || expression.Equals(""))
                return true;

            return false;
        }

        private bool IsDouble(string ocrValue)
        {
            ocrValue = ocrValue.Replace(",", "").Replace("(", "").Replace(")", "");
            return double.TryParse(ocrValue, out _);
        }

        private DataTable CreateBlankDataTable()
        {
            var dtDecimal = new DataTable();

            dtDecimal.Columns.Add(new DataColumn("EngagementOCRFieldID", typeof(int)));
            dtDecimal.Columns.Add(new DataColumn("EngagementID", typeof(string)));
            dtDecimal.Columns.Add(new DataColumn("OCRValue", typeof(string)));
            dtDecimal.Columns.Add(new DataColumn("UnCertainChar", typeof(string)));
            dtDecimal.Columns.Add(new DataColumn("Remark", typeof(string)));
            dtDecimal.Columns.Add(new DataColumn("IsDecimalFix", typeof(int)));
            dtDecimal.Columns.Add(new DataColumn("F2TComments", typeof(string)));

            dtUncertainFields = new DataTable();
            dtUncertainFields.Columns.Add(new DataColumn("EngagementOCRFieldID", typeof(int)));

            return dtDecimal;
        }

        public DataTable OCRDecimal(int intEngagementID)
        {
            dtDecimalTable = CreateBlankDataTable();
            var OCRFieldData_ds = Proc_GET_SPEngagementOCRField_DATA(intEngagementID);

            if (OCRFieldData_ds != null && OCRFieldData_ds.Tables.Count > 0 && OCRFieldData_ds.Tables[0].Rows.Count > 0)
            {
                var dtPages = OCRFieldData_ds.Tables[0].DefaultView.ToTable(true, "EngagementPageID");
                if (dtPages != null && dtPages.Rows.Count > 0)
                {
                    foreach (DataRow item in dtPages.Rows)
                    {
                        int ePageID = Convert.ToInt32(item["EngagementPageID"]);
                        int decimalNumberCount = 0;
                        int nonDecimalNumberCount = 0;

                        var drow = OCRFieldData_ds.Tables[0].Select($"EngagementPageID = {ePageID}");
                        if (drow != null && drow.Length > 0)
                        {
                            foreach (var val in drow)
                            {
                                if (!IsNumeric(val["OCRValue"]))
                                    continue;

                                if (IsDouble(val["OCRValue"].ToString()))
                                    decimalNumberCount++;
                                else
                                    nonDecimalNumberCount++;
                            }
                        }

                        if (decimalNumberCount > nonDecimalNumberCount && nonDecimalNumberCount > 0)
                        {
                            foreach (var val in drow)
                            {
                                if (!IsNumeric(val["OCRValue"]))
                                    continue;

                                if (!IsDouble(val["OCRValue"].ToString()))
                                    AddDecimalToOCRFieldValue(val, 1, 1, "Decimal Added");
                            }
                        }
                        else if (nonDecimalNumberCount > decimalNumberCount && decimalNumberCount > 0)
                        {
                            foreach (var val in drow)
                            {
                                if (!IsNumeric(val["OCRValue"]))
                                    continue;

                                if (IsDouble(val["OCRValue"].ToString()))
                                    RemoveDecimalFromOCRFieldValue(val, 1, 1, "Decimal Removed");
                            }
                        }
                        else if (decimalNumberCount > 0 && nonDecimalNumberCount > 0)
                        {
                            foreach (var val in drow)
                            {
                                if (!IsNumeric(val["OCRValue"]))
                                    continue;

                                MarkDecimalFromOCRFieldValue(val, 2);
                            }
                        }
                    }

                    if (dtDecimalTable.Rows.Count > 0)
                    {
                        Update_OCR_DECIMAL_UDT(intEngagementID, dtDecimalTable);
                    }
                }
            }

            return dtUncertainFields;
        }

        public void UpdateEvenPagesUncertainFields(int engagementID, DataTable dtUncertainFields)
        {
            try
            {
                var dtPages = dtUncertainFields.DefaultView.ToTable(true, "EngagementPageID");
                if (dtPages != null && dtPages.Rows.Count > 0)
                {
                    if (dtDecimalTable != null)
                    {
                        dtDecimalTable.Rows.Clear();
                    }
                    else
                    {
                        dtDecimalTable = CreateBlankDataTable();
                    }

                    foreach (DataRow item in dtPages.Rows)
                    {
                        int ePageID = Convert.ToInt32(item["EngagementPageID"]);
                        int decimalNumberCount = 0;
                        int nonDecimalNumberCount = 0;

                        var drow = dtUncertainFields.Select($"EngagementPageID = {ePageID}");
                        foreach (var drItem in drow)
                        {
                            if (Convert.ToString(drItem["AutoMatchedTL"]).ToUpper() == "Y" || Convert.ToString(drItem["AutoVerified"]).ToUpper() == "Y")
                            {
                                if (!IsNumeric(drItem["OCRValue"]))
                                    continue;

                                if (IsDouble(drItem["OCRValue"].ToString()))
                                    decimalNumberCount++;
                                else
                                    nonDecimalNumberCount++;
                            }
                        }

                        if (decimalNumberCount > 0 && nonDecimalNumberCount > 0)
                        {
                            foreach (var row in drow)
                            {
                                if ((Convert.ToString(row["AutoMatchedTL"]).ToUpper() == "N" || string.IsNullOrEmpty(Convert.ToString(row["AutoMatchedTL"]))) &&
                                    (Convert.ToString(row["AutoVerified"]).ToUpper() == "N" || string.IsNullOrEmpty(Convert.ToString(row["AutoVerified"]))))
                                {
                                    if (!IsNumeric(row["OCRValue"]))
                                        continue;

                                    var dr = dtDecimalTable.NewRow();
                                    dr["EngagementOCRFieldID"] = row["EngagementOCRFieldID"];
                                    dr["EngagementID"] = row["EngagementID"];
                                    dr["OCRValue"] = row["OCRValue"].ToString().Replace(",", "");
                                    dr["UnCertainChar"] = 1;
                                    dr["Remark"] = "Proc_UpdateDecimalOCRValues_V01";
                                    dr["IsDecimalFix"] = 2;
                                    dr["F2TComments"] += " AutoMatchTL Filed Mark as Only Uncertain";
                                    dtDecimalTable.Rows.Add(dr);
                                    dtDecimalTable.AcceptChanges();
                                }
                            }
                        }
                        else if (decimalNumberCount > 0)
                        {
                            foreach (var drItem in drow)
                            {
                                if (!IsNumeric(drItem["OCRValue"]))
                                    continue;

                                if ((Convert.ToString(drItem["AutoMatchedTL"]).ToUpper() == "N" || string.IsNullOrEmpty(Convert.ToString(drItem["AutoMatchedTL"]))) &&
                                    (Convert.ToString(drItem["AutoVerified"]).ToUpper() == "N" || string.IsNullOrEmpty(Convert.ToString(drItem["AutoVerified"]))) &&
                                    !IsDouble(drItem["OCRValue"].ToString()))
                                {
                                    AddDecimalToOCRFieldValue(drItem, 2, 1, "AutoMatchedTL Decimal Added");
                                }
                            }
                        }
                        else if (nonDecimalNumberCount > 0)
                        {
                            foreach (var drItem in drow)
                            {
                                if (!IsNumeric(drItem["OCRValue"]))
                                    continue;

                                if ((Convert.ToString(drItem["AutoMatchedTL"]).ToUpper() == "N" || string.IsNullOrEmpty(Convert.ToString(drItem["AutoMatchedTL"]))) &&
                                    (Convert.ToString(drItem["AutoVerified"]).ToUpper() == "N" || string.IsNullOrEmpty(Convert.ToString(drItem["AutoVerified"]))) &&
                                    IsDouble(drItem["OCRValue"].ToString()))
                                {
                                    RemoveDecimalFromOCRFieldValue(drItem, 2, 1, "AutoMatchedTL Decimal Removed");
                                }
                            }
                        }
                        else
                        {
                            foreach (var row in drow)
                            {
                                if (!IsNumeric(row["OCRValue"]))
                                    continue;

                                var dr = dtDecimalTable.NewRow();
                                dr["EngagementOCRFieldID"] = row["EngagementOCRFieldID"];
                                dr["EngagementID"] = row["EngagementID"];
                                dr["OCRValue"] = IsDouble(row["OCRValue"].ToString().Replace(",", ""))
                                    ? Decimal.Round(Convert.ToDecimal(row["OCRValue"].ToString().Replace(",", "")))
                                    : row["OCRValue"].ToString().Replace(",", "");
                                dr["UnCertainChar"] = 1;
                                dr["Remark"] = "Proc_UpdateDecimalOCRValues_V01";
                                dr["IsDecimalFix"] = 2;
                                dr["F2TComments"] += " AutoMatchTL Filed Mark as Only Uncertain";
                                dtDecimalTable.Rows.Add(dr);
                                dtDecimalTable.AcceptChanges();
                            }
                        }
                    }
                }

                if (dtDecimalTable.Rows.Count > 0)
                {
                    Update_OCR_DECIMAL_UDT(engagementID, dtDecimalTable);
                }
            }
            catch (Exception ex)
            {
                // Log the exception
            }
        }

        private bool IsNumeric(object value)
        {
            return double.TryParse(Convert.ToString(value), out _);
        }


        public bool Update_OCR_DECIMAL_UDT(int engagementId, DataTable updateocrudt)
        {
            // Log entry for starting the update

            var spParams = new SpParameter[2];
            spParams[0] = new SpParameter("@EngagementID", engagementId);
            spParams[1] = new SpParameter("@UDT_OCRValues", updateocrudt);
            CommonCode.GetEngagementDbCommon(engagementId).AddUpdateOrDelete("MBApp.Proc_UpdateDecimalOCRValues_V01", spParams, true, false);


            // Log entry for completing the update
            return true;
        }

        public DataSet Update_OCRDecimalUncertain(int engagementId)
        {
            DataSet dsSteps;


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", engagementId);
            dsSteps = CommonCode.GetEngagementDbCommon(engagementId).GetDataSet("[MBApp].[Proc_UpdateDecimalOCRValuesUncertain]", spParams, true);


            dsSteps.Tables[0].TableName = "UncertainFields";
            return dsSteps;
        }

        private void AddDecimalToOCRFieldValue(DataRow row, int isDecimalFix, int isUncertain, string comment)
        {
            try
            {
                var dr = dtDecimalTable.NewRow();
                dr["EngagementOCRFieldID"] = row["EngagementOCRFieldID"];
                dr["EngagementID"] = row["EngagementID"];
                string ocrValue = row["OCRValue"].ToString().Replace(",", "");

                try
                {
                    if (ocrValue.Length > 2 && !ocrValue.Contains("."))
                    {
                        ocrValue = ocrValue.Insert(ocrValue.Length - 2, ".");
                    }
                    else if (ocrValue.Length <= 2)
                    {
                        ocrValue = ocrValue.Insert(0, ".");
                    }
                    ocrValue = Math.Round(Convert.ToDecimal(ocrValue)).ToString();
                }
                catch
                {
                    ocrValue = "0";
                }

                dr["OCRValue"] = ocrValue;
                dr["UnCertainChar"] = isUncertain;
                dr["Remark"] = "Proc_UpdateDecimalOCRValues_V01";
                dr["IsDecimalFix"] = isDecimalFix;
                dr["F2TComments"] += dr["F2TComments"].ToString().Contains(comment) ? "" : comment;
                dtDecimalTable.Rows.Add(dr);
                dtDecimalTable.AcceptChanges();
            }
            catch (Exception ex)
            {
                // Log the exception
            }
        }

        private void RemoveDecimalFromOCRFieldValue(DataRow row, int isDecimalFix, int isUncertain, string comment)
        {
            var dr = dtDecimalTable.NewRow();
            dr["EngagementOCRFieldID"] = row["EngagementOCRFieldID"];
            dr["EngagementID"] = row["EngagementID"];
            string ocrValue = row["OCRValue"].ToString().Replace(",", "");
            dr["OCRValue"] = ocrValue.Replace(".", "");
            dr["UnCertainChar"] = isUncertain;
            dr["Remark"] = "Proc_UpdateDecimalOCRValues_V01";
            dr["IsDecimalFix"] = isDecimalFix;
            dr["F2TComments"] += dr["F2TComments"].ToString().Contains(comment) ? "" : comment;

            dtDecimalTable.Rows.Add(dr);
            dtDecimalTable.AcceptChanges();
        }

        private void MarkDecimalFromOCRFieldValue(DataRow row, int isDecimalFix)
        {
            string comment = " No Action from Decimal Rule";
            var dr = dtDecimalTable.NewRow();
            dr["EngagementOCRFieldID"] = row["EngagementOCRFieldID"];
            dr["EngagementID"] = row["EngagementID"];
            dr["OCRValue"] = row["OCRValue"].ToString().Replace(",", "");
            dr["UnCertainChar"] = 0;
            dr["Remark"] = "Proc_UpdateDecimalOCRValues_V01";
            dr["IsDecimalFix"] = isDecimalFix;
            dr["F2TComments"] += dr["F2TComments"].ToString().Contains(comment) ? "" : comment;

            dtDecimalTable.Rows.Add(dr);
            dtDecimalTable.AcceptChanges();
        }

        public DataSet Proc_GET_SPEngagementOCRField_DATA(int intEngagementID)
        {

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            return CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GET_SPEngagementOCRField_DATA", spParams, true);

        }

        private bool UpdatePrimaryVerificationCompleted(int intEngagementID)
        {

            var spParams = new SpParameter[2];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            spParams[1] = new SpParameter("@StatusUpdateFor", "PrimaryVerificationCompleted");
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementsStatus", spParams, true, false);

            spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_AfterPrimarySecondaryVerification_V01", spParams, true, false);

            return true;
        }

        private void DeleteSteplog(int intEngagementID)
        {
            int jobID = GetJobID(intEngagementID);

            var spParams = new SpParameter[2];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            spParams[1] = new SpParameter("@JobID", jobID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("MBAPP.Proc_DeleteJobIDFaxToTax", spParams, true, false);

        }

        //private int GetJobID(int intEngagementID)
        //{
        //    int jobID;

        //    var spParams = new SpParameter[1];
        //    spParams[0] = new SpParameter("@DestinationEngagementID", intEngagementID);
        //    jobID = (int)(CommonCode.GetEngagementDbCommon().GetData("MBAPP.Proc_GetJobIDFaxToTax", spParams, true));
        //    return jobID;
        //}



        public int GetJobID(int intEngagementID)
        {
            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@DestinationEngagementID", intEngagementID);

            using (var connection = CommonCode.GetEngagementDbCommon().GetConnection())
            {
                //connection.Open(); // Ensure the connection is open

                using (var command = new SqlCommand("MBAPP.Proc_GetJobIDFaxToTax", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@DestinationEngagementID", intEngagementID));

                    var result = command.ExecuteScalar();

                    if (result is int jobID)
                    {
                        return jobID;
                    }
                    else if (result is IConvertible convertible)
                    {
                        return convertible.ToInt32(null);
                    }
                    else if (result is Func<object> func)
                    {
                        var actualResult = func();
                        if (actualResult is int jobIDFromFunc)
                        {
                            return jobIDFromFunc;
                        }
                        else if (actualResult is IConvertible convertibleFromFunc)
                        {
                            return convertibleFromFunc.ToInt32(null);
                        }
                        else
                        {
                            throw new InvalidCastException($"Unable to cast object of type '{actualResult.GetType().FullName}' to type 'int'.");
                        }
                    }
                    else
                    {
                        throw new InvalidCastException($"Unable to cast object of type '{result.GetType().FullName}' to type 'int'.");
                    }
                }
            }
        }
        //public int CheckSteps(string strGuid, int intEngagementID)
        //{
        //    Class1040ScanChecker.ActivityType enmActivity;


        //        var spParams = new SpParameter[1];
        //        spParams[0] = new SpParameter("@intEngagementID", intEngagementID);
        //        enmActivity = (Class1040ScanChecker.ActivityType)CommonCode.GetEngagementDbCommon(intEngagementID).GetData("dbo.Proc_GetFuncCheckSteps", spParams, true);


        //    return (int)enmActivity;
        //}
        public int CheckSteps(string strGuid, int intEngagementID)
        {
            //LogEntry(CommonCode.Severity.Low, "DDPAgent", "CheckSteps started for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 959, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            Class1040ScanChecker.ActivityType enmActivity = default(Class1040ScanChecker.ActivityType);
            SpParameter[] spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@intEngagementID", intEngagementID);
            //var result = CommonCode.GetEngagementDbCommon(intEngagementID).GetData("dbo.Proc_GetFuncCheckSteps", spParams, true);
            using (var connection = CommonCode.GetEngagementDbCommon(intEngagementID).GetConnection())
            {
               using (var command = new SqlCommand("dbo.Proc_GetFuncCheckSteps", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    foreach (var spParam in spParams)
                    {
                        command.Parameters.Add(ConvertToSqlParameter(spParam));
                    }

                    // command.Parameters.AddRange(spParams);

                    var result = command.ExecuteScalar();
                    enmActivity = (Class1040ScanChecker.ActivityType)result;
                    
                }
            }
            return (int)enmActivity; 
        }

        public SqlParameter ConvertToSqlParameter(SpParameter spParam)
        {
            return new SqlParameter(spParam.Arg, spParam.ArgValue);
        }

        private Class1040ScanChecker GetVerificationItem(int intEngagementID, bool blnIsPrimaryVerification)
        {
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "GetVerificationItem started for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 960, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            DataSet ds;
            DataTable dt, dtpage = null;
            Class1040ScanVerificationItem objVerificationItem;
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.Verficiation);
            bool blnPayerNameExist = false;
            RWReference objPageRef;

            var strpage = new StringBuilder();
            var dsReviewWizard = new DataSet(); // Added by Ritesh on 14-06-2011
            string strOCRFieldID = ""; // Added by Ritesh on 14-06-2011

            // New variables added
            string strProcName = string.Empty;
            SpParameter[] spParams = new SpParameter[1];
            CommonCode.WriteDBLog("Retrieving Data For Verification...", 728, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Retrieving Data For Verification..."); // Added for debugging
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "Retrieving Data For Verification...", 728, 1, intEngagementID, CommonCode.TraceType.InfoLog);


            strProcName = blnIsPrimaryVerification ? "Proc_GetOCRPrimaryVerificationData" : "Proc_GetOCRVerificationData";
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet(strProcName, spParams, true);


            ds.Tables[0].TableName = "VerificationData";
            dt = ds.Tables["VerificationData"];

            // Added & Commented by Ritesh on 14-06-2011
            dsReviewWizard = ds;
            if (dt != null && blnIsPrimaryVerification && dt.Rows.Count > 0)
            {
                DataView dv = dt.DefaultView;
                int intTempID = 0;

                dv.RowFilter = "FaxDWPCode = 'FAX.CON.003'";
                if (dv.Count == 1)
                {
                    int uncertainVal = IsDBNull(dv[0]["UnCertainChar"]) ? 0 : Convert.ToInt32(dv[0]["UnCertainChar"]);
                    if (uncertainVal == 0)
                    {
                        intTempID = Convert.ToInt32(dv[0]["EngagementOCRFieldID"]);
                        for (int j = 0; j < ds.Tables[0].Rows.Count; j++)
                        {
                            if (Convert.ToInt32(ds.Tables[0].Rows[j]["EngagementOCRFieldID"]) == intTempID)
                            {
                                UpdateSingleAccNoField(intEngagementID,
                                    Convert.ToInt32(ds.Tables[0].Rows[j]["OCRTemplateID"]),
                                    ds.Tables[0].Rows[j]["FAXDWPCode"].ToString(),
                                    ds.Tables[0].Rows[j]["OCRValue"].ToString());
                                ds.Tables[0].Rows.RemoveAt(j);
                                dt = ds.Tables[0];
                                dsReviewWizard = null;
                                dsReviewWizard = ds;
                                break;
                            }
                        }
                    }
                }
                else if (dv.Count > 1)
                {
                    RemoveSingleAccNoField(dv, "FAX.CON.003", ref ds, ref dsReviewWizard, ref dt);
                }

                dv.RowFilter = null;

                // Repeat similar logic for "FAX.CON.186" and "FAX.GRT.089"
                // ...

                dv.Dispose();
                dv = null;

                SetChecker(blnIsPrimaryVerification, intEngagementID, ref strOCRFieldID, ref dsReviewWizard, ref objChecker);
            }

            if (!string.IsNullOrEmpty(strOCRFieldID))
            {
                string[] arrID = strOCRFieldID.Split(',');
                foreach (var id in arrID)
                {
                    for (int j = 0; j < dsReviewWizard.Tables[0].Rows.Count; j++)
                    {
                        if (dsReviewWizard.Tables[0].Rows[j]["EngagementOCRFieldID"].ToString() == id)
                        {
                            dsReviewWizard.Tables[0].Rows.RemoveAt(j);
                            break;
                        }
                    }
                }
            }

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    CommonCode.AppendString(row["engagementpageid"].ToString(), strpage);
                }

                CommonCode.WriteDBLog("Retrieving Data For Page Referencing...", 729, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);


                spParams = new SpParameter[3];
                spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                spParams[1] = new SpParameter("@ForData", 3);
                spParams[2] = new SpParameter("@EngagementPageID", strpage.ToString());
                ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_PageGetShowAll", spParams, true);


                ds.Tables[0].TableName = "PageFaxForms";
                dtpage = ds.Tables["PageFaxForms"];
            }

            foreach (DataRow row in dt.Rows)
            {
                objVerificationItem = new Class1040ScanVerificationItem(
                    Convert.ToInt32(row["EngagementID"]),
                    Convert.ToInt32(row["EngagementOCRFieldID"]),
                    Convert.ToInt32(row["EngagementPageID"]),
                    Convert.ToInt32(CommonCode.ReplaceNull(row["OCRTop"], "0")),
                    Convert.ToInt32(CommonCode.ReplaceNull(row["OCRLeft"], "0")),
                    Convert.ToInt32(CommonCode.ReplaceNull(row["OCRRight"], "0")),
                    Convert.ToInt32(CommonCode.ReplaceNull(row["OCRBottom"], "0")),
                    Convert.ToString(CommonCode.ReplaceNull(row["OCRValue"], "0")),
                    Convert.ToString(CommonCode.ReplaceNull(row["OCRVerifiedValue"], "0")),
                    false,
                    null,
                    Convert.ToString(CommonCode.ReplaceNull(row["FileName"], "")),
                    Convert.ToString(CommonCode.ReplaceNull(row["FaxFormShortName"], "")),
                    Convert.ToString(CommonCode.ReplaceNull(row["FaxFieldName"], "")),
                    false,
                    0,
                    Convert.ToInt32(row["DataType"]),
                    Convert.ToInt32(row["ClientPageDPI"]),
                    Convert.ToString(row["FileType"])
                );

                objVerificationItem.OCRTemplateID = Convert.ToInt32(row["OCRTemplateID"]);
                objVerificationItem.FaxDWPCode = row["FaxDWPCode"].ToString();

                if (row["FaxDWPCode"].ToString() == "FAX.CON.001" || row["FaxDWPCode"].ToString() == "FAX.GRT.001")
                {
                    blnPayerNameExist = true;
                }

                if (dtpage != null && dtpage.Rows.Count > 0)
                {
                    DataRow[] drow = dtpage.Select($"Engagementpageid={row["EngagementPageID"]} and Faxdwpcode='{row["faxdwpcode"]}'");
                    if (drow != null && drow.Length > 0)
                    {
                        foreach (var pageRow in drow)
                        {
                            objPageRef = new RWReference(
                                Convert.ToInt32(pageRow["EngagementPageID"]),
                                pageRow["Fieldvalue"].ToString(),
                                Convert.ToInt32(pageRow["FFX"]),
                                Convert.ToInt32(pageRow["FFY"]),
                                Convert.ToInt32(pageRow["FFHeight"]),
                                Convert.ToInt32(pageRow["FFwidth"]),
                                pageRow["FaxDwpcode"].ToString(),
                                IsDBNull(pageRow["Faxrownumber"]) ? 0 : Convert.ToInt32(pageRow["Faxrownumber"]),
                                Convert.ToInt32(pageRow["DataType"]),
                                Convert.ToInt32(pageRow["Faxformid"]),
                                Convert.ToInt32(pageRow["Faxformfieldid"]),
                                pageRow["faxformName"].ToString(),
                                pageRow["faxFieldname"].ToString(),
                                pageRow["Engformname"].ToString(),
                                Convert.ToInt32(pageRow["EngagementfaxformId"]),
                                Convert.ToInt32(pageRow["TaxFormInstanceNo"]),
                                Convert.ToInt32(pageRow["Uncertainchar"])
                            );
                            objVerificationItem.AddPageReference(objPageRef);
                        }
                    }
                }

                objChecker.AddVerficationItem(objVerificationItem);
            }

            dt = null;
            ds = null;

            if (blnIsPrimaryVerification && objChecker.CountVerificationItems <= 0)
            {
                UpdatePrimaryVerificationCompleted(intEngagementID);
            }

            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "GetVerificationItem completed for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 961, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            return objChecker;
        }

        //        private Class1040ScanChecker GetCorrectionItem(string strGuid, int intEngagementID)
        //        {
        //            DataSet ds;
        //            DataTable dt;

        //            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.Correction);
        //            bool blnNeedsCorrection;
        //            int intNeedsCorrection = -1;
        //            string strRuleType;
        //            string strRule;
        //            string strOCRRuleTip = string.Empty;

        //            int intOCRLeft;
        //            int intOCRTop;
        //            int intOCRRight;
        //            int intOCRBottom;

        //            DataSet dsDropDown;
        //            DataTable dtDropDown;
        //            string strDDCodeString;
        //            List<ListValues> objListValues = null;

        //            DataSet dsFaxData;
        //            DataTable dtFaxData;
        //            DataSet dsEngFormFieldData;
        //            DataTable dtEngFormFieldData;

        //            CommonCode.WriteDBLog("Retrieving Data For Correction...", 730, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);


        //            var spParams = new SpParameter[1];
        //            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
        //            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetOCRData", spParams, true);


        //            ds.Tables[0].TableName = "CorrectionData";
        //            dt = ds.Tables["CorrectionData"];

        //            if (dt.Rows.Count <= 0)
        //            {
        //                return objChecker;
        //            }

        //            strDDCodeString = GetDDCodeString(dt);

        //            if (strDDCodeString.Length > 0)
        //            {
        //                strDDCodeString = strDDCodeString.Substring(1);
        //            }

        //            dsDropDown = PopulateDropDownData(intEngagementID, strGuid, Convert.ToInt32(dt.Rows[0]["TaxSoftwareID"]),
        //                Convert.ToInt32(dt.Rows[0]["EngagementTypeID"]) == 5 ? 1 : Convert.ToInt32(dt.Rows[0]["EngagementTypeID"]),
        //                Convert.ToInt32(dt.Rows[0]["TaxYear"]), "DropDown", strDDCodeString);
        //            dtDropDown = dsDropDown.Tables["DropDown"];

        //            dsFaxData = PopulateFaxData(intEngagementID, "FaxData");
        //            dtFaxData = dsFaxData.Tables["FaxData"];

        //            dsEngFormFieldData = PopulateEngagementFormFieldData(intEngagementID, "EngFormFieldData");
        //            dtEngFormFieldData = dsEngFormFieldData.Tables["EngFormFieldData"];

        //            bool blnIsUncertainChar = false;

        //            foreach (DataRow row in dt.Rows)
        //            {
        //                Class1040ScanCorrectionItems objCorrectionItem;
        //                intNeedsCorrection = -1;

        //                if (row["ForCorrection"].ToString() == "Y" && Convert.ToInt32(row["EngagementFormFieldID"]) <= 0)
        //                {
        //                    for (int i = 1; i <= 10; i++)
        //                    {
        //                        strRuleType = $"OCRRuleType{i}";
        //                        strRule = $"OCRRule{i}";
        //                        strOCRRuleTip = $"OCRRuleTip{i}";

        //                        if (CommonCode.ReplaceNull(row[strRuleType], "0").ToString() == "1")
        //                        {
        //                            intNeedsCorrection = CheckComparison(Convert.ToString(CommonCode.ReplaceNull(row[strRule])), dtFaxData, dt, null, null,
        //                                Convert.ToInt32(row["EngagementPageID"]), Convert.ToInt32(row["FaxRowNumber"]),
        //                                CommonCode.ReplaceNull(row["PreOCRFormName"], "").ToString(), Convert.ToInt32(CommonCode.ReplaceNull(row["DataType"], "0")));

        //                            if ((Convert.ToInt32(row["DataType"]) == 5 || Convert.ToInt32(row["DataType"]) == 7) && intNeedsCorrection == 0)
        //                            {
        //                                objListValues = CheckList(CommonCode.ReplaceNull(row["OCRRule1"], "").ToString(), row["OCRValue"].ToString(),
        //                                    dtDropDown, dtFaxData, dt, null, null, null, true, dtEngFormFieldData);
        //                            }
        //                        }
        //                        else if (CommonCode.ReplaceNull(row[strRuleType], "0").ToString() == "2")
        //                        {
        //                            blnIsUncertainChar = false;
        //                            if (row["Verified"].ToString() == "Y" && (Convert.ToInt32(row["DataType"]) == 5 || Convert.ToInt32(row["DataType"]) == 7))
        //                            {
        //                                blnIsUncertainChar = true;
        //                            }

        //                            objListValues = CheckList(CommonCode.ReplaceNull(row[strRule], "").ToString(), row["OCRValue"].ToString(),
        //                                dtDropDown, dtFaxData, dt, null, null, null, blnIsUncertainChar, dtEngFormFieldData);
        //                        }
        //                        else if (CommonCode.ReplaceNull(row[strRuleType], "0").ToString() == "3")
        //                        {
        //                            blnNeedsCorrection = CheckRange(CommonCode.ReplaceNull(row[strRule], "").ToString(), row["OCRValue"].ToString());
        //                            if (blnNeedsCorrection)
        //                            {
        //                                intNeedsCorrection = 0;
        //                            }
        //                        }
        //                        else if (CommonCode.ReplaceNull(row[strRuleType], "0").ToString() == "4")
        //                        {
        //                            blnNeedsCorrection = CheckRequired(CommonCode.ReplaceNull(row[strRule], "").ToString(), row["OCRValue"].ToString());
        //                            if (blnNeedsCorrection)
        //                            {
        //                                intNeedsCorrection = 0;
        //                            }
        //                        }
        //                        else if (CommonCode.ReplaceNull(row[strRuleType], "0").ToString() == "5")
        //                        {
        //                            blnNeedsCorrection = CheckRegEx(CommonCode.ReplaceNull(row[strRule], "").ToString(), row["OCRValue"].ToString());
        //                            if (blnNeedsCorrection)
        //                            {
        //                                intNeedsCorrection = 0;
        //                            }
        //                        }

        //                        if (intNeedsCorrection == 0 || (objListValues != null && objListValues.Count > 0))
        //                        {
        //                            intNeedsCorrection = 0;
        //                            break;
        //                        }
        //                    }

        //                    if (intNeedsCorrection == 0 || (objListValues != null && objListValues.Count > 0) || (row["Verified"].ToString() == "Y" && Convert.ToInt32(row["DataType"]) == 5))
        //                    {
        //                        blnNeedsCorrection = false;

        //                        if (Convert.ToInt32(row["EngagementTypeID"]) == 5 && row["InSPVerification"].ToString() == "Y")
        //                        {
        //                            intOCRLeft = Convert.ToInt32(CommonCode.ReplaceNull(row["SPVOCRLeft"], "0"));
        //                            intOCRTop = Convert.ToInt32(CommonCode.ReplaceNull(row["SPVOCRTop"], "0"));
        //                            intOCRRight = Convert.ToInt32(CommonCode.ReplaceNull(row["SPVOCRRight"], "0"));
        //                            intOCRBottom = Convert.ToInt32(CommonCode.ReplaceNull(row["SPVOCRBottom"], "0"));
        //                        }
        //                        else
        //                        {
        //                            intOCRLeft = Convert.ToInt32(CommonCode.ReplaceNull(row["OCRLeft"], "0"));
        //                            intOCRTop = Convert.ToInt32(CommonCode.ReplaceNull(row["OCRTop"], "0"));
        //                            intOCRRight = Convert.ToInt32(CommonCode.ReplaceNull(row["OCRRight"], "0"));
        //                            intOCRBottom = Convert.ToInt32(CommonCode.ReplaceNull(row["OCRBottom"], "0"));
        //                        }
        //                        //objCorrectionItem = new Class1040ScanCorrectionItems(
        //                        //    Convert.ToInt32(row["EngagementID"]),
        //                        //    Convert.ToInt32(row["EngagementOCRFieldID"]),
        //                        //    CommonCode.ReplaceNull(row["OCRValue"], "").ToString(),
        //                        //    CommonCode.ReplaceNull(row["OCRValue"], "").ToString(),
        //                        //    false,
        //                        //    null,
        //                        //    null,
        //                        //    Convert.ToInt32(row["EngagementPageID"]),
        //                        //    intOCRTop,
        //                        //    intOCRLeft,
        //                        //    intOCRRight,
        //                        //    intOCRBottom,
        //                        //    CommonCode.ReplaceNull(row["FileName"], "").ToString(),
        //                        //    row["FaxFormIdentifier"].ToString(),
        //                        //    row["FaxFieldName"].ToString(),
        //                        //    false,
        //                        //    row[strOCRRuleTip].ToString(),
        //                        //    0,
        //                        //    Convert.ToInt32(row["DataType"]),
        //                        //    Convert.ToInt32(row["ClientPageDPI"]),
        //                        //    Convert.ToInt32(row["FileType"]).ToString()
        //                        //);
        //                        objCorrectionItem = new Class1040ScanCorrectionItems(
        //                        dt.Rows[intCount]["EngagementID"],
        //    dt.Rows[intCount]["EngagementOCRFieldID"],
        //    ReplaceNull(dt.Rows[intCount]["OCRValue"], ""),
        //    ReplaceNull(dt.Rows[intCount]["OCRValue"], ""),
        //    false,
        //    null,
        //    null,
        //    dt.Rows[intCount]["EngagementPageID"],
        //                        intOCRTop,
        //                        intOCRLeft,
        //                        intOCRRight,
        //                        intOCRBottom,
        //    ReplaceNull(dt.Rows[intCount]["FileName"], ""),
        //    dt.Rows[intCount]["FaxFormIdentifier"],
        //    dt.Rows[intCount]["FaxFieldName"],
        //                        false,
        //    dt.Rows[intCount][strOCRRuleTip],
        //    0,
        //    dt.Rows[intCount]["DataType"],
        //    dt.Rows[intCount]["ClientPageDPI"],
        //    dt.Rows[intCount]["FileType"]
        //);

        //                        objCorrectionItem.arrListValues = objListValues;
        //                        objChecker.AddCorrectionItem(objCorrectionItem);
        //                        objCorrectionItem = null;

        //                        if (objListValues.Length > 1)
        //                        {
        //                            Array.Resize(ref objListValues, 0);



        //                        //objCorrectionItem.ListValues = objListValues;
        //                        //objChecker.AddCorrectionItem(objCorrectionItem);
        //                    }
        //                    else
        //                    {
        //                        UpdateForCorrection(intEngagementID, Convert.ToInt32(row["EngagementOCRFieldID"]), row["OCRValue"].ToString(), "Y");
        //                    }
        //                }
        //            }

        //            dtDropDown = null;
        //            dsDropDown = null;
        //            dtFaxData = null;
        //            dsFaxData = null;
        //            dtEngFormFieldData = null;
        //            dsEngFormFieldData = null;
        //            dt = null;
        //            ds = null;

        //            return objChecker;
        //        }
        private Class1040ScanChecker GetCorrectionItem(string strGuid, int intEngagementID)
        {
            DataSet ds;
            DataTable dt;

            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.Correction);
            bool blnNeedsCorrection;
            int intNeedsCorrection = -1;
            string strRuleType;
            string strRule;
            string strOCRRuleTip = string.Empty;

            int intOCRLeft;
            int intOCRTop;
            int intOCRRight;
            int intOCRBottom;

            DataSet dsDropDown;
            DataTable dtDropDown;
            string strDDCodeString;
            List<ListValues> objListValues = null;

            DataSet dsFaxData;
            DataTable dtFaxData;
            DataSet dsEngFormFieldData;
            DataTable dtEngFormFieldData;
            CommonCode.WriteDBLog("Retrieving Data For Correction...", 730, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);

            SpParameter[] spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetOCRData", spParams, true);


            ds.Tables[0].TableName = "CorrectionData";
            dt = ds.Tables["CorrectionData"];

            if (dt.Rows.Count <= 0)
            {
                return objChecker;
            }

            strDDCodeString = GetDDCodeString(dt);

            if (strDDCodeString.Length > 0)
            {
                strDDCodeString = strDDCodeString.Substring(1, strDDCodeString.Length - 1);
            }

            dsDropDown = PopulateDropDownData(intEngagementID, strGuid,Convert.ToInt32( dt.Rows[0]["TaxSoftwareID"]), Convert.ToInt32(dt.Rows[0]["EngagementTypeID"]).Equals(5) ? 1 : Convert.ToInt32(dt.Rows[0]["EngagementTypeID"]) , Convert.ToInt32( dt.Rows[0]["TaxYear"]), "DropDown", strDDCodeString);
            dtDropDown = dsDropDown.Tables["DropDown"];

            dsFaxData = PopulateFaxData(intEngagementID, "FaxData");
            dtFaxData = dsFaxData.Tables["FaxData"];

            dsEngFormFieldData = PopulateEngagementFormFieldData(intEngagementID, "EngFormFieldData");
            dtEngFormFieldData = dsEngFormFieldData.Tables["EngFormFieldData"];

            bool blnIsUncertainChar = false;

            for (int intCount = 0; intCount < dt.Rows.Count; intCount++)
            {
                Class1040ScanCorrectionItems objCorrectionItem;
                intNeedsCorrection = -1;
                if (dt.Rows[intCount]["ForCorrection"].Equals("Y") && Convert.ToInt32(dt.Rows[intCount]["EngagementFormFieldID"]) <= 0)
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        strRuleType = "OCRRuleType" + i.ToString();
                        strRule = "OCRRule" + i.ToString();
                        strOCRRuleTip = "OCRRuleTip" + i.ToString();

                        if (CommonCode.ReplaceNull(dt.Rows[intCount][strRuleType], "0").Equals(1))
                        {
                           


intNeedsCorrection = CheckComparison(
 CommonCode.ReplaceNull(dt.Rows[intCount][strRule]).ToString(),
 dtFaxData,
 dt,
 null,
 null,
 Convert.ToInt32(dt.Rows[intCount]["EngagementPageID"]),
 Convert.ToInt32(dt.Rows[intCount]["FaxRowNumber"]),
 CommonCode.ReplaceNull(dt.Rows[intCount]["PreOCRFormName"], "").ToString(),
 Convert.ToInt32(CommonCode.ReplaceNull(dt.Rows[intCount]["DataType"], "0"))
);


                            if ((Convert.ToInt32(dt.Rows[intCount]["DataType"]) == 5 || Convert.ToInt32(dt.Rows[intCount]["DataType"]) == 7) && intNeedsCorrection == 0)
                            {
                                objListValues = CheckList(CommonCode.ReplaceNull(dt.Rows[intCount]["OCRRule1"], "").ToString(), dt.Rows[intCount]["OCRValue"].ToString(), dtDropDown, dtFaxData, dt, null, null, null, true, dtEngFormFieldData);
                            }
                        }
                        else if (CommonCode.ReplaceNull(dt.Rows[intCount][strRuleType], "0").Equals(2))
                        {
                            blnIsUncertainChar = false;
                            if (dt.Rows[intCount]["Verified"].Equals("Y") && (Convert.ToInt32(dt.Rows[intCount]["DataType"]) == 5 || Convert.ToInt32(dt.Rows[intCount]["DataType"]) == 7))
                            {
                                blnIsUncertainChar = true;
                            }
                            objListValues = CheckList(CommonCode.ReplaceNull(dt.Rows[intCount][strRule]).ToString(), dt.Rows[intCount]["OCRValue"].ToString(), dtDropDown, dtFaxData, dt, null, null, null, blnIsUncertainChar, dtEngFormFieldData);
                        }
                        else if (CommonCode.ReplaceNull(dt.Rows[intCount][strRuleType], "0").Equals(3))
                        {
                            blnNeedsCorrection = CheckRange(CommonCode.ReplaceNull(dt.Rows[intCount][strRule]).ToString(), dt.Rows[intCount]["OCRValue"].ToString());
                            if (blnNeedsCorrection)
                            {
                                intNeedsCorrection = 0;
                            }
                        }
                        else if (CommonCode.ReplaceNull(dt.Rows[intCount][strRuleType], "0").Equals(4))
                        {
                            blnNeedsCorrection = CheckRequired(CommonCode.ReplaceNull(dt.Rows[intCount][strRule]).ToString(), dt.Rows[intCount]["OCRValue"].ToString());
                            if (blnNeedsCorrection)
                            {
                                intNeedsCorrection = 0;
                            }
                        }
                        else if (CommonCode.ReplaceNull(dt.Rows[intCount][strRuleType], "0").Equals(5))
                        {
                            blnNeedsCorrection = CheckRegEx(CommonCode.ReplaceNull(dt.Rows[intCount][strRule]).ToString(), dt.Rows[intCount]["OCRValue"].ToString());
                            if (blnNeedsCorrection)
                            {
                                intNeedsCorrection = 0;
                            }
                        }

                        if (intNeedsCorrection == 0 || (objListValues != null && objListValues.Count > 0))
                        {
                            intNeedsCorrection = 0;
                            break;
                        }
                    }

                    if (intNeedsCorrection == 0 || (objListValues != null && objListValues.Count > 0) || (dt.Rows[intCount]["Verified"].Equals("Y") && Convert.ToInt32(dt.Rows[intCount]["DataType"]) == 5))
                    {
                        blnNeedsCorrection = false;

                        if (Convert.ToInt32(dt.Rows[intCount]["EngagementTypeID"]) == 5 && dt.Rows[intCount]["InSPVerification"].Equals("Y"))
                        {
                            intOCRLeft = Convert.ToInt32(CommonCode.ReplaceNull(dt.Rows[intCount]["SPVOCRLeft"], "0"));
                            intOCRTop = Convert.ToInt32(CommonCode.ReplaceNull(dt.Rows[intCount]["SPVOCRTop"], "0"));
                            intOCRRight = Convert.ToInt32(CommonCode.ReplaceNull(dt.Rows[intCount]["SPVOCRRight"], "0"));
                            intOCRBottom = Convert.ToInt32(CommonCode.ReplaceNull(dt.Rows[intCount]["SPVOCRBottom"], "0"));
                        }
                        else
                        {
                            intOCRLeft = Convert.ToInt32(CommonCode.ReplaceNull(dt.Rows[intCount]["OCRLeft"], "0"));
                            intOCRTop = Convert.ToInt32(CommonCode.ReplaceNull(dt.Rows[intCount]["OCRTop"], "0"));
                            intOCRRight = Convert.ToInt32(CommonCode.ReplaceNull(dt.Rows[intCount]["OCRRight"], "0"));
                            intOCRBottom = Convert.ToInt32(CommonCode.ReplaceNull(dt.Rows[intCount]["OCRBottom"], "0"));
                        }

                        objCorrectionItem = new Class1040ScanCorrectionItems(
                            Convert.ToInt32(dt.Rows[intCount]["EngagementID"]),
                            Convert.ToInt32(dt.Rows[intCount]["EngagementOCRFieldID"]),
                            CommonCode.ReplaceNull(dt.Rows[intCount]["OCRValue"], "").ToString(),
                            CommonCode.ReplaceNull(dt.Rows[intCount]["OCRValue"], "").ToString(),
                            false,
                            null,
                            null,
                            Convert.ToInt32(dt.Rows[intCount]["EngagementPageID"]),
                            intOCRTop,
                            intOCRLeft,
                            intOCRRight,
                            intOCRBottom,
                            CommonCode.ReplaceNull(dt.Rows[intCount]["FileName"], "").ToString(),
                            dt.Rows[intCount]["FaxFormIdentifier"].ToString(),
                            dt.Rows[intCount]["FaxFieldName"].ToString(),
                            false,
                            dt.Rows[intCount][strOCRRuleTip].ToString(),
                            0,
                            Convert.ToInt32(dt.Rows[intCount]["DataType"]),
                            Convert.ToInt32(dt.Rows[intCount]["ClientPageDPI"]),
                            dt.Rows[intCount]["FileType"].ToString()
                        );
                        objCorrectionItem.ListValues = objListValues;
                        objChecker.AddCorrectionItem(objCorrectionItem);
                        objCorrectionItem = null;
                    }
                    else
                    {
                        // Modified for new dbCommon call
                        UpdateForCorrection(intEngagementID, Convert.ToInt32(dt.Rows[intCount]["EngagementOCRFieldID"]), dt.Rows[intCount]["OCRValue"].ToString(), "Y");
                    }
                }
            }

            dtDropDown = null;
            dsDropDown = null;
            dtFaxData = null;
            dsFaxData = null;
            dtEngFormFieldData = null;
            dsEngFormFieldData = null;
            dt = null;
            ds = null;

            return objChecker;
        }
        public Class1040ScanChecker GetPreCorrectionItem(string strGuid, int intEngagementId,string IsK1PreRuleValidationOn, bool isDDP = false)
        {
            DataTable dtPreCorrectionData;
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.PreVerification);
            int engId;
            string tmpValue = string.Empty;

            DataSet dsDropDown;
            DataTable dtDropDown;
            string strDDCodeString;
            var strMarkUncertainId = new StringBuilder();
            var strRemoveUncertainId = new List<string>();
            var strRemoveUncertainValue = new List<string>();
            DataSet dsFaxData;
            DataTable dtFaxData;
            DataSet dsEngFormFieldData;
            DataTable dtEngFormFieldData;
            int result;

            if (CommonCode.CheckStepStatus(15, 0, intEngagementId, ref currentStatusTable, jobId))
                return objChecker;

            CommonCode.WriteDBLog("Evaluating Pre Rules...", 1033, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
            Console.WriteLine("Evaluating Pre Rules..."); // Added for debugging
            dtPreCorrectionData = GetEngagementOcrData(intEngagementId);

            string strQTYCodes = ",FAX.DIV.027,FAX.M9D.028,FAX.CON.059,FAX.CON.045,FAX.CON.053,FAX.CON.128,FAX.CON.127,FAX.CON.205,FAX.CON.206,FAX.CON.298,FAX.CON.329,FAX.CON.288,FAX.CON.263,FAX.CON.272,FAX.GRT.096,FAX.GRT.067,FAX.GRT.077,FAX.GRT.103,FAX.99R.022,FAX.99R.023,FAX.MER.021,FAX.MER.022,FAX.99B.020,FAX.99B.033,FAX.99B.043,FAX.99B.049,";

            if (dtPreCorrectionData.Rows.Count <= 0)
            {
                UpdatePreRuleCompleted(intEngagementId);
                return objChecker;
            }

            if (!isDDP)
            {
                CommonCode.WriteDBLog("Counting total Rows for Pre Rules...", 732, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
                Console.WriteLine("Counting total Rows for Pre Rules..."); // Added for debugging
                if (dtPreCorrectionData.Rows.Count >= 0)
                {
                    int intRowlimit = GetPreProcessDataLimit(intEngagementId);
                    CommonCode.WriteDBLog($"Validating total Rows for Pre Rules...intRowlimit-{intRowlimit}", 734, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
                    Console.WriteLine($"Validating total Rows for Pre Rules...intRowlimit-{intRowlimit}");
                    if (dtPreCorrectionData.Rows.Count > intRowlimit)
                    {
                        objChecker.DDPNewPreprocess = true;
                        return objChecker;
                    }
                }
            }

            CommonCode.WriteDBLog("Evaluating Old Pre Rules...", 735, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
            Console.WriteLine("Evaluating Old Pre Rules..."); // Added for debugging
            strDDCodeString = GetDDCodeString(dtPreCorrectionData);
            engId = intEngagementId;

            if (strDDCodeString.Length > 0)
                strDDCodeString = strDDCodeString.Substring(0, strDDCodeString.Length - 1);

            dsDropDown = PopulateDropDownData(intEngagementId, strGuid, Convert.ToInt32(dtPreCorrectionData.Rows[0]["TaxSoftwareID"]),
                Convert.ToInt32(dtPreCorrectionData.Rows[0]["EngagementTypeID"]) == 5 ? 1 : Convert.ToInt32(dtPreCorrectionData.Rows[0]["EngagementTypeID"]),
                Convert.ToInt32(dtPreCorrectionData.Rows[0]["TaxYear"]), "DropDown", strDDCodeString);
            dtDropDown = dsDropDown.Tables["DropDown"];

            dsFaxData = PopulateFaxData(intEngagementId, "FaxData");
            dtFaxData = dsFaxData.Tables["FaxData"];

            dsEngFormFieldData = PopulateEngagementFormFieldData(intEngagementId, "EngFormFieldData");
            dtEngFormFieldData = dsEngFormFieldData.Tables["EngFormFieldData"];

            strTempSucc = ",";
            strTempFail = ",";

            int subStepId = 0;
            bool matchFound = true;

            while (matchFound)
            {
                matchFound = CommonCode.CheckStepStatus(11, subStepId, intEngagementId, ref currentStatusTable, jobId);
                if (!matchFound) break;
                subStepId++;
            }

            int intCount1 = subStepId * 5000;
            int intCount = 0;

            var OcrDataList = CommonCode.GetPagePreCorrectionData(dtPreCorrectionData, 0, 0);
            var OcrDataList_rowfilterdata = OcrDataList.Where(correctionItem => correctionItem.DataType == 1 || correctionItem.DataType == 3).ToList();

            if (!CommonCode.CheckStepStatus(11, 0, intEngagementId, ref currentStatusTable, jobId))
            {
                CommonCode.WriteDBLog($"For Loop Started For Pre Rules Datatype - 1 And 3, Total Records: {OcrDataList_rowfilterdata.Count}", 736, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
                Console.WriteLine($"For Loop Started For Pre Rules Datatype - 1 And 3, Total Records: {OcrDataList_rowfilterdata.Count}", intEngagementId);
                foreach (var OcrItem in OcrDataList_rowfilterdata)
                {
                    try
                    {
                        var FaxformList = CommonCode.GetFaxformList(dtFaxData, OcrItem.EngagementPageID);
                        var FormfieldList = CommonCode.GetFormfieldList(dtEngFormFieldData, OcrItem.EngagementPageID);
                        var DropdownList = CommonCode.GetDropdownList(dtDropDown);

                        tmpValue = OcrItem.OCRValue;

                        if (string.Equals(OcrItem.ApplyDecimalRule ?? "N", "N", StringComparison.OrdinalIgnoreCase))
                        {
                            result = EvalPreRule(OcrDataList, OcrItem, FaxformList, FormfieldList, DropdownList, dtPreCorrectionData, null, dtFaxData, dtDropDown, dtEngFormFieldData);

                            if (result == 1)
                            {
                                RemoveUncertain(tmpValue, strRemoveUncertainValue, strRemoveUncertainId, OcrItem.EngagementOCRFieldID);
                            }
                            else if (result == 0 && string.Equals(OcrItem.Verified ?? "N", "N", StringComparison.OrdinalIgnoreCase))
                            {
                                MarkUncertain(strMarkUncertainId, OcrItem.EngagementOCRFieldID);
                            }

                            if (!strQTYCodes.Contains($",{OcrItem.FaxDWPCode},") && tmpValue.Contains(".") && !CheckDecimalPlace(tmpValue))
                            {
                                MarkUncertain(strMarkUncertainId, OcrItem.EngagementOCRFieldID);
                            }
                        }
                        else
                        {
                            if (string.Equals(OcrItem.Verified ?? "N", "Y", StringComparison.OrdinalIgnoreCase))
                            {
                                result = EvalPreRule(OcrDataList, OcrItem, FaxformList, FormfieldList, DropdownList, dtPreCorrectionData, null, dtFaxData, dtDropDown, dtEngFormFieldData);

                                if (result == 0 && tmpValue.Length > 1)
                                {
                                    tmpValue = tmpValue.Replace(".", "");
                                    if (tmpValue.Length > 1)
                                    {
                                        tmpValue = tmpValue.Substring(0, tmpValue.Length - 2) + "." + tmpValue.Substring(tmpValue.Length - 2);
                                        OcrItem.OCRValue = tmpValue;

                                        if (EvalPreRule(OcrDataList, OcrItem, FaxformList, FormfieldList, DropdownList, dtPreCorrectionData, null, dtFaxData, dtDropDown, dtEngFormFieldData) == 1)
                                        {
                                            RemoveUncertain(tmpValue, strRemoveUncertainValue, strRemoveUncertainId, OcrItem.EngagementOCRFieldID);
                                        }
                                    }
                                }
                                else if (result == 1)
                                {
                                    RemoveUncertain(tmpValue, strRemoveUncertainValue, strRemoveUncertainId, OcrItem.EngagementOCRFieldID);
                                }
                            }
                            else
                            {
                                result = EvalPreRule(OcrDataList, OcrItem, FaxformList, FormfieldList, DropdownList, dtPreCorrectionData, null, dtFaxData, dtDropDown, dtEngFormFieldData, true);

                                if (result == 0)
                                {
                                    MarkUncertain(strMarkUncertainId, OcrItem.EngagementOCRFieldID);
                                }

                                if (!strQTYCodes.Contains($",{OcrItem.FaxDWPCode},") && tmpValue.Contains(".") && !CheckDecimalPlace(tmpValue) && double.TryParse(tmpValue, out var numericValue) && numericValue > 0)
                                {
                                    MarkUncertain(strMarkUncertainId, OcrItem.EngagementOCRFieldID);
                                }
                            }
                        }

                        if (intCount % 5000 == 0 && intCount > 0)
                        {
                            WriteDBLogsJobId($"Evaluating Pre Rules...Total Records Processed-{intCount}", intEngagementId, jobId, 11, subStepId);
                            Console.WriteLine($"Evaluating Pre Rules...Total Records Processed-{intCount}", intEngagementId, jobId, 11, subStepId);
                            subStepId++;
                            RemoveUncertainInDB(strRemoveUncertainValue, strRemoveUncertainId, intEngagementId);
                            MarkUncertainInDB(strMarkUncertainId, intEngagementId);
                            AutoVerifiedFields(intEngagementId);
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log exception
                    }

                    intCount++;
                }

                WriteDBLogsJobId($"For Loop completed For Pre Rules Datatype - 1 and 3, Total Records: {OcrDataList_rowfilterdata.Count}", intEngagementId, jobId, 11);
                Console.WriteLine($"For Loop completed For Pre Rules Datatype - 1 and 3, Total Records: {OcrDataList_rowfilterdata.Count}", intEngagementId, jobId, 11);
            }

            subStepId = 0;
            intCount = 0;
            matchFound = true;

            while (matchFound)
            {
                matchFound = CommonCode.CheckStepStatus(12, subStepId, intEngagementId, ref currentStatusTable, jobId);
                if (!matchFound) break;
                subStepId++;
            }

            intCount1 = subStepId * 5000;

            OcrDataList_rowfilterdata = OcrDataList.Where(correctionItem => correctionItem.DataType == 4).ToList();

            if (!CommonCode.CheckStepStatus(12, 0, intEngagementId, ref currentStatusTable, jobId))
            {
                CommonCode.WriteDBLog($"For Loop Started For Pre Rules Datatype - 4, Total Records: {OcrDataList_rowfilterdata.Count}", 739, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
                Console.WriteLine($"For Loop Started For Pre Rules Datatype - 4, Total Records: {OcrDataList_rowfilterdata.Count}", intEngagementId, jobId, 12);
                foreach (var OcrItem in OcrDataList_rowfilterdata)
                {
                    try
                    {
                        var FaxformList = CommonCode.GetFaxformList(dtFaxData, OcrItem.EngagementPageID);
                        var FormfieldList = CommonCode.GetFormfieldList(dtEngFormFieldData, OcrItem.EngagementPageID);
                        var DropdownList = CommonCode.GetDropdownList(dtDropDown);
                        tmpValue = OcrItem.OCRValue;

                        result = EvalPreRule(OcrDataList, OcrItem, FaxformList, FormfieldList, DropdownList, dtPreCorrectionData, null, dtFaxData, dtDropDown, dtEngFormFieldData);

                        if (result == 1) // Rule Satisfied
                        {
                            RemoveUncertain(tmpValue, strRemoveUncertainValue, strRemoveUncertainId, OcrItem.EngagementOCRFieldID);
                        }
                        else if (result == 0) // Rule Failed
                        {
                            if (string.Equals(OcrItem.Verified ?? "N", "N", StringComparison.OrdinalIgnoreCase))
                            {
                                MarkUncertain(strMarkUncertainId, OcrItem.EngagementOCRFieldID);
                            }
                        }

                        if (intCount % 10000 == 0 && intCount > 0)
                        {
                            CommonCode.WriteDBLog($"Evaluating Pre Rules...Total Records Processed-{intCount}", 740, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
                            Console.WriteLine($"Evaluating Pre Rules...Total Records Processed-{intCount}", intEngagementId, jobId, 12);
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }

                        intCount++;
                    }
                    catch (Exception ex)
                    {
                        // Log exception
                    }
                }

                WriteDBLogsJobId($"For Loop completed For Pre Rules Datatype - 4, Total Records: {OcrDataList_rowfilterdata.Count}", intEngagementId, jobId, 12); // step-12
                Console.WriteLine($"For Loop completed For Pre Rules Datatype - 4, Total Records: {OcrDataList_rowfilterdata.Count}", intEngagementId, jobId, 12);
            }

            CommonCode.WriteDBLog("For Loop Completed for Pre Rules...", 742, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
            Console.WriteLine("For Loop Completed for Pre Rules...", intEngagementId, jobId, 12);
            RemoveUncertainInDB(strRemoveUncertainValue, strRemoveUncertainId, intEngagementId);
            MarkUncertainInDB(strMarkUncertainId, intEngagementId);
            AutoVerifiedFields(intEngagementId);

            if (!isDDP)
            {
                UpdatePreRuleCompleted(engId);
                CommonCode.WriteDBLog("UpdatePreRuleCompleted function called...", 743, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
                Console.WriteLine("UpdatePreRuleCompleted function called...", intEngagementId, jobId, 14);
            }

            if (!CommonCode.CheckStepStatus(13, 0, intEngagementId, ref currentStatusTable, jobId))
            {
                CommonCode.WriteDBLog("Start K1 Supplement Pre-rule evaluation ...", 744, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
                Console.WriteLine("Start K1 Supplement Pre-rule evaluation ...", intEngagementId, jobId, 13);// Step-13
                if (!string.IsNullOrEmpty(IsK1PreRuleValidationOn) && IsK1PreRuleValidationOn == "Y")
                {
                    var k1prerule = new K1PreRuleCalculation(CommonCode.ConfigConnectionKey);
                    int engagementTypeId = Convert.ToInt32(dtPreCorrectionData.Rows[0]["EngagementTypeID"]);
                    if (engagementTypeId == 5) engagementTypeId = 1;
                    if (engagementTypeId == 6) engagementTypeId = 3;

                    k1prerule.EvaluatePreRule(intEngagementId, Convert.ToInt32(dtPreCorrectionData.Rows[0]["TaxYear"]), engagementTypeId);
                }

                CommonCode.WriteDBLog("End K1 Supplement Pre-rule evaluation...", 745, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
                WriteDBLogsJobId("K1 Supplement Pre-rule evaluation ...", intEngagementId, jobId, 13);
                Console.WriteLine("K1 Supplement Pre-rule evaluation ...", intEngagementId, jobId, 13);// Step-13
            }

            if (!CommonCode.CheckStepStatus(14, 0, intEngagementId, ref currentStatusTable, jobId) && isDDP)
            {
                UpdatePreRuleCompleted(engId);

                var dsUncertainFields = Update_OCRDecimalUncertain(intEngagementId);
                if (dsUncertainFields != null && dsUncertainFields.Tables.Count > 0 && dsUncertainFields.Tables[0].Rows.Count > 0)
                {
                    UpdateEvenPagesUncertainFields(intEngagementId, dsUncertainFields.Tables[0]);
                }

                WriteDBLogsJobId("UpdatePreRuleCompleted function called...", intEngagementId, jobId, 14); // Step-14
                Console.WriteLine("UpdatePreRuleCompleted function called...", intEngagementId, jobId, 14);

                try
                {
                    SkipWorkFlowSteps(intEngagementId);
                }
                catch (Exception ex)
                {
                    // Log exception
                }
                //Console.WriteLine();
                CommonCode.WriteDBLog("UpdatePreRuleCompleted function called...", 747, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId, jobId, 14); // Step-14
                Console.WriteLine("UpdatePreRuleCompleted function called...", intEngagementId, jobId, 14);
            }

            WriteDBLogsJobId("Completed Pre Rules...", intEngagementId, jobId, 15); // Step-15
            Console.WriteLine("Completed Pre Rules...", intEngagementId, jobId, 15);
            //Console.WriteLine();
            return objChecker;
        }

        private DataTable GetEngagementOcrData(int intEngagementId)
        {
            DataTable dtPreCorrectionData;

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementId);
            dtPreCorrectionData = CommonCode.GetEngagementDbCommon(intEngagementId).GetDataTable("Proc_GetOCRData", spParams, "PreCorrectionData", true);

            return dtPreCorrectionData;
        }

        public bool GetPreProcessDomainSetting(int intEngagementID)
        {
            bool isNewPreprocess;
            DataTable dtProcess;


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            dtProcess = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataTable("Proc_CheckPreprocessDomain", spParams, "CheckDomain", true);


            isNewPreprocess = Convert.ToBoolean(dtProcess.Rows[0][0]);
            return isNewPreprocess;
        }

        public int GetPreProcessDataLimit(int intEngagementID)
        {
            int intTotalRows;
            DataTable dtProcess;


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            dtProcess = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataTable("Proc_CheckPreprocessLimit", spParams, "GetLimit", true);


            intTotalRows = Convert.ToInt32(dtProcess.Rows[0][0]);
            return intTotalRows;
        }

        public int RemoveUncertain_Test(ref string strOCRChangeValues, string strValue, int intEngagementID, long lngEngagementOCRFieldID)
        {
            var spParams = new SpParameter[3];
            int result = 0;

            switch (strOCRChangeValues.Trim())
            {
                case "":
                    strOCRChangeValues = "Update SPEngagementOCRField Set UncertainChar='0',OCRValue=@OCRValue " +
                                         "Where EngagementID=@EngagementID And EngagementOCRFieldID=@EngagementOCRFieldID";

                    spParams[0] = new SpParameter("@OCRValue", strValue);
                    spParams[1] = new SpParameter("@EngagementID", intEngagementID);
                    spParams[2] = new SpParameter("@EngagementOCRFieldID", lngEngagementOCRFieldID);
                    break;

                default:
                    strOCRChangeValues += " Update SPEngagementOCRField Set UncertainChar='0',OCRValue=@OCRValue " +
                                          "Where EngagementID=@EngagementID And EngagementOCRFieldID=@EngagementOCRFieldID";

                    spParams[0] = new SpParameter("@OCRValue", strValue);
                    spParams[1] = new SpParameter("@EngagementID", intEngagementID);
                    spParams[2] = new SpParameter("@EngagementOCRFieldID", lngEngagementOCRFieldID);
                    break;
            }

            if (strOCRChangeValues.Length > 0)
            {
                result = CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete(strOCRChangeValues, spParams, false, false);
            }

            return result;
        }

        private bool AutoVerifiedFields(int intEngagementID)
        {
            var spParams = new SpParameter[3];
            DataTable dtEngagementOCRFieldID;
            string[] columns = { "EngagementOCRFieldID" };
            string[] values = { strTempSucc.ToString() };

            if (strTempSucc.Length > 1)
            {
                strTempSucc = strTempSucc.Substring(1, strTempSucc.Length - 2);
                dtEngagementOCRFieldID = CommonCode.GetCustomizeDataTable(values, columns);


                spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                spParams[1] = new SpParameter("@UDT_EngagementOCRFieldID", dtEngagementOCRFieldID);
                spParams[2] = new SpParameter("@IsAutoVerified", true);
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_MarkEngagementOcrFieldAutoVerifiedBulk", spParams, true);

            }

            if (strTempFail.Length > 1)
            {
                strTempFail = strTempFail.Substring(1, strTempFail.Length - 2);
                values[0] = strTempFail.ToString();
                dtEngagementOCRFieldID = CommonCode.GetCustomizeDataTable(values, columns);

                spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                spParams[1] = new SpParameter("@UDT_EngagementOCRFieldID", dtEngagementOCRFieldID);
                spParams[2] = new SpParameter("@IsAutoVerified", false);
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_MarkEngagementOcrFieldAutoVerifiedBulk", spParams, true);

            }

            strTempSucc = "";
            strTempFail = "";
            return true;
        }

        private void RemoveUncertain(string strValue, List<string> strRemoveUncertainValue, List<string> strRemoveUncertainID, long lngEngagementOCRFieldID)
        {
            strRemoveUncertainValue.Add(strValue);
            strRemoveUncertainID.Add(lngEngagementOCRFieldID.ToString());
        }

        private void RemoveUncertainInDB(List<string> strRemoveUncertainValue, List<string> strRemoveUncertainID, int intEngagementID)
        {
            string[] columns = { "EngagementOCRFieldID", "EngagementId", "OCRValue", "UnCertainChar", "Remark", "IsDecimal" };
            List<string>[] values = { strRemoveUncertainID, null, strRemoveUncertainValue, null, null, null };

            DataTable dtEngagementOCRValue = CommonCode.GetCustomizeDataTable_List(values, columns);

            if (dtEngagementOCRValue.Rows.Count > 0)
            {

                var spParams = new SpParameter[3];
                spParams[0] = new SpParameter("@intEngagementID", intEngagementID);
                spParams[1] = new SpParameter("@UDT_OCRValue", dtEngagementOCRValue);
                spParams[2] = new SpParameter("@IsUnCertain", false);
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementOCRFieldMarkUncertain", spParams, true, false);

            }

            strRemoveUncertainID.Clear();
            strRemoveUncertainValue.Clear();
        }
        private bool MarkUncertain(StringBuilder strMarkUncertainID, long lngEngagementOCRFieldID)
        {
            CommonCode.AppendString(lngEngagementOCRFieldID.ToString(), strMarkUncertainID);
            return true;
        }

        private bool MarkUncertainInDB(StringBuilder strMarkUncertainId, int intEngagementID)
        {
            string[] columns = { "EngagementOCRFieldID", "EngagementId", "OCRValue", "UnCertainChar", "Remark", "IsDecimal" };
            string[] values = new string[6];
            values[0] = strMarkUncertainId.ToString();
            DataTable dtEngagementOCRFieldId = CommonCode.GetCustomizeDataTable(values, columns);


            var spParams = new SpParameter[3];
            spParams[0] = new SpParameter("@intEngagementID", intEngagementID);
            spParams[1] = new SpParameter("@UDT_OCRValue", dtEngagementOCRFieldId);
            spParams[2] = new SpParameter("@IsUnCertain", true);

            if (CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementOCRFieldMarkUncertain", spParams, true, false) > 0)
            {
                strMarkUncertainId.Clear();
                return true;
            }

            return false;
        }

        private int EvalPreRule(
            IList<OcrFieldData> OcrData,
            OcrFieldData OcrItem,
            IList<Faxformdata> Faxdata,
            IList<FormFieldData> Formfield,
            IList<DropdownData> Dropdowndata,
            DataTable dtPreCorrectionData,
            DataRow dataRowPreCorrection,
            DataTable dtfaxdata,
            DataTable dtDropDown,
            DataTable dtEngFormFieldData,
            bool OCRPreRule = true)
        {
            string strRuleType, strRule, strAutoVerify;
            bool blnNeedsCorrection = false;
            bool blnIsUncertainChar = false;
            List<ListValues> objListValues = null;
            int intNeedsCorrection = -1;
            bool blnAnyRuleSatisfied = false;

            int intEngPgID, intFRNo;
            string strPreRule, strPreRuleType, StrPreAutoVerify, strOCRIdentifier;

            blnIsRuleDefined = false;

            if (OCRPreRule)
            {
                for (int i = 1; i <= 5; i++)
                {
                    strRuleType = $"OCRPreRuleType{i}";
                    strRule = $"OCRPreRule{i}";
                    strAutoVerify = $"PreRuleAutoVerify{i}";

                    intEngPgID = IsDBNull(OcrItem.EngagementPageID) ? 0 : OcrItem.EngagementPageID;
                    intFRNo = IsDBNull(OcrItem.FaxRowNumber) ? -1 : OcrItem.FaxRowNumber;

                    strPreRule = CommonCode.GetOCRRule(i, OcrItem);
                    strPreRuleType = CommonCode.GetOCRPreRuleType(i, OcrItem);
                    StrPreAutoVerify = CommonCode.GetPreRuleAutoVerify(i, OcrItem).ToString();
                    strOCRIdentifier = IsDBNull(OcrItem.OCRIdentifierWithoutSPLChars) ? "" : OcrItem.OCRIdentifierWithoutSPLChars;

                    string strPRDiagInfo = string.Empty;
                    if (string.IsNullOrEmpty(strPreRule) || !IsCertainValuePresent(dtPreCorrectionData, OcrData, intEngPgID, intFRNo, strPreRule, strOCRIdentifier))
                    {
                        continue;
                    }

                    if (CommonCode.ReplaceNull(strPreRuleType, "0") == "1")
                    {
                        blnIsRuleDefined = true;
                        if (strPreRule.Contains("SUM", StringComparison.OrdinalIgnoreCase))
                        {
                            intFRNo = 0;
                        }
                        intNeedsCorrection = CheckComparison(
                            (string)CommonCode.ReplaceNull(strPreRule, ""),
                            dtfaxdata,
                            dtPreCorrectionData,
                            OcrData,
                            Faxdata,
                            OcrItem.EngagementPageID,
                            intFRNo,
                            (string)CommonCode.ReplaceNull(OcrItem.PreOCRFormName, ""),
                            OcrItem.DataType,
                            null,
                            0,
                            strPRDiagInfo);

                        if (StrPreAutoVerify != "1" && intNeedsCorrection == 1)
                        {
                            intNeedsCorrection = 2;
                        }
                        if (intNeedsCorrection == 1)
                        {
                            blnAnyRuleSatisfied = true;
                        }

                        string[] tmpar = { IsDBNull(OcrItem.FaxDWPCode) ? "" : OcrItem.FaxDWPCode };
                        var strArrCode = new StringBuilder();
                        foreach (var code in tmpar)
                        {
                            string strOCRID = GetOCRID(code, ref dtfaxdata, ref dtPreCorrectionData, Faxdata, OcrData, OcrItem.EngagementPageID, OcrItem.FaxRowNumber, (string)CommonCode.ReplaceNull(OcrItem.PreOCRFormName, ""));
                            string[] tmpOCRarr = strOCRID.Split(',');
                            foreach (var id in tmpOCRarr)
                            {
                                if (!strTempSucc.Contains($",{id},", StringComparison.OrdinalIgnoreCase))
                                {
                                    CommonCode.AppendString(id, strArrCode);
                                }
                            }
                        }

                        if (strArrCode.Length > 0)
                        {
                            string[] arrocrid = strArrCode.ToString().Split(',');
                            foreach (var id in arrocrid)
                            {
                                strTempSucc += $"{id},";
                            }
                        }
                    }
                    else if (CommonCode.ReplaceNull(strPreRuleType, "0") == "2")
                    {
                        blnIsUncertainChar = false;
                        blnIsRuleDefined = true;
                        if (OcrItem.Verified == "Y" && OcrItem.DataType == 5)
                        {
                            blnIsUncertainChar = true;
                        }
                        objListValues = CheckList(
                            (string)CommonCode.ReplaceNull(strPreRule, ""),
                            OcrItem.OCRValue,
                            dtDropDown,
                            dtfaxdata,
                            dtPreCorrectionData,
                            Dropdowndata,
                            Faxdata,
                            OcrData,
                            blnIsUncertainChar,
                            dtEngFormFieldData);
                    }
                    else if (CommonCode.ReplaceNull(strPreRuleType, "0") == "3")
                    {
                        blnIsRuleDefined = true;
                        blnNeedsCorrection = CheckRange((string)CommonCode.ReplaceNull(strPreRule, ""), OcrItem.OCRValue);
                        if (blnNeedsCorrection)
                        {
                            intNeedsCorrection = 0;
                        }
                    }
                    else if (CommonCode.ReplaceNull(strPreRuleType, "0") == "4")
                    {
                        blnIsRuleDefined = true;
                        blnNeedsCorrection = CheckRequired((string)CommonCode.ReplaceNull(strPreRule, ""), OcrItem.OCRValue);
                        if (blnNeedsCorrection)
                        {
                            intNeedsCorrection = 0;
                        }
                    }
                    else if (CommonCode.ReplaceNull(strPreRuleType, "0") == "5")
                    {
                        blnIsRuleDefined = true;
                        blnNeedsCorrection = CheckRegEx((string)CommonCode.ReplaceNull(strPreRule, ""), OcrItem.OCRValue);
                        if (blnNeedsCorrection)
                        {
                            intNeedsCorrection = 0;
                        }
                    }

                    if (intNeedsCorrection == 0 || (objListValues != null && objListValues.Count > 0))
                    {
                        intNeedsCorrection = 0;
                        break;
                    }
                }

                if (blnIsRuleDefined)
                {
                    if (blnAnyRuleSatisfied && intNeedsCorrection != 0)
                    {
                        intNeedsCorrection = 1;
                    }
                    return intNeedsCorrection == -1 ? 1 : intNeedsCorrection;
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                blnNeedsCorrection = false;
                for (int i = 1; i <= 5; i++)
                {
                    strRuleType = $"OCRRuleType{i}";
                    strRule = $"OCRRule{i}";
                    strPreRule = (string)CommonCode.ReplaceNull($"OcrItem.OCRRule{i}");
                    strPreRuleType = (string)CommonCode.ReplaceNull($"OcrItem.OCRPreRuleType{i}");

                    if (CommonCode.ReplaceNull(strPreRuleType, "0") == "1" ||
                        CommonCode.ReplaceNull(strPreRuleType, "0") == "2" ||
                        CommonCode.ReplaceNull(strPreRuleType, "0") == "3" ||
                        CommonCode.ReplaceNull(strPreRuleType, "0") == "4" ||
                        CommonCode.ReplaceNull(strPreRuleType, "0") == "5")
                    {
                        blnNeedsCorrection = true;
                        break;
                    }
                }
                return blnNeedsCorrection ? 1 : 0;
            }
        }
        private bool IsCertainValuePresent(DataTable dtMyCopy, IList<OcrFieldData> ocrdata, int intEngagementPageID, int intFaxRowNumber, string strOCRPreRule, string strOCRIdentifier)
        {
            try
            {
                string[] tmpar = FilterFaxCode(strOCRPreRule).Split(',');
                for (int i = 0; i < tmpar.Length; i++)
                {
                    if (tmpar[i].Contains("[SUM]"))
                    {
                        tmpar[i] = tmpar[i].Replace("[SUM]", "");
                    }
                    tmpar[i] = $"'{tmpar[i]}'";
                }

                if (tmpar.Length > 0)
                {
                    string strCodes = string.Join(",", tmpar.Distinct());
                    IList<OcrFieldData> OcrItem = null;

                    if (tmpar[0].Contains("OCR"))
                    {
                        OcrItem = ocrdata.Where(item => item.FaxRowNumber == intFaxRowNumber &&
                                                        strCodes.Contains($"'{item.OCRDWPCode}'") &&
                                                        item.UnCertainChar == 0).ToList();
                    }
                    else if (tmpar[0].Contains("FAX"))
                    {
                        OcrItem = ocrdata.Where(item => item.OCRIdentifierWithoutSPLChars == strOCRIdentifier &&
                                                        strCodes.Contains($"'{item.OCRDWPCode}'") &&
                                                        item.UnCertainChar == 0).ToList();
                    }

                    return OcrItem != null && OcrItem.Count > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        private string GetOCRID(string strCode, ref DataTable dtFaxData, ref DataTable dtOCRData, IList<Faxformdata> Faxdata, IList<OcrFieldData> ocrdata, int intPageID = 0, int intGroupInstance = 0, string OCRFormName = "")
        {
            string strValue = "";
            string FuncName = "";

            if (strCode.Contains("["))
            {
                FuncName = strCode.Substring(strCode.IndexOf("[") + 1, strCode.IndexOf("]") - strCode.IndexOf("[") - 1);
                strCode = strCode.Substring(strCode.IndexOf($"[{FuncName}]") + FuncName.Length + 2);
            }

            DataRow[] drRows;

            if (strCode.StartsWith("FAX", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(FuncName))
                {
                    if (!string.IsNullOrEmpty(OCRFormName))
                    {
                        if (intGroupInstance > 0)
                        {
                            drRows = dtFaxData.Select($"FaxDWPCode='{strCode}' AND InputForm='{OCRFormName.Replace("'", "''")}' AND FaxRowNumber={intGroupInstance}");
                            if (drRows.Length > 1)
                            {
                                drRows = dtFaxData.Select($"FaxDWPCode='{strCode}' AND InputForm='{OCRFormName.Replace("'", "''")}' AND EngagementPageID={intPageID} AND FaxRowNumber={intGroupInstance}");
                            }
                        }
                        else
                        {
                            drRows = dtFaxData.Select($"FaxDWPCode='{strCode}' AND InputForm='{OCRFormName.Replace("'", "''")}'");
                        }
                    }
                    else
                    {
                        drRows = dtFaxData.Select($"FaxDWPCode='{strCode}'");
                    }
                }
                else
                {
                    drRows = dtFaxData.Select($"FaxDWPCode='{strCode}' AND InputForm='{OCRFormName.Replace("'", "''")}'");
                }

                if (drRows.Length > 0)
                {
                    switch (FuncName.Trim().ToUpper())
                    {
                        case "SUM":
                            strValue = string.Join(",", drRows.Select(row => row["EngagementOCRFieldID"].ToString()));
                            break;
                        default:
                            strValue = drRows[0]["EngagementOCRFieldID"].ToString();
                            break;
                    }
                }
                else
                {
                    strValue = "0";
                }
            }
            else
            {
                if (intGroupInstance > 0)
                {
                    drRows = dtOCRData.Select($"OCRDWPCode='{strCode}' AND EngagementPageID={intPageID} AND FaxRowNumber={intGroupInstance}");
                }
                else
                {
                    drRows = dtOCRData.Select($"OCRDWPCode='{strCode}' AND EngagementPageID={intPageID}");
                }

                if (drRows.Length > 0)
                {
                    strValue = drRows[0]["EngagementOCRFieldID"].ToString();
                }
                else
                {
                    strValue = "0";
                }
            }

            return strValue;
        }

        private string FilterFaxCode(string txtRule)
        {
            string tmpstr = txtRule;
            var tmpstrSB = new StringBuilder();
            var tmpcol = new HashSet<string>();

            while (tmpstr.Contains("{"))
            {
                int startIndex = tmpstr.IndexOf("{") + 1;
                tmpstr = tmpstr.Substring(startIndex);

                if (!tmpstr.Contains("}"))
                {
                    break;
                }

                string extracted = tmpstr.Substring(0, tmpstr.IndexOf("}"));
                CommonCode.AppendString(extracted, tmpstrSB);

                if (!string.IsNullOrEmpty(tmpstrSB.ToString()) && !tmpcol.Contains(tmpstrSB.ToString()))
                {
                    tmpcol.Add(tmpstrSB.ToString());
                }

                tmpstr = tmpstr.Substring(tmpstr.IndexOf("}") + 1);
            }

            tmpstrSB.Clear();
            foreach (var item in tmpcol)
            {
                CommonCode.AppendString(item, tmpstrSB);
            }

            return tmpstrSB.ToString();
        }

        private bool CheckDecimalPlace(string tmpvalue)
        {
            int tmpplace = tmpvalue.IndexOf(".");
            if (tmpplace > -1)
            {
                if (tmpvalue.IndexOf(".", tmpplace + 1) > -1)
                {
                    return false;
                }

                return tmpplace == tmpvalue.Length - 3;
            }

            return false;
        }

        private DataTable GetXMPData(int intEngagementID, ref DataSet ds)
        {

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetXMPData", spParams, true);


            ds.Tables[0].TableName = "XMPToFax";
            return ds.Tables["XMPToFax"];
        }
        private void OCRToFax(int intEngagementID)
        {
            OCRToFax(intEngagementID, false);
        }

        private void OCRToFax(int intEngagementID, bool isCalledForPre)
        {
            DataSet ds = null;

            CommonCode.WriteDBLog("Converting XMP to Fax...", 749, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Converting XMP to Fax...", intEngagementID, 749, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            DataTable dt = GetXMPData(intEngagementID, ref ds);

            // Start XMP to Fax Conversion
            var objXMPToFax = new OCRToFax(CommonCode.ConfigConnectionKey);
            objXMPToFax.ConvertXMPToFax(ds);
            objXMPToFax = null;

            CommonCode.WriteDBLog("Converting Completed for XMP to Fax...", 750, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Converting Completed for XMP to Fax...", intEngagementID);
            CommonCode.WriteDBLog("Converting XML to Fax...", 751, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Converting XML to Fax...", intEngagementID);
            dt = GetXMPData(intEngagementID, ref ds);

            // Start XML to Fax Conversion
            var objXMLToFax = new OCRToFax(CommonCode.ConfigConnectionKey);
            objXMLToFax.ConvertXMPToFax(ds);
            objXMLToFax = null;

            CommonCode.WriteDBLog("Converting Completed for XML to Fax...", 752, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Converting Completed for XML to Fax...", intEngagementID);
            CommonCode.WriteDBLog("Converting OCR to Fax...", 753, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Converting OCR to Fax...", intEngagementID, 753, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            if (isCalledForPre)
            {
                UpdateBeforeOCRToFax_Pre(intEngagementID);
            }
            else
            {
                UpdateBeforeOCRToFax(intEngagementID);
            }


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetOCRData", spParams, true);


            ds.Tables[0].TableName = "OCRToFax";
            dt = ds.Tables["OCRToFax"];

            // Start OCR to Fax Conversion
            var objOCRToFax = new OCRToFax(CommonCode.ConfigConnectionKey);
            if (dt.Rows.Count > 0)
            {
                objOCRToFax.ConvertOCRToFax(ds);
            }
            else
            {
                objOCRToFax.UpdateAfterOCRToFax(intEngagementID);
            }
            objOCRToFax = null;

            CommonCode.WriteDBLog("Converting Completed for OCR to Fax...", 754, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Converting Completed for OCR to Fax...", intEngagementID);
            dt = null;
            ds = null;
        }

        private void UpdateBeforeOCRToFax(int intEngagementID)
        {
            CommonCode.WriteDBLog("Update Start for Before OCR to Fax...", 755, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Update Start for Before OCR to Fax...", intEngagementID);

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateBeforeOCRToFax_V01", spParams, true, false);


            CommonCode.WriteDBLog("Update End for Before OCR to Fax...", 756, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Update End for Before OCR to Fax...", intEngagementID);
        }

        private void UpdateBeforeOCRToFax_Pre(int intEngagementID)
        {
            CommonCode.WriteDBLog("Update Start for Before OCR to Fax...", 755, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Update Start for Before OCR to Fax...", intEngagementID, 755, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateBeforeOCRToFax", spParams, true, false);


            CommonCode.WriteDBLog("Update End for Before OCR to Fax...", 756, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Update End for Before OCR to Fax...", intEngagementID);
        }

        private void UpdateProformadCompleted(int intEngagementID)
        {

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteEngagementFaxData", spParams, true, false);

        }

        private void EvaluateFaxToTaxFormula(int intEngagementID, bool isMultiThreadingActive)
        {
            CommonCode.WriteDBLog("Evaluating Fax Formulas...", 757, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Evaluating Fax Formulas...", intEngagementID);
            var objEvaluateFormula = new EvaluateFormula();
            objEvaluateFormula.EvaluateFaxToTaxFormula(intEngagementID, isMultiThreadingActive);
            objEvaluateFormula = null;

            CommonCode.WriteDBLog("Evaluating Fax Formulas completed...", 758, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Evaluating Fax Formulas completed...", intEngagementID);
        }

        private Class1040ScanChecker GetParentAssociationItem(int intEngagementID)
        {
            DataSet ds;
            DataTable dt, dtpage = null;
            Class1040ScanParentAssoc objParentItem;
            Class1040ScanhangingFrms objUnassociatedFormsItem = null;
            RWReference objPageRef;
            RWPage objPage;
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.ParentAssociation);
            var strCodes = string.Empty;
            int intPrevEngagementFaxFormID = 0;
            var strpage = new StringBuilder();

            CommonCode.WriteDBLog("Retrieving Data For Parent Association...", 759, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Retrieving Data For Parent Association...");
            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetUnassociatedFaxForms", spParams, true);


            ds.Tables[0].TableName = "UnassociatedForms";
            dt = ds.Tables["UnassociatedForms"];

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    CommonCode.AppendString(row["engagementpageid"].ToString(), strpage);
                }

                CommonCode.WriteDBLog("Retrieving Data For Page Referencing...", 760, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);

                spParams = new SpParameter[3];
                spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                spParams[1] = new SpParameter("@ForData", 1);
                spParams[2] = new SpParameter("@EngagementPageID", strpage.ToString());
                ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_PageGetShowAll", spParams, true);


                ds.Tables[0].TableName = "PageFaxForms";
                dtpage = ds.Tables["PageFaxForms"];
            }

            foreach (DataRow row in dt.Rows)
            {
                if (intPrevEngagementFaxFormID != Convert.ToInt32(row["Engagementfaxformid"]))
                {
                    objUnassociatedFormsItem = new Class1040ScanhangingFrms(
                        Convert.ToInt32(row["EngagementID"]),
                        Convert.ToInt32(row["EngagementFaxFormID"]),
                        row["FormName"].ToString(),
                        (int)CommonCode.ReplaceNull(row["ParentEngagementFaxFormID"], "0"),
                        row["ParentFaxFormDWPCode"].ToString(),
                        Convert.ToInt32(row["EngagementPageID"]),
                        row["FileName"].ToString(),
                        Convert.ToInt32(row["ClientPageDPI"]),
                        (string)(row["FileType"]),
                        row["isAutomatched"].ToString()
                    );
                    objChecker.AddUnassociatedFormsItem(objUnassociatedFormsItem);
                    strCodes += row["ParentFaxFormDWPCode"] + ",";
                    intPrevEngagementFaxFormID = Convert.ToInt32(row["Engagementfaxformid"]);
                }

                if (Convert.ToInt32(CommonCode.ReplaceNull(row["EngagementPageID"], "0")) > 0)
                {
                    objPage = new RWPage(
                        Convert.ToInt32(row["EngagementPageID"]),
                        0,
                        "",
                        row["FileName"].ToString(),
                        "",
                        0,
                        0,
                        Convert.ToInt32(row["VirtualRotation"]),
                        false,
                        "",
                        Convert.ToInt32(row["ClientPageDPI"]),
                        row["FileType"].ToString()
                    );

                    if (dtpage != null && dtpage.Rows.Count > 0)
                    {
                        var drow = dtpage.Select($"Engagementpageid={row["EngagementPageID"]} And Engagementfaxformid={row["Engagementfaxformid"]}");
                        if (drow != null && drow.Length > 0)
                        {
                            foreach (var pageRow in drow)
                            {
                                objPageRef = new RWReference(
                                    Convert.ToInt32(pageRow["EngagementPageID"]),
                                    pageRow["Fieldvalue"].ToString(),
                                    Convert.ToInt32(pageRow["FFX"]),
                                    Convert.ToInt32(pageRow["FFY"]),
                                    Convert.ToInt32(pageRow["FFHeight"]),
                                    Convert.ToInt32(pageRow["FFwidth"]),
                                    pageRow["FaxDwpcode"].ToString(),
                                    Convert.ToInt32(CommonCode.ReplaceNull(pageRow["Faxrownumber"], "0")),
                                    Convert.ToInt32(pageRow["DataType"]),
                                    Convert.ToInt32(pageRow["Faxformid"]),
                                    Convert.ToInt32(pageRow["Faxformfieldid"]),
                                    pageRow["faxformName"].ToString(),
                                    pageRow["faxFieldname"].ToString(),
                                    pageRow["Engformname"].ToString(),
                                    Convert.ToInt32(pageRow["EngagementfaxformId"]),
                                    Convert.ToInt32(pageRow["TaxFormInstanceNo"]),
                                    Convert.ToInt32(pageRow["Uncertainchar"])
                                );
                                objPageRef.DropDownDWPCode = pageRow["DropDownDWPCode"].ToString();
                                objPageRef.EngagementFaxFormFieldID = Convert.ToInt32(pageRow["EngagementFaxFormFieldID"]);
                                objPageRef.Identifier = pageRow["Identifier"].ToString();
                                objPageRef.IsEditable = Convert.ToInt32(pageRow["IsEditable"]);
                                objPage.AddPageReference(objPageRef);
                                objPageRef = null;
                            }
                        }
                    }
                    objUnassociatedFormsItem.AddPage(objPage);
                    objPage = null;
                }
            }

            objUnassociatedFormsItem = null;

            var StrDocumentTypeID = new StringBuilder();
            foreach (DataRow row in dt.Rows)
            {
                if (Convert.ToInt32(CommonCode.ReplaceNull(row["EngagementFaxFormID"], "0")) > 0)
                {
                    CommonCode.AppendString(row["DocumentTypeID"].ToString(), StrDocumentTypeID);
                }
            }

            CommonCode.AppendString("0", StrDocumentTypeID);

            if (StrDocumentTypeID.ToString().Contains("5") && StrDocumentTypeID.ToString().Contains("3"))
            {
                StrDocumentTypeID = new StringBuilder("1");
            }
            else if (StrDocumentTypeID.ToString().Contains("5"))
            {
                StrDocumentTypeID = new StringBuilder("2");
            }
            else if (!StrDocumentTypeID.ToString().Contains("5"))
            {
                StrDocumentTypeID = new StringBuilder("3");
            }


            spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetParentFaxFormTree", spParams, true);


            ds.Tables[0].TableName = "ParentForms";
            dt = ds.Tables["ParentForms"];

            foreach (DataRow row in dt.Rows)
            {
                if (!((StrDocumentTypeID.ToString() == "2" && Convert.ToInt32(row["DocumentTypeID"]) == 3) ||
                      (StrDocumentTypeID.ToString() == "3" && Convert.ToInt32(row["DocumentTypeID"]) == 5)) &&
                    strCodes.Contains($",{row["FaxFormDWPCode"]},"))
                {
                    objParentItem = new Class1040ScanParentAssoc(
                        intEngagementID,
                        Convert.ToInt32(row["faxformid"]),
                        Convert.ToInt32(CommonCode.ReplaceNull(row["EngagementFaxFormID"], "0")),
                        row["FormName"].ToString(),
                        CommonCode.ReplaceNull(row["FaxFormInstance"], "").ToString(),
                        Convert.ToInt32(row["faxformid"]),
                        row["FaxFormDWPCode"].ToString(),
                        0,
                        "",
                        "",
                        0,
                        "",
                        "",
                        row["FaxFormName"].ToString(),
                        (Class1040ScanParentAssoc.OperationEnum)Operations.Unaffected,
                        (int)row["Addedbyuser"],
                        0,
                        0,
                        Convert.ToInt32(CommonCode.ReplaceNull(row["EngagementFormID"], "0"))
                    );
                    objChecker.AddParentAssocItem(objParentItem);
                }
            }

            dt = null;
            ds = null;

            if (objChecker.GetCollection().Count <= 0 || objChecker.GetParentFormsCollection().Count <= 0)
            {
                CommonCode.WriteDBLog("CFA Wizard Not Open.. ", 761, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine("CFA Wizard Not Open.. ", intEngagementID);
                UpdateParentCompleted(intEngagementID);
            }

            return objChecker;
        }
        private Class1040ScanChecker GetSupercededDocuments(int intEngagementID)
        {
            DataTable dtSupercededDocuments, dtpage = null;
            Class1040Superseded objSupercededDocuments;

            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.SupercededDocuments);
            var strpage = new StringBuilder();
            RWReference objPageRef;

            CommonCode.WriteDBLog("Retrieving Data For Superceded documents...", 762, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Retrieving Data For Superceded documents...");

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };
            dtSupercededDocuments = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataTable("Proc_GetSupercededDocuments", spParams, "SupercededDocuments", true);


            if (dtSupercededDocuments != null && dtSupercededDocuments.Rows.Count > 0)
            {
                foreach (DataRow row in dtSupercededDocuments.Rows)
                {
                    CommonCode.AppendString(row["engagementpageid"].ToString(), strpage);
                }

                CommonCode.WriteDBLog("Retrieving Data For Page Referencing...", 763, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);

                string pageid = strpage.Length > 0 ? strpage.ToString() : string.Empty;


                spParams = new SpParameter[]
               {
                new SpParameter("@EngagementID", intEngagementID),
                new SpParameter("@ForData", 4),
                new SpParameter("@EngagementPageID", pageid)
               };
                dtpage = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataTable("Proc_PageGetShowAll", spParams, "PageFaxForms", true);

            }

            foreach (DataRow row in dtSupercededDocuments.Rows)
            {
                objSupercededDocuments = new Class1040Superseded(
                    (int)row["EngagementID"],
                    (int)row["EngagementFaxFormID"],
                    (int)row["EngagementPageID"],
                    (string)row["FaxFormName"],
                    (string)row["PrimaryValue"],
                    (string)row["SecondaryValue"],
                    (string)row["FileName"],
                    (int)row["ClientPageDPI"],
                    (string)row["FileType"],
                    false
                );

                if (dtpage != null && dtpage.Rows.Count > 0)
                {
                    var drow = dtpage.Select($"Engagementpageid={row["EngagementPageID"]}");
                    if (drow != null && drow.Length > 0)
                    {
                        foreach (var pageRow in drow)
                        {
                            objPageRef = new RWReference(
                                (int)pageRow["EngagementPageID"],
                                (string)pageRow["Fieldvalue"],
                                (int)pageRow["FFX"],
                                (int)pageRow["FFY"],
                                (int)pageRow["FFHeight"],
                                (int)pageRow["FFwidth"],
                                (string)pageRow["FaxDwpcode"],
                                (int)(IsDBNull(pageRow["Faxrownumber"]) ? 0 : pageRow["Faxrownumber"]),
                                (int)pageRow["DataType"],
                                (int)pageRow["Faxformid"],
                                (int)pageRow["Faxformfieldid"],
                                (string)pageRow["faxformName"],
                                (string)pageRow["faxFieldname"],
                                (string)pageRow["Engformname"],
                                (int)pageRow["EngagementfaxformId"],
                                (int)pageRow["TaxFormInstanceNo"],
                                (int)pageRow["Uncertainchar"]
                            )
                            {
                                DropDownDWPCode = (string)pageRow["DropDownDWPCode"],
                                EngagementFaxFormFieldID = (int)pageRow["EngagementFaxFormFieldID"],
                                Identifier = (string)pageRow["Identifier"],
                                IsEditable = Convert.ToInt32(pageRow["IsEditable"])
                            };
                            objSupercededDocuments.AddPageReference(objPageRef);
                        }
                    }
                }
                objChecker.AddSupercededItem(objSupercededDocuments);
            }

            if (objChecker.GetCollection().Count <= 0)
            {
                UpdateSupercededCompleted(intEngagementID);
                CommonCode.WriteDBLog("Superceded Wizard Not Open...", 764, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine("Superceded Wizard Not Open...", intEngagementID);
            }

            return objChecker;
        }

        private Class1040ScanChecker GetProformaAssociationItem(int intEngagementID)
        {
            DataSet ds;
            DataTable dt, dtpage = null;
            Class1040ScanProformaForms objProformaFormItem;
            Class1040ScanFaxForms objFaxFormItem = null;
            RWPage objPage;
            RWReference objPageRef;
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.ProformaFormAssociation);
            int intPrevEngagementFormID = 0;
            bool blnIsMultiInstance = false;
            int intPrevFieldGroupID = 0, intPrevFaxRowNumber = 0, intPrevPageID = 0;

            CommonCode.WriteDBLog("Retrieving Data For Proforma Form Association...", 765, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Retrieving Data For Proforma Form Association...");

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_OCRProformadFormData_02", spParams, true);


            ds.Tables[0].TableName = "FaxForms";
            dt = ds.Tables["FaxForms"];

            objChecker.ProformadFormFieldGroup = Get_NFRProformadFormFieldGroup(intEngagementID);
            objChecker.FaxFormFieldGroup = Get_NFRFaxFormFieldGroup(intEngagementID);

            if (dt != null && dt.Rows.Count > 0)
            {
                CommonCode.WriteDBLog("Retrieving Data For Page Referencing...", 766, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);


                spParams = new SpParameter[]
               {
                new SpParameter("@EngagementID", intEngagementID),
                new SpParameter("@ForData", 2),
                new SpParameter("@EngagementPageID", "")
               };
                ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_PageGetShowAll", spParams, true);


                ds.Tables[0].TableName = "PageFaxForms";
                dtpage = ds.Tables["PageFaxForms"];
            }

            for (int intCount = 0; intCount < dt.Rows.Count; intCount++)
            {
                if (intPrevEngagementFormID == 0 || intPrevEngagementFormID != Convert.ToInt32(dt.Rows[intCount]["TaxFormInstanceNo"]))
                {
                    blnIsMultiInstance = dt.Rows[intCount]["IsMultiInstance"].ToString() == "N" ? false : true;

                    objFaxFormItem = new Class1040ScanFaxForms(
                        intEngagementID,
                        Convert.ToInt32(dt.Rows[intCount]["FormTypeID"]),
                        dt.Rows[intCount]["FormTypeName"].ToString(),
                        dt.Rows[intCount]["TaxInputForm"].ToString(),
                        Convert.ToInt32(dt.Rows[intCount]["SeqFt"]),
                        Convert.ToInt32(dt.Rows[intCount]["TaxFormSequenceNo"]),
                        Convert.ToInt32(dt.Rows[intCount]["TaxFormInstanceNo"]),
                        blnIsMultiInstance,
                        dt.Rows[intCount]["FormTypeDWPCode"].ToString(),
                        dt.Rows[intCount]["IsAutoMatched"].ToString()
                    );

                    objFaxFormItem.EngagementFormID = Convert.IsDBNull(dt.Rows[intCount]["EngagementFormID"]) ? 0 : Convert.ToInt32(dt.Rows[intCount]["EngagementFormID"]);
                    objFaxFormItem.EngagementFaxFormID = Convert.IsDBNull(dt.Rows[intCount]["EngagementFaxFormID"]) ? 0 : Convert.ToInt32(dt.Rows[intCount]["EngagementFaxFormID"]);
                    intPrevEngagementFormID = Convert.ToInt32(dt.Rows[intCount]["TaxFormInstanceNo"]);
                    objFaxFormItem.EntityNumber = Convert.IsDBNull(dt.Rows[intCount]["EntityNumber"]) ? "" : dt.Rows[intCount]["EntityNumber"].ToString();
                    objFaxFormItem.FaxRowNumber = Convert.IsDBNull(dt.Rows[intCount]["FaxRowNumber"]) ? 0 : Convert.ToInt32(dt.Rows[intCount]["FaxRowNumber"]);
                    objFaxFormItem.FromEngagementID = (int)CommonCode.ReplaceNull(dt.Rows[intCount]["FromEngagementID"], "0");

                    objChecker.AddFaxFormItem(objFaxFormItem);
                }


                if (Convert.ToBoolean(Convert.IsDBNull(dt.Rows[intCount]["EngagementPageID"]) ? 0 : Convert.ToInt32(dt.Rows[intCount]["EngagementPageID"]) > 0))
                {

                    objPage = new RWPage(
                     Convert.ToInt32(dt.Rows[intCount]["EngagementPageID"]),
                     0,
                     dt.Rows[intCount]["PageName"].ToString(),
                     dt.Rows[intCount]["FileName"].ToString(),
                     Convert.ToString(dt.Rows[intCount]["PageName"]),
                     Convert.ToInt32(dt.Rows[intCount]["SequenceNumber"]),
                     0,
                     Convert.ToInt32(dt.Rows[intCount]["VirtualRotation"]),
                     blnIsMultiInstance,
                     "",
                     Convert.ToInt32(dt.Rows[intCount]["ClientPageDPI"]),
                     dt.Rows[intCount]["FileType"].ToString()
                    );


                    if (dtpage != null && dtpage.Rows.Count > 0)
                    {
                        DataRow[] dRow = dtpage.Select($"Engagementpageid={dt.Rows[intCount]["EngagementPageID"]} And TaxFormInstanceNo={dt.Rows[intCount]["TaxFormInstanceNo"]}");
                        if (dRow != null && dRow.Length > 0)
                        {
                            for (int rpagecnt = 0; rpagecnt < dRow.Length; rpagecnt++)
                            {
                                objPageRef = new RWReference(
                                    Convert.ToInt32(dRow[rpagecnt]["EngagementPageID"]),
                                    dRow[rpagecnt]["Fieldvalue"].ToString(),
                                    Convert.ToInt32(dRow[rpagecnt]["FFX"]),
                                    Convert.ToInt32(dRow[rpagecnt]["FFY"]),
                                    Convert.ToInt32(dRow[rpagecnt]["FFHeight"]),
                                    Convert.ToInt32(dRow[rpagecnt]["FFwidth"]),
                                    dRow[rpagecnt]["FaxDwpcode"].ToString(),
                                    Convert.IsDBNull(dRow[rpagecnt]["Faxrownumber"]) ? 0 : Convert.ToInt32(dRow[rpagecnt]["Faxrownumber"]),
                                    Convert.ToInt32(dRow[rpagecnt]["DataType"].ToString()),
                                    Convert.ToInt32(dRow[rpagecnt]["Faxformid"]),
                                    Convert.ToInt32(dRow[rpagecnt]["Faxformfieldid"]),
                                    dRow[rpagecnt]["faxformName"].ToString(),
                                    dRow[rpagecnt]["faxFieldname"].ToString(),
                                    dRow[rpagecnt]["Engformname"].ToString(),
                                    Convert.ToInt32(dRow[rpagecnt]["EngagementfaxformId"]),
                                    Convert.ToInt32(dRow[rpagecnt]["TaxFormInstanceNo"]),
                                    Convert.ToInt32(dRow[rpagecnt]["Uncertainchar"].ToString())
                                );

                                objPageRef.DropDownDWPCode = dRow[rpagecnt]["DropDownDWPCode"].ToString();
                                objPageRef.EngagementFaxFormFieldID = Convert.ToInt32(dRow[rpagecnt]["EngagementFaxTaxFormFieldID"]);
                                objPageRef.Identifier = dRow[rpagecnt]["Identifier"].ToString();
                                objPageRef.IsEditable = Convert.ToInt32(dRow[rpagecnt]["IsEditable"]);
                                objPage.AddPageReference(objPageRef);
                                objPageRef = null;
                            }
                            dRow = null;
                        }
                    }
                    objFaxFormItem.AddPage(objPage);
                    objPage = null;
                }
            }

            if (objChecker.GetCollection().Count <= 0 || objChecker.GetProformaFormsCollection().Count <= 0)
            {
                CommonCode.WriteDBLog("NFR Wizard Not Open.. ", 767, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine("NFR Wizard Not Open...", intEngagementID);
                UpdateProformaFormCompleted(intEngagementID);
            }

            return objChecker;
        }
        private Class1040ScanChecker GetTaxParentAssociationItem(int intEngagementID)
        {
            DataSet ds;
            DataTable dt;
            Class1040ScanTaxParentAssociation objParentItem = null;
            Class1040ScanTaxParentAssociation objChildItem = null;
            RWPage objPage;
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.TaxParentAssociation);
            int intPrevEngagementFormID = 0;
            int intPrevFormTypeID = 0;
            bool blnIsMultiInstance = false;

            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "Retrieving Data For Tax Parent Association...", 987, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            CommonCode.WriteDBLog("Retrieving Data For Tax Parent Association...", 768, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Retrieving Data For Tax Parent Association...");
            // Retrieve child forms

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_OCRParentChildData_02", spParams, true);


            ds.Tables[0].TableName = "childForms";
            dt = ds.Tables["childForms"];

            for (int intCount = 0; intCount < dt.Rows.Count; intCount++)
            {
                if (intPrevEngagementFormID != Convert.ToInt32(dt.Rows[intCount]["EngagementFormID"]))
                {
                    if (intCount > 0)
                    {
                        objChecker.AddTaxChildItem(objChildItem);
                        objChildItem = null;
                    }

                    blnIsMultiInstance = dt.Rows[intCount]["IsMultiInstance"].ToString() != "N";
                    objChildItem = new Class1040ScanTaxParentAssociation(
                        intEngagementID,
                        Convert.ToInt32(dt.Rows[intCount]["FormTypeID"]),
                        dt.Rows[intCount]["FormTypeName"].ToString(),
                        dt.Rows[intCount]["FormTypeDWPCode"].ToString(),
                        Convert.ToInt32(dt.Rows[intCount]["EngagementFormID"]),
                        dt.Rows[intCount]["InputForm"].ToString(),
                        Convert.ToInt32(dt.Rows[intCount]["SeqFt"]),
                        0,
                        dt.Rows[intCount]["ParentFormDWPCode"].ToString(),
                        blnIsMultiInstance,
                        0
                    );

                    intPrevEngagementFormID = Convert.ToInt32(dt.Rows[intCount]["EngagementFormID"]);
                }


                if (Convert.ToInt32(dt.Rows[intCount]["EngagementPageID"] == DBNull.Value ? 0 : dt.Rows[intCount]["EngagementPageID"]) > 0)
                {
                    objPage = new RWPage(
                    Convert.ToInt32(dt.Rows[intCount]["EngagementPageID"]),
                    Convert.ToInt32(dt.Rows[intCount]["EngagementFormID"]),
                    dt.Rows[intCount]["PageName"].ToString(),
                    dt.Rows[intCount]["FileName"].ToString(),
                    dt.Rows[intCount]["FieldValue"].ToString(),
                    Convert.ToInt32(dt.Rows[intCount]["SequenceNumber"]),
                    Convert.ToInt32(dt.Rows[intCount]["FieldGroupInstance"]),
                    Convert.ToInt32(dt.Rows[intCount]["VirtualRotation"]),
                    blnIsMultiInstance,
                    dt.Rows[intCount]["ParentFormDWPCode"].ToString(),
                    Convert.ToInt32(dt.Rows[intCount]["ClientPageDPI"]),
                    dt.Rows[intCount]["FileType"].ToString()
                    );
                    objChildItem.AddPage(objPage);
                    objPage = null;
                }

            }

            if (dt.Rows.Count > 0)
            {
                objChecker.AddTaxChildItem(objChildItem);
                objChildItem = null;
            }

            // Retrieve parent forms

            spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_OCRParentChildData_01", spParams, true);


            ds.Tables[0].TableName = "ParentForms";
            dt = ds.Tables["ParentForms"];

            intPrevEngagementFormID = 0;
            intPrevFormTypeID = 0;

            for (int intCount = 0; intCount < dt.Rows.Count; intCount++)
            {
                if (intPrevFormTypeID != Convert.ToInt32(dt.Rows[intCount]["ParentFormTypeID"]) ||
                    intPrevEngagementFormID != Convert.ToInt32(dt.Rows[intCount]["EngagementFormID"]))
                {
                    if (intCount > 0)
                    {
                        objChecker.AddTaxParentItem(objParentItem);
                        objParentItem = null;
                    }

                    blnIsMultiInstance = dt.Rows[intCount]["IsMultiInstance"].ToString() != "N";
                    objParentItem = new Class1040ScanTaxParentAssociation(
                        intEngagementID,
                        Convert.ToInt32(dt.Rows[intCount]["ParentFormTypeID"]),
                        dt.Rows[intCount]["ParentFormTypeName"].ToString(),
                        dt.Rows[intCount]["ParentFormDWPCode"].ToString(),
                        Convert.ToInt32(dt.Rows[intCount]["EngagementFormID"]),
                        dt.Rows[intCount]["ParentInputForm"].ToString(),
                        Convert.ToInt32(dt.Rows[intCount]["ParentSeqFt"]),
                        0,
                        dt.Rows[intCount]["ParentFormDWPCode"].ToString(),
                        blnIsMultiInstance,
                        Convert.ToInt32(dt.Rows[intCount]["ProformadDataID"])
                    );

                    intPrevEngagementFormID = Convert.ToInt32(dt.Rows[intCount]["EngagementFormID"]);
                    intPrevFormTypeID = Convert.ToInt32(dt.Rows[intCount]["ParentFormTypeID"]);
                }

                if (Convert.ToInt32(dt.Rows[intCount]["EngagementPageID"] == DBNull.Value ? 0 : dt.Rows[intCount]["EngagementPageID"]) > 0)
                {
                    objPage = new RWPage(
                    Convert.ToInt32(dt.Rows[intCount]["EngagementPageID"]),
                    Convert.ToInt32(dt.Rows[intCount]["EngagementFormID"]),
                    dt.Rows[intCount]["PageName"].ToString(),
                    dt.Rows[intCount]["FileName"].ToString(),
                    dt.Rows[intCount]["PageName"].ToString(),
                    Convert.ToInt32(dt.Rows[intCount]["SequenceNumber"]),
                    0,
                    Convert.ToInt32(dt.Rows[intCount]["VirtualRotation"]),
                    blnIsMultiInstance,
                    dt.Rows[intCount]["ParentFormDWPCode"].ToString(),
                    Convert.ToInt32(dt.Rows[intCount]["ClientPageDPI"]),
                    dt.Rows[intCount]["FileType"].ToString()
  );
                    objParentItem.AddPage(objPage);
                    objPage = null;
                }
            }

            if (dt.Rows.Count > 0)
            {
                objChecker.AddTaxParentItem(objParentItem);
                objParentItem = null;
            }

            dt = null;
            ds = null;

            if (objChecker.GetCollection().Count <= 0 || objChecker.GetTaxParentCollection().Count <= 0)
            {
                UpdateTaxParentCompleted(intEngagementID);
            }

            //LogEntry(CommonCode.Severity.Low, "DDPAgent", $"GetTaxParentAssociationItem completed for EngagementID : {intEngagementID} -> {DateTime.Now}", 988, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            return objChecker;
        }

        private void UpdateForCorrection(int engagementID, int intEngagementOCRFieldID, string strCorrectedValue, string strForCorrection)
        {

            var spParams = new SpParameter[3];
            spParams[0] = new SpParameter("@EngagementOCRFieldID", intEngagementOCRFieldID);
            spParams[1] = new SpParameter("@OCRVerifiedValue", strCorrectedValue);
            spParams[2] = new SpParameter("@IsCorrection", strForCorrection);
            CommonCode.GetEngagementDbCommon(engagementID).AddUpdateOrDelete("Proc_UpdateOCRVerificationData", spParams, true);

        }

        private void Fax2Tax(int intEngagementID, string strActionType = "T")
        {
            CommonCode.WriteDBLog("Converting Fax Fields to Tax Fields...", 769, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Converting Fax Fields to Tax Fields...", intEngagementID);

            var spParams = new SpParameter[2];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            spParams[1] = new SpParameter("@ActionType", strActionType);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_GenerateEngagementTaxFormFromFaxForm", spParams, true, false);

        }

        private void Fax2Tax1(int intEngagementID)
        {
            CommonCode.WriteDBLog("Converting Fax Fields to Tax Fields - 1...", 770, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Converting Fax Fields to Tax Fields - 1...", intEngagementID);

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_GenerateEngagementTaxFormFromFaxForm_01", spParams, true, false);

        }

        public int GetRecentEngagementPageCount(int engagementId, int withLead)
        {
            int userFaxToTax = 0;


            var spParams = new SpParameter[3];
            spParams[0] = new SpParameter("@EngagementID", engagementId);
            spParams[1] = new SpParameter("@WithLead", withLead);
            spParams[2] = new SpParameter("@UseNewFax2Tax", userFaxToTax, ParameterDirection.Output);

            userFaxToTax = (int)CommonCode.GetEngagementDbCommon(engagementId).GetData("Proc_GetRecentEngagementPageCount", spParams, true);
            return userFaxToTax;

        }

        private int GetEngagementTypeId(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                return row.IsNull("Engagementtypeid") ? 0 : Convert.ToInt32(row["Engagementtypeid"]);
            }
            else
            {
                return -1;
            }
        }

        private void Fax2Tax2(int intEngagementID)
        {
            CommonCode.WriteDBLog("Converting Fax Fields to Tax Fields - 2...", 771, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Converting Fax Fields to Tax Fields - 2...", intEngagementID);
            DataTable dt;
            string result;
            int isEnableFax2TaxVal;

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);


            dt = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataTable("Proc_FaxToTaxSpEngagements", spParams, "FaxToTaxSpEngagements", true);


            int engagementTypeID = Convert.ToInt32( GetEngagementTypeId(dt));

            if (engagementTypeID >= 0)
            {
                int withLeadSheet = (engagementTypeID == 5 || engagementTypeID == 6) ? 1 : 0;

                //isEnableFax2TaxVal = GetRecentEngagementPageCount(intEngagementID, withLeadSheet);

                //var objFaxToTax2Api = new OrchFax2TaxApiCall(isEnableFax2TaxVal, intEngagementID, 1, Token.AuthToken, DomainId, UserId, null, "F2T");
                //result = objFaxToTax2Api.ApiRequestCall();

                CommonCode.WriteDBLog("Associate K-1 Pages...", 772, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);


                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_AssociateK1Pages", spParams, true, false);


                if (CommonCode.GetEngagementDBConnectionID(intEngagementID) > 1)
                {
                    CommonCode.WriteDBLog("Sync Engagements ...", 773, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                    Console.WriteLine("Sync Engagements ...", intEngagementID);


                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_SyncReviewWizardFlags", spParams, true, false);

                }
            }
        }
        private Class1040ScanChecker GetDuplicateItem(int intEngagementID, int intPreProcessing, Class1040ScanChecker.ActivityType enmActivity, bool blnWizardOption)
        {
            DataSet dsOrg, dsSrc;
            DataTable dtOrg, dtSrc;
            Class1040ScanDuplicateData objDuplicateItem;
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.Duplicate);
            bool blnMarkDuplicate = false, blnFieldExists = false, blnDuplicateProcessed = false;

            string strInputForm = string.Empty;
            string strFieldDWPCode = string.Empty;

            int intEngagementPageID, intFaxRowNumber, intSourceFaxRowNumber, intEngagementFaxFormID;

            DataRow[] drOrgRows, drSrcRows;

            CommonCode.WriteDBLog("Retrieving Data For Duplicate...", 774, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Retrieving Data For Duplicate...");

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID),
            new SpParameter("@PreProcessing", intPreProcessing)
            };
            dsSrc = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetDuplicateData", spParams, true);


            dsSrc.Tables[0].TableName = "DuplicateData";
            dtSrc = dsSrc.Tables["DuplicateData"];

            dsOrg = dsSrc.Copy();
            dtOrg = dsOrg.Tables[0];

            drOrgRows = dtOrg.Select("FaxFormType = '2' And Identifier = ''", "InputForm");
            drSrcRows = dtSrc.Select("FaxFormType = '1' And Identifier = ''", "InputForm");

            if (drOrgRows.Length > 0 && Convert.ToInt32(drOrgRows[0]["DuplicateProcessed"]) > 0)
            {
                blnDuplicateProcessed = true;
            }

            if (enmActivity == Class1040ScanChecker.ActivityType.Duplicate ||
                (enmActivity == Class1040ScanChecker.ActivityType.NewDuplicate && !blnDuplicateProcessed))
            {
                foreach (var orgRow in drOrgRows)
                {
                    blnMarkDuplicate = false;

                    strInputForm = ReplaceSP(orgRow["InputForm"].ToString(), "'", "''");
                    strFieldDWPCode = orgRow["FieldDWPCode"].ToString();

                    if (!string.IsNullOrEmpty(strInputForm))
                    {
                        drOrgRows = dtOrg.Select($"FaxFormType = '2' And FieldType = '' And FieldDWPCode = '{orgRow["FieldDWPCode"]}' And Identifier = '' And InputForm = '{strInputForm}'", "InputForm");

                        blnFieldExists = false;

                        drSrcRows = dtSrc.Select($"FaxFormType = '1' And InputForm = '{strInputForm}' And Identifier = ''", "InputForm");

                        if (drSrcRows.Length > 0)
                        {
                            foreach (var orgRowInner in drOrgRows)
                            {
                                if (string.IsNullOrEmpty(orgRowInner["FieldType"].ToString()))
                                {
                                    intEngagementPageID = Convert.ToInt32(orgRowInner["EngagementPageID"]);
                                    intFaxRowNumber = Convert.ToInt32(orgRowInner["FaxRowNumber"]);

                                    foreach (var srcRow in drSrcRows)
                                    {
                                        drOrgRows = dtOrg.Select($"FaxFormType = '2' And FieldType = '' And FieldDWPCode = '{srcRow["FieldDWPCode"]}' And Identifier = '' And InputForm = '{ReplaceSP(srcRow["InputForm"].ToString(), "'", "''")}' And FaxRowNumber = {intFaxRowNumber} And EngagementPageID = {intEngagementPageID}", "InputForm");

                                        if (drOrgRows.Length > 0)
                                        {
                                            blnFieldExists = true;
                                            if (!string.IsNullOrEmpty(drOrgRows[0]["FFValue"].ToString()))
                                            {
                                                drOrgRows[0]["DuplicateFieldID"] = srcRow["EngagementFaxTaxFormFieldID"];
                                                if (IsEqualValue(srcRow["FFValue"].ToString(), drOrgRows[0]["FFValue"].ToString(), Convert.ToInt32(srcRow["DataType"])))
                                                {
                                                    drOrgRows[0]["FieldType"] = "D";
                                                }
                                                else
                                                {
                                                    drOrgRows[0]["FieldType"] = "C";
                                                }
                                                drOrgRows[0]["DupFieldType"] = "D";
                                            }
                                        }
                                    }
                                }

                                drOrgRows = dtOrg.Select($"FaxFormType = '2' And Identifier = '' And FieldDWPCode = '{strFieldDWPCode}' And InputForm = '{strInputForm}'", "InputForm");
                            }
                        }

                        drOrgRows = dtOrg.Select("FaxFormType = '2' And Identifier = ''", "InputForm");

                        if (!blnFieldExists)
                        {
                            if (!IsFieldExist(dtOrg, dtSrc, strInputForm))
                            {
                                drSrcRows = dtSrc.Select($"FaxFormType = '1' And InputForm = '{strInputForm}' And Identifier = 'P'", "InputForm");
                                foreach (var srcRow in drSrcRows)
                                {
                                    drOrgRows = dtOrg.Select($"FaxFormType = '2' And FieldType = '' And FieldDWPCode = '{srcRow["FieldDWPCode"]}' And InputForm = '{ReplaceSP(srcRow["InputForm"].ToString(), "'", "''")}'", "InputForm");
                                    if (drOrgRows.Length > 0)
                                    {
                                        drOrgRows[0]["DuplicateFieldID"] = srcRow["EngagementFaxTaxFormFieldID"];
                                        drOrgRows[0]["FieldType"] = "D";
                                    }
                                }

                                drSrcRows = dtSrc.Select($"FaxFormType = '1' And InputForm = '{strInputForm}' And Identifier = 'S'", "InputForm");
                                foreach (var srcRow in drSrcRows)
                                {
                                    drOrgRows = dtOrg.Select($"FaxFormType = '2' And FieldType = '' And FieldDWPCode = '{srcRow["FieldDWPCode"]}' And InputForm = '{ReplaceSP(srcRow["InputForm"].ToString(), "'", "''")}'", "InputForm");
                                    if (drOrgRows.Length > 0)
                                    {
                                        drOrgRows[0]["DuplicateFieldID"] = srcRow["EngagementFaxTaxFormFieldID"];
                                        drOrgRows[0]["FieldType"] = "D";
                                    }
                                }
                            }

                            drOrgRows = dtOrg.Select($"FaxFormType = '2' And Identifier = '' And FieldType = '' And FieldDWPCode = '{strFieldDWPCode}'", "InputForm");
                        }

                        drOrgRows = dtOrg.Select("FaxFormType = '2' And Identifier = ''", "InputForm");
                        drSrcRows = dtSrc.Select("FaxFormType = '1' And Identifier = ''", "InputForm");
                    }
                }

                foreach (var srcRow in drSrcRows)
                {
                    blnMarkDuplicate = false;

                    intEngagementFaxFormID = Convert.ToInt32(srcRow["EngagementFaxFormID"]);

                    drOrgRows = dtOrg.Select($"FaxFormType = '2' And FieldType = '' And FieldDWPCode = '{srcRow["FieldDWPCode"]}' And Identifier = '' And DataType = 1", "InputForm");

                    foreach (var orgRow in drOrgRows)
                    {
                        if (!string.Equals(orgRow["InputForm"].ToString(), srcRow["InputForm"].ToString(), StringComparison.OrdinalIgnoreCase) &&
                            string.IsNullOrEmpty(orgRow["FieldType"].ToString()) &&
                            IsEqualValue(orgRow["FFValue"].ToString(), srcRow["FFValue"].ToString(), Convert.ToInt32(orgRow["DataType"])))
                        {
                            blnMarkDuplicate = false;

                            intEngagementPageID = Convert.ToInt32(orgRow["EngagementPageID"]);
                            intFaxRowNumber = Convert.ToInt32(orgRow["FaxRowNumber"]);
                            intSourceFaxRowNumber = Convert.ToInt32(srcRow["FaxRowNumber"]);

                            drOrgRows = dtOrg.Select($"FaxFormType = '2' And FieldType = '' And FaxRowNumber = {intFaxRowNumber} And EngagementPageID = {intEngagementPageID} And DataType = 1 And Identifier = ''", "InputForm");

                            foreach (var orgRowInner in drOrgRows)
                            {
                                drSrcRows = dtSrc.Select($"FaxFormType = '1' And Identifier = '' And FieldDWPCode = '{orgRowInner["FieldDWPCode"]}' And EngagementFaxFormID = {intEngagementFaxFormID} And DataType = 1 And FaxRowNumber = {intSourceFaxRowNumber}", "InputForm");

                                blnMarkDuplicate = false;
                                foreach (var srcRowInner in drSrcRows)
                                {
                                    if (!string.IsNullOrEmpty(srcRowInner["FFValue"].ToString()) &&
                                        IsEqualValue(orgRowInner["FFValue"].ToString(), srcRowInner["FFValue"].ToString(), Convert.ToInt32(orgRowInner["DataType"])))
                                    {
                                        blnMarkDuplicate = true;
                                    }
                                }

                                if (!blnMarkDuplicate)
                                {
                                    break;
                                }
                            }

                            drSrcRows = dtSrc.Select("FaxFormType = '1' And Identifier = ''", "InputForm");

                            if (blnMarkDuplicate)
                            {
                                drOrgRows = dtOrg.Select($"FaxFormType = '2' And FieldType = '' And Identifier = '' And FieldDWPCode = '{srcRow["FieldDWPCode"]}' And DataType = 1", "InputForm");

                                if (drOrgRows.Length > 0)
                                {
                                    if (enmActivity == Class1040ScanChecker.ActivityType.Duplicate)
                                    {
                                        objDuplicateItem = new Class1040ScanDuplicateData(
                                            Convert.ToInt32(srcRow["EngagementID"]),
                                            Convert.ToInt32(srcRow["EngagementFaxTaxFormFieldID"]),
                                            Convert.ToInt32(drOrgRows[0]["EngagementFaxTaxFormFieldID"]),
                                            srcRow["FaxFormShortName"].ToString(),
                                            srcRow["InputForm"].ToString(),
                                            Convert.ToInt32(srcRow["EngagementFaxFormID"]),
                                            drOrgRows[0]["FaxFormShortName"].ToString(),
                                            drOrgRows[0]["InputForm"].ToString(),
                                            Convert.ToInt32(drOrgRows[0]["EngagementFaxFormID"]),
                                            Convert.ToInt32(srcRow["EngagementPageID"]),
                                            srcRow["FileName"].ToString(),
                                            Convert.ToInt32(drOrgRows[0]["EngagementPageID"]),
                                            drOrgRows[0]["FileName"].ToString(),
                                            srcRow["FaxFieldName"].ToString(),
                                            srcRow["FFValue"].ToString(),
                                            drOrgRows[0]["FaxFieldName"].ToString(),
                                            drOrgRows[0]["FFValue"].ToString(),
                                            false,
                                            Convert.ToInt32(drOrgRows[0]["ClientPageDPI"]),
                                            Convert.ToInt32(srcRow["ClientPageDPI"]),
                                            drOrgRows[0]["FileType"].ToString(),
                                            srcRow["FileType"].ToString(),
                                            Convert.ToInt32(srcRow["FaxRowNumber"]),
                                            Convert.ToInt32(drOrgRows[0]["FaxRowNumber"])
                                        );
                                        objChecker.AddDuplicateItem(objDuplicateItem);
                                    }

                                    drOrgRows[0]["DuplicateFieldID"] = srcRow["EngagementFaxTaxFormFieldID"];
                                    drOrgRows[0]["DupFieldType"] = "D";
                                }
                            }
                        }
                    }
                }

                drOrgRows = dtOrg.Select("FaxFormType = '2'", "InputForm");

                foreach (var orgRow in drOrgRows)
                {
                    if (!string.IsNullOrEmpty(orgRow["FieldType"].ToString()))
                    {
                        UpdateDuplicateField(
                            Convert.ToInt32(orgRow["EngagementFaxTaxFormFieldID"]),
                            orgRow["FieldType"].ToString(),
                            Convert.ToInt32(orgRow["DuplicateFieldID"]),
                            2,
                            intEngagementID
                        );
                    }

                    if (enmActivity == Class1040ScanChecker.ActivityType.NewDuplicate && !string.IsNullOrEmpty(orgRow["DupFieldType"].ToString()))
                    {
                        UpdateProbableDuplicateField(
                            Convert.ToInt32(orgRow["EngagementFaxTaxFormFieldID"]),
                            orgRow["DupFieldType"].ToString(),
                            Convert.ToInt32(orgRow["DuplicateFieldID"]),
                            intEngagementID
                        );
                    }
                }

                if (drOrgRows.Length > 0 && enmActivity == Class1040ScanChecker.ActivityType.NewDuplicate)
                {
                    UpdateDuplicateField(
                        Convert.ToInt32(drOrgRows[0]["EngagementFaxTaxFormFieldID"]),
                        string.Empty,
                        1,
                        2,
                        intEngagementID,
                        1
                    );
                }
            }

            if (blnWizardOption)
            {
                if (enmActivity == Class1040ScanChecker.ActivityType.NewDuplicate)
                {
                    objChecker.DuplicateDataSet = GetDuplicateData(intEngagementID);
                }

                if (enmActivity == Class1040ScanChecker.ActivityType.NewDuplicate)
                {
                    if (!(objChecker.DuplicateDataSet.Tables[0].Rows.Count > 0 &&
                          objChecker.DuplicateDataSet.Tables[1].Rows.Count > 0 &&
                          objChecker.DuplicateDataSet.Tables[2].Rows.Count > 0))
                    {
                        UpdateDuplicateCompleted(intEngagementID);
                        CommonCode.WriteDBLog("Duplicate Wizard Not Open...", 775, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                        Console.WriteLine("Duplicate Wizard Not Open...", intEngagementID);
                    }
                }
                else
                {
                    if (objChecker.GetCollection().Count <= 0)
                    {
                        UpdateDuplicateCompleted(intEngagementID);
                        CommonCode.WriteDBLog("Duplicate Wizard Not Open...", 776, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                        Console.WriteLine("Duplicate Wizard Not Open...", intEngagementID);
                    }
                }

                if (enmActivity == Class1040ScanChecker.ActivityType.NewDuplicate)
                {
                    objChecker.selectedType = Class1040ScanChecker.ActivityType.NewDuplicate;
                }
            }
            else
            {
                UpdateDuplicateCompleted(intEngagementID);
                CommonCode.WriteDBLog("Duplicate Wizard is skipped by user...", 777, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine("Duplicate Wizard is skipped by user...", intEngagementID);
            }

            return objChecker;
        }
        private bool IsFieldExist(DataTable dtOrg, DataTable dtSrc, string strInputForm)
        {
            DataRow[] drOrgRows = dtOrg.Select($"FaxFormType = '2' AND FieldType = '' AND Identifier = '' AND InputForm = '{strInputForm}'", "InputForm");
            DataRow[] drSrcRows = dtSrc.Select($"FaxFormType = '1' AND FieldType = '' AND Identifier = '' AND InputForm = '{strInputForm}'", "InputForm");

            for (int i = 0; i < drOrgRows.Length; i++)
            {
                for (int j = 0; j < drSrcRows.Length; j++)
                {
                    if (drOrgRows[i]["FieldDWPCode"].ToString() == drSrcRows[j]["FieldDWPCode"].ToString())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsEqualValue(string strSrcValue, string strOrgValue, int intDataType)
        {
            if (string.IsNullOrEmpty(strSrcValue) || string.IsNullOrEmpty(strOrgValue))
            {
                return false;
            }

            if (intDataType == 1)
            {
                if (long.TryParse(strSrcValue, out long srcValue) &&
                    long.TryParse(strOrgValue, out long orgValue) &&
                    Math.Abs(srcValue - orgValue) <= 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private void UpdateDuplicateField(int intEngagementFaxTaxFormFieldID, string strFieldType, int intDuplicateFieldID, int intFromNext, int intEngagementID, int intUpdateSDDupFieldType = 0)
        {

            var spParams = new SpParameter[5];
            spParams[0] = new SpParameter("@EngagementFaxTaxFormFieldID", intEngagementFaxTaxFormFieldID);
            spParams[1] = new SpParameter("@FieldType", strFieldType);
            spParams[2] = new SpParameter("@DuplicateFieldID", intDuplicateFieldID);
            spParams[3] = new SpParameter("@FromNext", intFromNext);
            spParams[4] = new SpParameter("@UpdateSDDupFieldType", intUpdateSDDupFieldType);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateDuplicateData", spParams, true, false);

        }

        private void UpdateProbableDuplicateField(int intEngagementFaxTaxFormFieldID, string strDupFieldType, int intDuplicateFieldID, int intEngagementID)
        {

            var spParams = new SpParameter[3];
            spParams[0] = new SpParameter("@EngagementFaxTaxFormFieldID", intEngagementFaxTaxFormFieldID);
            spParams[1] = new SpParameter("@DupFieldType", strDupFieldType);
            spParams[2] = new SpParameter("@DuplicateFieldID", intDuplicateFieldID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateProbableDuplicateData", spParams, true, false);

        }

        private bool UpdateDuplicateCompleted(int intEngagementID)
        {

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagements", spParams, true);

            return true;
        }

        private int DeleteParentFields(int intEngFaxFormID, int intEngagementID)
        {


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@Engagementfaxformid", intEngFaxFormID);
            return CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_DeleteParentFields", spParams, true, false);

        }

        private int UpdateParentCompleted(int intEngagementID)
        {

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            return CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateParentCompleted", spParams, true, false);

        }

        private void UpdateSupercededCompleted(int intEngagementID)
        {

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSupercededCompleted", spParams, true, false);

        }

        private bool UpdateProformaFormCompleted(int intEngagementID)
        {


            var spParams = new SpParameter[2];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            spParams[1] = new SpParameter("@StatusUpdateFor", "ProformaAssocCompleted");
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementsStatus", spParams, true, false);

            return true;
        }

        private void UpdateTaxParentCompleted(int intEngagementID)
        {

            var spParams = new SpParameter[2];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            spParams[1] = new SpParameter("@TaxSoftwareID", 0);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateDefaultState", spParams, true, false);

            spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateTaxParentAssocCompleted", spParams, true, false);

        }

        private int UpdateTaxParentCompleted_Test(int intEngagementID)
        {

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            return CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateTaxParentAssocCompleted", spParams, true, true);

        }

        private void UpdatePreRuleCompleted(int intEngagementId)
        {

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementId);
            CommonCode.GetEngagementDbCommon(intEngagementId).AddUpdateOrDelete("Proc_UpdateOCRValueForCurrencySymbol", spParams, true, false);
            CommonCode.GetEngagementDbCommon(intEngagementId).AddUpdateOrDelete("dbo.Proc_UpdatePreRuleCompleted", spParams, true, false);
            CommonCode.GetEngagementDbCommon(intEngagementId).AddUpdateOrDelete("Proc_UpdateAfterPreVerification", spParams, true, false);

        }
        /// <summary>
        /// This is a ProformaMatching (Step 71)
        /// Under this we call OcrToFax, EvaluateFaxToTaxFormula, GetDuplicateItem, and Fax2Tax methods.
        /// </summary>
        /// <param name="intEngagementID"></param>
        /// <param name="isMultiThreadingActive"></param>
        private void UpdateProformadID(int intEngagementID, bool isMultiThreadingActive)
        {
            currentStatusTable = CommonCode.GetStepDetail(intEngagementID, jobId);

            // Step 7: OCRToFax
            if (!CommonCode.CheckStepStatus(7, 0, intEngagementID, ref currentStatusTable, jobId))
            {
                OCRToFax(intEngagementID, true);
                WriteDBLogsJobId($"Calling OCRToFax At , {DateTime.Now}", intEngagementID, jobId, 7); // Step-7
                Console.WriteLine($"Calling OCRToFax At , {DateTime.Now}");
            }

            // Step 8: EvaluateFaxToTaxFormula
            if (!CommonCode.CheckStepStatus(8, 0, intEngagementID, ref currentStatusTable, jobId))
            {
                EvaluateFaxToTaxFormula(intEngagementID, isMultiThreadingActive);
                WriteDBLogsJobId($"Calling EvaluateFaxToTaxFormula At , {DateTime.Now}", intEngagementID, jobId, 8); // Step-8
                Console.WriteLine($"Calling EvaluateFaxToTaxFormula At , {DateTime.Now}");
            }

            // Step 9: GetDuplicateItem
            if (!CommonCode.CheckStepStatus(9, 0, intEngagementID, ref currentStatusTable, jobId))
            {
                GetDuplicateItem(intEngagementID, 1, Class1040ScanChecker.ActivityType.Duplicate, true); // Changed by Ritesh on 10-06-2011
                WriteDBLogsJobId($"Calling GetDuplicateItem At , {DateTime.Now}", intEngagementID, jobId, 9); // Step-9
                Console.WriteLine($"Calling GetDuplicateItem At , {DateTime.Now}");
            }

            // Step 10: Fax2Tax
            if (!CommonCode.CheckStepStatus(10, 0, intEngagementID, ref currentStatusTable, jobId))
            {
                Fax2Tax(intEngagementID, "P");
                WriteDBLogsJobId($"Calling Fax2Tax At , {DateTime.Now}", intEngagementID, jobId, 10); // Step-10
                Console.WriteLine($"Calling Fax2Tax At , {DateTime.Now}");
            }
        }

        /// <summary>
        /// Populates diagnostic data for the given engagement ID.
        /// </summary>
        /// <param name="intEngagementID"></param>
        /// <param name="strTableName"></param>
        /// <returns></returns>
        private DataSet PopulateDiagnosticData(int intEngagementID, string strTableName)
        {
            DataSet ds;

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetDiagnosticData", spParams, true);

            ds.Tables[0].TableName = strTableName;
            return ds;
        }

        /// <summary>
        /// Updates Engagement SkipVerification as '1' when verification is skipped.
        /// </summary>
        /// <param name="intEngagementID"></param>
        /// <returns></returns>
        public bool UpdateEngVerificationOption(int intEngagementID)
        {

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID),
            new SpParameter("@StatusUpdateFor", "SkipVerification")
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementsStatus", spParams, true, false);

            return true;
        }

        #region Formula Evaluation

        private string GetDDCodeString(DataTable dt)
        {
            StringBuilder strDDCode = new StringBuilder();
            string strRuleType, strRule;
            foreach (DataRow row in dt.Rows)
            {
                if (row["ForCorrection"].ToString() == "Y")
                {
                    for (int j = 1; j <= 3; j++)
                    {
                        strRuleType = $"OCRRuleType{j}";
                        strRule = $"OCRRule{j}";
                        if (CommonCode.ReplaceNull(row[strRuleType], "0").ToString() == "2")
                        {
                            string[] arrRule = CommonCode.ReplaceNull(row[strRule], "").ToString().Split(',');
                            foreach (string rule in arrRule)
                            {
                                if (rule.StartsWith("[") && rule.EndsWith("]") &&
                                    !strDDCode.ToString().Contains($",{rule.Substring(1, rule.Length - 2)},", StringComparison.OrdinalIgnoreCase))
                                {
                                    CommonCode.AppendString(rule.Substring(1, rule.Length - 2), strDDCode);
                                }
                            }
                        }
                    }
                }
            }
            return strDDCode.ToString();
        }

        private int CheckComparison(string strExpression, DataTable dtFaxData, DataTable dtOCRData, IList<OcrFieldData> OcrItem, IList<Faxformdata> Faxdata, int intPageID, int intGroupInstance, string OCRFormName = "", int DataType = 0, DataSet dsEngFormFieldsData = null, int intEngFaxFormID = 0, string StrPrerRuleDiagInfo = "")
        {
            EvaluateFormula objEvalFormula = new EvaluateFormula();
            string strEval = string.Empty;
            string strEvaluateAt = "";

            if (dsEngFormFieldsData != null)
            {
                objEvalFormula.AssignDataset(dtFaxData.DataSet, dsEngFormFieldsData, (OcrFieldData)OcrItem, Faxdata);
                if (intGroupInstance > 0)
                {
                    strEvaluateAt = "G";
                }
            }

            strEval = objEvalFormula.ParseFormula1(intEngFaxFormID, strExpression, intGroupInstance, intPageID, strEvaluateAt, false, dtFaxData, dtOCRData, OCRFormName, DataType, StrPrerRuleDiagInfo);

            return strEval switch
            {
                "1" => 1,
                "2" => 2,
                _ => string.IsNullOrEmpty(strEval) ? 0 : int.Parse(strEval)
            };
        }

        private string GetValue(string strCode, DataTable dtFaxData, DataTable dtOCRData, IList<Faxformdata> faxData, IList<OcrFieldData> OCrData, DataTable dtEngFormFieldData, int intPageID = 0)
        {
            DataRow[] drRows;
            string strValue;

            if (strCode.StartsWith("FAX", StringComparison.OrdinalIgnoreCase))
            {
                drRows = dtFaxData.Select($"FaxDWPCode='{strCode}'");
                strValue = drRows.Length > 0 ? drRows[0]["FFValue"].ToString() : "0";
            }
            else if (strCode.StartsWith("OCR", StringComparison.OrdinalIgnoreCase))
            {
                drRows = dtOCRData.Select($"OCRDWPCode='{strCode}' AND EngagementPageID={intPageID}");
                strValue = drRows.Length > 0 ? (string.IsNullOrEmpty(drRows[0]["OCRValue"].ToString()) ? "0" : drRows[0]["OCRValue"].ToString()) : "0";
            }
            else
            {
                drRows = dtEngFormFieldData.Select($"FieldDWPCode='{strCode}'");
                strValue = drRows.Length > 0 ? (string.IsNullOrEmpty(drRows[0]["FieldValue"].ToString()) ? "0" : drRows[0]["FieldValue"].ToString()) : "0";
            }

            return strValue;
        }

        private List<ListValues> CheckList(string strExpression, string strData, DataTable dtDropDown, DataTable dtFaxData, DataTable dtOCRData, IList<DropdownData> DropDownData, IList<Faxformdata> Faxdata, IList<OcrFieldData> ocrData, bool blnIsUncertainChar, DataTable dtEngFormFieldData)
        {
            bool blnNeedsCorrection = true;
            string strValue = string.Empty;
            DataView dv;
            ListValues[] objLV = new ListValues[1];
            string[] arrExp = strExpression.Split(',');

            try
            {
                for (int intCount = 0; intCount < arrExp.Length; intCount++)
                {
                    if (arrExp[intCount].StartsWith("[") && arrExp[intCount].EndsWith("]"))
                    {
                        dv = new DataView(dtDropDown);
                        dv.RowFilter = "ParameterID='" + arrExp[intCount].Substring(1, arrExp[intCount].Length - 2) + "'";
                        var DDitem = DropDownData.Where(item => item.ParameterID == arrExp[intCount].Substring(1, arrExp[intCount].Length - 2)).ToList();
                        foreach (var item in DDitem)
                        {
                            if (!blnIsUncertainChar && strData.ToUpper() == item.ParameterDetailValue.ToUpper())
                            {
                                blnNeedsCorrection = false;
                                break;
                            }
                            AddListValues(ref objLV, item.ParameterDisplayName, item.ParameterDetailValue);
                        }
                        dv = null;
                    }
                    else if (arrExp[intCount].StartsWith("{") && arrExp[intCount].EndsWith("}"))
                    {
                        strValue = GetValue(arrExp[intCount].Substring(1, arrExp[intCount].Length - 2), dtFaxData, dtOCRData, Faxdata, ocrData, dtEngFormFieldData);
                        if (!blnIsUncertainChar && strData.ToUpper() == strValue.ToUpper())
                        {
                            blnNeedsCorrection = false;
                            break;
                        }
                        AddListValues(ref objLV, strValue, strValue);
                    }
                    else if (arrExp[intCount].StartsWith("<") && arrExp[intCount].EndsWith(">"))
                    {
                        dv = new DataView(dtEngFormFieldData);
                        dv.RowFilter = "FieldDWPCode='" + arrExp[intCount].Substring(1, arrExp[intCount].Length - 2) + "'";
                        for (int i = 0; i < dv.Count; i++)
                        {
                            if (!blnIsUncertainChar && strData.ToUpper() == dv[i]["FieldValue"].ToString().ToUpper())
                            {
                                blnNeedsCorrection = false;
                                break;
                            }
                            if (strValue != "0" && !string.IsNullOrEmpty(dv[i]["FieldValue"].ToString()))
                            {
                                AddListValues(ref objLV, dv[i]["FieldValue"].ToString(), dv[i]["FieldValue"].ToString());
                            }
                        }
                        IList<FormFieldData> FFItem = (IList<FormFieldData>)Faxdata.Where(item => item.FieldDWPCode == arrExp[intCount].Substring(1, arrExp[intCount].Length - 2)).ToList();
                        foreach (FormFieldData item in FFItem)
                        {
                            if (!blnIsUncertainChar && strData.ToUpper() == item.FieldValue.ToUpper())
                            {
                                blnNeedsCorrection = false;
                                break;
                            }
                            if (strValue != "0" && !string.IsNullOrEmpty(item.FieldValue))
                            {
                                AddListValues(ref objLV, item.FieldValue, item.FieldValue);
                            }
                        }
                        dv = null;
                    }
                    else if (arrExp[intCount].StartsWith("\"") && arrExp[intCount].EndsWith("\""))
                    {
                        if (!blnIsUncertainChar && strData.ToUpper() == arrExp[intCount].Substring(1, arrExp[intCount].Length - 2).ToUpper())
                        {
                            blnNeedsCorrection = false;
                            break;
                        }
                        AddListValues(ref objLV, arrExp[intCount].Substring(1, arrExp[intCount].Length - 2), arrExp[intCount].Substring(1, arrExp[intCount].Length - 2));
                    }
                }
            }
            catch (Exception ex)
            {
                // LogEntry(CommonCode.Severity.Low, "DDPAgent", "CheckList - Error Occured  " + ex.Message + " -> " + DateTime.Now + " strExpression = " + strExpression + " strData = " + strData, 997, 1, EngId, CommonCode.TraceType.InfoLog);
            }

            if (blnNeedsCorrection)
            {
                return objLV.ToList();
            }
            else
            {
                objLV = new ListValues[1];
                return objLV.ToList();
            }
        }

        private void AddListValues(ref ListValues[] objListValues, string strName, string strValue)
        {
            int intIndex = objListValues.Length;
            Array.Resize(ref objListValues, intIndex + 1);
            objListValues[intIndex] = new ListValues(strName, strValue, intIndex);
        }



        private bool CheckRequired(string strExpression, string strValue)
        {
            return strExpression == "Y" && string.IsNullOrEmpty(strValue);
        }

        private bool CheckRange(string strExpression, string strValue)
        {
            string[] arrExp = strExpression.Split(',');
            foreach (string exp in arrExp)
            {
                string[] arrData = exp.Split('-');
                if (arrData[0].Equals("MIN", StringComparison.OrdinalIgnoreCase) && int.Parse(arrData[1]) > strValue.Length)
                {
                    return true;
                }
                else if (arrData[0].Equals("MAX", StringComparison.OrdinalIgnoreCase) && int.Parse(arrData[1]) < strValue.Length)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckRegEx(string strExpression, string strValue)
        {
            return !Regex.IsMatch(strValue, strExpression, RegexOptions.IgnoreCase);
        }

        #endregion
        #region Common Functions

        public DataSet PopulateDropDownData(int engagementId, string strGuid, int intTaxSoftwareId, int intEngagementTypeId, int intTaxYear, string strTableName, string strDDCode)
        {
            DataSet dsDropDownData;

            var spParams = new SpParameter[]
            {
            new SpParameter("@Filter1", intTaxSoftwareId),
            new SpParameter("@Filter2", intEngagementTypeId),
            new SpParameter("@Filter3", intTaxYear),
            new SpParameter("@arrCode", strDDCode)
            };
            dsDropDownData = CommonCode.GetEngagementDbCommon().GetDataSet("Proc_DropDownListCode", spParams, true);


            dsDropDownData.Tables[0].TableName = strTableName;
            return dsDropDownData;
        }

        public DataSet PopulateFaxData(int engagementId, string strTableName)
        {
            DataSet dsFaxData;

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", engagementId)
            };
            dsFaxData = CommonCode.GetEngagementDbCommon(engagementId).GetDataSet("Proc_GetFaxFormFields", spParams, true);


            dsFaxData.Tables[0].TableName = strTableName;
            return dsFaxData;
        }

        public DataSet PopulateEngagementFormFieldData(int engagementId, string strTableName)
        {
            DataSet dsFormFieldData;

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", engagementId)
            };
            dsFormFieldData = CommonCode.GetEngagementDbCommon(engagementId).GetDataSet("Proc_GetEngagementFormFields", spParams, true);


            dsFormFieldData.Tables[0].TableName = strTableName;
            return dsFormFieldData;
        }

        private void UpdateOnCompletion(int intEngagementID)
        {

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID),
            new SpParameter("@CustomerStatusID", 4),
            new SpParameter("@SurePrepStatusID", 15)
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateEngagementStatus_OnCompletion", spParams, true, false);

            // Update in primary database
            CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("Proc_UpdateEngagementStatus_OnCompletion", spParams, true, false);

        }

        private Class1040ScanChecker.ActivityType GetNextStep(Class1040ScanChecker.ActivityType enmActivity)
        {
            if (enmActivity == Class1040ScanChecker.ActivityType.OCRToFax)
            {
                return Class1040ScanChecker.ActivityType.EvaluateFaxToTaxFormula;
            }
            else if (enmActivity == Class1040ScanChecker.ActivityType.EvaluateFaxToTaxFormula)
            {
                return Class1040ScanChecker.ActivityType.Fax2Tax;
            }
            else if (enmActivity == Class1040ScanChecker.ActivityType.Fax2Tax)
            {
                return 0;
            }
            else
            {
                return enmActivity;
            }
        }

        private string ReplaceSP(string strExpression, string strFind, string strReplacement)
        {
            if (!string.IsNullOrEmpty(strExpression))
            {
                return strExpression.Replace(strFind, strReplacement);
            }
            else
            {
                return string.Empty;
            }
        }

        #endregion
        public DataSet GetDuplicateData(int intEngagementId)
        {
            try
            {
                DataSet objTempDataSet;
                DataTable objTempDataTable1, objTempDataTable2, objTempDataTable3;

                CommonCode.WriteDBLog("Retrieving Data For New Duplicate...", 782, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);

                Console.WriteLine("Retrieving Data For New Duplicate..." + intEngagementId);
                var spParams = new SpParameter[1];
                spParams[0] = new SpParameter("@EngagementID", intEngagementId);

                objTempDataSet = CommonCode.GetEngagementDbCommon(intEngagementId).GetDataSet("Proc_DuplicateSourceDocData", spParams, true);
                objTempDataSet.Tables[0].TableName = "SourceTable";
                objTempDataTable1 = objTempDataSet.Tables[0].Copy();
                objTempDataSet = null;

                spParams = new SpParameter[1];
                spParams[0] = new SpParameter("@EngagementID", intEngagementId);
                objTempDataSet = CommonCode.GetEngagementDbCommon(intEngagementId).GetDataSet("Proc_DuplicateOrganizerData", spParams, true);
                objTempDataSet.Tables[0].TableName = "OrganizerTable";
                objTempDataTable2 = objTempDataSet.Tables[0].Copy();
                objTempDataSet = null;

                spParams = new SpParameter[1];
                spParams[0] = new SpParameter("@EngagementID", intEngagementId);
                objTempDataSet = CommonCode.GetEngagementDbCommon(intEngagementId).GetDataSet("Proc_DuplicateMasterData", spParams, true);
                objTempDataSet.Tables[0].TableName = "ColStruct";
                objTempDataTable3 = objTempDataSet.Tables[0].Copy();
                objTempDataSet = null;


                objTempDataSet = new DataSet();
                objTempDataSet.Tables.Add(objTempDataTable1);
                objTempDataSet.Tables.Add(objTempDataTable2);
                objTempDataSet.Tables.Add(objTempDataTable3);

                return objTempDataSet;
            }
            catch (Exception ex)
            {
                // Log the exception
                return null;
            }
        }

        public void GenerateDiagnostics(int intEngagementID)
        {
            DataSet ds, dsFaxData, dsEngFormFieldData;
            DataTable dt, dtFaxData, dtEngFormFieldData;
            bool blnNeedsCorrection;
            int intNeedsCorrection = -1;
            string strRuleType, strRule, strDiagnostics;

            CommonCode.WriteDBLog("Retrieving Data For Diagnostics...", 783, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Retrieving Data For Diagnostics...");
            ds = PopulateDiagnosticData(intEngagementID, "DiagnosticData");
            dt = ds.Tables["DiagnosticData"];

            dsFaxData = PopulateFaxData(intEngagementID, "FaxData");
            dtFaxData = dsFaxData.Tables["FaxData"];

            dsEngFormFieldData = PopulateEngagementFormFieldData(intEngagementID, "EngFormFieldData");
            dtEngFormFieldData = dsEngFormFieldData.Tables["EngFormFieldData"];

            foreach (DataRow row in dt.Rows)
            {
                intNeedsCorrection = -1;

                for (int i = 1; i <= 5; i++)
                {
                    strRuleType = $"DiagRuleType{i}";
                    strRule = $"DiagRule{i}";
                    strDiagnostics = $"Diagnostics{i}";

                    if (CommonCode.ReplaceNull(row[strRuleType], "0").ToString() == "1")
                    {
                        intNeedsCorrection = CheckComparison(
                            CommonCode.ReplaceNull(row[strRule], "").ToString(),
                            dtFaxData,
                            dt,
                            null,
                            null,
                            Convert.ToInt32(row["EngagementPageID"]),
                            Convert.ToInt32(row["FaxRowNumber"]),
                            CommonCode.ReplaceNull(row["InputForm"], "").ToString(),
                            Convert.ToInt32(CommonCode.ReplaceNull(row["DataType"], "0")),
                            dsEngFormFieldData,
                            Convert.ToInt32(row["EngagementFaxFormID"])
                        );
                    }
                    else if (CommonCode.ReplaceNull(row[strRuleType], "0").ToString() == "3")
                    {
                        blnNeedsCorrection = CheckRange(
                            CommonCode.ReplaceNull(row[strRule], "").ToString(),
                            row["OCRValue"].ToString()
                        );

                        if (blnNeedsCorrection)
                        {
                            intNeedsCorrection = 0;
                        }
                    }

                    if (intNeedsCorrection == 0)
                    {
                        blnNeedsCorrection = false;

                        UpdateDiagnostics(
                             intEngagementID,
                             Convert.ToInt32(row["EngagementPageID"]),
                             row["FFValue"].ToString(),
                             Convert.ToInt32(row["FaxFormFieldID"]),
                             Convert.ToInt32(row[strRuleType].ToString()),
                             row[strRule].ToString(),
                             row[strDiagnostics].ToString(),
                             Convert.ToInt32(row["EngagementFaxFormID"]),
                             row["InputForm"].ToString(),
                             Convert.ToInt32(row["FaxFormID"]),
                             Convert.ToInt32(row["EngagementFaxFormFieldID"]),
                             Convert.ToInt32(row["EngagementOCRFieldID"])
                         );

                        break;
                    }
                }
            }

            UpdateDiagnosticsCompleted(intEngagementID);

            dtFaxData = null;
            dsFaxData = null;
            dtEngFormFieldData = null;
            dsEngFormFieldData = null;
            dt = null;
            ds = null;
        }
        private bool UpdateDiagnosticsCompleted(int intEngagementID)
        {
            // Modified and removed extra spParam which was not required


            SpParameter[] spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);

            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_InsertIntoSPEngagementDiagnostics", spParams, true);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSPEngagementDiagnostics", spParams, true);


            return true;
        }

        private void UpdateDiagnostics(
            int intEngagementID,
            int intEngagementPageID,
            string strValue,
            int intFaxFormFieldID,
            int intDiagRuleType,
            string strDiagRule,
            string strDiagnostics,
            int intEngFaxFormID,
            string strInputForm,
            int intFaxFormID,
            int intEngFaxFormFieldID,
            int intEngagementOCRFieldID = 0)
        {

            SpParameter[] spParams = new SpParameter[12];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            spParams[1] = new SpParameter("@EngagementPageID", intEngagementPageID);
            spParams[2] = new SpParameter("@OCRValue", strValue);
            spParams[3] = new SpParameter("@FaxFormFieldID", intFaxFormFieldID);
            spParams[4] = new SpParameter("@DiagRuleType", intDiagRuleType);
            spParams[5] = new SpParameter("@DiagRule", strDiagRule);
            spParams[6] = new SpParameter("@Diagnostics", strDiagnostics);
            spParams[7] = new SpParameter("@EngagementFaxFormID", intEngFaxFormID);
            spParams[8] = new SpParameter("@FaxFormID", intFaxFormID);
            spParams[9] = new SpParameter("@InputForm", strInputForm);
            spParams[10] = new SpParameter("@EngagementFaxFormFieldID", intEngFaxFormFieldID);
            spParams[11] = new SpParameter("@EngagementOCRFieldID", intEngagementOCRFieldID);

            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateDiagnosticData", spParams, true, false);

        }

        #region Tax Exempt

        #region Tax Exempt New
        public Class1040ScanChecker GetTaxExemptData(int intEngagementID)
        {
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.TaxExempt);
            int TaxExemptOptionValue;

            CommonCode.WriteDBLog($"GetTaxExemptData started for EngagementID: {intEngagementID}", 1001, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine($"GetTaxExemptData started for EngagementID: {intEngagementID}");
            DataSet ds;

            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_TaxExemptData", spParams, true);


            ds.Tables[0].TableName = "TaxExempt";
            objChecker.DuplicateDataSet = ds;

            if (objChecker.DuplicateDataSet.Tables[0].Rows.Count <= 0)
            {
                CommonCode.WriteDBLog("No records found for TaxExempt", 784, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine("No records found for TaxExempt");
                UpdateTaxExemptCompleted(intEngagementID);
            }
            else
            {
                CommonCode.WriteDBLog($"Records found for TaxExempt: {objChecker.DuplicateDataSet.Tables[0].Rows.Count}", 785, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine($"Records found for TaxExempt: {objChecker.DuplicateDataSet.Tables[0].Rows.Count}");
                TaxExemptOptionValue = GetTaxExemptOptionValue(intEngagementID);

                switch (TaxExemptOptionValue)
                {
                    case 0:
                    case 4:
                    case 5:
                        // Do nothing
                        break;
                    case 1:
                        // Do not show wizard for previous year returns
                        UpdateTaxExemptData(objChecker.DuplicateDataSet, intEngagementID, true, 8);
                        objChecker.DuplicateDataSet = null;
                        CommonCode.WriteDBLog("TaxExempt: complete wizard", 786, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                        Console.WriteLine("TaxExempt: complete wizard");
                        break;
                    case 3:
                        // Update all selected items and do not show wizard
                        UpdateTaxExemptData(objChecker.DuplicateDataSet, intEngagementID, true, 8);
                        objChecker.DuplicateDataSet = null;
                        break;
                }
            }

            CommonCode.WriteDBLog($"GetTaxExemptData completed for EngagementID: {intEngagementID}", 1002, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine($"GetTaxExemptData completed for EngagementID: {intEngagementID}");
            return objChecker;
        }

        private DataSet UpdateAccruedInt(DataSet dsTaxExemptData, int intEngagementID, int intEditedBy)
        {
            DataSet dsAccruedIntData;
            DataView dvTaxExemptData;


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            dsAccruedIntData = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_TaxExemptData_AccruedInt", spParams, true);


            dvTaxExemptData = dsTaxExemptData.Tables[0].DefaultView;

            foreach (DataRow row in dsAccruedIntData.Tables[0].Rows)
            {
                string SNTValue, STValue, CheckValue, Amount;
                dvTaxExemptData.RowFilter = $"Description ='{row["Description"].ToString().Replace("'", "''")}'";

                if (dvTaxExemptData.Count > 0)
                {
                    Amount = dvTaxExemptData[0]["1099 Earning"].ToString();
                    SNTValue = dvTaxExemptData[0]["Exempt % or Amount"].ToString();
                    STValue = dvTaxExemptData[0]["StateTaxableAmt"].ToString();
                    CheckValue = dvTaxExemptData[0]["X"].ToString();

                    if (bool.Parse(CheckValue))
                    {
                        row["Exempt % or Amount"] = "0";
                        row["StateTaxableAmt"] = row["1099 Earning"];
                        row["X"] = CheckValue;
                    }
                    else
                    {
                        double SNTPercentage = double.Parse(SNTValue) / double.Parse(Amount);
                        row["Exempt % or Amount"] = (int)(SNTPercentage * int.Parse(row["1099 Earning"].ToString()));
                        row["StateTaxableAmt"] = int.Parse(row["1099 Earning"].ToString()) - int.Parse(row["Exempt % or Amount"].ToString());
                        row["X"] = CheckValue;
                    }
                }
            }

            foreach (DataRow row in dsAccruedIntData.Tables[0].Rows)
            {
                int intStateTaxableFieldID = int.Parse(row["StateTaxableFieldID"].ToString());
                int intStateNonTaxableFieldID = int.Parse(row["StateNonTaxableFieldID"].ToString());


                spParams = new SpParameter[3];
                spParams[0] = new SpParameter("@FFValue", row["StateTaxableAmt"]);
                spParams[1] = new SpParameter("@EditedBy", intEditedBy);
                spParams[2] = new SpParameter("@EngagementFaxFormFieldID", intStateTaxableFieldID);
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSPEngagementFaxFormField3", spParams, true);

                spParams[0] = new SpParameter("@FFValue", row["Exempt % or Amount"]);
                spParams[1] = new SpParameter("@EditedBy", intEditedBy);
                spParams[2] = new SpParameter("@EngagementFaxFormFieldID", intStateNonTaxableFieldID);
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSPEngagementFaxFormField3", spParams, true);

            }

            return dsAccruedIntData;
        }
        private bool UpdateTaxExemptData(DataSet dsTaxExemptData, int intEngagementID, bool blnTaxExemptCompleted, int intEditedBy)
        {
            int intStateTaxableFieldID;
            int intStateNonTaxableFieldID;
            int intStateTaxableAmt;
            int intEngagementTypeID, intTaxSoftwareID;
            foreach (DataRow row in dsTaxExemptData.Tables[0].Rows)
            {
                intStateTaxableFieldID = Convert.ToInt32(row["StateTaxableFieldID"]);
                intStateNonTaxableFieldID = Convert.ToInt32(row["StateNonTaxableFieldID"]);
                intStateTaxableAmt = Convert.ToInt32(row["StateTaxableAmt"]);


                SpParameter[] spParams = new SpParameter[3];
                spParams[0] = new SpParameter("@intStateTaxableFieldID", intStateTaxableFieldID);
                spParams[1] = new SpParameter("@FFValue", row["StateTaxableAmt"]);
                spParams[2] = new SpParameter("@intEditedBy", intEditedBy);
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementFaxFormField", spParams, true);

                spParams[0] = new SpParameter("@intStateTaxableFieldID", intStateNonTaxableFieldID);
                spParams[1] = new SpParameter("@FFValue", row["Exempt % or Amount"]);
                spParams[2] = new SpParameter("@intEditedBy", intEditedBy);
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementFaxFormField", spParams, true);

            }

            if (blnTaxExemptCompleted)
            {
                EvaluateFormula objEvaluateFormula = new EvaluateFormula();
                DataTable dtDataAftMapping;
                DataView dvData;
                string strWhere;
                string strDataValue = string.Empty;
                DataTable dtEngagementInfo;


                DataSet dsAccruedIntData;


                SpParameter[] spParams = new SpParameter[1];
                spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                dtEngagementInfo = CommonCode.GetEngagementDbCommon().GetDataTable("dbo.Proc_GetFieldsFromSPEngagements", spParams, "Engagements", true);


                intEngagementTypeID = Convert.ToInt32(dtEngagementInfo.Rows[0]["EngagementTypeID"]);
                intTaxSoftwareID = Convert.ToInt32(dtEngagementInfo.Rows[0]["TaxSoftwareID"]);

                if (intEngagementTypeID == 5)
                {
                    intEngagementTypeID = 1;
                }
                else if (intEngagementTypeID == 6 || intEngagementTypeID == 4)
                {
                    intEngagementTypeID = 3;
                }

                dsAccruedIntData = UpdateAccruedInt(dsTaxExemptData, intEngagementID, intEditedBy);
                dtDataAftMapping = objEvaluateFormula.EvaluateFaxToTaxFormula_TaxExempt(intEngagementID);
                dvData = dtDataAftMapping.DefaultView;

                foreach (DataRow row in dsTaxExemptData.Tables[0].Rows)
                {
                    intStateTaxableFieldID = Convert.ToInt32(row["StateTaxableFieldID"]);
                    intStateNonTaxableFieldID = Convert.ToInt32(row["StateNonTaxableFieldID"]);

                    if (row["StateTaxableAmt"].ToString() == "0")
                    {

                        spParams = new SpParameter[5];
                        spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                        spParams[1] = new SpParameter("@intStateTaxableFieldID", intStateTaxableFieldID);
                        spParams[2] = new SpParameter("@intDeleteCase1", 1);
                        spParams[3] = new SpParameter("@FieldDWPCode", string.Empty);
                        spParams[4] = new SpParameter("@Mappings", string.Empty);
                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);

                    }
                    else
                    {
                        dvData.RowFilter = $"EngagementFaxFormFieldID={intStateTaxableFieldID}";
                        if (dvData.Count > 0)
                        {
                            for (int i = 0; i < dvData.Count; i++)
                            {
                                strWhere = $" EngagementFaxFormFieldID={intStateTaxableFieldID} and ISNULL(OldFieldDWPCode,FieldDWPCode)='{dvData[i]["FieldDWPCode"]}' and Mappings='{dvData[i]["Mappings"]}'";
                                strDataValue = dvData[i]["TFValue"].ToString().Trim();


                                if (strDataValue == "0" || strDataValue == "")
                                {
                                    spParams = new SpParameter[5];
                                    spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                                    spParams[1] = new SpParameter("@intStateTaxableFieldID", intStateTaxableFieldID);
                                    spParams[2] = new SpParameter("@intDeleteCase1", 2);
                                    spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                    spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);
                                }
                                else
                                {
                                    spParams = new SpParameter[5];
                                    spParams[0] = new SpParameter("@intStateTaxableFieldID", intStateTaxableFieldID);
                                    spParams[1] = new SpParameter("@TFValue", strDataValue);
                                    spParams[2] = new SpParameter("@intEditedBy", intEditedBy);
                                    spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                    spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementFaxFormFieldTFValue", spParams, true);
                                }

                            }
                        }
                    }
                    // For non-state taxable
                    if (row["Exempt % Or Amount"].ToString() == "0")
                    {
                        // Inline delete 3 - converted to procedure call

                        spParams = new SpParameter[5];
                        spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                        spParams[1] = new SpParameter("@intStateTaxableFieldID", intStateNonTaxableFieldID);
                        spParams[2] = new SpParameter("@intDeleteCase1", 3);
                        spParams[3] = new SpParameter("@FieldDWPCode", string.Empty);
                        spParams[4] = new SpParameter("@Mappings", string.Empty);
                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);

                    }
                    else
                    {
                        dvData.RowFilter = $"EngagementFaxFormFieldID={intStateNonTaxableFieldID}";
                        if (dvData.Count > 0)
                        {
                            for (int i = 0; i < dvData.Count; i++)
                            {
                                strWhere = $" EngagementFaxFormFieldID={intStateNonTaxableFieldID} And ISNULL(OldFieldDWPCode, FieldDWPCode)='{dvData[i]["FieldDWPCode"]}' and Mappings='{dvData[i]["Mappings"]}'";
                                strDataValue = dvData[i]["TFValue"].ToString().Trim();


                                if (strDataValue == "0" || string.IsNullOrEmpty(strDataValue))
                                {
                                    // Inline delete 4 - converted to procedure call
                                    spParams = new SpParameter[5];
                                    spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                                    spParams[1] = new SpParameter("@intStateTaxableFieldID", intStateNonTaxableFieldID);
                                    spParams[2] = new SpParameter("@intDeleteCase1", 4);
                                    spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                    spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);
                                }
                                else
                                {
                                    // Inline update 10 - converted to procedure call
                                    spParams = new SpParameter[5];
                                    spParams[0] = new SpParameter("@intStateTaxableFieldID", intStateNonTaxableFieldID);
                                    spParams[1] = new SpParameter("@TFValue", strDataValue);
                                    spParams[2] = new SpParameter("@intEditedBy", intEditedBy);
                                    spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                    spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementFaxFormFieldTFValue", spParams, true);
                                }

                            }
                        }
                    }
                }


                if (dsAccruedIntData != null && dsAccruedIntData.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in dsAccruedIntData.Tables[0].Rows)
                    {
                        intStateTaxableFieldID = Convert.ToInt32(row["StateTaxableFieldID"]);
                        intStateNonTaxableFieldID = Convert.ToInt32(row["StateNonTaxableFieldID"]);

                        // For state taxable
                        if (row["StateTaxableAmt"].ToString() == "0")
                        {

                            spParams = new SpParameter[5];
                            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                            spParams[1] = new SpParameter("@intStateTaxableFieldID", intStateTaxableFieldID);
                            spParams[2] = new SpParameter("@intDeleteCase1", 5);
                            spParams[3] = new SpParameter("@FieldDWPCode", string.Empty);
                            spParams[4] = new SpParameter("@Mappings", string.Empty);
                            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);

                        }
                        else
                        {
                            dvData.RowFilter = $"EngagementFaxFormFieldID={intStateTaxableFieldID}";
                            if (dvData.Count > 0)
                            {
                                for (int i = 0; i < dvData.Count; i++)
                                {
                                    strWhere = $" EngagementFaxFormFieldID={intStateTaxableFieldID} and ISNULL(OldFieldDWPCode,FieldDWPCode)='{dvData[i]["FieldDWPCode"]}' and Mappings='{dvData[i]["Mappings"]}' and IsNull(TaxFormIdentifier,'N')<>'Y'";
                                    strDataValue = dvData[i]["TFValue"].ToString().Trim();


                                    if (strDataValue == "0" || strDataValue == "")
                                    {
                                        // Inline delete 6 - converted to procedure call
                                        spParams = new SpParameter[5];
                                        spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                                        spParams[1] = new SpParameter("@intStateTaxableFieldID", intStateTaxableFieldID);
                                        spParams[2] = new SpParameter("@intDeleteCase1", 6);
                                        spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                        spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);
                                    }
                                    else
                                    {
                                        // Inline update 11 - converted to procedure call
                                        spParams = new SpParameter[5];
                                        spParams[0] = new SpParameter("@intStateTaxableFieldID", intStateTaxableFieldID);
                                        spParams[1] = new SpParameter("@TFValue", strDataValue);
                                        spParams[2] = new SpParameter("@intEditedBy", intEditedBy);
                                        spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                        spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementFaxFormFieldTFValue", spParams, true);
                                    }

                                }
                            }
                        }

                        // For non-state taxable
                        if (row["Exempt % or Amount"].ToString() == "0")
                        {

                            spParams = new SpParameter[5];
                            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                            spParams[1] = new SpParameter("@intStateTaxableFieldID", intStateNonTaxableFieldID);
                            spParams[2] = new SpParameter("@intDeleteCase1", 7);
                            spParams[3] = new SpParameter("@FieldDWPCode", string.Empty);
                            spParams[4] = new SpParameter("@Mappings", string.Empty);
                            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);

                        }
                        else
                        {
                            dvData.RowFilter = $"EngagementFaxFormFieldID={intStateNonTaxableFieldID}";
                            if (dvData.Count > 0)
                            {
                                for (int i = 0; i < dvData.Count; i++)
                                {
                                    strWhere = $" EngagementFaxFormFieldID={intStateNonTaxableFieldID} and ISNULL(OldFieldDWPCode,FieldDWPCode)='{dvData[i]["FieldDWPCode"]}' and Mappings='{dvData[i]["Mappings"]}' and IsNull(TaxFormIdentifier,'N')<>'Y'";
                                    strDataValue = dvData[i]["TFValue"].ToString().Trim();


                                    if (strDataValue == "0" || strDataValue == "")
                                    {
                                        // Inline delete 8 - converted to procedure call
                                        spParams = new SpParameter[5];
                                        spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                                        spParams[1] = new SpParameter("@intStateTaxableFieldID", intStateNonTaxableFieldID);
                                        spParams[2] = new SpParameter("@intDeleteCase1", 8);
                                        spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                        spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);
                                    }
                                    else
                                    {
                                        // Inline update 12 - converted to procedure call
                                        spParams = new SpParameter[5];
                                        spParams[0] = new SpParameter("@intStateTaxableFieldID", intStateNonTaxableFieldID);
                                        spParams[1] = new SpParameter("@TFValue", strDataValue);
                                        spParams[2] = new SpParameter("@intEditedBy", intEditedBy);
                                        spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                        spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementFaxFormFieldTFValue", spParams, true);
                                    }

                                }
                            }
                        }
                    }
                }
                string strEngagementFaxFormID;
                long intFaxIdentifer;
                DataSet dsEngInfo = null;
                bool IsDelete = false;
                bool CompleteDelete = false;

                strEngagementFaxFormID = GetFaxIdentifierForAdditionalUpdates(intEngagementID, dsTaxExemptData, ref dsEngInfo);

                intEngagementTypeID = Convert.ToInt32(dsEngInfo.Tables[0].Rows[0]["EngagementTypeID"]);
                intTaxSoftwareID = Convert.ToInt32(dsEngInfo.Tables[0].Rows[0]["TaxSoftwareID"]);
                switch (intEngagementTypeID)
                {
                    case 3:
                    case 6: // 1041
                        if (intTaxSoftwareID == 3)
                        {
                            IsDelete = true;
                        }
                        break;
                    case 1:
                    case 5: // 1040
                        switch (intTaxSoftwareID)
                        {
                            case 4:
                                IsDelete = true;
                                break;
                            case 1:
                            case 2:
                            case 3:
                                CompleteDelete = true;
                                break;
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(strEngagementFaxFormID))
                {
                    dvData.RowFilter = $"EngagementFaxFormFieldID in({strEngagementFaxFormID})";
                    if (dvData.Count > 0)
                    {
                        for (int i = 0; i < dvData.Count; i++)
                        {
                            intFaxIdentifer = Convert.ToInt64(dvData[i]["EngagementFaxFormFieldID"]);
                            strWhere = $" EngagementFaxFormFieldID={intFaxIdentifer} and ISNULL(OldFieldDWPCode,FieldDWPCode)='{dvData[i]["FieldDWPCode"]}' and Mappings='{dvData[i]["Mappings"]}'";

                            string strValue;
                            try
                            {
                                strValue = Math.Round(Convert.ToDouble(dvData[i]["TFValue"]), 2).ToString();
                            }
                            catch (Exception)
                            {
                                strValue = dvData[i]["TFValue"].ToString();
                            }


                            if (IsDelete)
                            {
                                if (strValue == "0" || string.IsNullOrEmpty(strValue))
                                {
                                    // Inline delete 9 - converted to procedure call
                                    spParams = new SpParameter[5];
                                    spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                                    spParams[1] = new SpParameter("@intStateTaxableFieldID", intFaxIdentifer);
                                    spParams[2] = new SpParameter("@intDeleteCase1", 9);
                                    spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                    spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);
                                }
                                else
                                {
                                    // Inline update 13 - converted to procedure call
                                    spParams = new SpParameter[5];
                                    spParams[0] = new SpParameter("@intStateTaxableFieldID", intFaxIdentifer);
                                    spParams[1] = new SpParameter("@TFValue", strValue);
                                    spParams[2] = new SpParameter("@intEditedBy", intEditedBy);
                                    spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                    spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementFaxFormFieldTFValue", spParams, true);
                                }
                            }
                            else if (CompleteDelete)
                            {
                                if (strValue == "0" || string.IsNullOrEmpty(strValue))
                                {
                                    // Inline delete 10 - converted to procedure call
                                    spParams = new SpParameter[5];
                                    spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                                    spParams[1] = new SpParameter("@intStateTaxableFieldID", intFaxIdentifer);
                                    spParams[2] = new SpParameter("@intDeleteCase1", 10);
                                    spParams[3] = new SpParameter("@FieldDWPCode", string.Empty);
                                    spParams[4] = new SpParameter("@Mappings", string.Empty);
                                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteSPEngagementFaxTaxFormField", spParams, true);
                                }
                                else
                                {
                                    // Inline update 14 - converted to procedure call
                                    spParams = new SpParameter[5];
                                    spParams[0] = new SpParameter("@intStateTaxableFieldID", intFaxIdentifer);
                                    spParams[1] = new SpParameter("@TFValue", strValue);
                                    spParams[2] = new SpParameter("@intEditedBy", intEditedBy);
                                    spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                    spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementFaxFormFieldTFValue", spParams, true);
                                }
                            }
                            else
                            {
                                // Inline update 15 - converted to procedure call
                                spParams = new SpParameter[5];
                                spParams[0] = new SpParameter("@intStateTaxableFieldID", intFaxIdentifer);
                                spParams[1] = new SpParameter("@TFValue", strValue);
                                spParams[2] = new SpParameter("@intEditedBy", intEditedBy);
                                spParams[3] = new SpParameter("@FieldDWPCode", dvData[i]["FieldDWPCode"]);
                                spParams[4] = new SpParameter("@Mappings", dvData[i]["Mappings"]);
                                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSPEngagementFaxFormFieldTFValue", spParams, true);
                            }

                        }
                    }
                }
            }


            if (blnTaxExemptCompleted)
            {
                AddDataToSubmissionDataForExport(dsTaxExemptData, intEngagementID);

                SpParameter[] spParams = new SpParameter[1];
                spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateTaxExempt", spParams, true);

            }

            return true;

        }

        #endregion
        private bool AddDataToSubmissionDataForExport(DataSet dsTaxExemptData, int intEngagementID)
        {
            string strSubmissionDate = string.Empty;
            string strClientNumber = string.Empty;
            int intTaxYear = 0, intDomainID = 0, intSubmissionID = 0;


            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };

            DataSet dsTemp = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("dbo.Proc_GetFieldsSPEngagements", spParams);

            if (dsTemp.Tables[0].Rows.Count > 0)
            {
                strClientNumber = dsTemp.Tables[0].Rows[0]["ClientNumber"].ToString();
                intTaxYear = Convert.ToInt32(dsTemp.Tables[0].Rows[0]["TaxYear"]);
                intDomainID = Convert.ToInt32(dsTemp.Tables[0].Rows[0]["DomainID"]);
                if (string.IsNullOrEmpty(strSubmissionDate))
                    strSubmissionDate = dsTemp.Tables[0].Rows[0]["DateSubmitted"].ToString();
            }

            spParams = new SpParameter[]
            {
            new SpParameter("@DomainID", intDomainID),
            new SpParameter("@TaxYear", intTaxYear),
            new SpParameter("@ClientNumber", strClientNumber)
            };

            intSubmissionID = (Convert.ToInt32(CommonCode.GetEngagementDbCommon(intEngagementID).GetData("dbo.Proc_GetFieldsSPTaxExemptSubMissionData", spParams, true)) + 1);


            foreach (DataRow dr in dsTaxExemptData.Tables[0].Rows)
            {
                try
                {
                    spParams = new SpParameter[]
                    {
                    new SpParameter("@DomainID", intDomainID),
                    new SpParameter("@EngagementID", intEngagementID),
                    new SpParameter("@ClientNumber", strClientNumber),
                    new SpParameter("@DomainID", intDomainID),
                    new SpParameter("@TaxYear", intTaxYear),
                    new SpParameter("@AmountFieldID", dr["AmountFieldID"]),
                    new SpParameter("@BrokerName", dr["Broker Name"]),
                    new SpParameter("@AccountNumber", dr["Account Number"]),
                    new SpParameter("@Description", dr["Description"]),
                    new SpParameter("@1099Earning", dr["1099 Earning"]),
                    new SpParameter("@StateTaxableAmt", dr["StateTaxableAmt"]),
                    new SpParameter("@StateNonTaxableAmt", dr["Exempt % or Amount"]),
                    new SpParameter("@EngagementPageID", dr["EngagementPageID"]),
                    new SpParameter("@StateTaxableFieldID", dr["StateTaxableFieldID"]),
                    new SpParameter("@StateNonTaxableFieldID", dr["StateNonTaxableFieldID"]),
                    new SpParameter("@EngagementFaxFormID", dr["EngagementFaxFormID"]),
                    new SpParameter("@MuniApplied", dr["MuniApplied"]),
                    new SpParameter("@SubmissionID", intSubmissionID),
                    new SpParameter("@SubmissionDate", Convert.ToDateTime(strSubmissionDate))
                    };

                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_InsTaxExemptSubMissionData", spParams, true);
                }
                catch (Exception)
                {
                    // Intentionally left blank
                }
            }


            return true;
        }

        private string GetFaxIdentifierForAdditionalUpdates(int intEngagementID, DataSet dsTaxExemptData, ref DataSet dsEngInfo)
        {
            DataTable dtTemp;
            DataSet dsEngagementFaxFormID;

            var arrEngagementFaxFormID = new List<object>();
            var strEngagementFaxFormID = new StringBuilder();
            var strTemp = new StringBuilder();
            string[] columns = new string[1];
            SpParameter[] spParams = null;

            dsEngInfo = GetEngagementInfo(intEngagementID);
            dtTemp = dsEngInfo.Tables[0];

            if (dtTemp != null)
            {
                switch (Convert.ToInt32(dtTemp.Rows[0]["EngagementTypeID"]))
                {
                    case 1:
                    case 5:
                        switch (Convert.ToInt32(dtTemp.Rows[0]["TaxSoftwareID"]))
                        {
                            case 4:
                                foreach (DataRow row in dsTaxExemptData.Tables[0].Rows)
                                {
                                    if (!arrEngagementFaxFormID.Contains(row["EngagementFaxFormID"]))
                                    {
                                        arrEngagementFaxFormID.Add(row["EngagementFaxFormID"]);
                                    }
                                }

                                foreach (var id in arrEngagementFaxFormID)
                                {
                                    if (string.IsNullOrWhiteSpace(strTemp.ToString()))
                                    {
                                        strTemp.Append(id);
                                    }
                                    else
                                    {
                                        strTemp.Append($",{id}");
                                    }
                                }

                                columns[0] = "FaxDwpCode";
                                string[] values = { "'FAX.CON.121','FAX.CON.055','FAX.GRT.022','FAX.INT.024','FAX.MIN.015','FAX.CON.225','FAX.GRT.098'" };

                                DataTable dtFaxDwpCode = CommonCode.GetCustomizeDataTable(values, columns);
                                columns[0] = "EngagementFaxFormID";
                                values[0] = strTemp.ToString();
                                DataTable dtEngagementFaxFormID = CommonCode.GetCustomizeDataTable(values, columns);


                                spParams = new SpParameter[]
                                {
                                new SpParameter("@EngagementID", intEngagementID),
                                new SpParameter("@UDT_EngagementFaxFormID", dtEngagementFaxFormID),
                                new SpParameter("@UDT_FaxDWPCode", dtFaxDwpCode)
                                };

                                dsEngagementFaxFormID = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("dbo.Proc_GetFaxFormFieldData", spParams, true);


                                if (dsEngagementFaxFormID.Tables[0].Rows.Count > 0)
                                {
                                    foreach (DataRow row in dsEngagementFaxFormID.Tables[0].Rows)
                                    {
                                        if (string.IsNullOrWhiteSpace(strEngagementFaxFormID.ToString()))
                                        {
                                            strEngagementFaxFormID.Append(row["EngagementFaxFormFieldID"]);
                                        }
                                        else
                                        {
                                            strEngagementFaxFormID.Append($",{row["EngagementFaxFormFieldID"]}");
                                        }
                                    }
                                }
                                break;

                                // Additional cases for TaxSoftwareID can be added here
                        }
                        break;

                        // Additional cases for EngagementTypeID can be added here
                }
            }

            return strEngagementFaxFormID.ToString();
        }

        public DataSet GetEngagementInfo(int intEngagementID)
        {

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };

            return CommonCode.GetEngagementDbCommon().GetDataSet("dbo.Proc_GetFieldsFromSPEngagements", spParams, true);

        }

        #endregion
        public Class1040ScanChecker GetTaxExemptInterestData(int intEngagementID)
        {
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.TaxExempt);
            int TaxExemptOptionValue;

            // Log entry for starting the method
            CommonCode.WriteDBLog($"GetTaxExemptInterestData started for EngagementID: {intEngagementID}", 1003, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine($"GetTaxExemptInterestData started for EngagementID: {intEngagementID}");
            DataSet ds = new DataSet();


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngID", intEngagementID);
            ds = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_getTaxExemptData", spParams, true);


            ds.Tables[0].TableName = "TaxExempt";
            objChecker.DuplicateDataSet = ds;

            if (objChecker.DuplicateDataSet.Tables[0].Rows.Count <= 0)
            {
                CommonCode.WriteDBLog("No records found for TaxExempt", 787, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine("No records found for TaxExempt");
                UpdateTaxExemptCompleted(intEngagementID);
            }
            else
            {
                CommonCode.WriteDBLog($"Records found for TaxExempt: {objChecker.DuplicateDataSet.Tables[0].Rows.Count}", 788, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine($"Records found for TaxExempt: {objChecker.DuplicateDataSet.Tables[0].Rows.Count}");
                TaxExemptOptionValue = GetTaxExemptOptionValue(intEngagementID);

                switch (TaxExemptOptionValue)
                {
                    case 0:
                        // Do nothing
                        break;
                    case 1:
                        UpdateTaxExemptCompleted(intEngagementID);
                        objChecker.DuplicateDataSet = null;
                        CommonCode.WriteDBLog("TaxExempt: complete wizard", 789, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                        Console.WriteLine("TaxExempt: complete wizard");
                        break;
                    case 2:
                        objChecker.DuplicateDataSet = MarkAllStateTaxable(objChecker.DuplicateDataSet, intEngagementID, false);
                        CommonCode.WriteDBLog("TaxExempt: Update all records as State Taxable and show wizard", 790, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                        Console.WriteLine("TaxExempt: Update all records as State Taxable and show wizard");
                        break;
                    case 3:
                        MarkAllStateTaxable(objChecker.DuplicateDataSet, intEngagementID, true);
                        objChecker.DuplicateDataSet = null;
                        CommonCode.WriteDBLog("TaxExempt: Update all records as State Taxable and do not show wizard", 791, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                        Console.WriteLine("TaxExempt: Update all records as State Taxable and do not show wizard");
                        break;
                }
            }

            CommonCode.WriteDBLog($"GetTaxExemptInterestData completed for EngagementID: {intEngagementID}", 1004, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine($"GetTaxExemptInterestData completed for EngagementID: {intEngagementID}");
            return objChecker;
        }

        private bool UpdateTaxExemptCompleted(int intEngagementID)
        {
            try
            {

                var spParams = new SpParameter[1];
                spParams[0] = new SpParameter("@EngagementID", intEngagementID);
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateTaxExempt", spParams, true);

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool UpdateTaxExemptDescriptionRecords(string strDescriptionID, int intEngagementID)
        {
            var spParams = new SpParameter[2];
            spParams[0] = new SpParameter("@EngagementFaxFormFieldID", strDescriptionID);
            spParams[1] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSpEngfaxformfield", spParams, true);

            return true;
        }

        private int GetTaxExemptOptionValue(int intEngagementID)
        {
            int optionvalue = 0;
            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            //return (int)CommonCode.GetEngagementDbCommon(intEngagementID).GetData("dbo.Proc_GetTaxexemptOption", spParams, true);
            using (var connection = CommonCode.GetEngagementDbCommon(intEngagementID).GetConnection())
            {
                using (var command = new SqlCommand("dbo.Proc_GetTaxexemptOption", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    foreach (var spParam in spParams)
                    {
                        command.Parameters.Add(ConvertToSqlParameter(spParam));
                    }

                    // command.Parameters.AddRange(spParams);

                    var result = command.ExecuteScalar();
                    optionvalue = (int)result;

                }
            }
            return (int)optionvalue;
        }

        private void AddUpdateTaxExemptData(DataSet dsTaxExemptData, int intEngagementID, bool blnTaxExemptCompleted, int intCreatedBy)
        {
            var strDescriptionID = new StringBuilder();


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_DeleteTaxExemptData", spParams, true, false);

            if (dsTaxExemptData.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in dsTaxExemptData.Tables[0].Rows)
                {
                    if (Convert.ToBoolean(row["X- if state taxable"]))
                    {
                        spParams = new SpParameter[6];
                        spParams[0] = new SpParameter("@EngagementFaxFormFieldID", row["ID"]);
                        spParams[1] = new SpParameter("@FFValue", row["State Taxable"]);
                        spParams[2] = new SpParameter("@CreatedBy", intCreatedBy);
                        spParams[3] = new SpParameter("@EngID", intEngagementID);
                        spParams[4] = new SpParameter("@TaxExemptCompleted", 0);
                        spParams[5] = new SpParameter("@FaxDWPCode", row["FaxDWPCode"]);
                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_addTaxExemptRecord", spParams, true, false);

                        if (!string.IsNullOrWhiteSpace(row["FaxDWPCode_Desc"].ToString()))
                        {
                            spParams = new SpParameter[6];
                            spParams[0] = new SpParameter("@EngagementFaxFormFieldID", row["ID_Desc"]);
                            spParams[1] = new SpParameter("@FFValue", row["Description"]);
                            spParams[2] = new SpParameter("@CreatedBy", intCreatedBy);
                            spParams[3] = new SpParameter("@EngID", intEngagementID);
                            spParams[4] = new SpParameter("@TaxExemptCompleted", 0);
                            spParams[5] = new SpParameter("@FaxDWPCode", row["FaxDWPCode_Desc"]);
                            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_addTaxExemptRecord", spParams, true, false);

                            if (strDescriptionID.Length == 0)
                            {
                                strDescriptionID.Append(row["ID_Desc"]);
                            }
                            else
                            {
                                strDescriptionID.Append($",{row["ID_Desc"]}");
                            }
                        }
                    }
                }
            }

            if (blnTaxExemptCompleted)
            {
                spParams = new SpParameter[6];
                spParams[0] = new SpParameter("@EngagementFaxFormFieldID", 0);
                spParams[1] = new SpParameter("@FFValue", "");
                spParams[2] = new SpParameter("@CreatedBy", intCreatedBy);
                spParams[3] = new SpParameter("@EngID", intEngagementID);
                spParams[4] = new SpParameter("@TaxExemptCompleted", 1);
                spParams[5] = new SpParameter("@FaxDWPCode", "");
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_addTaxExemptRecord", spParams, true, false);

                if (strDescriptionID.Length > 0)
                {
                    UpdateTaxExemptDescriptionRecords(strDescriptionID.ToString(), intEngagementID);
                }
            }

        }

        private DataSet MarkAllStateTaxable(DataSet dsTaxExemptData, int intEngagementID, bool blnTaxExemptCompleted)
        {
            int intCreatedBy = 5;

            // Update dataset rows
            foreach (DataRow row in dsTaxExemptData.Tables[0].Rows)
            {
                row["X- if state taxable"] = true;
                row["State Taxable"] = row["Tax Exempt Interest"];
            }

            AddUpdateTaxExemptData(dsTaxExemptData, intEngagementID, blnTaxExemptCompleted, intCreatedBy);
            return dsTaxExemptData;
        }

        private int EngagementTaxYear(int intEngagementID)
        {

            int taxyear = 0;
            SpParameter[] spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            //var result = CommonCode.GetEngagementDbCommon(intEngagementID).GetData("dbo.Proc_GetFuncCheckSteps", spParams, true);
            using (var connection = CommonCode.GetEngagementDbCommon().GetConnection())
            {
                using (var command = new SqlCommand("dbo.Proc_GetTaxYear", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    foreach (var spParam in spParams)
                    {
                        command.Parameters.Add(ConvertToSqlParameter(spParam));
                    }

                    // command.Parameters.AddRange(spParams);

                    var result = command.ExecuteScalar();
                    taxyear = (int)result;

                }
            }
            return (int)taxyear;


        }
    
    private bool ResidentState(int intEngagementID)
        {
            bool blnResidentState = false;
            int intRecordcount = 0;

            string connectionString = GenModule.configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("Proc_CheckResidentStateData", connection);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@EngID", intEngagementID));
                command.Parameters.Add(new SqlParameter("@RecordCount", intRecordcount) { Direction = ParameterDirection.Output });

                connection.Open(); // Ensure the connection is open

                object result = command.ExecuteScalar();
                if (result != null)
                {
                    intRecordcount = Convert.ToInt32(result);
                }
            }

            if (intRecordcount > 0)
            {
                blnResidentState = true;
                //SurePrepLogger.LogEntry(SurePrepLogger.Severity.Low, "DDPAgent", "No records found for TaxExempt", 792, 1, intEngagementID, SurePrepLogger.TraceType.InfoLog);
                CommonCode.WriteDBLog("No records found for TaxExempt", 792, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine("No records found for TaxExempt");
                UpdateTaxExemptCompleted(intEngagementID);
            }

            return blnResidentState;
        }

       
        private Class1040ScanChecker GetEngagementInfo(int intEngagementID, ref Class1040ScanChecker objChecker)
        {
            DataTable dtEngagementData;
            int intEngId;
            int intTaxYear;

            // Log entry for starting the method
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", $"GetEngagementInfo started for EngagementID : {intEngagementID} -> {DateTime.Now}", 1006, 1, intEngagementID, CommonCode.TraceType.InfoLog);


            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };
            dtEngagementData = CommonCode.GetEngagementDbCommon().GetDataTable("dbo.Proc_GetSpEngagement", spParams, "PopulateEngagement", true);


            foreach (DataRow row in dtEngagementData.Rows)
            {
                objChecker.AnywhereAccess = row["AnywhereAccess"].ToString() == "1";
                objChecker.TaxSoftWareID = Convert.ToInt32(row["TaxSoftwareID"]);
                intEngId = Convert.ToInt32(row["EngagementTypeID"]);
                intTaxYear = Convert.ToInt32(row["TaxYear"]);

                if (intEngId == 3 || intEngId == 4 || intEngId == 6)
                {
                    objChecker.EngagementTypeID = 3;
                }
                else
                {
                    objChecker.EngagementTypeID = 1;
                }

                objChecker.TaxYear = intTaxYear;
            }

            // Log entry for completing the method
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", $"GetEngagementInfo completed for EngagementID : {intEngagementID} -> {DateTime.Now}", 1007, 1, intEngagementID, CommonCode.TraceType.InfoLog);

            dtEngagementData = null;
            return objChecker;
        }
        public DataSet GetEngagementWizardSteps(string strGuid, int intEngagementID)
        {
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", $"GetEngagementWizardSteps started for EngagementID : {intEngagementID} -> {DateTime.Now}", 1008, 1, intEngagementID, CommonCode.TraceType.InfoLog);

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };
            return CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetSPEngagementWizardSteps_NET", spParams, true);

        }

        private void SetChecker(bool blnIsPrimaryVerification, int intEngagementID, ref string strOCRFieldID, ref DataSet dsReviewWizard, ref Class1040ScanChecker objChecker)
        {
            if (blnIsPrimaryVerification)
            {
                var objEvaluate = new EvaluateFormula();
                DataTable TypeVariationDataTable, TypeVariationDataTable1;
                var strOCRFieldID1 = new StringBuilder();
                var dvTypeVariation = new DataView();
                objChecker.selectedType = Class1040ScanChecker.ActivityType.PrimaryVerification;


                var spParams = new SpParameter[]
                {
                new SpParameter("@EngagementID", intEngagementID),
                new SpParameter("@Csteps", 1)
                };
                TypeVariationDataTable = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataTable("Proc_GetTypeVariationTemplates", spParams, "TypeVariationData", true);


                dvTypeVariation = TypeVariationDataTable.DefaultView;
                if (dsReviewWizard != null && dsReviewWizard.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in dsReviewWizard.Tables[0].Rows)
                    {
                        try
                        {
                            if ((row["FaxDWPCode"].ToString() == "FAX.CON.001" || row["FaxDWPCode"].ToString() == "FAX.GRT.001") &&
                                TypeVariationDataTable != null && TypeVariationDataTable.Rows.Count > 0)
                            {
                                dvTypeVariation.RowFilter = $"OCRtemplateID = {row["OCRTemplateID"]}";
                                if (dvTypeVariation != null && dvTypeVariation.Count > 0)
                                {
                                    dvTypeVariation.RowFilter = null;
                                    dvTypeVariation.RowFilter = $"BrokerName = '{row["OCRValue"].ToString().Replace("'", "''")}' And OCRTemplateID = {row["OCRTemplateID"]}";

                                    if (dvTypeVariation.Count >= 1)
                                    {
                                        CommonCode.AppendString(row["EngagementOCRFieldID"].ToString(), strOCRFieldID1);
                                        dvTypeVariation.RowFilter = null;
                                    }
                                    else
                                    {
                                        string[] arrMatch = new string[1];
                                        int[] arrPercentage = new int[1];
                                        int[] arrMatchID = new int[1];
                                        int intID1 = 0;
                                        int intCounter = 0;
                                        string strTempBrokerName = string.Empty;

                                        dvTypeVariation.RowFilter = null;


                                        spParams = new SpParameter[]
                                       {
                                        new SpParameter("@OCRTemplateID", Convert.ToInt32(row["OCRTemplateID"]))
                                       };
                                        TypeVariationDataTable1 = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataTable("Proc_GetTypeVariation1_Templates", spParams, "TypeVariationData1", true);


                                        if (TypeVariationDataTable1 != null && TypeVariationDataTable1.Rows.Count > 0)
                                        {
                                            foreach (DataRow typeRow in TypeVariationDataTable1.Rows)
                                            {
                                                int intPercentage = 0;
                                                int intMaxLength = 0;
                                                int intVal = 0;
                                                string strCase = row["OCRValue"].ToString();
                                                string strWhen = typeRow["BrokerName"].ToString();

                                                intMaxLength = Math.Max(strCase.Replace(" ", "").Length, strWhen.Replace(" ", "").Length);
                                                // intVal = objEvaluate.Levenshtein_distance(strCase, strWhen);
                                                intPercentage = Convert.ToInt32(((double)(intMaxLength - intVal) / intMaxLength) * 100);

                                                if (intPercentage >= 80)
                                                {
                                                    Array.Resize(ref arrMatch, intCounter + 1);
                                                    Array.Resize(ref arrMatchID, intCounter + 1);
                                                    Array.Resize(ref arrPercentage, intCounter + 1);

                                                    arrMatch[intCounter] = typeRow["BrokerName"].ToString();
                                                    arrMatchID[intCounter] = Convert.ToInt32(typeRow["OCRNewTemplateVariationID"]);
                                                    arrPercentage[intCounter] = intPercentage;
                                                    intCounter++;
                                                }
                                            }
                                        }

                                        if (arrMatch.Length > 0)
                                        {
                                            if (arrMatch.Length == 1)
                                            {
                                                row["OCRVerifiedValue"] = arrMatch[0];
                                            }
                                            else
                                            {
                                                for (int j = 0; j < arrPercentage.Length - 1; j++)
                                                {
                                                    if (arrPercentage[j] >= arrPercentage[j + 1])
                                                    {
                                                        intID1 = arrMatchID[j];
                                                        strTempBrokerName = arrMatch[j];
                                                    }
                                                    else
                                                    {
                                                        intID1 = arrMatchID[j + 1];
                                                        strTempBrokerName = arrMatch[j + 1];
                                                    }
                                                }
                                                if (intID1 > 0)
                                                {
                                                    row["OCRVerifiedValue"] = strTempBrokerName;
                                                }
                                            }
                                        }
                                        UpdateChecker(dvTypeVariation, ref objChecker);
                                    }
                                }
                                else
                                {
                                    CommonCode.AppendString(row["EngagementOCRFieldID"].ToString(), strOCRFieldID1);
                                    dvTypeVariation.RowFilter = null;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Intentionally left blank
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(strOCRFieldID1.ToString()))
                {
                    if (string.IsNullOrWhiteSpace(strOCRFieldID))
                    {
                        strOCRFieldID = strOCRFieldID1.ToString();
                    }
                    else
                    {
                        strOCRFieldID += "," + strOCRFieldID1.ToString();
                    }
                }
            }
        }

        private void UpdateChecker(DataView dvTypeVariation, ref Class1040ScanChecker objChecker)
        {
            foreach (DataRowView row in dvTypeVariation)
            {
                int oCRTemplateVariationId = Convert.ToInt32(row["OCRTemplateVariationID"]);
                string oCRTemplateName = row["OCRTemplateName"] == DBNull.Value ? "" : row["OCRTemplateName"].ToString();
                int oCRTemplateId = row["OCRTemplateID"] == DBNull.Value ? 0 : Convert.ToInt32(row["OCRTemplateID"]);
                string brokerName = row["BrokerName"] == DBNull.Value ? "" : row["BrokerName"].ToString();

                var objSPVTypeVariation = new SPVTypeVariation(oCRTemplateVariationId, oCRTemplateId, oCRTemplateName, brokerName);
                objChecker.AddTypeVarationItem(objSPVTypeVariation);
            }
        }
        private bool MarkUnCertainFields_Skip(int intEngagementID)
        {
            try
            {
                // LogEntry(CommonCode.Severity.Low, "DDPAgent", $"MarkUnCertainFields_Skip started for EngagementID : {intEngagementID} -> {DateTime.Now}", 1009, 1, intEngagementID, CommonCode.TraceType.InfoLog);


                var spParams = new SpParameter[]
                {
                new SpParameter("@EngagementID", intEngagementID)
                };
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_Updatespengagementocrfield", spParams, true);


                UpdateEngVerificationOption(intEngagementID);
                // LogEntry(CommonCode.Severity.Low, "DDPAgent", $"MarkUnCertainFields_Skip completed for EngagementID : {intEngagementID} -> {DateTime.Now}", 1010, 1, intEngagementID, CommonCode.TraceType.InfoLog);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Class1040ScanChecker EvaluateVerificationItem(int intEngagementId)
        {
            DataTable correctionData;
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.Correction);
            bool blnNeedsCorrection = false;
            int intNeedsCorrection = -1;
            string strRuleType, strRule, strOCRRuleTip;

            DataSet dsDropDown, dsFaxData, dsEngFormFieldData;
            DataTable dtDropDown, dtFaxData, dtEngFormFieldData;
            string strDDCodeString;

            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "Retrieving Data For Correction...", 1011, 1, intEngagementId, CommonCode.TraceType.InfoLog);
            CommonCode.WriteDBLog("Retrieving Data For Correction...", 793, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementId);
            Console.WriteLine("Retrieving Data For Correction...");

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementId)
            };
            correctionData = CommonCode.GetEngagementDbCommon(intEngagementId).GetDataTable("Proc_GetOCRData", spParams, "CorrectionData", true);


            if (correctionData.Rows.Count <= 0)
            {
                return objChecker;
            }

            strDDCodeString = GetDDCodeString(correctionData);

            if (!string.IsNullOrEmpty(strDDCodeString))
            {
                strDDCodeString = strDDCodeString.Substring(1);
            }

            this.EngId = intEngagementId; // BIN-6033 to fix the issue of Me.EngId not being set for PopulateDropDownData_Skip
            dsDropDown = PopulateDropDownData_Skip(
                Convert.ToInt32(correctionData.Rows[0]["TaxSoftwareID"]),
                Convert.ToInt32(correctionData.Rows[0]["EngagementTypeID"]) == 5 ? 1 : Convert.ToInt32(correctionData.Rows[0]["EngagementTypeID"]),
                Convert.ToInt32(correctionData.Rows[0]["TaxYear"]),
                "DropDown",
                strDDCodeString
            );
            dtDropDown = dsDropDown.Tables["DropDown"];

            dsFaxData = PopulateFaxData(intEngagementId, "FaxData");
            dtFaxData = dsFaxData.Tables["FaxData"];

            dsEngFormFieldData = PopulateEngagementFormFieldData(intEngagementId, "EngFormFieldData");
            dtEngFormFieldData = dsEngFormFieldData.Tables["EngFormFieldData"];

            List<ListValues> objListValues = null;
            foreach (DataRow row in correctionData.Rows)
            {
                intNeedsCorrection = -1;
                for (int i = 1; i <= 10; i++)
                {
                    strRuleType = $"OCRRuleType{i}";
                    strRule = $"OCRRule{i}";
                    strOCRRuleTip = $"OCRRuleTip{i}";

                    if (Convert.ToInt32(CommonCode.ReplaceNull(row[strRuleType], "0")) == 1)
                    {
                        intNeedsCorrection = CheckComparison(
                            CommonCode.ReplaceNull(row[strRule], "").ToString(),
                            dtFaxData,
                            correctionData,
                            null,
                            null,
                            Convert.ToInt32(row["EngagementPageID"]),
                            Convert.ToInt32(row["FaxRowNumber"]),
                            CommonCode.ReplaceNull(row["PreOCRFormName"], "").ToString(),
                            Convert.ToInt32(CommonCode.ReplaceNull(row["DataType"], "0"))
                        );
                    }
                    else if (Convert.ToInt32(CommonCode.ReplaceNull(row[strRuleType], "0")) == 2)
                    {
                        objListValues = CheckList(
                            CommonCode.ReplaceNull(row[strRule], "").ToString(),
                            row["OCRValue"].ToString(),
                            dtDropDown,
                            dtFaxData,
                            correctionData,
                            null,
                            null,
                            null,
                            false,
                            dtEngFormFieldData
                        );
                    }
                    else if (Convert.ToInt32(CommonCode.ReplaceNull(row[strRuleType], "0")) == 3)
                    {
                        blnNeedsCorrection = CheckRange(CommonCode.ReplaceNull(row[strRule], "").ToString(), row["OCRValue"].ToString());
                        if (blnNeedsCorrection)
                        {
                            intNeedsCorrection = 0;
                        }
                    }
                    else if (Convert.ToInt32(CommonCode.ReplaceNull(row[strRuleType], "0")) == 4)
                    {
                        blnNeedsCorrection = CheckRequired(CommonCode.ReplaceNull(row[strRule], "").ToString(), row["OCRValue"].ToString());
                        if (blnNeedsCorrection)
                        {
                            intNeedsCorrection = 0;
                        }
                    }
                    else if (Convert.ToInt32(CommonCode.ReplaceNull(row[strRuleType], "0")) == 5)
                    {
                        blnNeedsCorrection = CheckRegEx(CommonCode.ReplaceNull(row[strRule], "").ToString(), row["OCRValue"].ToString());
                        if (blnNeedsCorrection)
                        {
                            intNeedsCorrection = 0;
                        }
                    }

                    if (intNeedsCorrection == 0 || (objListValues != null && objListValues.Count > 0))
                    {
                        intNeedsCorrection = 0;
                        break;
                    }
                }

                if (intNeedsCorrection == 0 || (objListValues != null && objListValues.Count > 0) || Convert.ToInt32(row["DataType"]) == 5)
                {
                    blnNeedsCorrection = false;
                    if (Convert.ToInt32(row["DataType"]) == 1)
                    {
                        GenerateBusinessDiagnostic(row, intEngagementId);
                    }
                }
            }

            if (_dtBusinessDiagnostic != null && _dtBusinessDiagnostic.Rows.Count > 0)
            {
                InsertDiagnostic(intEngagementId);
            }

            dtDropDown = null;
            dsDropDown = null;
            dtFaxData = null;
            dsFaxData = null;
            dtEngFormFieldData = null;
            dsEngFormFieldData = null;
            correctionData = null;

            // LogEntry(CommonCode.Severity.Low, "DDPAgent", $"EvaluateVerificationItem completed for EngagementID : {intEngagementId} -> {DateTime.Now}", 1012, 1, intEngagementId, CommonCode.TraceType.InfoLog);
            return objChecker;
        }

        private void GenerateBusinessDiagnostic(DataRow drwFaxFormFieldRow, int intEngagementId)
        {
            AddBusinessDiagnosticData(drwFaxFormFieldRow);
            if (_dtBusinessDiagnostic.Rows.Count > 5000)
            {
                InsertDiagnostic(intEngagementId);
            }
        }

        private void GetBusinessDiagnosticTable()
        {
            _dtBusinessDiagnostic = new DataTable();

            var dcBusinessDiagnostic = new DataColumn
            {
                ColumnName = "EngagementPageID",
                DataType = typeof(int)
            };
            _dtBusinessDiagnostic.Columns.Add(dcBusinessDiagnostic);

            dcBusinessDiagnostic = new DataColumn
            {
                ColumnName = "OCRValue",
                DataType = typeof(string)
            };
            _dtBusinessDiagnostic.Columns.Add(dcBusinessDiagnostic);

            dcBusinessDiagnostic = new DataColumn
            {
                ColumnName = "FaxFormFieldID",
                DataType = typeof(int)
            };
            _dtBusinessDiagnostic.Columns.Add(dcBusinessDiagnostic);

            dcBusinessDiagnostic = new DataColumn
            {
                ColumnName = "FaxFormID",
                DataType = typeof(int)
            };
            _dtBusinessDiagnostic.Columns.Add(dcBusinessDiagnostic);

            dcBusinessDiagnostic = new DataColumn
            {
                ColumnName = "Diagnostics",
                DataType = typeof(string)
            };
            _dtBusinessDiagnostic.Columns.Add(dcBusinessDiagnostic);

            dcBusinessDiagnostic = new DataColumn
            {
                ColumnName = "EngagementOCRFieldID",
                DataType = typeof(int)
            };
            _dtBusinessDiagnostic.Columns.Add(dcBusinessDiagnostic);
        }

        private void AddBusinessDiagnosticData(DataRow drwFaxFormFieldRow)
        {
            if (_dtBusinessDiagnostic == null)
            {
                GetBusinessDiagnosticTable();
            }

            DataRow drBusinessDiagnostic = _dtBusinessDiagnostic.NewRow();

            drBusinessDiagnostic["EngagementPageId"] = drwFaxFormFieldRow["EngagementPageId"];
            drBusinessDiagnostic["OCRValue"] = ReplaceStringAsNull((string)drwFaxFormFieldRow["OCRValue"]);
            drBusinessDiagnostic["FaxFormFieldId"] = ReplaceStringAsNull((string)drwFaxFormFieldRow["FaxFormFieldId"]);
            drBusinessDiagnostic["FaxFormId"] = drwFaxFormFieldRow["FaxFormID"];
            drBusinessDiagnostic["Diagnostics"] = ReplaceStringAsNull((string)drwFaxFormFieldRow["OCRRuleTip1"]);
            drBusinessDiagnostic["EngagementOCRFieldId"] = drwFaxFormFieldRow["EngagementOCRFieldId"];

            _dtBusinessDiagnostic.Rows.Add(drBusinessDiagnostic);
        }

        private void InsertDiagnostic(int intEngagementId)
        {

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementId),
            new SpParameter("@UDT_OCRValue", _dtBusinessDiagnostic)
            };

            CommonCode.GetEngagementDbCommon(intEngagementId).AddUpdateOrDelete("[dbo].[Proc_MBInsertEngBusinessDiagnostic_Bulk]", spParams, true);


            GetBusinessDiagnosticTable();
        }

        private void UpdateVerification_Skip(int intEngagementID)
        {
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", $"UpdateVerification_Skip started for EngagementID : {intEngagementID} -> {DateTime.Now}", 1013, 1, intEngagementID, CommonCode.TraceType.InfoLog);


            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };

            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_AfterNewVerification", spParams, true, false);


            UpdateVerificationCompleted(intEngagementID, "N"); // This Function Updates Verification process as Complete
                                                               // LogEntry(CommonCode.Severity.Low, "DDPAgent", $"UpdateVerification_Skip completed for EngagementID : {intEngagementID} -> {DateTime.Now}", 1014, 1, intEngagementID, CommonCode.TraceType.InfoLog);
        }

        private bool UpdateVerificationCompleted(int intEngagementID, string strInSPVerification)
        {


            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };

            if (strInSPVerification == "Y")
            {
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_VerificationCompleted", spParams, true, false);
            }
            else
            {
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_SPEngagementsVerificationCompleted", spParams, true, false);
            }


            return true;
        }
        private string ReplaceStringAsNull(string strData)
        {
            // strData = strData.Replace("'", "''");
            return string.IsNullOrEmpty(strData) ? "" : strData;
        }

        private DataSet PopulateDropDownData_Skip(int intTaxSoftwareID, int intEngagementTypeID, int intTaxYear, string strTableName, string strDDCode)
        {
            SpParameter[] spParams = new SpParameter[4];
            DataSet dropDownListDataSet = new DataSet();


            spParams[0] = new SpParameter("@Filter1", intTaxSoftwareID);
            spParams[1] = new SpParameter("@Filter2", intEngagementTypeID);
            spParams[2] = new SpParameter("@Filter3", intTaxYear);
            spParams[3] = new SpParameter("@arrCode", strDDCode);
            dropDownListDataSet = CommonCode.GetEngagementDbCommon().GetDataSet("Proc_DropDownListCode", spParams, true);


            dropDownListDataSet.Tables[0].TableName = strTableName;
            return dropDownListDataSet;
        }
        private bool ModifyParentFields_2011(
    int intP_PrimaryEngFaxFormID,
    int intP_ParentFrmAssoc_FaxFormID,
    string strP_PIValue,
    string strForm,
    int intP_SecondaryEngFaxFormID,
    int intEngagementID,
    string intP_CFAIdentifierID,
    string StrP_CFAValue)
        {
            string sqlstring = string.Empty;
            string strIdentifierId = string.Empty;
            SpParameter[] spParams;


            if (intP_PrimaryEngFaxFormID > 0 || intP_SecondaryEngFaxFormID > 0)
            {
                spParams = new SpParameter[]
                {
                new SpParameter("@FFValue", strForm),
                new SpParameter("@Engagementfaxformid", intP_ParentFrmAssoc_FaxFormID)
                };
                CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSpEngagementFaxForm", spParams, false, false);

                if (intP_PrimaryEngFaxFormID > 0)
                {
                    spParams = new SpParameter[]
                    {
                    new SpParameter("@FFValue", strP_PIValue),
                    new SpParameter("@Engagementfaxformid", intP_ParentFrmAssoc_FaxFormID),
                    new SpParameter("@Engagementfaxformfieldid", intP_PrimaryEngFaxFormID)
                    };
                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSpEngagementFaxFormField2", spParams, true, false);
                    strIdentifierId = intP_PrimaryEngFaxFormID + ",";
                }

                if (intP_SecondaryEngFaxFormID > 0)
                {
                    spParams = new SpParameter[]
                    {
                    new SpParameter("@FFValue", strP_PIValue),
                    new SpParameter("@Engagementfaxformid", intP_ParentFrmAssoc_FaxFormID),
                    new SpParameter("@Engagementfaxformfieldid", intP_SecondaryEngFaxFormID)
                    };
                    CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSpEngagementFaxFormField2", spParams, true, false);
                    strIdentifierId += intP_SecondaryEngFaxFormID + ",";
                }

                if (!string.IsNullOrEmpty(intP_CFAIdentifierID))
                {
                    string[] ids = intP_CFAIdentifierID.Split(',');
                    string[] strValues = StrP_CFAValue.Split(',');

                    for (int i = 0; i < ids.Length; i++)
                    {
                        spParams = new SpParameter[]
                        {
                        new SpParameter("@FFValue", strValues[i]),
                        new SpParameter("@Engagementfaxformid", intP_ParentFrmAssoc_FaxFormID),
                        new SpParameter("@Engagementfaxformfieldid", int.Parse(ids[i]))
                        };
                        CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSpEngagementFaxFormField2", spParams, true, false);
                    }

                    strIdentifierId += intP_CFAIdentifierID + ",";
                }

                if (strIdentifierId.Length > 1)
                {
                    strIdentifierId = strIdentifierId.Substring(0, strIdentifierId.Length - 1);
                }

                UpdateAllParentFields_2011(intP_ParentFrmAssoc_FaxFormID, intEngagementID, strIdentifierId);
            }


            return true;
        }

        private void UpdateAllParentFields_2011(int intEngFaxFormID, int intEngagementID, string strIdentifierID)
        {

            var sqlParam = new SpParameter[]
            {
            new SpParameter("@EngFaxFormID", intEngFaxFormID),
            new SpParameter("@EngagementID", intEngagementID),
            new SpParameter("@strIdentifierID", strIdentifierID)
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Update_PrimaryfaxFormFields_2011", sqlParam, true);

        }
        private Class1040ScanChecker GetParentAssociationItem_2011(int intEngagementID)
        {
            DataSet dataSet;
            DataTable dataTable;
            DataTable pageData = null;
            Class1040ScanParentAssoc objParentItem;
            Class1040ScanhangingFrms objUnassociatedFormsItem = null;
            var objChecker = new Class1040ScanChecker(Class1040ScanChecker.ActivityType.ParentAssociation);
            var codes = new StringBuilder();
            RWReference objPageRef;
            RWPage objPage;
            int intPrevEngagementFaxFormID = 0;
            var pageIds = new StringBuilder();

            // Log entry
            CommonCode.WriteDBLog("Retrieving Data For Parent Association...", 794, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
            Console.WriteLine("Retrieving Data For Parent Association...");
            // Retrieve unassociated forms

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };
            dataSet = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetUnassociatedFaxForms_2011", spParams, true);


            dataSet.Tables[0].TableName = "UnassociatedForms";
            dataTable = dataSet.Tables["UnassociatedForms"];

            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    CommonCode.AppendString(row["engagementpageid"].ToString(), pageIds);
                }

                // Log entry
                CommonCode.WriteDBLog("Retrieving Data For Page Referencing...", 795, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine("Retrieving Data For Page Referencing...");
                // Retrieve page referencing data

                spParams = new SpParameter[]
               {
                new SpParameter("@EngagementID", intEngagementID),
                new SpParameter("@ForData", 1),
                new SpParameter("@EngagementPageID", pageIds.ToString())
               };
                dataSet = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_PageGetShowAll", spParams, true);


                dataSet.Tables[0].TableName = "PageFaxForms";
                pageData = dataSet.Tables["PageFaxForms"];
            }

            foreach (DataRow row in dataTable.Rows)
            {
                if (intPrevEngagementFaxFormID != Convert.ToInt32(row["Engagementfaxformid"]))
                {
                    objUnassociatedFormsItem = new Class1040ScanhangingFrms(
                        Convert.ToInt32(row["EngagementID"]),
                        (int)CommonCode.ReplaceNull(row["EngagementFaxFormID"], "0"),
                        row["FormName"].ToString(),
                        (int)CommonCode.ReplaceNull(row["ParentEngagementFaxFormID"], "0"),
                        row["ParentFaxFormDWPCode"].ToString(),
                        Convert.ToInt32(row["EngagementPageID"]),
                        row["FileName"].ToString(),
                        Convert.ToInt32(row["ClientPageDPI"]),
                        row["FileType"].ToString(),
                        Convert.ToString(row["isautomatched"])
                    );

                    objUnassociatedFormsItem.P_HanginFrms_EngagementFieldGroupID = (int)CommonCode.ReplaceNull(row["EngagementFieldGroupID"], "0");
                    objUnassociatedFormsItem.FaxDWPCode = row["FaxDWPCode"] == DBNull.Value ? "" : row["FaxDWPCode"].ToString();
                    objUnassociatedFormsItem.P_FaxID = (int)CommonCode.ReplaceNull(row["FaxFormID"], "0");
                    objUnassociatedFormsItem.P_NewChildFaxFormID = (int)CommonCode.ReplaceNull(row["NewChildFaxFormID"], "0");

                    objChecker.AddUnassociatedFormsItem(objUnassociatedFormsItem);
                    CommonCode.AppendString(row["ParentFaxFormDWPCode"].ToString(), codes);
                    intPrevEngagementFaxFormID = Convert.ToInt32(row["Engagementfaxformid"]);
                }

                if (Convert.ToInt32(row["EngagementPageID"]) > 0)
                {


                    objPage = new RWPage(
                        Convert.ToInt32(row["EngagementPageID"]),
                        0,
                        "",
                        row["FileName"].ToString(),
                        "",
                        0,
                        0,
                        Convert.ToInt32(row["VirtualRotation"]), 
                        false,
                        "",
                        Convert.ToInt32(row["ClientPageDPI"]),  //int
                        row["FileType"].ToString()



                    );

                    if (pageData != null && pageData.Rows.Count > 0)
                    {
                        var drow = pageData.Select($"Engagementpageid={row["EngagementPageID"]} And Engagementfaxformid={row["Engagementfaxformid"]}");
                        if (drow != null && drow.Length > 0)
                        {
                            foreach (var pageRow in drow)
                            {
                                objPageRef = new RWReference(
                                    Convert.ToInt32(pageRow["EngagementPageID"]),
                                    pageRow["Fieldvalue"].ToString(),
                                    Convert.ToInt32(pageRow["FFX"]),
                                    Convert.ToInt32(pageRow["FFY"]),
                                    Convert.ToInt32(pageRow["FFHeight"]),
                                    Convert.ToInt32(pageRow["FFwidth"]),
                                    pageRow["FaxDwpcode"].ToString(),
                                    pageRow["Faxrownumber"] == DBNull.Value ? 0 : Convert.ToInt32(pageRow["Faxrownumber"]),
                                    Convert.ToInt32(pageRow["DataType"]),
                                    Convert.ToInt32(pageRow["Faxformid"]),
                                    Convert.ToInt32(pageRow["Faxformfieldid"]),
                                    pageRow["faxformName"].ToString(),
                                    pageRow["faxFieldname"].ToString(),
                                    pageRow["Engformname"].ToString(),
                                    Convert.ToInt32(pageRow["EngagementfaxformId"]),
                                    Convert.ToInt32(pageRow["TaxFormInstanceNo"]),
                                    Convert.ToInt32(pageRow["Uncertainchar"].ToString())
                                );

                                objPageRef.DropDownDWPCode = pageRow["DropDownDWPCode"].ToString();
                                objPageRef.EngagementFaxFormFieldID = Convert.ToInt32(pageRow["EngagementFaxFormFieldID"]);
                                objPageRef.Identifier = pageRow["Identifier"].ToString();
                                objPageRef.IsEditable = Convert.ToInt32(pageRow["IsEditable"]);
                                objPage.AddPageReference(objPageRef);
                            }
                        }
                    }

                    objUnassociatedFormsItem.AddPage(objPage);
                }
            }

            // Additional processing for distinct DocumentTypeID
            var StrDocumentTypeID = new StringBuilder();
            foreach (DataRow row in dataTable.Rows)
            {
                if (Convert.ToInt32(row["EngagementFaxFormID"]) > 0)
                {
                    CommonCode.AppendString(row["DocumentTypeID"].ToString(), StrDocumentTypeID);
                }
            }

            StrDocumentTypeID.Append("0");
            if (StrDocumentTypeID.ToString().Contains("5") && StrDocumentTypeID.ToString().Contains("3"))
            {
                StrDocumentTypeID = new StringBuilder("1");
            }
            else if (StrDocumentTypeID.ToString().Contains("5"))
            {
                StrDocumentTypeID = new StringBuilder("2");
            }
            else if (!StrDocumentTypeID.ToString().Contains("5"))
            {
                StrDocumentTypeID = new StringBuilder("3");
            }

            // Retrieve additional data

            spParams = new SpParameter[]
           {
            new SpParameter("@EngagementID", intEngagementID)
           };

            dataSet = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetParentFaxFormTree_2011", spParams, true);
            dataSet.Tables[0].TableName = "ParentForms";
            dataTable = dataSet.Tables["ParentForms"];

            dataSet = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetFaxChildCFA", spParams, true);
            dataSet.Tables[0].TableName = "ChildForms";
            objChecker.ChildForms = dataSet.Tables[0];

            dataSet = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_FieldGroupCFA", spParams, true);
            dataSet.Tables[0].TableName = "FieldGroups";
            objChecker.FieldGroups = dataSet.Tables[0];

            dataSet = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetAllCFAIdentifiers", spParams, true);
            dataSet.Tables[0].TableName = "AllCFAIdentifiers";
            objChecker.AllCFAIdentifiers = dataSet.Tables[0];

            dataSet = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetAssociatedFaxForms_NEW", spParams, true);
            dataSet.Tables[0].TableName = "AllForms";
            objChecker.AllForms = dataSet.Tables[0];

            dataSet = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_GetFieldGroupParent_NEW", spParams, true);
            dataSet.Tables[0].TableName = "dsFieldGroupParent";
            objChecker.FieldGroupParent = dataSet.Tables[0];


            foreach (DataRow row in dataTable.Rows)
            {
                if ((StrDocumentTypeID.ToString() == "2" && Convert.ToInt32(row["DocumentTypeID"]) == 3) ||
                    (StrDocumentTypeID.ToString() == "3" && Convert.ToInt32(row["DocumentTypeID"]) == 5))
                {
                    objParentItem = null;
                }
                else
                {
                    objParentItem = new Class1040ScanParentAssoc(
                        intEngagementID,
                        Convert.ToInt32(row["faxformid"]),
                        Convert.ToInt32(CommonCode.ReplaceNull(row["EngagementFaxFormID"], "0")),
                        row["FormName"].ToString(),
                        CommonCode.ReplaceNull(row["FaxFormInstance"], "").ToString(),
                        Convert.ToInt32(row["faxformid"]),
                        row["FaxFormDWPCode"].ToString(),
                        0,
                        "",
                        "",
                        0,
                        "",
                        "",
                        row["FaxFormName"].ToString(),
                        (Class1040ScanParentAssoc.OperationEnum)Operations.Unaffected,
                        Convert.ToInt32(row["AddedByUser"].ToString()),
                        0,
                        0,
                        Convert.ToInt32(CommonCode.ReplaceNull(row["EngagementFormID"], "0"))
                    );

                    objParentItem.P_EntityID = row["ParentEngagementFaxFormID"] == DBNull.Value ? 0 : Convert.ToInt32(row["ParentEngagementFaxFormID"]);
                    objParentItem.P_ParentFaxID = row["ParentFaxFormID"] == DBNull.Value ? 0 : Convert.ToInt32(row["ParentFaxFormID"]);
                    objParentItem.P_FaxFormType = row["FaxFormType"] == DBNull.Value ? 0 : Convert.ToInt32(row["FaxFormType"]);
                    objParentItem.P_ChildFaxDWPCode = row["ChildFaxFormDWPCode"] == DBNull.Value ? "" : row["ChildFaxFormDWPCode"].ToString();
                    objParentItem.P_NewChildFaxFormID = Convert.ToInt32(CommonCode.ReplaceNull(row["NewChildFaxFormID"], "0"));
                    objParentItem.P_CFASequence = Convert.ToInt32(CommonCode.ReplaceNull(row["CFASequence"], "100"));
                    objParentItem.P_SelfParent = row["IsSelfParent"] == DBNull.Value ? false : Convert.ToBoolean(row["IsSelfParent"]);

                    objChecker.AddParentAssocItem(objParentItem);
                }
            }

            if (objChecker.GetCollection().Count <= 0 || objChecker.GetParentFormsCollection().Count <= 0)
            {
                CommonCode.WriteDBLog("CFA Wizard Not Open.. ", 796, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, intEngagementID);
                Console.WriteLine("CFA Wizard Not Open.. ");
                UpdateParentCompleted(intEngagementID);
            }

            return objChecker;
        }

        public Class1040ScanChecker UpdateParentAssociationItem_2011(string strGuid, Class1040ScanChecker b, bool blnSkipWizard, bool isMultiThreadingActive, string IsK1PreRuleValidationOn)
        {
            Class1040ScanChecker objChecker = b;
            List<object> objUnassociatedForms = objChecker.GetCollection();
            List<Class1040ScanParentAssoc> objParentForms = objChecker.GetParentFormsCollection();
            StringBuilder strFormName = new StringBuilder();

            // Log entry
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "UpdateParentAssociationItem_2011 started GUID : " + strGuid + " -> " + DateTime.Now, 1017, 1, EngId, CommonCode.TraceType.InfoLog);

            foreach (var objParent in objParentForms)
            {
                if (!string.IsNullOrEmpty(objParent.P_PIValue))
                {
                    CommonCode.AppendString(objParent.P_PIValue, strFormName, "");
                }
                if (!string.IsNullOrEmpty(objParent.P_SIValue))
                {
                    CommonCode.AppendString(" (" + objParent.P_SIValue + ")", strFormName, "");
                }

                switch (objParent.P_Operation)
                {
                    case Class1040ScanParentAssoc.OperationEnum.Inserted:
                        // No action for inserted
                        break;

                    case Class1040ScanParentAssoc.OperationEnum.Modified:
                        ModifyParentFields_2011(
                            objParent.P_PrimaryEngFaxFormID,
                            objParent.P_ParentFrmAssoc_FaxFormID,
                            objParent.P_PIValue,
                            strFormName.ToString(),
                            objParent.P_SecondaryEngFaxFormID,
                            objParent.P_ParentFrmAssoc_EngagementID,
                            objParent.P_CFAIdentifier,
                            objParent.P_CFAValue
                        );
                        break;

                    case Class1040ScanParentAssoc.OperationEnum.Deleted:
                        DeleteParentFields(objParent.P_ParentFrmAssoc_FaxFormID, objParent.P_ParentFrmAssoc_EngagementID);
                        break;
                }
            }
            for (int intCounter1 = 0; intCounter1 < objUnassociatedForms.Count; intCounter1++)
            {
                int intnadd;
                if (objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_ParentId > 0)
                {
                    if (objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_AutoPageMatched == "N")
                    {
                        intnadd = 1;
                    }
                    else
                    {
                        intnadd = 0;
                    }
                    UpdateForParentAssociation_2011(
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_EngagementID,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_FaxFormID,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_ParentId,
                        intnadd,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_EngagementFieldGroupID,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_NewChildFaxFormID
                    );
                }
                else
                {
                    UpdateForParentAssociation_2011(
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_EngagementID,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_FaxFormID,
                        0,
                        0,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_EngagementFieldGroupID,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_NewChildFaxFormID
                    );
                    UpdateDiagnosticsUnassociated(
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_EngagementID,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_FaxFormID,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_FaxID,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_PageId,
                        objChecker.GetUnassociatedFormsItem(intCounter1).P_HanginFrms_FormName
                    );
                }
            }


            if (blnSkipWizard)
            {
                objChecker.FromNext = true;
            }

            if (objChecker.FromNext)
            {
                if (objChecker.TaxYear >= 2010)
                {
                    UpdateSelfParent(objChecker.GetUnassociatedFormsItem(0).P_HanginFrms_EngagementID);
                }

                CommonCode.WriteDBLog("CFA Wizard End By User.. ", 798, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, objChecker.GetParentAssocItem(0).P_ParentFrmAssoc_EngagementID);
                Console.WriteLine("CFA Wizard End By User.. ");
                UpdateParentCompleted(objChecker.GetParentAssocItem(0).P_ParentFrmAssoc_EngagementID);
                UpdateAssociatedFaxFormData_2011(objChecker.GetParentAssocItem(0).P_ParentFrmAssoc_EngagementID);
            }

            return GetData(strGuid, objChecker.GetParentAssocItem(0).P_ParentFrmAssoc_EngagementID, isMultiThreadingActive,IsK1PreRuleValidationOn);
        }

        private void UpdateForParentAssociation_2011(int intEngagementID, int intFaxFormID, int intParentId, int intnewadded, int intFieldId, int intNewChildFaxFormID)
        {

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID),
            new SpParameter("@EngagementFaxFormID", intFaxFormID),
            new SpParameter("@ParentFaxFormId", intParentId),
            new SpParameter("@newadded", intnewadded),
            new SpParameter("@EngagementFieldGroupID", intFieldId),
            new SpParameter("@NewChildFaxFormID", intNewChildFaxFormID)
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateParentfaxForm_2011", spParams, true, false);

        }

        private void UpdateSelfParent(int intEngagementID)
        {

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateSelfParent", spParams, true, false);

        }

        private void UpdateDiagnosticsUnassociated(int intEngagementID, int intEngFaxFormID, int intFaxFormID, int intEngagementPageID, string strFormName)
        {

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID),
            new SpParameter("@EngagementFaxFormID", intEngFaxFormID),
            new SpParameter("@FaxFormID", intFaxFormID),
            new SpParameter("@EngagementPageID", intEngagementPageID),
            new SpParameter("@FormName", strFormName)
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateDiagnosticUnassociated", spParams, true, false);

        }
        private void UpdateAssociatedFaxFormData_2011(int intEngagementID)
        {

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_UpdateAssociatedFaxFormData_2011", spParams, true, false);

        }

        private void RemoveSingleAccNoField(DataView VerificationDataView, string FaxDwpCode, ref DataSet VerificationDataSet, ref DataSet ReviewWizardDataSet, ref DataTable VerificationDataTable)
        {
            string[] oCRFieldIDArray;
            var oCRFieldId = new StringBuilder();
            string[] oCRTemplateIdArray;
            var oCRTemplateId = new StringBuilder();

            for (int recordCount = 0; recordCount < VerificationDataView.Count; recordCount++)
            {
                if (string.IsNullOrWhiteSpace(oCRTemplateId.ToString()))
                {
                    CommonCode.AppendString(VerificationDataView[recordCount]["OCRTemplateID"].ToString(), oCRTemplateId);
                }
                else if (!oCRTemplateId.ToString().Contains(VerificationDataView[recordCount]["OCRTemplateID"].ToString()))
                {
                    CommonCode.AppendString(VerificationDataView[recordCount]["OCRTemplateID"].ToString(), oCRTemplateId);
                }
            }

            if (!string.IsNullOrWhiteSpace(oCRTemplateId.ToString()))
            {
                oCRTemplateIdArray = oCRTemplateId.ToString().Split(',');
                if (oCRTemplateIdArray != null && oCRTemplateIdArray.Length > 1)
                {
                    foreach (var templateId in oCRTemplateIdArray)
                    {
                        VerificationDataView.RowFilter = null;
                        VerificationDataView.RowFilter = $"FaxDWPCode = '{FaxDwpCode}' And OCRTemplateID = {int.Parse(templateId)}";
                        if (VerificationDataView.Count == 1)
                        {
                            int uncertainVal = VerificationDataView[0]["UnCertainChar"] == DBNull.Value ? 0 : Convert.ToInt32(VerificationDataView[0]["UnCertainChar"]);
                            if (uncertainVal == 0)
                            {
                                CommonCode.AppendString(VerificationDataView[0]["EngagementOCRFieldID"].ToString(), oCRFieldId);
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(oCRFieldId.ToString()))
                    {
                        oCRFieldIDArray = oCRFieldId.ToString().Split(',');
                        if (oCRFieldIDArray != null && oCRFieldIDArray.Length > 0)
                        {
                            foreach (var fieldId in oCRFieldIDArray)
                            {
                                for (int j = 0; j < VerificationDataSet.Tables[0].Rows.Count; j++)
                                {
                                    if (Convert.ToInt32(VerificationDataSet.Tables[0].Rows[j]["EngagementOCRFieldID"]) == int.Parse(fieldId))
                                    {
                                        UpdateSingleAccNoField(
                                            Convert.ToInt32(VerificationDataSet.Tables[0].Rows[j]["EngagementID"]),
                                            Convert.ToInt32(VerificationDataSet.Tables[0].Rows[j]["OCRTemplateID"]),
                                            VerificationDataSet.Tables[0].Rows[j]["FAXDWPCode"].ToString(),
                                            VerificationDataSet.Tables[0].Rows[j]["OCRValue"].ToString()
                                        );
                                        VerificationDataSet.Tables[0].Rows.RemoveAt(j);
                                        VerificationDataTable = VerificationDataSet.Tables[0];
                                        ReviewWizardDataSet = VerificationDataSet;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool UpdateSingleAccNoField(int EngagementID, int OCRTemplateID, string FaxDWPCode, string OCRValue)
        {

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", EngagementID),
            new SpParameter("@OCRTemplateID", OCRTemplateID),
            new SpParameter("@FaxDWPCode", FaxDWPCode),
            new SpParameter("@OCRValue", OCRValue)
            };
            CommonCode.GetEngagementDbCommon(EngagementID).AddUpdateOrDelete("dbo.Proc_UpdateSingleAccNoField", spParams, true, false);

            return true;
        }

        #region "NFR - Display Single Instance forms And Field Group"

        // This is Left side screen Field Group data
        public DataSet Get_NFRProformadFormFieldGroup(int intEngagementID)
        {
            var objChecker = new Class1040ScanChecker();
            var dsProformadFormFieldGroup = new DataSet();


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            dsProformadFormFieldGroup = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_OCRProformadFieldGroupData_01", spParams, true);


            dsProformadFormFieldGroup.Tables[0].TableName = "ProformadFormFieldGroup";
            objChecker.ProformadFormFieldGroup = dsProformadFormFieldGroup;
            return dsProformadFormFieldGroup;
        }

        // This is Right side screen Field Group data
        public DataSet Get_NFRFaxFormFieldGroup(int intEngagementID)
        {
            var objChecker = new Class1040ScanChecker();
            var dsFaxFormFieldGroup = new DataSet();


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            dsFaxFormFieldGroup = CommonCode.GetEngagementDbCommon(intEngagementID).GetDataSet("Proc_OCRProformadFieldGroupData_02", spParams, true);


            dsFaxFormFieldGroup.Tables[0].TableName = "FaxFormFieldGroup";
            objChecker.FaxFormFieldGroup = dsFaxFormFieldGroup;
            return dsFaxFormFieldGroup;
        }

        #endregion

        // Till here for NFR - Display Single Instance forms and Field Group - Jyoti on 26-Oct-12
        public bool CheckAuthentication(string strGuid, int intEngID, int intDomainId, int intUserID)
        {
            strGuid = CommonCode.ReplaceSPLCharacters(strGuid);


            SpParameter[] spParams = new SpParameter[2];
            spParams[0] = new SpParameter("@EngagementID", intEngID);
            spParams[1] = new SpParameter("@speguid", strGuid);

            if (Convert.ToInt32(CommonCode.GetEngagementDbCommon().GetData("dbo.Proc_GetDataToCheckAuthentication", spParams, true)) > 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }


        #region "BDP"
        public void ExecuteBDPProcedure(int intEngagementID)
        {
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "ExecuteBDPProcedure started for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1018, 1, intEngagementID, CommonCode.TraceType.InfoLog);


            var spParams = new SpParameter[1];
            spParams[0] = new SpParameter("@EngagementID", intEngagementID);
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("dbo.Proc_UpdateOCRForNonConfigDoc", spParams, true, false);


            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "ExecuteBDPProcedure completed for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1019, 1, intEngagementID, CommonCode.TraceType.InfoLog);
        }
        #endregion

        #region "SURE-2303"
        public void Update_EngFlags(int intEngagementID)
        {
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "Update_EngFlags started for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1020, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            DataSet dsEngagement = GetEngFlags(intEngagementID);
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "GetEngFlags completed for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1021, 1, intEngagementID, CommonCode.TraceType.InfoLog);


            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID),
            new SpParameter("@UDT_WizardFlags", dsEngagement.Tables[0])
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("[MBRW].[Proc_UpdateEngFlags]", spParams, true, false);

            Console.WriteLine("Update_EngFlags completed for EngagementID:" + intEngagementID);
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "Update_EngFlags completed for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1022, 1, intEngagementID, CommonCode.TraceType.InfoLog);
        }

        private DataSet GetEngFlags(int intEngagementID)
        {
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "GetEngFlags started for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1023, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            DataSet dsEngagement = new DataSet();


            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID)
            };
            dsEngagement = CommonCode.GetEngagementDbCommon().GetDataSet("[MBRW].[Proc_GetEngagementFlags]", spParams, true);


            dsEngagement.Tables[0].TableName = "EngFlags";
            return dsEngagement;
        }
        #endregion

        #region "Skip workflow steps"
        public void SkipWorkFlowSteps(int intEngagementID)
        {
            AssignToBOTUser = Convert.ToInt16( GenModule.configuration.GetSection("AssignToBOTUser").Value);
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "SkipWorkFlowSteps started for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1024, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            if (AssignToBOTUser == 0)
            {
                return;
            }
            if (MarkPageAutoVerified(intEngagementID) > 0)
            {
                MarkBinderForBot(intEngagementID);
                AssignBotUser(intEngagementID, 1);
            }
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "SkipWorkFlowSteps completed for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1025, 1, intEngagementID, CommonCode.TraceType.InfoLog);
        }

        public int MarkPageAutoVerified(int intEngagementID)
        {
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "MarkPageAutoVerified started for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1026, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            int ibotCount = 0;

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", intEngagementID),
            new SpParameter("@BotBinderFlag", 0, ParameterDirection.Output)
            };
            CommonCode.GetEngagementDbCommon(intEngagementID).AddUpdateOrDelete("Proc_MarkPageAutoVerified", spParams, true, false, true);
            ibotCount = (int)spParams[1].ArgValue;

            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "MarkPageAutoVerified completed for EngagementID : " + intEngagementID + " -> " + DateTime.Now, 1027, 1, intEngagementID, CommonCode.TraceType.InfoLog);
            return ibotCount;
        }

        public bool MarkBinderForBot(int intEngagementID)
        {
            try
            {

                var spParams = new SpParameter[]
                {
                new SpParameter("@EngagementID", intEngagementID)
                };
                CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("Proc_MarkBinderForBot", spParams, true, false);

                return true;
            }
            catch (Exception ex)
            {
                // LogEntry(CommonCode.Severity.Low, "DDPAgent", "MarkBinderForBot - Error Occurred : " + ex.Message + " -> " + DateTime.Now, 1001, 1, intEngagementID, CommonCode.TraceType.ErrorLog);
                return false;
            }
        }

        public bool AssignBotUser(int intEngagementID, int intAssign2BOT)
        {
            try
            {

                var spParams = new SpParameter[]
                {
                new SpParameter("@EngagementID", intEngagementID),
                new SpParameter("@MgrFlag", 1)
                };
                CommonCode.GetEngagementDbCommon().AddUpdateOrDelete("Proc_AssignBotUser", spParams, true, false);

                return true;
            }
            catch (Exception ex)
            {
                // LogEntry(CommonCode.Severity.Low, "DDPAgent", "AssignBotUser - Error Occurred : " + ex.Message + " -> " + DateTime.Now + " intAssign2BOT = " + intAssign2BOT, 1029, 1, intEngagementID, CommonCode.TraceType.ErrorLog);
                return false;
            }
        }
        #endregion

        public bool Update_ApplyDecimalRule(int engagementId)
        {
            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "ApplyDecimalRule execution Begin  : " + engagementId + " -> " + DateTime.Now, 1001, 1, engagementId, CommonCode.TraceType.InfoLog);

            var spParams = new SpParameter[]
            {
            new SpParameter("@EngagementID", engagementId)
            };
            CommonCode.GetEngagementDbCommon(engagementId).AddUpdateOrDelete("Proc_ApplyDecimalRule", spParams, true, false);

            // LogEntry(CommonCode.Severity.Low, "DDPAgent", "ApplyDecimalRule execution End for EngagementID : " + engagementId + " -> " + DateTime.Now, 1031, 1, engagementId, CommonCode.TraceType.InfoLog);
            return true;
        }

        public bool WriteDBLogsJobId(string strMessage, int engagementId, int jobId = 0, int stepId = 0, int subStepId = 0, bool ISSPEL = false)
        {
            try
            {
                if (stepId > 0)
                {
                    return CommonCode.WriteDdpDBLog(strMessage, engagementId, jobId, stepId, subStepId, ISSPEL);
                }

                var spParams = new SpParameter[]
                {
                new SpParameter("EngagementID", engagementId),
                new SpParameter("strMessage", strMessage)
                };
                return (Convert.ToInt32(CommonCode.GetEngagementDbCommon(engagementId).GetData("Proc_InsertSPEngagementCurrentStatus", spParams, true)) > 0);

            }
            catch (Exception ex)
            {
                // LogEntry(CommonCode.Severity.Low, "DDPAgent", "CommonCode -> WriteDBLog - Error Occurred : " + ex.Message + " -> " + DateTime.Now + " strMessage = " + strMessage + " ISSPEL = " + ISSPEL, 1111111, 1, engagementId, CommonCode.TraceType.ErrorLog);
                return false;
            }
        }



    }


}