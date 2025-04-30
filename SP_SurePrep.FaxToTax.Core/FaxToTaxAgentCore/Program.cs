using FaxToTaxAgentCore;
using FaxToTaxDataValidations;
using Microsoft.Extensions.Configuration;
using System;
public class Program
{
    public static void Main(string[] args)
    {
        
        AgentProcessor sp = new AgentProcessor();
        int engID= 617678 ;
        int jobID= 3178521;

        //Console.WriteLine("Enter BinderId");
        //engID = Convert.ToInt32(Console.ReadLine());

        //Console.WriteLine("Enter JobId");
        //jobID = Convert.ToInt32(Console.ReadLine());


        GenModule.configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
        string strOCRValidation = GenModule.configuration.GetSection("OCRValidation").Value;
        int isLogSeverity = Convert.ToInt32(GenModule.configuration.GetSection("LogSeverity").Value);
        string WCFPath = GenModule.configuration.GetSection("WCFPath").Value;
        string ssK1PreRuleValidationOn = GenModule.configuration.GetSection("IsK1PreRuleValidationOn").Value;

        sp.StartProcess(engID, jobID, WCFPath, strOCRValidation, isLogSeverity, "faxtotax", ssK1PreRuleValidationOn);
    }

}