using System;
using System.Diagnostics;

namespace FaxToTaxTemplateLib.QueueManagerClasses
{

  

    [Serializable]
    public class Common_SPError
    {
        public enum CommandStatusEnum
        {
            EXECUTEDSUCCESSFULLY = 1,
            EXECUTIONFAIL
        }

        public enum ErrorEnum
        {
            NOERROR,
            ERROROCCURED,
            BADPDF,
            SUBJOBSERROR,
            PROCEDUREEXECUTION,
            NOTHINGIDENTIFIED,
            JOBALREADYCOMPLETED,
            INVALIDDOMAIN,
            INVALIDLOCATOR,
            FILENOTFOUND,
            JOBSCANCELLED,
            INVALIDPASSWORD,
            LOCATOROPEN,
            NOTHINGRECOGNISED,
            NODOCFORPROCESSING,
            WEBSERVICENOTFOUND,
            WEBSERVERNOTFOUND
        }

        public enum EngagementTypeEnum
        {
            ALLENGAGEMENTYPE,
            TYPE1040,
            TYPE1040BINDER,
            TYPE1041,
            TYPE1041BINDER,
            SCAN1040
        }

        public enum TaxSoftwareEnum
        {
            ALLTAXSOFTWARE,
            GOSYSTEM,
            LACERTE,
            PROSYSTEM,
            ULTRATAX,
            PROSERIES,
            GLOBALFX
        }

        public enum JobTypeenum
        {
            IMAGECONVERSION = 101,
            IMAGEREPAIR = 102,
            SCAN1040IMAGEPREPROCESSING = 103,
            GTLLOCATORINFO = 110,
            AXTRANSFERIMPORT = 111,
            PORTING = 112,
            RESUBMITPORTING = 113,
            AXTRANFERPDFUPLOAD = 114,
            SPEL_BINDERPROCESSING = 115,
            ULTRAZPX_PREPORTING = 116,
            OCR_READ = 121,
            OCR_FAXFORM = 122,
            CCR = 123,
            OCR_RDC_READ = 124,
            EXPORT = 131,
            AXTRANSFEREXPORT = 132,
            IMPORTTR = 133,
            ULTRAZPX_POSTEXPORT = 134,
            PRINTING = 141,
            UPLOADINGTOSHAREPOINT = 142,
            RESPONSETOGTL = 143,
            GTLNOTIFICATION = 151,
            GTLDOCUMENTINFO = 152,
            CHANGE_STATUS = 161
        }

        public enum SubJobTypeenum
        {
            AX_TRANSFER_AUTHENTICATE_IMPORT = 201,
            AX_TRANSFER_CHECK_IMPORT = 202,
            AX_TRANSFER_AUTHENTICATE_EXPORT = 203,
            AX_TRANSFER_CHECK_EXPORT = 204,
            AX_TRANSFER_AUTHENTICATE_IMPORT_TR = 205,
            AX_TRANSFER_CHECK_IMPORT_TR = 206,
            Ax_Transfers_PDF_Authenticate = 207,
            Ax_Transfers_PDF_Check = 208,
            AX_TRANSFER_AUTHENTICATE_GTLVALIDATION = 209,
            AX_TRANSFER_VALIDATE_GTLVALIDATION = 210,
            OCR_IDENTIFICATION_BATCHES = 211,
            OCRRECOGNISTIONSTATE_XD = 212,
            CCR_OCRRECOGNITION = 213,
            OCR_DENODONORMALIZER = 214,
            CCR_DENODONORMALIZER = 215,
            OCR_RDC_IDENTIFICATION_BATCHES = 216,
            OCR_RDC_RECOGNISTIONSTATE_XD = 217,
            OCR_RDC_DENODONORMALIZER = 218,
            GTLSUBMISSIONNOTIFICATION = 241,
            GTLSTATUSCHANGENOTIFICATION = 242,
            GTLALERTNOTIFICATION = 243,
            GTLNOTESNOTIFICATION = 244
        }

