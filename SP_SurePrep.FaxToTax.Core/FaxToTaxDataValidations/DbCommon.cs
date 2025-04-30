using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxDataValidations
{
    public class DbCommon
    {
        public string _connectionString;
        public SqlConnection _sqlConnection;
        public SqlCommand _sqlCommand;

        public DbCommon(string ConfigConnectionKey)
        {
            GenModule.ConfigConnectionKey = ConfigConnectionKey;
            GenModule.configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            _connectionString = GetConfigValue(ConfigConnectionKey);
        }

        public DbCommon(int intEngagementID, string ConfigConnectionKey)
        {
            GenModule.ConfigConnectionKey = ConfigConnectionKey;
            GenModule.configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            _connectionString = GetConfigValue(ConfigConnectionKey);
            _connectionString = GetEngagementDbConnection(intEngagementID);
        }

        public DbCommon()
        {
        }

        private string GetConfigValue(string ConfigKey)
        {
           return GenModule.configuration.GetConnectionString("DefaultConnection").ToString();
        }

        private string GetEngagementDbConnection(int intEngagementID)
        {
            DataTable dt;
            try
            {
                string strSQL;
                int intEngConID;
                SqlConnectionStringBuilder conStringBuilder = new SqlConnectionStringBuilder();

                strSQL = "Select * From dbo.Func_GetDBConnection(@EngagementID)";
                SpParameter spParam = new SpParameter("EngagementID", intEngagementID);
                SpParameter[] spParams = new SpParameter[1];
                spParams[0] = spParam;
                dt = GetDataTable(strSQL, spParams, "SecConnection");

                if (dt.Rows.Count > 0)
                {
                    intEngConID = Convert.ToInt32(dt.Rows[0]["DBConnectionID"]);
                    if (intEngConID > 0)
                    {
                        conStringBuilder.Add("server", dt.Rows[0]["DBServer"].ToString());
                        conStringBuilder.Add("DataBase", dt.Rows[0]["DBName"].ToString());
                        conStringBuilder.Add("UID", dt.Rows[0]["DBLogin"].ToString());
                        conStringBuilder.Add("PWD", dt.Rows[0]["DBPassword"].ToString());
                        return conStringBuilder.ConnectionString;
                    }
                    return _connectionString;
                }
                else
                {
                    throw new Exception("No db connection found for the engagement Id");
                }
            }
            finally
            {
                dt = null;
            }
        }

        /// <summary>
        /// Returns SqlConnection object
        /// </summary>
        /// <returns></returns>
        public SqlConnection GetConnection()
        {
            _sqlConnection = new SqlConnection(_connectionString);
            if (_sqlConnection.State != ConnectionState.Open) _sqlConnection.Open();
            return _sqlConnection;
        }

        /// <summary>
        /// Returns SqlCommand object
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="spParameters"></param>
        /// <param name="isProcedure"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        /// 
        private SqlCommand GetCommand(string commandText, SpParameter[] spParameters, bool isProcedure, SqlConnection conn)
        {
            _sqlCommand = new SqlCommand();
            _sqlCommand.Connection = conn;
            _sqlCommand.CommandType = isProcedure ? CommandType.StoredProcedure : CommandType.Text;
            _sqlCommand.CommandText = commandText;
            _sqlCommand.CommandTimeout = 0;
            if (spParameters != null)
            {
                foreach (var spParameter in spParameters)
                {
                    SqlParameter sqlParam = new SqlParameter(spParameter.Arg, spParameter.ArgValue);
                    if (spParameter.ArgDirection == ParameterDirection.Output) sqlParam.Direction = ParameterDirection.Output;
                    _sqlCommand.Parameters.Add(sqlParam);
                }
            }
            return _sqlCommand;
        }

        /// <summary>
        /// Returns DataTable based on given arguments
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="spParameters"></param>
        /// <param name="tableName"></param>
        /// <param name="isProcedure"></param>
        /// <returns></returns>
        public DataTable GetDataTable(string commandText, SpParameter[] spParameters, string tableName = "table1", bool isProcedure = false)
        {
            SqlConnection conn = GetConnection();
            try
            {
                SqlCommand comm = GetCommand(commandText, spParameters, isProcedure, conn);
                SqlDataAdapter dataAdaptor = new SqlDataAdapter(comm);
                DataSet ds = new DataSet();
                dataAdaptor.Fill(ds, string.IsNullOrWhiteSpace(tableName) ? "table1" : tableName);
                return ds.Tables[0];
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

      

        /// <summary>
        /// Returns first column of the first row in the result set returned by the query
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="spParameters"></param>
        /// <param name="isProcedure"></param>
        /// <returns></returns>
        public object GetData(string commandText, SpParameter[] spParameters, bool isProcedure = false)
        {
            SqlConnection conn = GetConnection();
            SqlCommand comm = null;
            try
            {
                comm = GetCommand(commandText, spParameters, isProcedure, conn);
                return comm.ExecuteScalar;
            }
            finally
            {
                comm.Dispose();
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        /// <summary>
        /// Returns DataSet based on given arguments
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="spParameters"></param>
        /// <param name="isProcedure"></param>
        /// <returns></returns>
        public DataSet GetDataSet(string commandText, SpParameter[] spParameters, bool isProcedure = false)
        {
            DataSet ds = new DataSet();
            SqlConnection conn = GetConnection();
            try
            {
                SqlCommand comm = GetCommand(commandText, spParameters, isProcedure, conn);
                SqlDataAdapter dataAdaptor = new SqlDataAdapter(comm);
                dataAdaptor.Fill(ds);
                return ds;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        /// <summary>
        /// Facilitates to fetch, Add, Update and Delete data in the database
        /// It also helps you to get the first column value of the first row for the returned result set
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="spParameters"></param>
        /// <param name="isProcedure"></param>
        /// <param name="isExecuteScalar"></param>
        /// <returns></returns>
        public int AddUpdateOrDelete(string commandText, SpParameter[] spParameters, bool isProcedure = false, bool isExecuteScalar = false, bool isOutParameter = false)
        {
            SqlConnection conn = GetConnection();
            SqlCommand comm = null;
            try
            {
                comm = GetCommand(commandText, spParameters, isProcedure, conn);
                if (isExecuteScalar)
                {
                    return Convert.ToInt32(comm.ExecuteScalar());
                }
                int result = comm.ExecuteNonQuery();
                if (!isOutParameter)
                {
                    return result;
                }
                else
                {
                    for (int intCount = 0; intCount < comm.Parameters.Count; intCount++)
                    {
                        spParameters[intCount].ArgValue = comm.Parameters[intCount].Value;
                    }
                }
            }
            finally
            {
                comm.Dispose();
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
            return 0;
        }

        /// <summary>
        /// Validate the connection string.
        /// </summary>
        /// <param name="connectionString">connectionString</param>
        /// <returns>Empty string if validation fails, else parsed connection string.</returns>
        //public string GetValidatedConnectionString(string connectionString)
        //{
        //    SqlConnectionStringBuilder conStringBuilder = new SqlConnectionStringBuilder(connectionString);
        //    if (string.IsNullOrWhiteSpace(conStringBuilder.DataSource) | string.IsNullOrWhiteSpace(conStringBuilder.InitialCatalog) | string.IsNullOrWhiteSpace(conStringBuilder.UserID) | string.IsNullOrWhiteSpace(conStringBuilder.Password))
        //        return string.Empty;
        //    conStringBuilder.DataSource = conStringBuilder.DataSource.Trim();
        //    conStringBuilder.InitialCatalog = conStringBuilder.InitialCatalog.Trim();
        //    conStringBuilder.UserID = conStringBuilder.UserID.Trim();
        //    conStringBuilder.Password = conStringBuilder.Password.Trim();

        //    return conStringBuilder.ConnectionString;
        //}
    }
}
