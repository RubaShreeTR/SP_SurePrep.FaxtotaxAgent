using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class RWPage
    {
        public int ID { get; set; }
        public int InputFormID { get; set; }
        public string PageName { get; }
        public string FileName { get; }
        public string DisplayName { get; }
        public int CurSequence { get; set; }
        public int FieldGroupInstance { get; set; }
        public int PageRotation { get; set; }
        public bool IsMultiInstance { get; set; }
        public string FormTypeDWPCode { get; set; }
        public int UpdatedEngagementFormID { get; set; }
        public int UpdatedFieldGroupInstance { get; set; }
        public int ClientPageDPI { get; set; }
        public string FileType { get; set; }
        public float SkewAngle { get; set; }
        public List<RWReference> PageReferences { get; set; }

        //public RWPage(
        //  int id,
        //  int inputFormID,
        //  string pageName,
        //  string strFileName,
        //  string displayName,
        //  int curSequence,
        //  int fieldGroupInstance,
        //  int pageRotation,
        //  bool isMultiInstance,
        //  string formTypeDWPCode,
        //  int clientPageDPI,
        //  string fileType,
        //  float skewAngle)
        //{
        //    ID = id;
        //    InputFormID = inputFormID;
        //    PageName = pageName;
        //    FileName = strFileName;
        //    DisplayName = displayName;
        //    CurSequence = curSequence;
        //    FieldGroupInstance = fieldGroupInstance;
        //    PageRotation = pageRotation;
        //    IsMultiInstance = isMultiInstance;
        //    FormTypeDWPCode = formTypeDWPCode;
        //    ClientPageDPI = clientPageDPI;
        //    FileType = fileType;
        //    SkewAngle = skewAngle;
        //    PageReferences = new List<RWReference>();
        //}

        public RWPage(int id,
          int inputFormID,
          string pageName,
          string strFileName,
          string displayName,
          int curSequence,
          int fieldGroupInstance,
          int pageRotation,
          bool isMultiInstance,
          string formTypeDWPCode,
          int clientPageDPI,
          string fileType)
        {
            ID = id;
            InputFormID = inputFormID;
            PageName = pageName;
            FileName = strFileName;
            DisplayName = displayName;
            CurSequence = curSequence;
            FieldGroupInstance = fieldGroupInstance;
            PageRotation = pageRotation;
            IsMultiInstance = isMultiInstance;
            FormTypeDWPCode = formTypeDWPCode;
            ClientPageDPI = clientPageDPI;
            FileType = fileType;
            PageReferences = new List<RWReference>();
        }

        public void AddPageReference(RWReference reference)
        {
            PageReferences.Add(reference);
        }
    }

}
