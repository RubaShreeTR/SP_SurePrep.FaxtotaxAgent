
using System;
using System.Configuration;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace FaxToTaxDataValidations
{
    

    public class MuniBondLogic
    {
        private DataTable dtMuniBondMaster;

        private DataTable dtFundNames;

        private DataTable dtMuniBondAbbr;

        private DataSet dsMaster;

        private string strHomeState;

        private int intTaxYear;

        private int intEngagementID;

        public string HomeState => strHomeState;

        public int TaxYear => intTaxYear;

        public int EngagementID => intEngagementID;

        public DataSet MasterTables
        {
            get
            {
                return dsMaster;
            }
            set
            {
                dsMaster = value;
                dtMuniBondMaster = dsMaster.Tables[0];
                dtFundNames = dsMaster.Tables[1];
                dtMuniBondAbbr = dsMaster.Tables[2];
            }
        }

        public MuniBondLogic()
        {
            strHomeState = "";
        }

        public MuniBondLogic(int intEngagementID)
        {
            strHomeState = "";
            this.intEngagementID = intEngagementID;
        }

        public bool ProcessEngagementForMuniBond()
        {
            int engagementID = EngagementID;
            string empty = string.Empty;
            SPDatabase sPDatabase = new SPDatabase(engagementID);
            bool result;
            try
            {
                if (GetServiceTypeID(engagementID) == 15)
                {
                    result = false;
                }
                else
                {
                    intTaxYear = GetTaxYear(engagementID);
                    if (intTaxYear >= 2009)
                    {
                        int num = GetTaxExemptOptionValue(engagementID);
                        if (IsHomeState())
                        {
                            switch (strHomeState.ToUpper())
                            {
                                case "TX":
                                case "WA":
                                case "FL":
                                case "NV":
                                    if (true)
                                    {
                                        num = 0;
                                    }

                                    break;
                            }
                        }

                        DataTable descriptionFields = GetDescriptionFields(engagementID);
                        switch (num)
                        {
                            case 2:
                            case 3:
                                foreach (DataRow row in descriptionFields.Rows)
                                {
                                    row["SNTPercentage"] = 0;
                                    row["STPercentage"] = 1;
                                }

                                break;
                            case 0:
                            case 1:
                                foreach (DataRow row2 in descriptionFields.Rows)
                                {
                                    row2["SNTPercentage"] = 1;
                                    row2["STPercentage"] = 0;
                                }

                                break;
                            case 4:
                            case 5:
                                if (!IsHomeState())
                                {
                                    foreach (DataRow row3 in descriptionFields.Rows)
                                    {
                                        row3["SNTPercentage"] = 0;
                                        row3["STPercentage"] = 0;
                                    }

                                    break;
                                }

                                if (MasterTables == null)
                                {
                                    GetMuniBondMasterTables();
                                }

                                foreach (DataRow row4 in descriptionFields.Rows)
                                {
                                    string strResponse = string.Empty;
                                    int intMuniApplied = 0;
                                    string muniBondPercentage = GetMuniBondPercentage(Conversions.ToString(Operators.ConcatenateObject(row4["Description"], "".Trim())), HomeState, ref strResponse, ref intMuniApplied);
                                    strResponse = Strings.Mid(strResponse, 1, 499);
                                    if (Operators.CompareString(muniBondPercentage, "", TextCompare: false) != 0)
                                    {
                                        row4["SNTPercentage"] = muniBondPercentage;
                                        row4["STPercentage"] = 1.0 - Conversions.ToDouble(muniBondPercentage);
                                        row4["Response"] = strResponse;
                                        row4["MuniApplied"] = intMuniApplied;
                                        continue;
                                    }

                                    switch (num)
                                    {
                                        case 4:
                                            row4["MuniApplied"] = -1;
                                            row4["SNTPercentage"] = 1;
                                            row4["STPercentage"] = 0;
                                            row4["Response"] = strResponse;
                                            break;
                                        case 5:
                                            row4["MuniApplied"] = -1;
                                            row4["SNTPercentage"] = 0;
                                            row4["STPercentage"] = 1;
                                            row4["Response"] = strResponse;
                                            break;
                                    }
                                }

                                break;
                            default:
                                empty = "Tax Exempt Wizard not applicable";
                                result = false;
                                goto end_IL_0018;
                        }

                        string empty2 = string.Empty;
                        sPDatabase.Openconnection();
                        foreach (DataRow row5 in descriptionFields.Rows)
                        {
                            long num2 = Conversions.ToLong(row5["EngagementOCRFieldID"]);
                            string text = Conversions.ToString(row5["SNTPercentage"]);
                            string text2 = Conversions.ToString(row5["STPercentage"]);
                            int num3 = Conversions.ToInteger(row5["MuniApplied"]);
                            empty2 = Conversions.ToString(row5["Response"]);
                            sPDatabase.InitializedCommand();
                            sPDatabase.SetProcedure("Proc_InsertTaxExemptMuniCodes");
                            sPDatabase.cmd.Parameters.AddWithValue("@EngagementID", (object)engagementID);
                            sPDatabase.cmd.Parameters.AddWithValue("@EngagementOCRFieldID", (object)num2);
                            sPDatabase.cmd.Parameters.AddWithValue("@StateNonTaxablePer", (object)text);
                            sPDatabase.cmd.Parameters.AddWithValue("@StateTaxablePer", (object)text2);
                            sPDatabase.cmd.Parameters.AddWithValue("@IsMuniBondApplied", (object)num3);
                            sPDatabase.cmd.Parameters.AddWithValue("@Comment", (object)empty2);
                            sPDatabase.cmd.ExecuteNonQuery();
                        }

                        goto IL_0628;
                    }

                    empty = "Invalid tax year for new Tax Exempt Wizard.";
                    result = false;
                }

            end_IL_0018:;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                empty = ex2.Message;
                WriteErrorLog(ex2, ex2.Message, engagementID);
                result = false;
                ProjectData.ClearProjectError();
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }

            goto IL_062e;
        IL_0628:
            result = true;
            goto IL_062e;
        IL_062e:
            return result;
        }

        private int GetTaxYear(int intEngagementID)
        {
            SPDatabase sPDatabase = new SPDatabase(0);
            try
            {
                sPDatabase.Openconnection();
                string commandText = "Select TaxYear From SPEngagements with (nolock) where EngagementID=" + Conversions.ToString(intEngagementID) + "";
                sPDatabase.InitializedCommand();
                sPDatabase.cmd.CommandType = CommandType.Text;
                sPDatabase.cmd.CommandText = commandText;
                return Conversions.ToInteger(sPDatabase.cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }
        }

        private int GetServiceTypeID(int intEngagementID)
        {
            SPDatabase sPDatabase = new SPDatabase(0);
            try
            {
                sPDatabase.Openconnection();
                string commandText = "Select Isnull(ServiceTypeID,0) as ServiceTypeID From SPEngagements with (nolock) where EngagementID=" + Conversions.ToString(intEngagementID) + "";
                sPDatabase.InitializedCommand();
                sPDatabase.cmd.CommandType = CommandType.Text;
                sPDatabase.cmd.CommandText = commandText;
                return Conversions.ToInteger(sPDatabase.cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }
        }

        private int GetTaxExemptOptionValue(int intEngagementID)
        {
            SPDatabase sPDatabase = new SPDatabase(0);
            try
            {
                sPDatabase.Openconnection();
                string commandText = "Select Isnull(TaxexemptOption,-1) From SPEngagements with (nolock) where EngagementID=" + Conversions.ToString(intEngagementID) + "";
                sPDatabase.InitializedCommand();
                sPDatabase.cmd.CommandType = CommandType.Text;
                sPDatabase.cmd.CommandText = commandText;
                return Conversions.ToInteger(sPDatabase.cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }
        }

        public bool IsHomeState()
        {
            int engagementID = EngagementID;
            SPDatabase sPDatabase = new SPDatabase(engagementID);
            try
            {
                sPDatabase.Openconnection();
                sPDatabase.InitializedCommand();
                sPDatabase.SetProcedure("Proc_GetResidentState");
                sPDatabase.cmd.Parameters.AddWithValue("@EngagementID", (object)engagementID);
                DataSet dataSet = sPDatabase.DataSet_Procedure("tblHomeState");
                if (dataSet != null && dataSet.Tables[0].Rows.Count > 0)
                {
                    strHomeState = Conversions.ToString(Operators.ConcatenateObject(dataSet.Tables[0].Rows[0][0], ""));
                }

                if (strHomeState == null)
                {
                    strHomeState = "";
                }

                strHomeState = strHomeState.Trim();
                if (strHomeState.Trim().Length == 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }
        }

        private DataTable GetDescriptionFields(int intEngagementID)
        {
            SPDatabase sPDatabase = new SPDatabase(intEngagementID);
            DataSet dataset;
            try
            {
                sPDatabase.Openconnection();
                string strsql = "Select EngagementOCRFieldID,OCRValue as Description,'' as SNTPercentage,'' as STPercentage,'' as Response,0 as MuniApplied, isnull(UnCertainChar,0) as UnCertainCount  From SPEngagementOCRField with(nolock) Where EngagementID=" + Conversions.ToString(intEngagementID) + " and FaxDWPCode in(select FaxDWPcodeDesc from SPTaxExemptMuniCodes with (Nolock))";
                dataset = sPDatabase.GetDataset(strsql, "DescriptionFields");
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }

            return dataset.Tables[0];
        }

        private bool GetMuniBondMasterTables()
        {
            SPDatabase sPDatabase = new SPDatabase(0);
            try
            {
                sPDatabase.Openconnection();
                string strsql = "select * from SPTaxExemptMuniData with (nolock) Where TaxYear=" + Conversions.ToString(intTaxYear) + ";select distinct FundName from SPTaxExemptMuniData with (nolock) where MatchPercentage=80 and TaxYear=" + Conversions.ToString(intTaxYear) + ";select * from SPTaxExemptAbbrData with (nolock) Where TaxYear=" + Conversions.ToString(intTaxYear) + "";
                DataSet dataset = sPDatabase.GetDataset(strsql, "MuniBondMaster");
                MasterTables = dataset;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }

            return true;
        }

        public string GetMuniBondPercentage(string strFundName, string strHomeState, ref string strResponse, ref int intMuniApplied)
        {
            string PerCentage = string.Empty;
            string strNewFundName = string.Empty;
            bool flag = false;
            string text = string.Empty;
            IsMatch_100(dtMuniBondMaster, strFundName, strHomeState, ref PerCentage, ref strResponse);
            if (Operators.CompareString(PerCentage, string.Empty, TextCompare: false) == 0)
            {
                IsMatch_80(dtMuniBondMaster, dtFundNames, strFundName, strHomeState, ref PerCentage, ref strResponse);
            }
            else
            {
                flag = true;
            }

            if (Operators.CompareString(PerCentage, string.Empty, TextCompare: false) == 0)
            {
                IsMatch_100(dtMuniBondMaster, strFundName, strHomeState, ref PerCentage, ref strResponse, blnUseHomeState: false);
                if (Operators.CompareString(PerCentage, string.Empty, TextCompare: false) == 0)
                {
                    IsMatch_80(dtMuniBondMaster, dtFundNames, strFundName, strHomeState, ref PerCentage, ref strResponse, blnUseHomeState: false);
                }
                else
                {
                    flag = true;
                }
            }
            else
            {
                flag = true;
            }

            if (Operators.CompareString(PerCentage, string.Empty, TextCompare: false) == 0)
            {
                if (ApplyAbbrevations_General(strFundName, ref strNewFundName) == 1)
                {
                    IsMatch_100(dtMuniBondMaster, strNewFundName, strHomeState, ref PerCentage, ref strResponse);
                    if (Operators.CompareString(PerCentage, string.Empty, TextCompare: false) == 0)
                    {
                        IsMatch_80(dtMuniBondMaster, dtFundNames, strNewFundName, strHomeState, ref PerCentage, ref strResponse);
                    }
                    else
                    {
                        flag = true;
                    }

                    if (Operators.CompareString(PerCentage, string.Empty, TextCompare: false) == 0)
                    {
                        IsMatch_100(dtMuniBondMaster, strNewFundName, strHomeState, ref PerCentage, ref strResponse, blnUseHomeState: false);
                        if (Operators.CompareString(PerCentage, string.Empty, TextCompare: false) == 0)
                        {
                            IsMatch_80(dtMuniBondMaster, dtFundNames, strNewFundName, strHomeState, ref PerCentage, ref strResponse, blnUseHomeState: false);
                        }
                        else
                        {
                            flag = true;
                        }
                    }
                    else
                    {
                        flag = true;
                    }
                }
            }
            else
            {
                flag = true;
            }

            if (Operators.CompareString(PerCentage, string.Empty, TextCompare: false) == 0)
            {
                ApplyAbbrevations_State(strFundName, strHomeState, ref PerCentage, ref strResponse);
            }
            else
            {
                flag = true;
            }

            if (flag)
            {
                PerCentage = Conversions.ToString(Conversions.ToDouble(PerCentage) / 100.0);
                text = "Exempt % determined based on Description";
                intMuniApplied = 1;
            }
            else if (Operators.CompareString(PerCentage, string.Empty, TextCompare: false) != 0)
            {
                if (Operators.CompareString(PerCentage, "1", TextCompare: false) == 0)
                {
                    text = "Deemed Non-Taxable as Resident State is found in Description";
                    intMuniApplied = 2;
                }
                else if (Operators.CompareString(PerCentage, "0", TextCompare: false) == 0)
                {
                    text = "Deemed Taxable as Non-Resident State is found in Description";
                    intMuniApplied = 3;
                }
            }

            if (Operators.CompareString(text, string.Empty, TextCompare: false) != 0)
            {
                strResponse = strResponse + "(" + text + ")";
            }

            return PerCentage;
        }

        private int IsMatch_100(DataTable dtMuniBondMaster, string strFundName, string strState, ref string PerCentage, ref string strResponse, bool blnUseHomeState = true)
        {
            try
            {
                DataView defaultView = dtMuniBondMaster.DefaultView;
                if (!blnUseHomeState)
                {
                    defaultView.RowFilter = "Fundname = '" + strFundName.Replace("'", "''") + "' and State not in('GU', 'PR', 'VI', 'USP')";
                }
                else
                {
                    defaultView.RowFilter = "Fundname = '" + strFundName.Replace("'", "''") + "' and State='" + strState + "' and MatchPercentage=100 and State not in('GU', 'PR', 'VI', 'USP')";
                }

                if (defaultView.Count > 0)
                {
                    if (!blnUseHomeState)
                    {
                        PerCentage = (0.0 + NonResidentStatesPerCentage(strFundName)).ToString();
                    }
                    else
                    {
                        PerCentage = Operators.AddObject(defaultView[0]["Percentage"], NonResidentStatesPerCentage(strFundName)).ToString();
                    }

                    strResponse = "Match found in 100% master. FundName: " + strFundName;
                    return 1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
                DataView defaultView = null;
            }
        }

        private double NonResidentStatesPerCentage(string strFundName)
        {
            DataRow[] array = dtMuniBondMaster.Select("Fundname = '" + strFundName.Replace("'", "''") + "' and State in('GU', 'PR', 'VI', 'USP')");
            checked
            {
                double num5 = default(double);
                if (array.Length > 0)
                {
                    int num = array.Length - 1;
                    int num2 = 0;
                    while (true)
                    {
                        int num3 = num2;
                        int num4 = num;
                        if (num3 <= num4)
                        {
                            num5 = Conversions.ToDouble(Operators.AddObject(num5, array[num2]["Percentage"]));
                            num2++;
                            continue;
                        }

                        break;
                    }
                }
                else
                {
                    num5 = 0.0;
                }

                return num5;
            }
        }

        private int IsMatch_80(DataTable dtMuniBondMaster, DataTable dtFundName, string strFundName, string strHomeState, ref string PerCentage, ref string strResponse, bool blnUseHomeState = true)
        {
            DataView defaultView = default(DataView);
            try
            {
                defaultView = dtMuniBondMaster.DefaultView;
                defaultView.RowFilter = "Fundname = '" + strFundName.Replace("'", "''") + "' and MatchPercentage=80 and State not in('GU', 'PR', 'VI', 'USP')";
                if (defaultView.Count > 0)
                {
                    if (!blnUseHomeState)
                    {
                        defaultView.RowFilter = "Fundname = '" + strFundName.Replace("'", "''") + "' and MatchPercentage=80 and State not in('GU', 'PR', 'VI', 'USP')";
                    }
                    else
                    {
                        defaultView.RowFilter = "Fundname = '" + strFundName.Replace("'", "''") + "' and State='" + strHomeState + "' and MatchPercentage=80 and State not in('GU', 'PR', 'VI', 'USP')";
                    }

                    if (defaultView.Count > 0)
                    {
                        if (!blnUseHomeState)
                        {
                            PerCentage = (0.0 + NonResidentStatesPerCentage(strFundName)).ToString();
                        }
                        else
                        {
                            PerCentage = Operators.AddObject(defaultView[0]["Percentage"], NonResidentStatesPerCentage(strFundName)).ToString();
                        }

                        strResponse = "Match found in 80% master.\r\nFundname : " + strFundName;
                        return 1;
                    }
                }
                else
                {
                    if (dtFundName.Columns.Count == 1)
                    {
                        DataColumn dataColumn = new DataColumn("MatchPer");
                        dataColumn.DataType = Type.GetType("System.Int32");
                        dtFundName.Columns.Add(dataColumn);
                    }

                    foreach (DataRow row in dtFundName.Rows)
                    {
                        row[1] = StringComparePercentage(row[0].ToString().Replace(" ", ""), strFundName.Replace(" ", ""));
                    }

                    DataView defaultView2 = dtFundName.DefaultView;
                    defaultView2.Sort = "MatchPer desc";
                    string text = defaultView2[0]["Fundname"].ToString();
                    if (Operators.ConditionalCompareObjectLess(defaultView2[0]["MatchPer"], 80, TextCompare: false))
                    {
                        strResponse = "Match not found. Text matching percentage less than 80%. (Text comparion % " + defaultView2[0]["MatchPer"].ToString() + ")";
                        return 0;
                    }

                    if (!blnUseHomeState)
                    {
                        defaultView.RowFilter = "Fundname = '" + text.Replace("'", "''") + "' and MatchPercentage=80 and State not in('GU', 'PR', 'VI', 'USP')";
                    }
                    else
                    {
                        defaultView.RowFilter = "Fundname = '" + text.Replace("'", "''") + "' and State='" + strHomeState + "' and MatchPercentage=80 and State not in('GU', 'PR', 'VI', 'USP')";
                    }

                    if (defaultView.Count > 0)
                    {
                        if (!blnUseHomeState)
                        {
                            PerCentage = (0.0 + NonResidentStatesPerCentage(text)).ToString();
                        }
                        else
                        {
                            PerCentage = Operators.AddObject(defaultView[0]["Percentage"], NonResidentStatesPerCentage(text)).ToString();
                        }

                        strResponse = "Match found in 80% master.\r\nFundname : " + text;
                        return 1;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
                defaultView.RowFilter = "";
                defaultView = null;
            }
        }

        private int ApplyAbbrevations_General(string strFundName, ref string strNewFundName)
        {
            int result = default(int);
            try
            {
                DataView defaultView = dtMuniBondAbbr.DefaultView;
                string[] array = strFundName.Split(new char[1] { ' ' });
                string[] array2 = array;
                for (int i = 0; i < array2.Length; i = checked(i + 1))
                {
                    string text = array2[i];
                    text = text.Trim().Replace("'", "''");
                    defaultView.RowFilter = "AbbreviationType='G' and StateAbbreviation='" + text + "'";
                    if (defaultView.Count > 0)
                    {
                        strNewFundName = strNewFundName + " " + defaultView[0]["StateFullName"].ToString().Trim();
                        result = 1;
                    }
                    else
                    {
                        strNewFundName = strNewFundName + " " + text.Trim();
                    }
                }

                defaultView.RowFilter = "";
                strNewFundName = strNewFundName.Trim();
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
                DataView defaultView = null;
            }

            return result;
        }

        private int ApplyAbbrevations_State(string strFundName, string strHomeState, ref string PerCentage, ref string strResponse)
        {
            checked
            {
                try
                {
                    DataView defaultView = dtMuniBondAbbr.DefaultView;
                    string[] array = strFundName.Split(new char[1] { ' ' });
                    string[] array2 = array;
                    for (int i = 0; i < array2.Length; i++)
                    {
                        string text = array2[i];
                        text = text.Trim().Replace("'", "''");
                        if (Operators.CompareString(text.ToLower(), strHomeState.ToLower(), TextCompare: false) == 0)
                        {
                            PerCentage = "1";
                            return 1;
                        }

                        defaultView.RowFilter = "AbbreviationType='S' and State='" + strHomeState + "' and StateAbbreviation='" + text + "'";
                        if (defaultView.Count > 0)
                        {
                            PerCentage = "1";
                            strResponse = "Match found using state abbreviations.";
                            return 1;
                        }

                        if (text.Length < 4)
                        {
                            continue;
                        }

                        defaultView.RowFilter = "AbbreviationType='S' and State='" + strHomeState + "' and MatchPercentage=80";
                        if (defaultView.Count <= 0)
                        {
                            continue;
                        }

                        int num = defaultView.Count - 1;
                        int num2 = 0;
                        while (true)
                        {
                            int num3 = num2;
                            int num4 = num;
                            if (num3 > num4)
                            {
                                break;
                            }

                            if (StringComparePercentage(text, Conversions.ToString(defaultView[num2]["StateAbbreviation"])) >= 80)
                            {
                                strResponse = "Match found using state abbreviations.";
                                PerCentage = "1";
                                return 1;
                            }

                            num2++;
                        }
                    }

                    string[] array3 = array;
                    for (int j = 0; j < array3.Length; j++)
                    {
                        string text2 = array3[j];
                        text2 = text2.Trim().Replace("'", "''");
                        defaultView.RowFilter = "AbbreviationType='S' and StateAbbreviation='" + text2 + "'";
                        if (defaultView.Count > 0)
                        {
                            strResponse = "Match found using state abbreviations.";
                            PerCentage = "0";
                            return 1;
                        }

                        if (text2.Length < 4)
                        {
                            continue;
                        }

                        defaultView.RowFilter = "AbbreviationType='S' and State <> '" + strHomeState + "' and MatchPercentage=80";
                        if (defaultView.Count <= 0)
                        {
                            continue;
                        }

                        int num5 = defaultView.Count - 1;
                        int num6 = 0;
                        while (true)
                        {
                            int num7 = num6;
                            int num4 = num5;
                            if (num7 > num4)
                            {
                                break;
                            }

                            if (StringComparePercentage(text2, Conversions.ToString(defaultView[num6]["StateAbbreviation"])) >= 80)
                            {
                                strResponse = "Match found using state abbreviations.";
                                PerCentage = "0";
                                return 1;
                            }

                            num6++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ProjectData.SetProjectError(ex);
                    Exception ex2 = ex;
                    throw ex2;
                }

                return 0;
            }
        }

        private int StringComparePercentage(string String1, string String2)
        {
            int num = ((String1.Length <= String2.Length) ? String2.Length : String1.Length);
            int num2 = Levenshtein_distance(String1, String2);
            return checked((int)Math.Round(Convert.ToDouble((double)(num - num2) / (double)num) * 100.0));
        }

        private int Levenshtein_distance(string s, string t)
        {
            checked
            {
                int result = default(int);
                try
                {
                    int length = s.Length;
                    int length2 = t.Length;
                    if (length == 0)
                    {
                        result = length2;
                        return result;
                    }

                    if (length2 == 0)
                    {
                        result = length;
                        return result;
                    }

                    int[,] array = new int[length + 1, length2 + 1];
                    int num = length;
                    int num2 = 0;
                    while (true)
                    {
                        int num3 = num2;
                        int num4 = num;
                        if (num3 > num4)
                        {
                            break;
                        }

                        array[num2, 0] = num2;
                        num2++;
                    }

                    int num5 = length2;
                    int num6 = 0;
                    while (true)
                    {
                        int num7 = num6;
                        int num4 = num5;
                        if (num7 > num4)
                        {
                            break;
                        }

                        array[0, num6] = num6;
                        num6++;
                    }

                    int num8 = length;
                    num2 = 1;
                    while (true)
                    {
                        int num9 = num2;
                        int num4 = num8;
                        if (num9 > num4)
                        {
                            break;
                        }

                        string left = s.Substring(num2 - 1, 1);
                        int num10 = length2;
                        num6 = 1;
                        while (true)
                        {
                            int num11 = num6;
                            num4 = num10;
                            if (num11 > num4)
                            {
                                break;
                            }

                            string right = t.Substring(num6 - 1, 1);
                            int num12 = ((Operators.CompareString(left, right, TextCompare: false) != 0) ? 1 : 0);
                            array[num2, num6] = Math.Min(Math.Min(array[num2 - 1, num6] + 1, array[num2, num6 - 1] + 1), array[num2 - 1, num6 - 1] + num12);
                            num6++;
                        }

                        num2++;
                    }

                    result = array[length, length2];
                    return result;
                }
                catch (Exception ex)
                {
                    ProjectData.SetProjectError(ex);
                    Exception ex2 = ex;
                    ProjectData.ClearProjectError();
                }

                return result;
            }
        }

        public bool WriteErrorLog(Exception ex, string strErrorMsg, int intEngagementID)
        {
            string text = "";
            GenModule.configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            string MuniBondLogPath = GenModule.configuration.GetSection("MuniBondLogPath").Value;
            string text2 = MuniBondLogPath.ToString();
            try
            {
                string text3 = text2 + "\\" + Strings.Format(DateAndTime.Now, "ddMMyyyy") + "\\" + Conversions.ToString(intEngagementID);
                text = Conversions.ToString(intEngagementID) + "_Error.txt";
                if (!Directory.Exists(text3))
                {
                    Directory.CreateDirectory(text3);
                }

                StreamWriter streamWriter = new StreamWriter(text3 + "\\" + text, append: true);
                streamWriter.WriteLine("---------------------------------------------------------------");
                streamWriter.WriteLine("Engagementid - " + Conversions.ToString(intEngagementID));
                streamWriter.WriteLine("Error Comments - " + strErrorMsg);
                streamWriter.WriteLine("Message  - " + ex.Message);
                streamWriter.WriteLine("StackTrace  - " + ex.StackTrace);
                streamWriter.WriteLine("Source  - " + ex.Source.ToString());
                streamWriter.WriteLine("TargetSite  - " + ex.TargetSite.ToString());
                streamWriter.WriteLine("--------------------------------------  ------------------------");
                streamWriter.Close();
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                Exception ex3 = ex2;
                ProjectData.ClearProjectError();
            }
            finally
            {
                StreamWriter streamWriter = null;
                string text3 = null;
                text = null;
            }

            return true;
        }
    }
}
