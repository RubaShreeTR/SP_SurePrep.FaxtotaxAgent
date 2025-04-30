using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Data.SqlClient;
using FaxToTaxDataValidations;

namespace FaxToTaxTemplateLib.QueueManagerClasses
{
    #region Assembly Queue_Manager, Version=3.0.0.5, Culture=neutral, PublicKeyToken=6f6c53f053c1c70c
    // C:\Git_SureprepProj\ReviewWizard\FaxToTaxAgentCore\AgentsSurePrepDLLs\Queue_Manager.dll
    // Decompiled with ICSharpCode.Decompiler 8.1.1.7464
    #endregion


    public class SPDatabase
    {
        public enum EnumConnection
        {
            Product10401041,
            ProductLoanBeam
        }

        private SqlConnection SqlConn;

        private SqlCommand cmd;

        private Exception SPError;

        public Exception SavingError
        {
            get
            {
                return SPError;
            }
            set
            {
                SPError = value;
            }
        }

        public SPDatabase()
        {
            //IL_0009: Unknown result type (might be due to invalid IL or missing references)
            //IL_0013: Expected O, but got Unknown
            SqlConn = new SqlConnection();
            InitializedCommand();
            SqlConn.ConnectionString = GenModule.configuration.GetSection("ConnectionString").Value;
        }

        public SPDatabase(EnumConnection ConnectionType)
        {
            //IL_0009: Unknown result type (might be due to invalid IL or missing references)
            //IL_0013: Expected O, but got Unknown
            SqlConn = new SqlConnection();
            InitializedCommand();
            switch (ConnectionType)
            {
                case EnumConnection.Product10401041:
                    SqlConn.ConnectionString = GenModule.configuration.GetSection("ConnectionString").Value;
                    break;
                case EnumConnection.ProductLoanBeam:
                    SqlConn.ConnectionString = GenModule.configuration.GetSection("ConnectionString").Value;
                    break;
            }
        }

        public void InitializedCommand()
        {
            //IL_004d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0057: Expected O, but got Unknown
            int num;
            try
            {
                num = Conversions.ToInteger(GenModule.configuration.GetSection("WaitTime").Value);
                if (!Versioned.IsNumeric(num))
                {
                    num = 0;
                }
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                num = 0;
                ProjectData.ClearProjectError();
            }

            cmd = null;
            cmd = new SqlCommand();
            cmd.Connection = SqlConn;
            cmd.CommandTimeout = num;
        }

        public void SetProcedure(string strProcName)
        {
            cmd.Parameters.Clear();
            cmd.CommandText = "dbo." + strProcName;
            cmd.CommandType = CommandType.StoredProcedure;
        }

        public object Openconnection()
        {
            try
            {
                if (SqlConn.State != ConnectionState.Open)
                {
                    SqlConn.Open();
                    cmd.Connection = SqlConn;
                    cmd.CommandTimeout = 600;
                }
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }

            object result = default;
            return result;
        }

        public object Closeconnection()
        {
            try
            {
                if (SqlConn.State != 0)
                {
                    SqlConn.Close();
                    cmd = null;
                }
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception sPError = ex;
                SPError = sPError;
                ProjectData.ClearProjectError();
            }

            object result = default;
            return result;
        }

        public DataSet GetDataset(string strsql, string strName_DataTable)
        {
            //IL_0009: Unknown result type (might be due to invalid IL or missing references)
            //IL_000f: Expected O, but got Unknown
            try
            {
                SqlDataAdapter val = new SqlDataAdapter(strsql, SqlConn);
                DataSet dataSet = new DataSet();
                ((DbDataAdapter)(object)val).Fill(dataSet, strName_DataTable);
                return dataSet;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
        }

        internal int intExecuteNonQuery(string strsql)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got Unknown
            try
            {
                SqlCommand val = new SqlCommand();
                val.Connection = SqlConn;
                val.CommandType = CommandType.Text;
                val.CommandText = strsql;
                return val.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
        }

        internal int intExecuteScalar(string strsql)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got Unknown
            try
            {
                SqlCommand val = new SqlCommand();
                val.Connection = SqlConn;
                val.CommandType = CommandType.Text;
                val.CommandText = strsql;
                return Conversions.ToInteger(val.ExecuteScalar());
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
        }

        internal string strExecuteScalar(string strsql)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got Unknown
            try
            {
                SqlCommand val = new SqlCommand();
                val.Connection = SqlConn;
                val.CommandType = CommandType.Text;
                val.CommandText = strsql;
                return Conversions.ToString(val.ExecuteScalar());
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
        }

        public DataSet DataSet_Procedure(string strTableName)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got Unknown
            try
            {
                SqlDataAdapter val = new SqlDataAdapter();
                DataSet dataSet = new DataSet();
                val.SelectCommand = cmd;
                ((DbDataAdapter)(object)val).Fill(dataSet, strTableName);
                return dataSet;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
        }

        public void AddSqlParameter(SqlParameter objsqlParam)
        {
            cmd.Parameters.Add(objsqlParam);
        }

        public void ExecuteProcedure()
        {
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
        }

        public string GetParameterValue(string strParamName)
        {
            return Conversions.ToString(cmd.Parameters[strParamName].Value);
        }
    }
}
