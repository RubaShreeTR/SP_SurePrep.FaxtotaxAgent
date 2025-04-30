using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxDataValidations
{
    public class SpParameter
    {

        private string _arg;
        private object _argValue;
        private ParameterDirection _argDirection;

        public string Arg
        {
            get
            {
                return _arg;
            }
            set
            {
                _arg = value;
            }
        }

        public object ArgValue
        {
            get
            {
                return _argValue;
            }
            set
            {
                _argValue = value;
            }
        }

        public ParameterDirection ArgDirection
        {
            get
            {
                return _argDirection;
            }
            set
            {
                _argDirection = value;
            }
        }

        public SpParameter(string arg, object argValue)
        {
            _arg = arg;
            _argValue = argValue;
        }
        public SpParameter(string arg, object argValue, ParameterDirection argDirection)
        {
            _arg = arg;
            _argValue = argValue;
            _argDirection = argDirection;
        }
    }
}
