using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FaxToTaxTemplateLib.QueueManagerClasses
{
   
        public class Engagement : Common_SPError
        {
            public enum EnumProcessingType
            {
                STANDARD = 1,
                EXPEDIATE
            }

            public enum EnumServiceCenter
            {
                AHMEDABAD = 9,
                MUMBAI = 6,
                NEWPORT = 3
            }

            public enum EnumCustomerStatus
            {
                AWAITINGSUBMISSION = 1,
                INPREPARATION,
                READYFORREVIEW,
                COMPLETE
            }

            public enum EnumLock
            {
                UNLOCK,
                LOCK
            }

            public enum EnumSureprepStatus
            {
                AWAITINGSUBMISSION = 1,
                INPREPARATION = 6,
                INREVIEW = 8,
                INCORRECTION = 10,
                READYFORREVIEW = 14,
                COMPLETE = 15
            }

            public enum EnumProcessedBy
            {
                SUREPREP = 1,
                SUREPREPASSIGNED,
                INHOUSE
            }

            private int intEngID;
            private string strDomainAbbreviation;
            private int intTaxYear;
            private EnumSureprepStatus E_SureprepStatus;
            private EnumCustomerStatus E_CustomerEngagement;
            private EnumServiceCenter E_ServiceCenter;

            public int EngagementID => intEngID;

            public string DomainAbbreviation
            {
                get => strDomainAbbreviation;
                set => strDomainAbbreviation = value;
            }

            public int TaxYear
            {
                get => intTaxYear;
                set => intTaxYear = value;
            }

            public EnumSureprepStatus SureprepStatus
            {
                get => E_SureprepStatus;
                set => E_SureprepStatus = value;
            }

            public EnumCustomerStatus CustomerStatus
            {
                get => E_CustomerEngagement;
                set => E_CustomerEngagement = value;
            }

            public EnumServiceCenter ServiceCenter
            {
                get => E_ServiceCenter;
                set => E_ServiceCenter = value;
            }

            public Engagement() { }

            public Engagement(int intEngagementID)
            {
                intEngID = intEngagementID;
            }

            public Engagement(int intEngagementID, string domainAbbreviation, TaxSoftwareEnum taxSoft, EngagementTypeEnum engType, EnumProcessedBy processBy, EnumCustomerStatus customerStatus, EnumSureprepStatus sureprepStatus, ProcessingTypeEnum processType, int intTaxYear)
            {
                intEngID = intEngagementID;
                strDomainAbbreviation = domainAbbreviation;
                S_Taxsoftware = taxSoft;
                this.intTaxYear = intTaxYear;
                S_EngagementType = engType;
                S_ProcessedBy = (ProcessedByenum)processBy;
                E_SureprepStatus = sureprepStatus;
                E_CustomerEngagement = customerStatus;
                S_ProcessingType = processType;
            }
        }
    
}
