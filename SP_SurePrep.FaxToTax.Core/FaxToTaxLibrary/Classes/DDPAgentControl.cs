using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxLibrary.Classes
{
    public class DDPAgentControl
    {
        private static bool _isAPIEnabled;

        public static bool isAPIEnabled
        {
            get
            {
                return DDPAgentControl._isAPIEnabled;
            }
            set
            {
                DDPAgentControl._isAPIEnabled = value;
            }
        }
    }
}
