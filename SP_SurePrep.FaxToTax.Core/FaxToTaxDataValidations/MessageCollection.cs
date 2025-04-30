using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaxToTaxDataValidations
{
    public class MessageCollection
    {
        public static string GetMessage(string msgId, int maxLength, int LengthBeforeDecimal, int LengthAfterDecimal)
        {
            string msg = "";

            switch (msgId)
            {
                case "1001":
                    msg = "Invalid Amount";
                    break;
                case "1002":
                    msg = "Percentage cannot be negative or exceed 1.";
                    break;
                case "1003":
                    msg = "Invalid(Percentage)";
                    break;
                case "1004":
                    msg = "Invalid Data.";
                    break;
                case "1005":
                    msg = "Invalid Amount";
                    break;
                case "1006":
                    msg = "Value must be ###-###-####";
                    break;
                case "1007":
                    msg = "Invalid SSN \r\n\r\nValue must be SSN: ###-##-####";
                    break;
                case "1008":
                    msg = "Invalid SSN/EIN \r\n\r\nValue must be SSN: ###-##-#### or EIN: ##-#######";
                    break;
                case "1009":
                    msg = "Invalid EIN \r\n\r\nValue must be EIN: ##-#######";
                    break;
                case "1010":
                    msg = "Invalid Date.";
                    break;
                case "1011":
                    msg = "Select the correct value from the list.";
                    break;
                case "1012":
                    msg = "Description length cannot be less than " + maxLength.ToString();
                    break;
                case "1013":
                    msg = "Description length cannot be greater than " + maxLength.ToString();
                    break;
                case "1014":
                    msg = "Invalid Integer Field";
                    break;
                case "1015":
                    int intLengthBeforeDecimal = 0;
                    int inLengthAfterDecimal = 0;
                    if (LengthBeforeDecimal > 0 || LengthAfterDecimal > 0)
                    {
                        intLengthBeforeDecimal = LengthBeforeDecimal;
                        inLengthAfterDecimal = LengthAfterDecimal;
                    }
                    msg = GenerateQuantityMessage(intLengthBeforeDecimal, inLengthAfterDecimal);
                    break;
                case "1016":
                    int maxLocalLength = 0;
                    if (maxLength > 0)
                    {
                        maxLocalLength = maxLength;
                    }
                    msg = GenerateMaxLenthMessage(maxLocalLength);
                    break;
                default:
                    break;
            }
            return msg;
        }

        private static string GenerateMaxLenthMessage(int maxLenght)
        {
            string msg = "Value must be between 0 and ";
            if (maxLenght > 0)
            {
                string nineString = "";
                for (int cnt = 1; cnt <= maxLenght; cnt++)
                {
                    nineString += "9";
                }
                msg += nineString;
            }
            else
            {
                msg = "Value must be between 0 and 999";
            }
            return msg;
        }

        private static string GenerateQuantityMessage(int intLengthBeforeDecimal, int inLengthAfterDecimal)
        {
            string msg = "Value must be between 0 and ";
            if (inLengthAfterDecimal == 0 && intLengthBeforeDecimal == 0)
            {
                return "Value must be between 0 and 999 (xxx.xxxx)";
            }

            if (intLengthBeforeDecimal > 0)
            {
                string nineString = "";
                string xbeforeString = "";
                for (int cnt = 1; cnt <= intLengthBeforeDecimal; cnt++)
                {
                    nineString += "9";
                    xbeforeString += "x";
                }
                msg += nineString + " (" + xbeforeString;

                if (inLengthAfterDecimal > 0)
                {
                    string xAfterString = "";
                    for (int cnt = 1; cnt <= inLengthAfterDecimal; cnt++)
                    {
                        xAfterString += "x";
                    }
                    msg += "." + xAfterString + ")";
                }
                else
                {
                    msg += ")";
                }
            }

            return msg;
        }
    }

}