        public enum JobForenum
        {
            IMAGECONVERSION = 1,
            IMAGEREPAIR,
            AXTRANSFER,
            PORTING,
            OCR,
            EXPORT,
            PRINTING,
            UPLOAD_SERVICECENTER,
            CHANGE_STATUS,
            FAX_TAXCONVERSION,
            IMAGEPREPROCESSING,
            PORTINGFORRESUBMIT,
            AXTRANFERPDFUPLOAD
        }

        public enum SubJobForenum
        {
            AX_TRANSFER_AGENT = 3,
            OCR_AGENT = 5,
            AXTRANFERPDFUPLOAD = 13
        }

        public enum ProcessingTypeEnum
        {
            NOTYPE,
            STANDARD,
            EXPEDIATE
        }

        public enum ProcessedByenum
        {
            ALLPROCESSED,
            SUREPREP,
            SUREPREPASSIGNED,
            INHOUSE
        }

        public enum JoblinkWithenum
        {
            SPEXPRESS = 1,
            SCAN1040,
            FORMS,
            OCR,
            AGENT,
            FILEROOM,
            DASHBOARD,
            OTHERS
        }

        private int intErrorNumber;

        private string strErrorDescription;

        private Exception objExcep;

        private EngagementTypeEnum enumEngagementType;

        private JobTypeenum enumJobType;

        private ProcessedByenum enumProcessedBy;

        private TaxSoftwareEnum enumTaxSoftware;

        private JoblinkWithenum enumJoblinkWith;

        private TaxSoftwareEnum enumTaxsoftwareID;

        private ProcessingTypeEnum enumProcessingType;

        private ErrorEnum enumError;

        private int intTaxYear;

        private JobForenum enumJobFor;

        private SubJobForenum enumSubJobFor;

        public JobForenum S_JobFor
        {
            get
            {
                return enumJobFor;
            }
            set
            {
                enumJobFor = value;
            }
        }

        public SubJobForenum S_SubJobJobFor
        {
            get
            {
                return enumSubJobFor;
            }
            set
            {
                enumSubJobFor = value;
            }
        }

        public int S_Taxyear
        {
            get
            {
                return intTaxYear;
            }
            set
            {
                intTaxYear = value;
            }
        }

        public TaxSoftwareEnum S_Taxsoftware
        {
            get
            {
                return enumTaxsoftwareID;
            }
            set
            {
                enumTaxsoftwareID = value;
            }
        }

        public EngagementTypeEnum S_EngagementType
        {
            get
            {
                return enumEngagementType;
            }
            set
            {
                enumEngagementType = value;
            }
        }

        public JoblinkWithenum S_JoblinkedWith
        {
            get
            {
                return enumJoblinkWith;
            }
            set
            {
                enumJoblinkWith = value;
            }
        }

        public JobTypeenum S_JobType
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

        public int S_ErrorNumber
        {
            get
            {
                return intErrorNumber;
            }
            set
            {
                intErrorNumber = value;
            }
        }

        public ProcessedByenum S_ProcessedBy
        {
            get
            {
                return enumProcessedBy;
            }
            set
            {
                enumProcessedBy = value;
            }
        }

        public ProcessingTypeEnum S_ProcessingType
        {
            get
            {
                return enumProcessingType;
            }
            set
            {
                enumProcessingType = value;
            }
        }

        public string S_ErrorDescription
        {
            get
            {
                return strErrorDescription;
            }
            set
            {
                strErrorDescription = value;
            }
        }

        public ErrorEnum S_ErrorOccured
        {
            get
            {
                return enumError;
            }
            set
            {
                enumError = value;
            }
        }

        public Exception S_ExceptionObject
        {
            get
            {
                return objExcep;
            }
            set
            {
                objExcep = value;
            }
        }

        [DebuggerNonUserCode]
        public Common_SPError()
        {
        }
    }
}
