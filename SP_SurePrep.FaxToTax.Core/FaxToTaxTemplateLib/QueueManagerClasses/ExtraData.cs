using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib.QueueManagerClasses
{
    [Serializable]
    public class ExtraData
    {
        public enum enumDataType
        {
            DataInteger = 1,
            DataString,
            DataDate
        }

        public enum ExtraDataFilter
        {
            PUBLICATIONID = 1,
            PAGEID,
            AXTRANFERGUID,
            OCRTEMPLATENAME,
            PAGEPATH,
            PROCESSINGORDER
        }

        private string strDataKey;

        private string strDataValue;

        private enumDataType E_DataType;

        private ExtraDataFilter E_Filter;

        public ExtraDataFilter DataFilter => E_Filter;

        public string DataKey => strDataKey;

        public string DataValue => strDataValue;

        public enumDataType DataType => E_DataType;

        public ExtraData(string key, string D_Value, enumDataType DataType, ExtraDataFilter Filter)
        {
            strDataKey = key;
            strDataValue = D_Value;
            E_DataType = DataType;
            E_Filter = Filter;
        }

        public ExtraData()
        {
        }
    }
}
