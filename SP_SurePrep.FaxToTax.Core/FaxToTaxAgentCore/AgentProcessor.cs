using System;
using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel;

namespace FaxToTaxAgentCore
{
  

    public class AgentProcessor
    {
        //private void GetData(long EngId, string AuthToken)
        //{
        //    DataTable dt;
        //    int cntTemp;
        //    string tmpstr;

        //    try
        //    {
        //        var tmpcls = new ProcessLib();
        //        tmpcls.Constr = GenModule.ConnectionString;
        //        tmpcls.FillConnection();
        //        dt = FaxToTax_ApiWrapper.GetFaxToTaxSPEngagementsDataTable(AuthToken, (int)EngId);
        //        cntTemp = 0;

        //        if (dt.Rows.Count > 0)
        //        {
        //            cntTemp++;
        //            var row = dt.Rows[0];
        //            GenModule.domainname = Convert.IsDBNull(row["domainabbreviation"]) ? "NULL" : row["domainabbreviation"].ToString();
        //            GenModule.lngclientNumber = Convert.IsDBNull(row["clientnumber"]) ? "NULL" : row["clientnumber"].ToString();
        //            GenModule.TaxYear = Convert.IsDBNull(row["taxyear"]) ? 0 : Convert.ToInt32(row["TAXYEAR"]);
        //            GenModule.EngagementTypeID = Convert.IsDBNull(row["Engagementtypeid"]) ? 0 : Convert.ToInt32(row["Engagementtypeid"]);
        //            GenModule.TaxSoftwareID = Convert.IsDBNull(row["taxsoftwareid"]) ? 0 : Convert.ToInt32(row["taxsoftwareid"]);
        //            GenModule.IsMergeBinder = Convert.ToBoolean(row["MergeBinder"]);

        //            DataTable tmpdt = FaxToTax_ApiWrapper.GetFaxToTaxSPTaxSoftwareDataTable(AuthToken, (int)EngId, GenModule.TaxSoftwareID);
        //            GenModule.TaxSoftwareName = tmpdt.Rows[0][0].ToString();
        //        }
        //        else
        //        {
        //            return;
        //        }

        //        tmpcls = null;
        //        dt.Dispose();
        //        dt = null;
        //    }
        //    catch (Exception ex)
        //    {
        //        // SurePrepLogger.LogEntry(Severity.Low, "FaxToTax", "Error Occured in GetData: " + ex.Message + " ==>> " + DateTime.Now, 11065, 1, EngId, TraceType.ErrorLog);
        //    }
        //}

      public void StartProcess(int EngagementID, int JobId, string StrWSPath, string OCRValidation, int LogSeverity, string AgentName, string IsK1PreRuleValidationOn,
                                  bool blnSPELJOB = false)
        {
            // ----Used for Serilog logging 
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            
            var plib = new ProcessLib(StrWSPath)
            {
                engagementID = EngagementID,
                AgentId = 1,
                JbType = "PREPROCESSING",
                IsK1PreRuleValidationOn = IsK1PreRuleValidationOn
            };
            plib.SetInitialParameter();
            //plib.SetInitialParameter( AuthToken);
            plib.StartAgentProcess(StrWSPath, OCRValidation, LogSeverity, JobId,IsK1PreRuleValidationOn, blnSPELJOB);
        }
    }

}
