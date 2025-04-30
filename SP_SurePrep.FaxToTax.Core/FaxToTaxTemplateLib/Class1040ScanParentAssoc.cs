using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class Class1040ScanParentAssoc : IComparable
    {
        private int FaxFormFDTID;
        private int FaxFormID;
        private string FaxFormInstance;
        private int ParentID;
        private string DisplayName;
        private int EngagementId;
        private Class1040ScanParentAssoc.OperationEnum enmOperation;
        private string FaxFormDWPCode;
        private int PrimaryIdentifierID;
        private string PIName;
        private string PIValue;
        private int SecondaryIdentifierID;
        private string SIName;
        private string SIValue;
        private string StrParentName;
        private int m_AddedByUser;
        private int PrimaryEngFaxFormID;
        private int SecondaryEngFaxFormID;
        private int m_EngagementFormID;
        private int intEntityID;
        private int EngagementFieldGroupID;
        private int intParentFaxID;
        private int intFaxFormType;
        private string IntCFAFormFieldID;
        private string IntCFAIdentifier;
        private string StrCFAValue;
        private string FormFieldID;
        private string ChildFaxDWPCode;
        private int NewChildFaxFormID;
        private int CFASequence;
        private bool isSelfParent;

        public Class1040ScanParentAssoc(
          int EngagementID,
          int FaxFormFDTID,
          int FaxFormID,
          string DisplayName,
          string FaxFormInstance,
          int ParentID,
          string FaxFormDWPCode,
          int PrimaryIdentifierID,
          string PIName,
          string PIValue,
          int SecondaryIdentifierID,
          string SIName,
          string SIValue,
          string ParentName,
          Class1040ScanParentAssoc.OperationEnum Operation,
          int AddedByUser,
          int PrimaryEngFaxFormID,
          int SecondaryEngFaxFormID,
          int intEngagementFormID)
        {
            this.ChildFaxDWPCode = "";
            this.P_ParentFrmAssoc_EngagementID = EngagementID;
            this.P_ParentFrmAssoc_FaxFormFDTID = FaxFormFDTID;
            this.P_ParentFrmAssoc_FaxFormID = FaxFormID;
            this.P_ParentFrmAssoc_DisplayName = DisplayName;
            this.P_ParentFrmAssoc_FaxFormInstance = FaxFormInstance;
            this.P_ParentFrmAssoc_ParentID = ParentID;
            this.P_ParentFrmAssoc_FaxFormDWPCode = FaxFormDWPCode;
            this.P_PrimaryIdentifID = PrimaryIdentifierID;
            this.P_PIName = PIName;
            this.P_PIValue = PIValue;
            this.P_SecondaryIdentifID = SecondaryIdentifierID;
            this.P_SIName = SIName;
            this.P_SIValue = SIValue;
            this.ParentName = ParentName;
            this.enmOperation = Operation;
            this.ParentID = FaxFormFDTID;
            this.m_AddedByUser = AddedByUser;
            this.P_PrimaryEngFaxFormID = PrimaryEngFaxFormID;
            this.P_SecondaryEngFaxFormID = SecondaryEngFaxFormID;
            this.m_EngagementFormID = intEngagementFormID;
            this.P_EntityID = this.intEntityID;
            this.P_ParentFaxID = this.intParentFaxID;
            this.P_FaxFormType = this.intFaxFormType;
            this.P_CFAFormFieldID = this.IntCFAFormFieldID;
            this.P_FormFieldID = this.FormFieldID;
            this.P_ChildFaxDWPCode = this.ChildFaxDWPCode;
            this.P_NewChildFaxFormID = this.NewChildFaxFormID;
            this.P_CFASequence = this.CFASequence;
            this.P_SelfParent = this.isSelfParent;
        }

        public int P_ParentFrmAssoc_EngagementID
        {
            get => this.EngagementId;
            set => this.EngagementId = value;
        }

        public int P_ParentFrmAssoc_FaxFormFDTID
        {
            get => this.FaxFormFDTID;
            set => this.FaxFormFDTID = value;
        }

        public int P_ParentFrmAssoc_FaxFormID
        {
            get => this.FaxFormID;
            set => this.FaxFormID = value;
        }

        public string P_ParentFrmAssoc_FaxFormInstance
        {
            get => this.FaxFormInstance;
            set => this.FaxFormInstance = value;
        }

        public string P_ParentFrmAssoc_DisplayName
        {
            get => this.DisplayName;
            set => this.DisplayName = value;
        }

        public int P_ParentFrmAssoc_ParentID
        {
            get => this.ParentID;
            set => this.ParentID = value;
        }

        public string P_ParentFrmAssoc_FaxFormDWPCode
        {
            get => this.FaxFormDWPCode;
            set => this.FaxFormDWPCode = value;
        }

        public Class1040ScanParentAssoc.OperationEnum P_Operation
        {
            get => this.enmOperation;
            set => this.enmOperation = value;
        }

        public int P_PrimaryIdentifID
        {
            get => this.PrimaryIdentifierID;
            set => this.PrimaryIdentifierID = value;
        }

        public string P_PIName
        {
            get => this.PIName;
            set => this.PIName = value;
        }

        public string P_PIValue
        {
            get => this.PIValue;
            set => this.PIValue = value;
        }

        public int P_SecondaryIdentifID
        {
            get => this.SecondaryIdentifierID;
            set => this.SecondaryIdentifierID = value;
        }

        public string P_SIName
        {
            get => this.SIName;
            set => this.SIName = value;
        }

        public string P_SIValue
        {
            get => this.SIValue;
            set => this.SIValue = value;
        }

        public string ParentName
        {
            get => this.StrParentName;
            set => this.StrParentName = value;
        }

        public int P_AddedByUser
        {
            get => this.m_AddedByUser;
            set => this.m_AddedByUser = value;
        }

        public int P_PrimaryEngFaxFormID
        {
            get => this.PrimaryEngFaxFormID;
            set => this.PrimaryEngFaxFormID = value;
        }

        public int P_SecondaryEngFaxFormID
        {
            get => this.SecondaryEngFaxFormID;
            set => this.SecondaryEngFaxFormID = value;
        }

        public int P_EngagementFormID
        {
            get => this.m_EngagementFormID;
            set => this.m_EngagementFormID = value;
        }

        public int P_EntityID
        {
            get => this.intEntityID;
            set => this.intEntityID = value;
        }

        public int P_ParentFaxID
        {
            get => this.intParentFaxID;
            set => this.intParentFaxID = value;
        }

        public int P_FaxFormType
        {
            get => this.intFaxFormType;
            set => this.intFaxFormType = value;
        }

        public int P_EngagementFieldGroupID
        {
            get => this.EngagementFieldGroupID;
            set => this.EngagementFieldGroupID = value;
        }

        public string P_CFAFormFieldID
        {
            get => this.IntCFAFormFieldID;
            set => this.IntCFAFormFieldID = value;
        }

        public string P_CFAIdentifier
        {
            get => this.IntCFAIdentifier;
            set => this.IntCFAIdentifier = value;
        }

        public string P_CFAValue
        {
            get => this.StrCFAValue;
            set => this.StrCFAValue = value;
        }

        public string P_FormFieldID
        {
            get => this.FormFieldID;
            set => this.FormFieldID = value;
        }

        public string P_ChildFaxDWPCode
        {
            get => this.ChildFaxDWPCode;
            set => this.ChildFaxDWPCode = value;
        }

        public int P_NewChildFaxFormID
        {
            get => this.NewChildFaxFormID;
            set => this.NewChildFaxFormID = value;
        }

        public int P_CFASequence
        {
            get => this.CFASequence;
            set => this.CFASequence = value;
        }

        public bool P_SelfParent
        {
            get => this.isSelfParent;
            set => this.isSelfParent = value;
        }

        public int CompareTo(object obj)
        {
            Class1040ScanParentAssoc class1040ScanParentAssoc = (Class1040ScanParentAssoc)obj;
            int num1 = checked(this.P_CFASequence - class1040ScanParentAssoc.P_CFASequence);
            int num2;
            if (num1 == 0)
            {
                num2 = this.P_EngagementFormID;
                num1 = num2.CompareTo(class1040ScanParentAssoc.P_EngagementFormID);
            }
            if (num1 == 0)
            {
                num2 = this.P_ParentFrmAssoc_FaxFormID;
                num1 = num2.CompareTo(class1040ScanParentAssoc.P_ParentFrmAssoc_FaxFormID);
            }
            return num1;
        }

        public enum OperationEnum
        {
            Unaffected = 1,
            Inserted = 2,
            Modified = 3,
            Deleted = 4,
        }
    }
}
