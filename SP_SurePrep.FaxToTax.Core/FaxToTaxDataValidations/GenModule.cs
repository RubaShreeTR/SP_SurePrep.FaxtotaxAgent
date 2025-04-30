using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace FaxToTaxDataValidations
{
    public static class GenModule
    {
        #region Variables
       public static string ConnectionString;
        public static string ConfigConnectionKey = "ConnectionStrings";
        public static string IniPath;
        public static int EngagementCount;

        public static string strEngagement;
        public static string sourcePath;
        public static string targetFilePath;

        public static int EngagementTypeID;
        public static int TaxYear;
        public static int TaxSoftwareID;

        public static string lngclientNumber;
        public static string domainname, TaxSoftwareName;
        public static long engagementID;
        public static int JobId;
        public static bool IsMergeBinder;
        public static IConfiguration configuration;
        public static bool blnRepeatValidation = false;
        public static int GetJunkDataAttempts = 1;
        public static string CleanAtEnd = "N";
        public static int WaitTime = 3000;
        public static int _jobRetryValue = 5;


        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileStringA", CharSet = CharSet.Ansi)]
        public static extern int GetPrivateProfileString(
         string lpApplicationName,
         string lpKeyName,
         string lpDefault,
         StringBuilder lpReturnedString,
         int nSize,
         string lpFileName);

        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileStringA", CharSet = CharSet.Ansi)]
        public static extern int WritePrivateProfileString(
        string lpApplicationName,
        string lpKeyName,
        string lpString,
        string lpFileName);



        public enum Status
        {
            READY_FOR_EXPORTING = 47,
            IN_EXPORTING = 48,
            COMPLETE = 6,
            ERROR_IN_PROCESS = 6
        }

        #endregion

        public static void WriteINI(string section, string key, string value, string fullPath)
        {
            WritePrivateProfileString(section, key.ToUpper(), value, fullPath);
        }

        public static string ReadValueFromINI(string profile, string subProfile, string path)
        {
            var temp = new StringBuilder(256);
            int count = GetPrivateProfileString(profile, subProfile, "", temp, temp.Capacity, path);
            return count > 0 ? temp.ToString(0, count) : string.Empty;
        }

        public static void ErrorLogFile(string message, int agentId = -1, long engId = 0, long engCnt = 0)
        {
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string logDir = Path.Combine(path, "Log");
            string dateDir = Path.Combine(logDir, DateTime.Now.ToString("ddMMyyyy"));

            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            if (!Directory.Exists(dateDir)) Directory.CreateDirectory(dateDir);

            string logFilePath = agentId == -1
                ? Path.Combine(dateDir, "Log.txt")
                : Path.Combine(dateDir, $"{engId}-{DateTime.Now:ddMMyyyy}.txt");

            using (var writer = File.AppendText(logFilePath))
            {
                writer.WriteLine($"{engCnt + 1} , {engId} , {message}");
            }
        }

        public static void LogFile(string message, bool debugMode = false, int agentId = -1, long engId = 0, long engCnt = 0)
        {
            if (!debugMode) return;

            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string dateDir = Path.Combine(path, "Log", DateTime.Now.ToString("ddMMyyyy"));

            if (!Directory.Exists(dateDir)) Directory.CreateDirectory(dateDir);

            string logFilePath = engId > 0
                ? Path.Combine(path, "Log", $"{engId}.txt")
                : Path.Combine(dateDir, "Log.txt");

            using (var writer = File.AppendText(logFilePath))
            {
                writer.WriteLine($"{engCnt + 1} , {engId} , {message}");
            }
        }

        public static bool IsFileInUse(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
            }
            catch
            {
                return true;
            }
            return false;
        }
    }
}
