using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class ServerHelper
    {
        public int UserID { get; set; }
        public string ServerResponse { get; set; }
        public Exception Exception { get; set; }
        public int ErrorNumber { get; set; }
        public ErrorType ErrorOccured { get; set; }
        public int CustomErrorNumber { get; set; }
        public string ErrorDescription { get; set; }
        protected string SessionID { get; set; }

        public ServerHelper() { }

        public ServerHelper(int userID) => this.UserID = userID;

        public ServerHelper(int userID, string sessionID)
        {
            this.UserID = userID;
            this.SessionID = sessionID;
        }

        public ServerHelper(string sessionID) => this.SessionID = sessionID;

        public enum ErrorType
        {
            NoError,
            NoNetConnection,
            ServerBusy,
            SessionTimeOut,
            InternalError,
            DamagedPdf,
            PasswordProtectePDF,
            NoImageFound,
            InSufficentBalance,
        }

        public enum TaxSoftwareEnum
        {
            GoSystem = 1,
            Lacerte = 2,
            ProSystem = 3,
            UltraTax = 4,
            ProSeries = 5,
            GlobalFx = 6,
        }

        public enum OperationEnum
        {
            NoChange,
            Insert,
            Edit,
            Delete,
            CannotSave,
        }

        public enum RoleTypeEnum
        {
            STAFF,
            ADMIN,
            REVIEWER,
        }

        public enum SchDExpOptionEnum
        {
            FirstOption = 1,
            SecondOption = 2,
        }

        public enum GoSysAccActivationEnum
        {
            Activated,
            MailPendingToGosys,
            MailSendToGosys,
            AccActivatedbyGosys,
            SendtoAXAgent,
            ActivationError,
        }
    }
}
