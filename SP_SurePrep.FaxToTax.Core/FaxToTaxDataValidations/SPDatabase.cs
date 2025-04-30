using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FaxToTaxDataValidations
{
   

    internal class SPDatabase
    {
        public SqlConnection conn;

        public SqlCommand cmd;

        private Exception SPError;

        public string CSIR;

        public SPDatabase(int intEngagementid)
        {
            //IL_002d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0037: Expected O, but got Unknown
            CSIR = "driver={sql server};" + GenModule.configuration.GetConnectionString("DefaultConnection");
            conn = new SqlConnection();
            InitializedCommand();
            if (intEngagementid == 0)
            {
                conn.ConnectionString = GenModule.configuration.GetConnectionString("DefaultConnection");
            }
            else
            {
                conn.ConnectionString = GetPrimaryDataBaseConnectionString(intEngagementid);
            }
        }

        public void InitializedCommand()
        {
            //IL_0009: Unknown result type (might be due to invalid IL or missing references)
            //IL_0013: Expected O, but got Unknown
            cmd = null;
            cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.CommandTimeout = 1000;
        }

        public void SetProcedure(string strProcName)
        {
            cmd.Parameters.Clear();
            cmd.CommandText = "dbo." + strProcName;
            cmd.CommandType = CommandType.StoredProcedure;
        }

        public void Openconnection()
        {
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    cmd.Connection = conn;
                    cmd.CommandTimeout = 1000;
                }
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception sPError = ex;
                SPError = sPError;
                ProjectData.ClearProjectError();
            }
        }

        public void Closeconnection()
        {
            try
            {
                if (conn.State != 0)
                {
                    conn.Close();
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
        }

        public DataSet GetDataset(string strsql, string strName_DataTable)
        {
            //IL_0008: Unknown result type (might be due to invalid IL or missing references)
            //IL_000e: Expected O, but got Unknown
            SqlDataAdapter val = new SqlDataAdapter(strsql, conn);
            try
            {
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
            finally
            {
                ((Component)(object)val).Dispose();
                val = null;
            }
        }

        internal int intExecuteNonQuery(string strsql)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got Unknown
            try
            {
                SqlCommand val = new SqlCommand();
                val.Connection = conn;
                val.CommandType = CommandType.Text;
                val.CommandText = strsql;
                int result = val.ExecuteNonQuery();
                ((Component)(object)val).Dispose();
                val = null;
                return result;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
            }
        }

        internal int intExecuteNonQuery(string strsql, int intEngId)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got Unknown
            try
            {
                SqlCommand val = new SqlCommand();
                val.Connection = conn;
                val.CommandType = CommandType.Text;
                val.CommandText = strsql;
                int result = val.ExecuteNonQuery();
                ((Component)(object)val).Dispose();
                val = null;
                return result;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
            }
        }

        internal int intExecuteScalar(string strsql)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got Unknown
            try
            {
                SqlCommand val = new SqlCommand();
                val.Connection = conn;
                val.CommandType = CommandType.Text;
                val.CommandText = strsql;
                int result = Conversions.ToInteger(val.ExecuteScalar());
                ((Component)(object)val).Dispose();
                val = null;
                return result;
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                throw ex2;
            }
            finally
            {
            }
        }

        public DataSet ConvertDataReaderToDataSet(SqlDataReader reader)
        {
            DataSet dataSet = new DataSet();
            DataTable schemaTable = reader.GetSchemaTable();
            DataTable dataTable = new DataTable();
            checked
            {
                int num = schemaTable.Rows.Count - 1;
                int num2 = 0;
                while (true)
                {
                    int num3 = num2;
                    int num4 = num;
                    if (num3 > num4)
                    {
                        break;
                    }

                    DataRow dataRow = schemaTable.Rows[num2];
                    string columnName = Conversions.ToString(dataRow["ColumnName"]);
                    DataColumn column = new DataColumn(columnName, (Type)dataRow["DataType"]);
                    dataTable.Columns.Add(column);
                    num2++;
                }

                dataSet.Tables.Add(dataTable);
                while (reader.Read())
                {
                    DataRow dataRow2 = dataTable.NewRow();
                    int num5 = reader.FieldCount - 1;
                    num2 = 0;
                    while (true)
                    {
                        int num6 = num2;
                        int num4 = num5;
                        if (num6 > num4)
                        {
                            break;
                        }

                        dataRow2[num2] = RuntimeHelpers.GetObjectValue(reader.GetValue(num2));
                        num2++;
                    }

                    dataTable.Rows.Add(dataRow2);
                }

                return dataSet;
            }
        }

        public DataSet DataSet_Procedure(string strTableName)
        {
            //IL_0001: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Expected O, but got Unknown
            SqlDataAdapter val = new SqlDataAdapter();
            try
            {
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
            finally
            {
                ((Component)(object)val).Dispose();
                val = null;
            }
        }

        //protected virtual void Finalize()
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

        public string GetPrimaryDataBaseConnectionString(int intEngagementID)
        {
            SPDatabase sPDatabase = new SPDatabase(0);
            string text = string.Empty;
            try
            {
                sPDatabase.Openconnection();
                DataSet dataset = sPDatabase.GetDataset("select 1 as a,* from dbo.Func_GetDBConnection(" + Conversions.ToString(intEngagementID) + ")", "DBConn");
                text = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("server=", dataset.Tables[0].Rows[0]["DBServer"]), ";"));
                text = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(text + "database=", dataset.Tables[0].Rows[0]["DBName"]), ";"));
                text = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(text + "uid=", dataset.Tables[0].Rows[0]["DBLogin"]), ";"));
                text = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(text + "pwd=", dataset.Tables[0].Rows[0]["DBPassword"]), ";"));
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception ex2 = ex;
                ProjectData.ClearProjectError();
            }
            finally
            {
                sPDatabase.Closeconnection();
                sPDatabase = null;
            }

            return text;
        }
    }
}
