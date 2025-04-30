using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxTemplateLib
{
    [Serializable]
    public class ListValues
    {
        public string DetailName { get; }
        public string Value { get; }
        public int Sequence { get; }

        public ListValues(string detailName, string value, int sequence)
        {
            DetailName = detailName;
            Value = value;
            Sequence = sequence;
        }
    }
}
