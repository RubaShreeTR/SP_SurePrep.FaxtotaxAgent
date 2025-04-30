using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxDataValidations
{
    public struct BinderEnums
    {
        public enum TaxSoftware
        {
            [Description("None")]
            None = 0,

            [Description("GoSystem Tax RS")]
            GoSystem = 1,

            [Description("Lacerte")]
            Lacerte = 2,

            [Description("ProSystem fx Tax")]
            ProSystem = 3,

            [Description("UltraTax")]
            UltraTax = 4,

            [Description("ProSeries")]
            ProSeries = 5,

            [Description("N/A")]
            Others = 6,

            [Description("CCH Axcess Tax")]
            CCHAccesTax = 7,

            [Description("Global fx")]
            GlobalFx = 8
        }

        public enum DataType
        {
            None = 0,
            Amount = 1,
            Percent = 2,
            AorP = 3,
            Description = 4,
            DropDown = 5,
            DateType = 6,
            SSN = 7,
            EIN = 8,
            MonthYear = 9,
            IntegerType = 10,
            MonthYY = 11,
            MonthDay = 12,
            Quantity = 13,
            SSNorEIN = 14,
            Telephone = 15
        }
    }

}
