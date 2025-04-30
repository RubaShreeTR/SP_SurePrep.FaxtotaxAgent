using System.Data;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class Class1040ScanChecker : ServerHelper
    {
        private List<Class1040ScanVerificationItem> objVerficationList;
        private List<Class1040ScanCorrectionItems> objCorrectionList;
        private List<Class1040ScanParentAssoc> objParentList;
        private List<Class1040ScanhangingFrms> objUnassociatedFormsList;
        private List<Class1040Superseded> objSupercededList;
        private List<Class1040ScanDuplicateData> objDuplicateList;
        private List<Class1040ScanTaxParentAssociation> objTaxParentList;
        private List<Class1040ScanTaxParentAssociation> objTaxChildList;
        private List<Class1040ScanProformaForms> objProformaFormsList;
        private List<Class1040ScanFaxForms> objFaxFormsList;
        private List<SPVTypeVariation> objTypeVariationList;
        private List<SpPreRulePages> objPreRulePageList;
        private bool blnFromNext;
        private Exception serverexcep;
        private string strServerResponse;
        public Class1040ScanChecker.ActivityType selectedType;
        public DataSet DuplicateDataSet;
        private bool blnAnywhereAccess;
        private string strShowFilePath;
        private int IntTaxSoftWare;
        private int IntEngagementTypeID;
        private int intServiceTypeID;
        private DataTable dsChildForms;
        private DataTable dsFieldGroups;
        private DataTable dsAllCFAIdentifiers;
        private DataTable dsAllForms;
        private DataTable dsFieldGroupParent;
        private int intTaxYear;
        private DataSet dsProformadFormFieldGroup;
        private DataSet dsFaxFormFieldGroup;
        private bool blnIsSSGDomain;
        private bool IsDDPNewPreprocess;

        public Class1040ScanChecker(Class1040ScanChecker.ActivityType Type)
        {
            this.selectedType = Type;
            switch (Type)
            {
                case Class1040ScanChecker.ActivityType.Verficiation:
                    this.objVerficationList = new List<Class1040ScanVerificationItem>();
                    this.objTypeVariationList = new List<SPVTypeVariation>();
                    break;
                case Class1040ScanChecker.ActivityType.Correction:
                    this.objCorrectionList = new List<Class1040ScanCorrectionItems>();
                    break;
                case Class1040ScanChecker.ActivityType.ParentAssociation:
                    this.objParentList = new List<Class1040ScanParentAssoc>();
                    this.objUnassociatedFormsList = new List<Class1040ScanhangingFrms>();
                    break;
                case Class1040ScanChecker.ActivityType.SupercededDocuments:
                    this.objSupercededList = new List<Class1040Superseded>();
                    break;
                case Class1040ScanChecker.ActivityType.Duplicate:
                    this.objDuplicateList = new List<Class1040ScanDuplicateData>();
                    break;
                case Class1040ScanChecker.ActivityType.ProformaFormAssociation:
                    this.objProformaFormsList = new List<Class1040ScanProformaForms>();
                    this.objFaxFormsList = new List<Class1040ScanFaxForms>();
                    break;
                case Class1040ScanChecker.ActivityType.TaxParentAssociation:
                    this.objTaxParentList = new List<Class1040ScanTaxParentAssociation>();
                    this.objTaxChildList = new List<Class1040ScanTaxParentAssociation>();
                    break;
                case Class1040ScanChecker.ActivityType.PrimaryVerification:
                    this.objVerficationList = new List<Class1040ScanVerificationItem>();
                    this.objTypeVariationList = new List<SPVTypeVariation>();
                    break;
                case Class1040ScanChecker.ActivityType.PreVerification:
                    this.objPreRulePageList = new List<SpPreRulePages>();
                    break;
            }
        }

        public Class1040ScanChecker() => this.P_ScanChecker_selectedType = this.selectedType;

        //public Class1040ScanChecker(string SessionID)
        //  : base(SessionID)
        //{
        //    this.P_ScanChecker_selectedType = this.selectedType;
        //}

        public int CountVerificationItems => this.objVerficationList.Count;

        public int AddVerficationItem(Class1040ScanVerificationItem ObjVerificationItem)
        {
            this.objVerficationList.Add(ObjVerificationItem);
            return this.objVerficationList.Count;
        }

        public int CountCorrectionItems => this.objCorrectionList.Count;

        public int AddCorrectionItem(Class1040ScanCorrectionItems objCorrectionItem)
        {
            this.objCorrectionList.Add(objCorrectionItem);
            return this.objCorrectionList.Count;
        }

        public int AddParentAssocItem(Class1040ScanParentAssoc objParentAssocItem)
        {
            this.objParentList.Add(objParentAssocItem);
            return this.objParentList.Count;
        }

        public int AddUnassociatedFormsItem(Class1040ScanhangingFrms objUnassociatedFormsItem)
        {
            this.objUnassociatedFormsList.Add(objUnassociatedFormsItem);
            return this.objUnassociatedFormsList.Count;
        }

        public int AddSupercededItem(Class1040Superseded objSupercededItem)
        {
            this.objSupercededList.Add(objSupercededItem);
            return this.objSupercededList.Count;
        }

        public int AddDuplicateItem(Class1040ScanDuplicateData ObjDuplicateItem)
        {
            this.objDuplicateList.Add(ObjDuplicateItem);
            return this.objDuplicateList.Count;
        }

        public int AddTaxParentItem(Class1040ScanTaxParentAssociation objTaxParentItem)
        {
            this.objTaxParentList.Add(objTaxParentItem);
            return this.objTaxParentList.Count;
        }

        public int AddTaxChildItem(Class1040ScanTaxParentAssociation objTaxChildItem)
        {
            this.objTaxChildList.Add(objTaxChildItem);
            return this.objTaxChildList.Count;
        }

        public int AddProformaFormItem(Class1040ScanProformaForms objProformaFormItem)
        {
            this.objProformaFormsList.Add(objProformaFormItem);
            return this.objProformaFormsList.Count;
        }

        public int AddFaxFormItem(Class1040ScanFaxForms objFaxFormItem)
        {
            this.objFaxFormsList.Add(objFaxFormItem);
            return this.objFaxFormsList.Count;
        }

        public int AddTypeVarationItem(SPVTypeVariation objTypeVarationItem)
        {
            this.objTypeVariationList.Add(objTypeVarationItem);
            return this.objTypeVariationList.Count;
        }

        //public int AddPreRulePages(SpPreRulePages objPreRulePages)
        //{
        //    this.objPreRulePageList.Add(objPreRulePages);
        //    return this.objPreRulePageList.Count;
        //}

        //public SpPreRulePages GetPreRulePages(int intCounter) => this.objPreRulePageList[intCounter];

        //public int GetPreRulePageCount() => this.objPreRulePageList.Count;

        //public Class1040ScanCorrectionItems GetCorrectionItem(int intCounter) => this.objCorrectionList[intCounter];

        //public Class1040ScanVerificationItem GetVerficationItem(int intCounter) => this.objVerficationList[intCounter];

        public Class1040ScanParentAssoc GetParentAssocItem(int intCounter) => this.objParentList[intCounter];

        public Class1040ScanhangingFrms GetUnassociatedFormsItem(int intCounter) => this.objUnassociatedFormsList[intCounter];

        public Class1040Superseded GetSupercededItem(int intCounter) => this.objSupercededList[intCounter];

        //public Class1040ScanDuplicateData GetDuplicateItem(int intCounter) => this.objDuplicateList[intCounter];

        //public Class1040ScanTaxParentAssociation GetTaxParentItem(int intCounter) => this.objTaxParentList[intCounter];

        //public Class1040ScanTaxParentAssociation GetTaxChildItem(int intCounter) => this.objTaxChildList[intCounter];

        //public Class1040ScanProformaForms GetProformaFormItem(int intCounter) => this.objProformaFormsList[intCounter];

        //public Class1040ScanFaxForms GetFaxFormItem(int intCounter) => this.objFaxFormsList[intCounter];

        //public SPVTypeVariation GetTypeVariationItem(int intCounter) => this.objTypeVariationList[intCounter];

        public List<object> GetCollection()
        {
            switch (this.selectedType)
            {
                case Class1040ScanChecker.ActivityType.Verficiation:
                    return new List<object>(this.objVerficationList);
                case Class1040ScanChecker.ActivityType.Correction:
                    return new List<object>(this.objCorrectionList);
                case Class1040ScanChecker.ActivityType.ParentAssociation:
                    return new List<object>(this.objUnassociatedFormsList);
                case Class1040ScanChecker.ActivityType.SupercededDocuments:
                    return new List<object>(this.objSupercededList);
                case Class1040ScanChecker.ActivityType.Duplicate:
                    return new List<object>(this.objDuplicateList);
                case Class1040ScanChecker.ActivityType.ProformaFormAssociation:
                    return new List<object>(this.objFaxFormsList);
                case Class1040ScanChecker.ActivityType.TaxParentAssociation:
                    return new List<object>(this.objTaxChildList);
                case Class1040ScanChecker.ActivityType.PrimaryVerification:
                    return new List<object>(this.objVerficationList);
                default:
                    throw new InvalidOperationException("Invalid ActivityType");
            }
        }

        public List<Class1040ScanParentAssoc> GetParentFormsCollection() => this.objParentList;

        public List<Class1040ScanTaxParentAssociation> GetTaxParentCollection() => this.objTaxParentList;

        public List<Class1040ScanProformaForms> GetProformaFormsCollection() => this.objProformaFormsList;

        //public List<SPVTypeVariation> GetTypeVariationCollection() => this.objTypeVariationList;

        public Class1040ScanChecker.ActivityType P_ScanChecker_selectedType
        {
            get => this.selectedType;
            set => this.selectedType = value;
        }

        public bool FromNext
        {
            get => this.blnFromNext;
            set => this.blnFromNext = value;
        }

        public bool AnywhereAccess
        {
            get => this.blnAnywhereAccess;
            set => this.blnAnywhereAccess = value;
        }

        //public string ShowFilePath
        //{
        //    get => this.strShowFilePath;
        //    set => this.strShowFilePath = value;
        //}

        public int TaxSoftWareID
        {
            get => this.IntTaxSoftWare;
            set => this.IntTaxSoftWare = value;
        }

        public int EngagementTypeID
        {
            get => this.IntEngagementTypeID;
            set => this.IntEngagementTypeID = value;
        }

        public DataTable ChildForms
        {
            get => this.dsChildForms;
            set => this.dsChildForms = value;
        }

        public DataTable FieldGroups
        {
            get => this.dsFieldGroups;
            set => this.dsFieldGroups = value;
        }

        public DataTable AllCFAIdentifiers
        {
            get => this.dsAllCFAIdentifiers;
            set => this.dsAllCFAIdentifiers = value;
        }

        public DataTable AllForms
        {
            get => this.dsAllForms;
            set => this.dsAllForms = value;
        }

        public DataTable FieldGroupParent
        {
            get => this.dsFieldGroupParent;
            set => this.dsFieldGroupParent = value;
        }

        public int TaxYear
        {
            get => this.intTaxYear;
            set => this.intTaxYear = value;
        }

        public DataSet ProformadFormFieldGroup
        {
            get => this.dsProformadFormFieldGroup;
            set => this.dsProformadFormFieldGroup = value;
        }

        public DataSet FaxFormFieldGroup
        {
            get => this.dsFaxFormFieldGroup;
            set => this.dsFaxFormFieldGroup = value;
        }

        //public bool IsSSGDomain
        //{
        //    get => this.blnIsSSGDomain;
        //    set => this.blnIsSSGDomain = value;
        //}

        public bool DDPNewPreprocess
        {
            get => this.IsDDPNewPreprocess;
            set => this.IsDDPNewPreprocess = value;
        }

        public enum ActivityType
        {
            Verficiation = 1,
            Correction = 2,
            ParentAssociation = 3,
            SupercededDocuments = 4,
            Duplicate = 5,
            ProformaFormAssociation = 6,
            TaxParentAssociation = 7,
            PrimaryVerification = 11, // 0x0000000B
            NewVerification = 12, // 0x0000000C
            DeleteProforma = 20, // 0x00000014
            OCRToFax = 21, // 0x00000015
            Diagnostic = 22, // 0x00000016
            TaxExempt = 41, // 0x00000029
            EvaluateFaxToTaxFormula = 51, // 0x00000033
            NewDuplicate = 52, // 0x00000034
            Fax2Tax = 61, // 0x0000003D
            Fax2Tax2 = 62, // 0x0000003E
            ProformaMaching = 71, // 0x00000047
            PreVerification = 81, // 0x00000051
        }
    }
}
