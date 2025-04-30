using System;
using System.Data;

namespace FaxToTaxTemplateLib.QueueManagerClasses
{
    [Serializable]
    public class Jobs : Common_SPError
    {
        public enum JobStatusenum
        {
            INERROR = -1,
            COMPLETED,
            QUEUED,
            INPROCESS,
            ERROR_CANPROCEED,
            WITHSUPPORT
        }

        public enum JobOccuranceenum
        {
            JOBEXIST = 1,
            SUBJOBEXIST,
            NOSUBJOBS,
            NOJOBS,
            JOBCOMPLETED
        }

        private int intJobID;

        private int intGroupID;

        private int intWorkFlowStepID;

        private string strJobDescription;

        private int intJobEngagementStatus;

        private int intEngagementID;

        private int intDomainID;

        private string strDomainAbbreviation;

        private string strClientNumber;

        private DateTime dtJobQueuedDate;

        private DateTime dtJobPickedDate;

        private DateTime dtJobReturnedDate;

        private int intAgentID;

        private int intProcessID;

        private string strAgentResponse;

        private JobTypeenum enumJobType;

        private JobStatusenum enumJobStatus;

        private DataTable dsExtraData;

        private JobOccuranceenum enumJobExist;

        public int JobId => intJobID;

        public int GroupId
        {
            get
            {
                return intGroupID;
            }
            set
            {
                intGroupID = value;
            }
        }

        public int WorkFlowStepId
        {
            get
            {
                return intWorkFlowStepID;
            }
            set
            {
                intWorkFlowStepID = value;
            }
        }

        public string JobDescription
        {
            get
            {
                return strJobDescription;
            }
            set
            {
                strJobDescription = value;
            }
        }

        public int Engagementid
        {
            get
            {
                return intEngagementID;
            }
            set
            {
                intEngagementID = value;
            }
        }

        public int DomainID
        {
            get
            {
                return intDomainID;
            }
            set
            {
                intDomainID = value;
            }
        }

        public string DomainAbbreviation
        {
            get
            {
                return strDomainAbbreviation;
            }
            set
            {
                strDomainAbbreviation = value;
            }
        }

        public string ClientNumber
        {
            get
            {
                return strClientNumber;
            }
            set
            {
                strClientNumber = value;
            }
        }

        public DateTime JobQueuedDate
        {
            get
            {
                return dtJobQueuedDate;
            }
            set
            {
                dtJobQueuedDate = value;
            }
        }

        public DateTime JobPickedDate
        {
            get
            {
                return dtJobPickedDate;
            }
            set
            {
                dtJobPickedDate = value;
            }
        }

        public DateTime JobReturnedDate
        {
            get
            {
                return dtJobReturnedDate;
            }
            set
            {
                dtJobReturnedDate = value;
            }
        }

        public JobStatusenum JobStatus
        {
            get
            {
                return enumJobStatus;
            }
            set
            {
                enumJobStatus = value;
            }
        }

        public string AgentResponse
        {
            get
            {
                return strAgentResponse;
            }
            set
            {
                strAgentResponse = value;
            }
        }

        public int AgentID
        {
            get
            {
                return intAgentID;
            }
            set
            {
                intAgentID = value;
            }
        }

        public JobTypeenum JobType
        {
            get
            {
                return enumJobType;
            }
            set
            {
                enumJobType = value;
            }
        }

        public JobOccuranceenum JobExist
        {
            get
            {
                return enumJobExist;
            }
            set
            {
                enumJobExist = value;
            }
        }

        public int ProcessID
        {
            get
            {
                return intProcessID;
            }
            set
            {
                intProcessID = value;
            }
        }

        public DataTable ExtraData
        {
            get
            {
                return dsExtraData;
            }
            set
            {
                dsExtraData = value;
            }
        }

        public Jobs(int JobID)
        {
            intJobID = JobID;
            S_ErrorOccured = ErrorEnum.NOERROR;
        }
    }
}
