using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class Class1040ScanhangingFrms
    {
        private int EngagementId;
        private string Operation;
        private int FaxFormID;
        private string FormName;
        private int ParentId;
        private string ParentDWPCode;
        private int PageID;
        private string PageName;
        private int intClientPageDPI;
        private string strFileType;
        private string StrAutoPageMatched;
        private int EngagementFieldGroupID;
        private int ParentFaxFormID;
        private string strFaxDWPCode;
        private int FaxID;
        private int NewChildFaxFormID;

        public List<RWPage> Pages { get; private set; }

        public Class1040ScanhangingFrms(
          int EngagementID,
          int FaxformId,
          string FormName,
          int ParentId,
          string ParentDWPCode,
          int PageID,
          string PageName,
          int intClientPageDPI,
          string strFileType,
          string strPagematched)
        {
            this.P_HanginFrms_FaxFormID = FaxformId;
            this.P_HanginFrms_FormName = FormName;
            this.P_HanginFrms_Operation = this.Operation;
            this.P_HanginFrms_ParentId = ParentId;
            this.P_HanginFrms_ParentDWPCode = ParentDWPCode;
            this.P_HanginFrms_PageId = PageID;
            this.P_HanginFrms_PageName = PageName;
            this.P_HanginFrms_EngagementID = EngagementID;
            this.ClientPageDPI = intClientPageDPI;
            this.FileType = strFileType;
            this.intClientPageDPI = intClientPageDPI;
            this.strFileType = strFileType;
            this.StrAutoPageMatched = strPagematched;
            this.P_HanginFrms_EngagementFieldGroupID = this.EngagementFieldGroupID;
            this.P_ParentFaxFormID = this.ParentFaxFormID;
            this.P_FaxID = this.FaxID;
            this.P_NewChildFaxFormID = this.NewChildFaxFormID;
            this.Pages = new List<RWPage>();
        }

        public int P_HanginFrms_EngagementID
        {
            get => this.EngagementId;
            set => this.EngagementId = value;
        }

        public string P_HanginFrms_Operation
        {
            get => this.Operation;
            set => this.Operation = value;
        }

        public int P_HanginFrms_FaxFormID
        {
            get => this.FaxFormID;
            set => this.FaxFormID = value;
        }

        public string P_HanginFrms_FormName
        {
            get => this.FormName;
            set => this.FormName = value;
        }

        public int P_HanginFrms_ParentId
        {
            get => this.ParentId;
            set => this.ParentId = value;
        }

        public string P_HanginFrms_ParentDWPCode
        {
            get => this.ParentDWPCode;
            set => this.ParentDWPCode = value;
        }

        public int P_HanginFrms_PageId
        {
            get => this.PageID;
            set => this.PageID = value;
        }

        public string P_HanginFrms_PageName
        {
            get => this.PageName;
            set => this.PageName = value;
        }

        public int ClientPageDPI
        {
            get => this.intClientPageDPI;
            set => this.intClientPageDPI = value;
        }

        public string FileType
        {
            get => this.strFileType;
            set => this.strFileType = value;
        }

        public string P_HanginFrms_AutoPageMatched
        {
            get => this.StrAutoPageMatched;
            set => this.StrAutoPageMatched = value;
        }

        public int P_HanginFrms_EngagementFieldGroupID
        {
            get => this.EngagementFieldGroupID;
            set => this.EngagementFieldGroupID = value;
        }

        public int P_ParentFaxFormID
        {
            get => this.ParentFaxFormID;
            set => this.ParentFaxFormID = value;
        }

        public int P_FaxID
        {
            get => this.FaxID;
            set => this.FaxID = value;
        }

        public int P_NewChildFaxFormID
        {
            get => this.NewChildFaxFormID;
            set => this.NewChildFaxFormID = value;
        }

        public string FaxDWPCode
        {
            get => this.strFaxDWPCode;
            set => this.strFaxDWPCode = value;
        }

        public void AddPage(RWPage oPage)
        {
            this.Pages.Add(oPage);
        }
    }
}
