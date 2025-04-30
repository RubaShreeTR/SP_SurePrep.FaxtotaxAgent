using System;
namespace FaxToTaxTemplateLib.QueueManagerClasses
{
    

    [Serializable]
    public class Response : Common_SPError
    {
        private CommandStatusEnum enumExecutionStatus;

        private bool blnMainJobcomplete;

        private int intSureprepEngagementStatus;

        private int intCustomerEngagementStatus;

        private int intJobGroupID;

        public CommandStatusEnum ExecutionStatus
        {
            get
            {
                return enumExecutionStatus;
            }
            set
            {
                enumExecutionStatus = value;
            }
        }

        public int SureprepEngagementStatusID
        {
            get
            {
                return intSureprepEngagementStatus;
            }
            set
            {
                intSureprepEngagementStatus = value;
            }
        }

        public int CustomerEngagementStatusID
        {
            get
            {
                return intCustomerEngagementStatus;
            }
            set
            {
                intCustomerEngagementStatus = value;
            }
        }

        public bool MainJobComplete
        {
            get
            {
                return blnMainJobcomplete;
            }
            set
            {
                blnMainJobcomplete = value;
            }
        }

        public int JobGroupId
        {
            get
            {
                return intJobGroupID;
            }
            set
            {
                intJobGroupID = value;
            }
        }

        public Response(CommandStatusEnum ExecutionStatus)
        {
            enumExecutionStatus = ExecutionStatus;
        }
    }
}
