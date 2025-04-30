using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FaxToTaxDataValidations
{
    [Serializable]
    public abstract class SPDataTypeValidation
    {
        public BinderEnums.TaxSoftware TaxSoftwareId { get; set; }
        public int TaxYear { get; set; }


        public string Validate(IOCRReference ocrRef, bool isFax2Tax = false)
        {
            string validationResult = string.Empty;
            switch (ocrRef.DataType)
            {
                case BinderEnums.DataType.Amount:
                    validationResult = DoAmountValidation(ocrRef, true);
                    break;
                case BinderEnums.DataType.Percent:
                    validationResult = DoPercentValidation(ocrRef, true);
                    break;
                case BinderEnums.DataType.AorP:
                    validationResult = DoAmtPrcntValidation(ocrRef, true);
                    break;
                case BinderEnums.DataType.SSN:
                    validationResult = DoSSNValidation(ocrRef, true);
                    break;
                case BinderEnums.DataType.SSNorEIN:
                    validationResult = DoSSNEINValidation(ocrRef, true);
                    break;
                case BinderEnums.DataType.EIN:
                    validationResult = DoEINValidation(ocrRef, true);
                    break;
                case BinderEnums.DataType.DateType:
                    validationResult = DoDateValidation(ocrRef, (int)TaxSoftwareId, TaxYear);
                    if (isFax2Tax && string.IsNullOrWhiteSpace(validationResult) && !string.IsNullOrWhiteSpace(ocrRef.ValueToUpdate))
                    {
                        ocrRef.IsUpdateRequried = true;
                    }
                    break;
                case BinderEnums.DataType.MonthYear:
                    if (!isFax2Tax) ocrRef.AcceptVarious = false;
                    validationResult = DoDateMMYYYYValidation(ocrRef, (int)TaxSoftwareId, TaxYear);
                    if (isFax2Tax && string.IsNullOrWhiteSpace(validationResult) && !string.IsNullOrWhiteSpace(ocrRef.ValueToUpdate))
                    {
                        ocrRef.IsUpdateRequried = true;
                    }
                    break;
                case BinderEnums.DataType.MonthDay:
                    if (!isFax2Tax) ocrRef.AcceptVarious = false;
                    validationResult = DoDateMMDDValidation(ocrRef, (int)TaxSoftwareId, TaxYear);
                    if (isFax2Tax && string.IsNullOrWhiteSpace(validationResult) && !string.IsNullOrWhiteSpace(ocrRef.ValueToUpdate))
                    {
                        ocrRef.IsUpdateRequried = true;
                    }
                    break;
                case BinderEnums.DataType.MonthYY:
                    if ((int)TaxSoftwareId == 3)
                    {
                        ocrRef.AcceptVarious = true;
                    }
                    else
                    {
                        ocrRef.AcceptVarious = false;
                    }
                    validationResult = DoDateMMYYValidation(ocrRef, (int)TaxSoftwareId, TaxYear);
                    if (isFax2Tax && string.IsNullOrWhiteSpace(validationResult) && !string.IsNullOrWhiteSpace(ocrRef.ValueToUpdate))
                    {
                        ocrRef.IsUpdateRequried = true;
                    }
                    break;
                case BinderEnums.DataType.DropDown:
                    //Dim dataView As New DataView
                    //validationResult = DoDropDownValidation(OCRValue, FaxDWPCode, dataView);
                    break;
                case BinderEnums.DataType.Description:
                    validationResult = DoDescValidation(ocrRef, 0, ocrRef.MaxLength);
                    break;
                case BinderEnums.DataType.IntegerType:
                    validationResult = DoIntegerValidation(ocrRef, ocrRef.MaxLength);
                    break;
                case BinderEnums.DataType.Quantity:
                    validationResult = DoQuantityValidation(ocrRef, ocrRef.FaxDWPCode, ocrRef.LengthBeforeDecimal, ocrRef.LengthAfterDecimal, (int)TaxSoftwareId);
                    break;
                case BinderEnums.DataType.Telephone:
                    validationResult = IsValidTEL(ocrRef);
                    break;
            }
            if (string.IsNullOrWhiteSpace(validationResult)) return validationResult;
            return MessageCollection.GetMessage(validationResult, ocrRef.MaxLength, ocrRef.LengthBeforeDecimal, ocrRef.LengthAfterDecimal);
        }

        #region SPExpress Quantity Inputform Data Type Validations

        private string DoQuantityValidation(IOCRReference ocrRef, string argDWPCode, int LengthBeforeDecimal, int LengthAfterDecimal, int intTaxSoftwareID)
        {
            try
            {
                string strString;
                string StrNumber;
                string orgVal = ocrRef.OCRValue;
                if (ocrRef.OCRValue != "")
                {
                    string theVal = string.Empty;
                    string theVal1;
                    try
                    {
                        ocrRef.OCRValue = ocrRef.OCRValue.Trim();
                        theVal1 = ocrRef.OCRValue;
                        theVal = theVal1.Replace(" ", "");
                        theVal = theVal.Replace(",", ""); // Added by jayanta 18/7/2012

                        if (((theVal.Contains(".") && theVal.Length <= 1) || theVal.EndsWith("-") || theVal.Contains(",")) && !(argDWPCode == "WAG.042" || argDWPCode == "WAG.210" || argDWPCode == "WAG.194" || argDWPCode == "WAG.202" || argDWPCode == "GAM.025" || argDWPCode == "REC.012"))
                        {
                            ocrRef.OCRValue = "";
                            return "1015";
                        }
                        if (theVal.Length != 0 && !IsANumber(theVal))
                        {
                            ocrRef.OCRValue = "";
                            return "1015";
                        }
                    }
                    catch (Exception)
                    {
                        // FuncWebServiceErrorCheck(ex)
                    }

                    string ValueNow;
                    string[] TempValue;
                    int IntBefore, IntAfter;
                    string str1 = "X";
                    string strNum1 = "9";
                    ValueNow = theVal;
                    TempValue = ValueNow.Split('.');
                    if (argDWPCode == "WAG.042" || argDWPCode == "WAG.210" || argDWPCode == "WAG.194" || argDWPCode == "WAG.202" || argDWPCode == "GAM.025" || argDWPCode == "REC.012")
                    {
                        IntBefore = TempValue[0].Replace(",", "").Length;
                    }
                    else
                    {
                        IntBefore = TempValue[0].Length;
                    }

                    if (TempValue.Length > 1)
                    {
                        IntAfter = TempValue[1].Length;
                    }
                    else
                    {
                        IntAfter = 0;
                    }
                    strString = str1.PadRight(LengthBeforeDecimal, str1[0]) + "." + str1.PadLeft(LengthAfterDecimal, str1[0]);
                    StrNumber = strNum1.PadRight(LengthBeforeDecimal, strNum1[0]);

                    if ((LengthBeforeDecimal > 0 && IntBefore > LengthBeforeDecimal) || (LengthAfterDecimal > 0 && IntAfter > LengthAfterDecimal) && intTaxSoftwareID == 4 && (argDWPCode == "REN.281" || argDWPCode == "REN.284" || argDWPCode == "REN.275" || argDWPCode == "REN.276" || argDWPCode == "REN.278" || argDWPCode == "REN.488" || argDWPCode == "FHC.017"))
                    {
                        ocrRef.OCRValue = "";
                        // MessageBox.Show("Value must be between -99 and 999 (xxx.xx)", "SurePrep Express", MessageBoxButtons.OK)
                        return "1015";
                    }
                    else if ((LengthBeforeDecimal > 0 && IntBefore > LengthBeforeDecimal) || (LengthAfterDecimal > 0 && IntAfter > LengthAfterDecimal) && intTaxSoftwareID == 4 && (argDWPCode == "WAG.042" || argDWPCode == "WAG.210" || argDWPCode == "WAG.194" || argDWPCode == "WAG.202"))
                    {
                        ocrRef.OCRValue = "";
                        // MessageBox.Show("Value must be between -99999 and 999999 (xxxxxx.xx)", "SurePrep Express", MessageBoxButtons.OK)
                        return "1015";
                    }
                    else if ((LengthBeforeDecimal > 0 && IntBefore > LengthBeforeDecimal) || (LengthAfterDecimal > 0 && IntAfter > LengthAfterDecimal) && intTaxSoftwareID == 4 && argDWPCode == "GAM.025")
                    {
                        ocrRef.OCRValue = "";
                        // MessageBox.Show("Value must be between -999999 and 9999999 (xxxxxxx.xx)", "SurePrep Express", MessageBoxButtons.OK)
                        return "1015";
                    }
                    else if (argDWPCode == "REN.281" || argDWPCode == "REN.284" || argDWPCode == "REN.275" || argDWPCode == "REN.276" || argDWPCode == "REN.278" || argDWPCode == "REN.488" || argDWPCode == "FHC.017" || argDWPCode == "WAG.042" || argDWPCode == "WAG.210" || argDWPCode == "WAG.194" || argDWPCode == "WAG.202" || argDWPCode == "GAM.025" || (argDWPCode == "REC.012" && !((LengthBeforeDecimal > 0 && IntBefore > LengthBeforeDecimal) || (LengthAfterDecimal > 0 && IntAfter > LengthAfterDecimal))))
                    {
                        ocrRef.OCRValue = theVal;
                    }
                    else
                    {
                        ValueNow = theVal.Replace("-", "");
                        TempValue = ValueNow.Split('.');
                        IntBefore = TempValue[0].Length;
                        if (TempValue.Length > 1)
                        {
                            IntAfter = TempValue[1].Length;
                        }
                        else
                        {
                            IntAfter = 0;
                        }
                        if (theVal.Contains("-") || theVal.Contains(","))
                        {
                            ocrRef.OCRValue = "";
                            // MessageBox.Show("Value must be between 0 and " & StrNumber & " (" & strString & ") ", "SurePrep Express", MessageBoxButtons.OK)
                            return "1015";
                        }
                        else if ((LengthBeforeDecimal > 0 && IntBefore > LengthBeforeDecimal) || (LengthAfterDecimal > 0 && IntAfter > LengthAfterDecimal))
                        {
                            ocrRef.OCRValue = "";
                            // MessageBox.Show("Value must be between 0 and " & StrNumber & " (" & strString & ") ", "SurePrep Express", MessageBoxButtons.OK)
                            return "1015";
                        }
                        else
                        {
                            ocrRef.ValueToUpdate = ocrRef.OCRValue;
                            ocrRef.IsUpdateRequried = (ocrRef.OCRValue != orgVal);
                            ocrRef.OCRValue = FormatAmt(theVal, false); // Modified by jayanta on 19/7/12 to remove roundoff and place formatting
                        }
                    }
                }
                return string.Empty;
            }
            catch (Exception)
            {
                ocrRef.OCRValue = "";
                return "1015";
            }
        }
        #endregion

        private string DoAmountValidation(IOCRReference ocrRef, bool isFaxToTax = false)
        {
            bool ExponetialValConverted = false;
            string theVal = ocrRef.OCRValue.Trim();

            if (theVal == null)
            {
                return string.Empty;
            }
            theVal = theVal.Replace(" ", "");

            if (isFaxToTax)
            {
                if (theVal.Contains("E+") || theVal.Contains("e+") || theVal.Contains("E-") || theVal.Contains("e-"))
                {
                    try
                    {
                        decimal decNum = decimal.Parse(theVal, System.Globalization.NumberStyles.Float);
                        theVal = decNum.ToString();
                        ExponetialValConverted = true;
                    }
                    catch (Exception)
                    {
                        ExponetialValConverted = false;
                        ocrRef.OCRValue = "";
                        return "1001";
                    }
                }

                if (theVal.Length != 0 && theVal.Length <= 9)
                {
                    if (theVal.Length > 0)
                    {
                        if (!IsANumber(theVal))
                        {
                            string DigitExtarctedVal = "";
                            DigitExtarctedVal = Regex.Replace(theVal, "\\D", "");
                            if (DigitExtarctedVal != "")
                            {
                                ocrRef.ValueToUpdate = DigitExtarctedVal;
                                ocrRef.IsUpdateRequried = true;
                                ocrRef.OCRValue = FormatAmt(theVal);
                            }
                            else
                            {
                                ocrRef.OCRValue = "";
                                return "1001";
                            }
                        }
                        else
                        {
                            ocrRef.ValueToUpdate = theVal;
                            ocrRef.IsUpdateRequried = true;
                            ocrRef.OCRValue = FormatAmt(theVal);
                        }
                    }
                }
                else if (theVal.Length > 9)
                {
                    theVal = theVal.Substring(0, 9);
                    ocrRef.ValueToUpdate = theVal;
                    ocrRef.IsUpdateRequried = true;
                    ocrRef.OCRValue = FormatAmt(theVal);
                }
                return string.Empty;
            }

            if (!isFaxToTax && theVal.Length != 0)
            {
                if (!IsANumber(theVal))
                {
                    ocrRef.OCRValue = "";
                    return "1001";
                }
                else
                {
                    if (theVal == "0")
                    {
                        ocrRef.OCRValue = "0";
                    }
                    else
                    {
                        ocrRef.OCRValue = FormatAmt(theVal);
                    }
                }
            }

            theVal = null;
            return string.Empty;
        }

        private string DoPercentValidation(IOCRReference ocrRef, bool isFaxToTax = false)
        {
            string theVal = ocrRef.OCRValue.Trim();

            if (theVal == null)
            {
                return string.Empty;
            }
            theVal = theVal.Replace(" ", "");

            if (isFaxToTax)
            {
                if (theVal.Length != 0)
                {
                    if (!PercentageCheck(theVal))
                    {
                        ocrRef.OCRValue = "";
                        return "1001";
                    }
                    else
                    {
                        ocrRef.OCRValue = theVal;
                        if (theVal.Length > 1)
                        {
                            ocrRef.F2tComment = "Updated by Divided by 100 logic";
                            theVal = Math.Round(double.Parse(theVal) / 100, 4).ToString("0.##########");
                            ocrRef.ValueToUpdate = theVal;
                            ocrRef.IsUpdateRequried = true;
                        }
                        else
                        {
                            ocrRef.OCRValue = theVal;
                        }
                    }
                }
                return string.Empty;
            }

            if (!isFaxToTax && theVal.Length != 0)
            {
                if (!IsANumber(theVal))
                {
                    ocrRef.OCRValue = "";
                    return "1002";
                }
                else if (double.Parse(theVal) == 0 || double.Parse(theVal) < 0 || double.Parse(theVal) > 1)
                {
                    ocrRef.OCRValue = "";
                    return "1003";
                }
                else
                {
                    if (theVal.IndexOf(".") == 0)
                    {
                        theVal = "0" + theVal;
                        ocrRef.OCRValue = theVal;
                    }
                    ocrRef.OCRValue = Math.Round(double.Parse(theVal), 4).ToString("0.##########");
                    ocrRef.OCRValue = string.Format("0.##########", theVal);

                    if (theVal != "")
                    {
                        if (theVal.IndexOf(".") == 0)
                        {
                            theVal = "0" + theVal;
                        }
                        ocrRef.OCRValue = string.Format("0.0#########", theVal);
                    }
                    else
                    {
                        ocrRef.OCRValue = theVal;
                    }
                }
            }

            theVal = null;
            return string.Empty;
        }

        private string DoAmtPrcntValidation(IOCRReference ocrRef, bool isFaxToTax = false)
        {
            string theVal = ocrRef.OCRValue.Trim();
            if (theVal == null)
            {
                return string.Empty;
            }
            theVal = theVal.Replace(" ", "");
            if (isFaxToTax)
            {
                if (theVal.Length != 0)
                {
                    if (!IsANumber(theVal))
                    {
                        ocrRef.OCRValue = "";
                        return "1001";
                    }
                    else
                    {
                        if ((double.Parse(theVal) > 0 && double.Parse(theVal) < 1) || (double.Parse(theVal) < 0 && double.Parse(theVal) > -1))
                        {
                            ocrRef.OCRValue = DoPercentValidation(ocrRef, true);
                        }
                        else
                        {
                            ocrRef.OCRValue = FormatAmt(theVal);
                        }
                    }
                }
                else
                {
                    ocrRef.OCRValue = "";
                    return "1001";
                }
                return string.Empty;
            }

            if (!isFaxToTax && theVal.Length != 0)
            {
                if (!IsANumber(theVal))
                {
                    ocrRef.OCRValue = "";
                    return "1004";
                }
                else
                {
                    if ((double.Parse(theVal) >= 0 && double.Parse(theVal) <= 1) || (double.Parse(theVal) < 0 && double.Parse(theVal) > -1))
                    {
                        string strResult = DoPercentValidation(ocrRef, true);
                        if (strResult != string.Empty)
                        {
                            return strResult;
                        }
                    }
                    else if (double.Parse(theVal) > 1)
                    {
                        theVal = theVal.Replace(" ", "");
                        if (theVal.Length != 0)
                        {
                            if (!IsANumber(theVal))
                            {
                                ocrRef.OCRValue = "";
                                return "1005";
                            }
                            else
                            {
                                if (theVal == "0")
                                {
                                    ocrRef.OCRValue = "0";
                                }
                                else
                                {
                                    ocrRef.OCRValue = FormatAmt(theVal, false);
                                }
                            }
                        }
                    }
                }
            }
            theVal = null;

            return string.Empty;
        }



        private string FormatAmt(string argAmt, bool blnDoRounding = true)
        {
            if (argAmt.Length <= 0)
            {
                return "";
            }
            else
            {
                if (double.TryParse(argAmt, out _))
                {
                    bool negFlag = false;
                    if (argAmt[0] == '-')
                    {
                        negFlag = true;
                    }
                    if (blnDoRounding)
                    {
                        argAmt = string.Format("{0:C0}", double.Parse(argAmt));
                    }
                    else
                    {
                        int intDigits;
                        if (argAmt.Contains("."))
                        {
                            intDigits = argAmt.Length - argAmt.IndexOf(".");
                        }
                        else
                        {
                            intDigits = 0;
                        }
                        argAmt = string.Format("{0:C" + intDigits + "}", double.Parse(argAmt));
                    }
                    if (argAmt == "$0")
                    {
                        return "0";
                    }
                    else
                    {
                        if (!negFlag)
                        {
                            return argAmt.Substring(1);
                        }
                        else
                        {
                            argAmt = argAmt.Substring(2);
                            return "-" + argAmt;
                        }
                    }
                }
                else
                {
                    return "";
                }
            }
        }

        private bool IsANumber(string argAmt)
        {
            if (argAmt.Length <= 0)
            {
                return false;
            }
            else
            {
                foreach (char chTemp in argAmt)
                {
                    if ((chTemp < '0' || chTemp > '9') && chTemp != '-' && chTemp != ',' && chTemp != '.')
                    {
                        return false;
                    }
                }
                return double.TryParse(argAmt, out _);
            }
        }

        private string IsValidTEL(IOCRReference ocrRef)
        {
            string tellVal = "";
            string orgVal = ocrRef.OCRValue;
            if (!string.IsNullOrEmpty(ocrRef.OCRValue))
            {
                tellVal = ocrRef.OCRValue.Trim();
                if (Regex.IsMatch(tellVal, @"\d{3}-\d{3}-\d{4}$") && tellVal.Length == 12)
                {
                    ocrRef.ValueToUpdate = tellVal;
                }
                else if (double.TryParse(tellVal, out _) && tellVal.Length == 10)
                {
                    ocrRef.OCRValue = tellVal.Substring(0, 3) + "-" + tellVal.Substring(3, 3) + "-" + tellVal.Substring(6, 4);
                    ocrRef.ValueToUpdate = tellVal;
                }
                else
                {
                    ocrRef.OCRValue = "";
                    return "1006";
                }
            }
            ocrRef.IsUpdateRequried = (orgVal != ocrRef.OCRValue);
            return string.Empty;
        }

        private string DoSSNValidation(IOCRReference ocrRef, bool isFaxToTax = false)
        {
            string ssnVal = "";
            if (ocrRef.OCRValueVerifiedvalue.Trim().Length >= 9)
            {
                ssnVal = ocrRef.OCRValueVerifiedvalue.Trim().Replace(" ", "-");
                ocrRef.OCRValue = ocrRef.OCRValueVerifiedvalue;
            }
            else
            {
                ssnVal = ocrRef.OCRValue.Trim().Replace(" ", "-");
            }
            if (ssnVal.Length != 0)
            {
                if (double.TryParse(ssnVal, out _) && ssnVal.Length == 9)
                {
                    ssnVal = ssnVal.Substring(0, 3) + "-" + ssnVal.Substring(3, 2) + "-" + ssnVal.Substring(5, 4);
                }
                if (IsValidSSN(ssnVal))
                {
                    ocrRef.ValueToUpdate = ssnVal;
                }
                else if (ssnVal == ".")
                {
                    ocrRef.ValueToUpdate = ssnVal;
                }
                else
                {
                    if (ssnVal.Length >= 4)
                    {
                        if (!double.TryParse(ssnVal.Substring(ssnVal.Length - 4), out _))
                        {
                            ocrRef.OCRValue = ".";
                        }
                    }
                    else
                    {
                        ocrRef.OCRValue = ".";
                        return "1007";
                    }
                }
            }
            if (ocrRef.OCRValue != ssnVal)
            {
                ocrRef.OCRValue = ssnVal;
                ocrRef.IsUpdateRequried = true;
            }
            return string.Empty;
        }

        private bool IsValidSSN(string argVal)
        {
            return Regex.IsMatch(argVal, @"\d{3}-\d{2}-\d{4}$");
        }

        private bool IsValidEIN(string argVal)
        {
            return Regex.IsMatch(argVal, @"\d{2}-\d{7}$");
        }

        private string DoSSNEINValidation(IOCRReference ocrRef, bool isFaxToTax = false)
        {
            string ssnVal = "";
            if (ocrRef.OCRValueVerifiedvalue.Trim().Length >= 9)
            {
                ssnVal = ocrRef.OCRValueVerifiedvalue.Trim().Replace(" ", "-");
                ocrRef.OCRValue = ocrRef.OCRValueVerifiedvalue;
            }
            else
            {
                ssnVal = ocrRef.OCRValue.Trim().Replace(" ", "-");
            }

            if (!string.IsNullOrEmpty(ocrRef.OCRValue))
            {
                if (Regex.IsMatch(ssnVal, @"\d{3}-\d{2}-\d{4}$") && ssnVal.Length == 11)
                {
                    ocrRef.ValueToUpdate = ssnVal;
                }
                else if (Regex.IsMatch(ssnVal, @"\d{2}-\d{7}$") && ssnVal.Length == 10)
                {
                    ocrRef.ValueToUpdate = ssnVal;
                }
                else if (double.TryParse(ssnVal, out _) && ssnVal.Length == 9)
                {
                    ssnVal = ssnVal.Substring(0, 3) + "-" + ssnVal.Substring(3, 2) + "-" + ssnVal.Substring(5, 4);
                    ocrRef.ValueToUpdate = ssnVal;
                }
                else if (ssnVal == ".")
                {
                    ocrRef.ValueToUpdate = ssnVal;
                }
                else
                {
                    if (ssnVal.Length >= 4)
                    {
                        if (!double.TryParse(ssnVal.Substring(ssnVal.Length - 4), out _))
                        {
                            ocrRef.OCRValue = ".";
                        }
                    }
                    else
                    {
                        ocrRef.OCRValue = ".";
                        return "1008";
                    }
                }
                if (ocrRef.OCRValue != ssnVal)
                {
                    ocrRef.OCRValue = ssnVal;
                    ocrRef.IsUpdateRequried = true;
                }
            }
            return string.Empty;
        }

        private string DoEINValidation(IOCRReference ocrRef, bool isFaxToTax = false)
        {
            string ssnVal = "";
            if (ocrRef.OCRValueVerifiedvalue.Trim().Length >= 9)
            {
                ssnVal = ocrRef.OCRValueVerifiedvalue.Trim().Replace(" ", "-");
                ocrRef.OCRValue = ocrRef.OCRValueVerifiedvalue;
            }
            else
            {
                ssnVal = ocrRef.OCRValue.Trim().Replace(" ", "-");
            }

            if (ssnVal.Length != 0)
            {
                if (double.TryParse(ssnVal, out _) && ssnVal.Length == 9)
                {
                    ssnVal = ssnVal.Substring(0, 2) + "-" + ssnVal.Substring(2, 7);
                    ocrRef.ValueToUpdate = ssnVal;
                }
                if (IsValidEIN(ssnVal))
                {
                    ocrRef.ValueToUpdate = ssnVal;
                }
                else if (ssnVal == ".")
                {
                    ocrRef.ValueToUpdate = ssnVal;
                }
                else if (ssnVal.Length >= 4)
                {
                    if (!double.TryParse(ssnVal.Substring(ssnVal.Length - 4), out _))
                    {
                        ocrRef.OCRValue = ".";
                    }
                }
                else
                {
                    return "1009";
                }
            }
            if (ocrRef.OCRValue != ssnVal)
            {
                ocrRef.OCRValue = ssnVal;
                ocrRef.IsUpdateRequried = true;
            }
            return string.Empty;
        }

        private string DoDateValidation(IOCRReference ocrRef, int taxSoftwareId, int taxYear)
        {
            try
            {
                string sender = ocrRef.OCRValue.Trim();
                bool invalidDate = false;
                if (DateOnly.TryParse(sender, out _))
                {
                    sender = DateOnly.Parse(sender).ToString();
                }
                else if (!string.IsNullOrWhiteSpace(sender) && !double.TryParse(sender, out _))
                {
                    if (sender == "99/99/9999" || sender.ToUpper() == "VAR" || sender.ToUpper() == "VARIOUS")
                    {
                        invalidDate = false;
                    }
                    else
                    {
                        invalidDate = true;
                    }
                }
                string dtVal = sender;

                string errCode = SkipVariousDate(taxSoftwareId, ocrRef.FaxDWPCode, dtVal);
                if (invalidDate)
                {
                    if (errCode != "") return errCode;
                    return "1010";
                }
                if (dtVal.Length != 0)
                {
                    if (ocrRef.AcceptVarious && (dtVal == "99999999" || dtVal == "99/99/9999"))
                    {
                        if (errCode != "") return errCode;
                        dtVal = AutoConVarDate(dtVal, taxSoftwareId, taxYear);
                        ocrRef.OCRValue = dtVal;
                        ocrRef.ValueToUpdate = dtVal;
                        return string.Empty;
                    }
                    else if (ocrRef.AcceptVarious && (dtVal.ToUpper() == "VAR" || dtVal.ToUpper() == "VARIOUS"))
                    {
                        if (errCode != "") return errCode;
                        dtVal = AutoConVarDate(dtVal, taxSoftwareId, taxYear);
                        ocrRef.OCRValue = dtVal;
                        ocrRef.ValueToUpdate = dtVal;
                        return string.Empty;
                    }
                    if (taxSoftwareId == (int)BinderEnums.TaxSoftware.Lacerte)
                    {
                        if (double.TryParse(dtVal, out _))
                        {
                            if (dtVal.Length == 9 && dtVal[0] == '-')
                            {
                                dtVal = dtVal.Substring(0, 3) + "/" + dtVal.Substring(3, 2) + "/" + dtVal.Substring(5, 4);
                            }
                            else if (dtVal.Length == 8 && dtVal[0] != '-')
                            {
                                dtVal = dtVal.Substring(0, 2) + "/" + dtVal.Substring(2, 2) + "/" + dtVal.Substring(4, 4);
                            }
                        }
                        else
                        {
                            if (dtVal[0] == '-')
                            {
                                string strTemp = dtVal.Substring(1, dtVal.Length - 1);
                                dtVal = "-" + PadDateMonth(strTemp);
                            }
                            else
                            {
                                dtVal = PadDateMonth(dtVal);
                            }
                        }
                    }
                    else
                    {
                        if (double.TryParse(dtVal, out _))
                        {
                            if (dtVal.Length == 8)
                            {
                                dtVal = dtVal.Substring(0, 2) + "/" + dtVal.Substring(2, 2) + "/" + dtVal.Substring(4, 4);
                            }
                            else if (dtVal.Length == 6)
                            {
                                dtVal = dtVal.Substring(0, 2) + "/" + dtVal.Substring(2, 2) + "/" + dtVal.Substring(4, 2);
                                if (DateOnly.TryParse(dtVal, out _))
                                {
                                    int MyIntYear = DateOnly.Parse(dtVal).Year;
                                    dtVal = dtVal.Substring(0, 2) + "/" + dtVal.Substring(3, 2) + "/" + MyIntYear;
                                }
                            }
                        }
                        else
                        {
                            dtVal = PadDateMonth(dtVal);
                            if (taxSoftwareId == 1)
                            {
                                dtVal = dtVal.Replace("-", "/");
                                if (dtVal.Length == 8 && DateOnly.TryParse(dtVal, out _))
                                {
                                    if (dtVal.IndexOf("/") > -1 && dtVal.IndexOf("/") != dtVal.LastIndexOf("/"))
                                    {
                                        int M = DateOnly.Parse(dtVal).Month;
                                        int D = DateOnly.Parse(dtVal).Day;
                                        dtVal = (M.ToString().Length <= 1 ? "0" + M.ToString() : M.ToString()) + "/" + (D.ToString().Length <= 1 ? "0" + D.ToString() : D.ToString()) + "/" + DateTime.Parse(dtVal).Year.ToString();
                                    }
                                }
                            }
                        }
                    }
                    if (!CheckVarDate(dtVal, taxSoftwareId, ocrRef.FaxDWPCode, ocrRef.AcceptVarious))
                    {
                        if (!IsValidStringDate(dtVal))
                        {
                            if (errCode != "") return errCode;
                            return "1010";
                        }
                        else
                        {
                            if (!DateOnly.TryParse(ocrRef.OCRValue, out _))
                            {
                                ocrRef.OCRValue = dtVal;
                                ocrRef.ValueToUpdate = dtVal;
                            }
                        }
                    }
                    else
                    {
                        if (dtVal.ToUpper() == "VAR" && ocrRef.AcceptVarious)
                        {
                            dtVal = "VAR";
                        }
                        if (dtVal.ToUpper() == "VARIOUS" && ocrRef.AcceptVarious)
                        {
                            dtVal = "VARIOUS";
                        }
                        if (dtVal.ToUpper() == "MULTIPLE" && ocrRef.AcceptVarious)
                        {
                            dtVal = "MULTIPLE";
                        }
                        if (dtVal.ToUpper() == "UNAVAILABLE" && ocrRef.AcceptVarious)
                        {
                            dtVal = "UNAVAILABLE";
                        }
                        if (dtVal.ToUpper() == "UNKNOWN" && ocrRef.AcceptVarious)
                        {
                            dtVal = "UNKNOWN";
                        }
                        if (ocrRef.OCRValue != dtVal)
                        {
                            ocrRef.ValueToUpdate = dtVal;
                        }
                        ocrRef.OCRValue = dtVal;
                    }
                }
                dtVal = null;

                return string.Empty;
            }
            catch (Exception)
            {
                return "1010";
            }
        }

        private string SkipVariousDate(int intTaxSoftwareID, string dwpCd, string dateValue)
        {
            try
            {
                if (intTaxSoftwareID == (int)BinderEnums.TaxSoftware.GoSystem)
                {
                    if ("CGL.004,CON.040,OGC.004".Contains(dwpCd))
                    {
                        return "1017";
                    }
                    else if ("OGC.005,CON.041,CGL.005".Contains(dwpCd))
                    {
                        return "1018";
                    }
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        private bool CheckVarDate(string argDate, int intTaxSoftwareID, string argDWPCode, bool blnAcceptVAR)
        {
            if (argDate.Length != 0)
            {
                if (argDate.ToUpper() == "VAR" && intTaxSoftwareID == 1 && blnAcceptVAR)
                {
                    return true;
                }
                else if (argDate == "99/99/9999" && (intTaxSoftwareID == 3 || intTaxSoftwareID == 6) && blnAcceptVAR)
                {
                    return true;
                }
                else if (argDate.ToUpper() == "VARIOUS" && intTaxSoftwareID == 4 && blnAcceptVAR)
                {
                    return true;
                }
                else if (argDate.ToUpper() == "VARIOUS" && intTaxSoftwareID == 5 && blnAcceptVAR)
                {
                    return true;
                }
                else if (argDate[0] == '-' && intTaxSoftwareID == 2)
                {
                    if (double.TryParse(argDate, out _))
                    {
                        if (argDate.Length == 9 && argDate[0] == '-')
                        {
                            argDate = argDate.Substring(0, 3) + "/" + argDate.Substring(3, 2) + "/" + argDate.Substring(5, 4);
                        }
                    }
                    else
                    {
                        argDate = PadDateMonth(argDate);
                    }
                    string strVal = argDate.Substring(1, argDate.Length - 1);

                    string[] strDate = strVal.Split(new char[] { '\\', '/', '-', '.', ':' });
                    if (strDate.Length > 2)
                    {
                        return IsValidStringDate(strVal);
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (argDate[0] != '-' && intTaxSoftwareID == 2)
                {
                    string strVal = argDate.Substring(0, argDate.Length);

                    string[] strDate = strVal.Split(new char[] { '\\', '/', '-', '.', ':' });
                    if (strDate.Length > 2)
                    {
                        return IsValidStringDate(strVal);
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (argDate.ToUpper() == "VARIOUS" && (intTaxSoftwareID == 4 || intTaxSoftwareID == 5) && blnAcceptVAR)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        private string PadDateMonth(string argVal)
        {
            string strVal = argVal;
            if (argVal.Length <= 9 && argVal.IndexOf("/") > -1 && (argVal.IndexOf("/") != argVal.LastIndexOf("/")) && IsValidStringDate(argVal))
            {
                string[] arrDate = argVal.Split('/');
                for (int intDateCount = 0; intDateCount < arrDate.Length; intDateCount++)
                {
                    if (arrDate[intDateCount].Length == 1)
                    {
                        arrDate[intDateCount] = "0" + arrDate[intDateCount];
                    }
                }
                strVal = arrDate[0] + "/" + arrDate[1] + "/" + arrDate[2];
            }
            return strVal;
        }

        private bool IsValidStringDate(string strVal)
        {
            try
            {
                string tempdate;
                if (!Regex.IsMatch(strVal, "(0[1-9]|1[012])[- /.:](0[1-9]|[12][0-9]|3[01])[- /.:](19|20)\\d\\d|((\\d{2}))"))
                {
                    if (Regex.IsMatch(strVal, "^\\d{1,2}(\\/|-)\\d{1,2}\\1\\d{2}$"))
                    {
                        string pattern = "^((0?[13578]|10|12)(-|\\/)(([1-9])|(0[1-9])|([12])([0-9]?)|(3[01]?))(-|\\/)((19)([2-9])(\\d{1})|(20)([01])(\\d{1})|([8901])(\\d{1}))|(0?[2469]|11)(-|\\/)(([1-9])|(0[1-9])|([12])([0-9]?)|(3[0]?))(-|\\/)((19)([2-9])(\\d{1})|(20)([01])(\\d{1})|([8901])(\\d{1})))$";
                        if (Regex.IsMatch(strVal, pattern))
                        {
                            int m = DateOnly.Parse(strVal).Month;
                            int d = DateOnly.Parse(strVal).Day;
                            int y = DateOnly.Parse(strVal).Year;
                            if (m == 2 && y % 4 == 0 && d >= 30)
                            {
                                return false;
                            }
                            else if (m == 2 && y % 4 != 0 && d >= 29)
                            {
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                }
                else
                {
                    tempdate = strVal.Replace(".", "/").Replace(":", "/").Replace("-", "/");
                    if (DateOnly.TryParse(tempdate, out _))
                    {
                        // left blank
                    }
                    else
                    {
                        return false;
                    }
                    if (DateOnly.TryParse(strVal, out _))
                    {
                        int strYear = DateOnly.Parse(strVal).Year;
                        if (strYear > 2)
                        {
                            if (strYear < 1900 || strYear > 3000)
                            {
                                return false;
                            }
                            else
                            {
                                if (Regex.IsMatch(strVal, "(0[1-9]|1[012])[- /.:](0[1-9]|[12][0-9]|3[01])[- /.:](19|20)\\d\\d|((\\d{2}))"))
                                {
                                    string[] strDate = strVal.Split(new char[] { '\\', '/', '-', '.', ':' });
                                    if (strDate.Length > 2) goto lend0;
                                    lend0:
                                    if (char.IsNumber(strDate[0][0]) && char.IsNumber(strDate[1][0]) && char.IsNumber(strDate[2][0]) && strDate[2].Length < 5)
                                    {
                                        if (strDate[2].Length > 2)
                                        {
                                            if (int.Parse(strDate[0]) > 12 || int.Parse(strDate[0]) == 0 || int.Parse(strDate[1]) > 31 || int.Parse(strDate[1]) == 0 || int.Parse(strDate[2]) == 0 || int.Parse(strDate[2]) < 1900 || int.Parse(strDate[2]) > 3000)
                                            {
                                                return false;
                                            }
                                            else if (char.IsDigit(strDate[0][0]) && char.IsDigit(strDate[1][0]) && char.IsDigit(strDate[2][0]))
                                            {
                                                return true;
                                            }
                                            else
                                            {
                                                return false;
                                            }
                                        }
                                        else
                                        {
                                            if (char.IsDigit(strDate[0][0]) && char.IsDigit(strDate[1][0]) && char.IsDigit(strDate[2][0]))
                                            {
                                                if (int.Parse(strDate[0]) > 12 || int.Parse(strDate[0]) == 0 || int.Parse(strDate[1]) > 31 || int.Parse(strDate[1]) == 0)
                                                {
                                                    return false;
                                                }
                                                else if (char.IsDigit(strDate[0][0]) && char.IsDigit(strDate[1][0]) && char.IsDigit(strDate[2][0]))
                                                {
                                                    return true;
                                                }
                                                else
                                                {
                                                    return false;
                                                }
                                            }
                                            else
                                            {
                                                return false;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        else if (Regex.IsMatch(strVal, "(0[1-9]|1[012])[- /.:](0[1-9]|[12][0-9]|3[01])[- /.:](19|20)\\d\\d|((\\d{2}))"))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (Regex.IsMatch(strVal, "(0[1-9]|1[012])[- /.:](0[1-9]|[12][0-9]|3[01])[- /.:](19|20)\\d\\d|((\\d{2}))"))
                    {
                        tempdate = strVal.Replace(".", "/").Replace(":", "/").Replace("-", "/");
                        if (DateOnly.TryParse(tempdate, out _))
                        {
                            // left blank
                        }
                        else
                        {
                            return false;
                        }
                        string[] strDate = strVal.Split(new char[] { '\\', '/', '-', '.', ':' });
                        if (strDate.Length > 2) goto lend;
                        lend:
                        if (char.IsNumber(strDate[0][0]) && char.IsNumber(strDate[1][0]) && char.IsNumber(strDate[2][0]) && strDate[2].Length < 5)
                        {
                            if (strDate[2].Length > 2)
                            {
                                if (int.Parse(strDate[0]) > 12 || int.Parse(strDate[0]) == 0 || int.Parse(strDate[1]) > 31 || int.Parse(strDate[1]) == 0 || int.Parse(strDate[2]) == 0 || int.Parse(strDate[2]) < 1900 || int.Parse(strDate[2]) > 3000)
                                {
                                    return false;
                                }
                                else if (char.IsDigit(strDate[0][0]) && char.IsDigit(strDate[1][0]) && char.IsDigit(strDate[2][0]))
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                if (char.IsDigit(strDate[0][0]) && char.IsDigit(strDate[1][0]) && char.IsDigit(strDate[2][0]))
                                {
                                    if (int.Parse(strDate[0]) > 12 || int.Parse(strDate[0]) == 0 || int.Parse(strDate[1]) > 31 || int.Parse(strDate[1]) == 0)
                                    {
                                        return false;
                                    }
                                    else if (char.IsDigit(strDate[0][0]) && char.IsDigit(strDate[1][0]) && char.IsDigit(strDate[2][0]))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string AutoConVarDate(string argdtVal, int intTaxSoftwareID, int intTaxYear)
        {
            switch (intTaxSoftwareID)
            {
                case (int)BinderEnums.TaxSoftware.GoSystem:
                    return "VAR";
                case (int)BinderEnums.TaxSoftware.Lacerte:
                    return "-12/31/" + intTaxYear;
                case (int)BinderEnums.TaxSoftware.ProSystem:
                    return "99/99/9999";
                case (int)BinderEnums.TaxSoftware.GlobalFx:
                    return "99/99/9999";
                case (int)BinderEnums.TaxSoftware.UltraTax:
                    return "Various";
                case (int)BinderEnums.TaxSoftware.ProSeries:
                    return "Various";
                default:
                    return null;
            }
        }

        private string DoDateMMYYYYValidation(IOCRReference ocrRef, int taxSoftwareId, int taxYear)
        {
            try
            {
                string dtVal = ocrRef.OCRValue.Trim();
                if (dtVal.Length != 0)
                {
                    if (ocrRef.AcceptVarious && (dtVal == "99999999" || dtVal == "99/99/99"))
                    {
                        dtVal = AutoConVarDateMMYYYY(dtVal, taxSoftwareId, taxYear);
                        ocrRef.OCRValue = dtVal;
                        ocrRef.ValueToUpdate = dtVal;
                        return string.Empty;
                    }
                    else if (ocrRef.AcceptVarious && (dtVal.ToUpper() == "VAR" || dtVal.ToUpper() == "VARIOUS"))
                    {
                        dtVal = AutoConVarDateMMYYYY(dtVal, taxSoftwareId, taxYear);
                        ocrRef.OCRValue = dtVal;
                        ocrRef.ValueToUpdate = dtVal;
                        return string.Empty;
                    }
                    if (dtVal.Length >= 8 && DateOnly.TryParse(dtVal, out _))
                    {
                        dtVal = DateOnly.Parse(dtVal).ToString("MM/yyyy");
                    }
                    if (taxSoftwareId == (int)BinderEnums.TaxSoftware.Lacerte)
                    {
                        if (double.TryParse(dtVal, out _))
                        {
                            if (dtVal.Length == 7 && dtVal[0] == '-')
                            {
                                dtVal = dtVal.Substring(0, 3) + "/" + dtVal.Substring(3, 4);
                            }
                            else if (dtVal.Length == 6 && dtVal[0] != '-')
                            {
                                dtVal = dtVal.Substring(0, 2) + "/" + dtVal.Substring(2, 4);
                            }
                        }
                        else
                        {
                            if (dtVal[0] == '-')
                            {
                                string strTemp = dtVal.Substring(1, dtVal.Length - 1);
                                dtVal = "-" + PadMMYYYY(strTemp);
                            }
                            else
                            {
                                dtVal = PadMMYYYY(dtVal);
                            }
                        }
                    }
                    else
                    {
                        if (double.TryParse(dtVal, out _))
                        {
                            if (dtVal.Length == 6)
                            {
                                dtVal = dtVal.Substring(0, 2) + "/" + dtVal.Substring(2, 4);
                            }
                        }
                        else
                        {
                            dtVal = PadMMYYYY(dtVal);
                        }
                    }
                    if (!CheckVarDateMMYYYY(dtVal, taxSoftwareId, ocrRef.FaxDWPCode, ocrRef.AcceptVarious))
                    {
                        if (!CheckMMYYYYDate(dtVal))
                        {
                            return "1010";
                        }
                        else
                        {
                            ocrRef.OCRValue = dtVal;
                            ocrRef.ValueToUpdate = dtVal;
                        }
                    }
                    else
                    {
                        if (dtVal.ToUpper() == "VAR" && ocrRef.AcceptVarious)
                        {
                            dtVal = "VAR";
                        }
                        if (dtVal.ToUpper() == "VARIOUS" && ocrRef.AcceptVarious)
                        {
                            dtVal = "VARIOUS";
                        }
                        if (dtVal.ToUpper() == "MULTIPLE" && ocrRef.AcceptVarious)
                        {
                            dtVal = "MULTIPLE";
                        }
                        if (dtVal.ToUpper() == "UNAVAILABLE" && ocrRef.AcceptVarious)
                        {
                            dtVal = "UNAVAILABLE";
                        }
                        if (dtVal.ToUpper() == "UNKNOWN" && ocrRef.AcceptVarious)
                        {
                            dtVal = "UNKNOWN";
                        }
                        ocrRef.OCRValue = dtVal;
                        ocrRef.ValueToUpdate = dtVal;
                    }
                }
                dtVal = null;

                return string.Empty;
            }
            catch (Exception)
            {
                return "1010";
            }
        }
        private string DoDateMMDDValidation(IOCRReference ocrRef, int taxsoftwareId, int taxYear)
        {
            try
            {
                string dtVal = ocrRef.OCRValue.Trim();
                if (dtVal.Length != 0)
                {
                    if (ocrRef.AcceptVarious && (dtVal == "9999" || dtVal == "99/99"))
                    {
                        dtVal = AutoConVarDateMMYY(dtVal, taxsoftwareId, taxYear);
                    }

                    if (dtVal.Length >= 8 && DateOnly.TryParse(dtVal, out _))
                    {
                        dtVal = DateOnly.Parse(dtVal).ToString("MM/dd");
                    }

                    if (double.TryParse(dtVal, out _))
                    {
                        if (dtVal.Length == 4)
                        {
                            dtVal = dtVal.Substring(0, 2) + "/" + dtVal.Substring(2, 2);
                        }
                    }
                    else
                    {
                        dtVal = PadMMYY(dtVal);
                    }

                    if (!CheckVarDateMMYY(dtVal, taxsoftwareId, ocrRef.FaxDWPCode, ocrRef.AcceptVarious))
                    {
                        if (!CheckMMYYDate(dtVal, "DD"))
                        {
                            return "1010";
                        }
                        else
                        {
                            ocrRef.OCRValue = dtVal;
                            ocrRef.ValueToUpdate = dtVal;
                        }
                    }
                    else
                    {
                        if (dtVal.ToUpper() == "VAR" && ocrRef.AcceptVarious)
                        {
                            dtVal = "VAR";
                        }
                        if (dtVal.ToUpper() == "VARIOUS" && ocrRef.AcceptVarious)
                        {
                            dtVal = "VARIOUS";
                        }
                        if (dtVal.ToUpper() == "MULTIPLE" && ocrRef.AcceptVarious)
                        {
                            dtVal = "MULTIPLE";
                        }
                        if (dtVal.ToUpper() == "UNAVAILABLE" && ocrRef.AcceptVarious)
                        {
                            dtVal = "UNAVAILABLE";
                        }
                        if (dtVal.ToUpper() == "UNKNOWN" && ocrRef.AcceptVarious)
                        {
                            dtVal = "UNKNOWN";
                        }
                        ocrRef.OCRValue = dtVal;
                        ocrRef.ValueToUpdate = dtVal;
                    }
                }
                dtVal = null;
                return string.Empty;
            }
            catch (Exception)
            {
                return "1010";
            }
        }

        private string DoDateMMYYValidation(IOCRReference ocrRef, int taxsoftwareId, int taxYear)
        {
            try
            {
                string dtVal = ocrRef.OCRValue.Trim();
                if (dtVal.Length != 0)
                {
                    if (ocrRef.AcceptVarious && (dtVal == "9999" || dtVal == "99/99"))
                    {
                        dtVal = AutoConVarDateMMYY(dtVal, taxsoftwareId, taxYear);
                    }
                    if (dtVal.Length >= 8 && DateOnly.TryParse(dtVal, out _))
                    {
                        dtVal = DateOnly.Parse(dtVal).ToString("MM/yy");
                    }
                    if (taxsoftwareId == 2)
                    {
                        if (double.TryParse(dtVal, out _))
                        {
                            if (dtVal.Length == 5 && dtVal[0] == '-')
                            {
                                dtVal = dtVal.Substring(0, 3) + "/" + dtVal.Substring(3, 2);
                            }
                            else if (dtVal.Length == 4 && dtVal[0] != '-')
                            {
                                dtVal = dtVal.Substring(0, 2) + "/" + dtVal.Substring(2, 2);
                            }
                        }
                        else
                        {
                            if (dtVal[0] == '-')
                            {
                                string strTemp = dtVal.Substring(1, dtVal.Length - 1);
                                dtVal = "-" + PadMMYY(strTemp);
                            }
                            else
                            {
                                dtVal = PadMMYY(dtVal);
                            }
                        }
                    }
                    else
                    {
                        if (double.TryParse(dtVal, out _))
                        {
                            if (dtVal.Length == 4)
                            {
                                dtVal = dtVal.Substring(0, 2) + "/" + dtVal.Substring(2, 2);
                            }
                        }
                        else
                        {
                            dtVal = PadMMYY(dtVal);
                        }
                    }
                    if (!CheckVarDateMMYY(dtVal, taxsoftwareId, ocrRef.FaxDWPCode, ocrRef.AcceptVarious))
                    {
                        if (!CheckMMYYDate(dtVal, "YY"))
                        {
                            return "1010";
                        }
                        else
                        {
                            ocrRef.OCRValue = dtVal;
                            ocrRef.ValueToUpdate = dtVal;
                        }
                    }
                    else
                    {
                        if (dtVal.ToUpper() == "VAR" && ocrRef.AcceptVarious)
                        {
                            dtVal = "VAR";
                        }
                        if (dtVal.ToUpper() == "VARIOUS" && ocrRef.AcceptVarious)
                        {
                            dtVal = "VARIOUS";
                        }
                        if (dtVal.ToUpper() == "MULTIPLE" && ocrRef.AcceptVarious)
                        {
                            dtVal = "MULTIPLE";
                        }
                        if (dtVal.ToUpper() == "UNAVAILABLE" && ocrRef.AcceptVarious)
                        {
                            dtVal = "UNAVAILABLE";
                        }
                        if (dtVal.ToUpper() == "UNKNOWN" && ocrRef.AcceptVarious)
                        {
                            dtVal = "UNKNOWN";
                        }
                        ocrRef.OCRValue = dtVal;
                        ocrRef.ValueToUpdate = dtVal;
                    }
                }
                dtVal = null;
                return string.Empty;
            }
            catch (Exception)
            {
                return "1010";
            }
        }

        private string AutoConVarDateMMYYYY(string argdtVal, int intTaxSoftwareID, int intTaxYear)
        {
            switch (intTaxSoftwareID)
            {
                case 1:
                    return "VAR";
                case 2:
                    return "-12/" + intTaxYear;
                case 3:
                case 6:
                    return "99/9999";
                case 4:
                    return "Various";
                case 5:
                    return "Various";
                default:
                    return null;
            }
        }

        private string AutoConVarDateMMYY(string argdtVal, int intTaxSoftwareID, int intTaxYear)
        {
            switch (intTaxSoftwareID)
            {
                case 1:
                    return "VAR";
                case 2:
                    return "-12/" + intTaxYear.ToString().Substring(2, 2);
                case 3:
                case 6:
                    return "99/99";
                case 4:
                    return "Various";
                case 5:
                    return "Various";
                default:
                    return null;
            }
        }

        private bool CheckMMYYYYDate(string argVal)
        {
            if (argVal.Length != 7)
            {
                return false;
            }
            else
            {
                string strOne = argVal.Substring(0, 2);
                string strTwo = argVal.Substring(2, 1);
                string strThree = argVal.Substring(3, 4);
                if (IsValidStringDate(strOne + strTwo + "01" + strTwo + strThree))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool CheckMMYYDate(string argVal, string strType)
        {
            if (argVal.Length != 5)
            {
                return false;
            }
            else
            {
                string strOne = argVal.Substring(0, 2);
                string strTwo = argVal.Substring(2, 1);
                string strThree = argVal.Substring(3, 2);

                switch (strType)
                {
                    case "YY":
                        if (DateOnly.TryParse("#" + strOne + strTwo + "01" + strTwo + strThree + "#", out _))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case "DD":
                        if (IsValidStringDate(strOne + strTwo + strThree + strTwo + DateTime.Now.Year.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                }
            }
            return true;
        }

        private bool CheckVarDateMMYYYY(string argDate, int intTaxSoftwareID, string argDWPCode, bool blnAcceptVAR)
        {
            if (argDate.Length != 0)
            {
                if (argDate.ToUpper() == "VAR" && intTaxSoftwareID == 1 && blnAcceptVAR)
                {
                    return true;
                }
                else if (argDate == "99/9999" && (intTaxSoftwareID == 3 || intTaxSoftwareID == 6) && blnAcceptVAR)
                {
                    return true;
                }
                else if (intTaxSoftwareID == 2)
                {
                    if (argDate[0] == '-')
                    {
                        if (double.TryParse(argDate, out _))
                        {
                            if (argDate.Length == 7 && argDate[0] == '-')
                            {
                                argDate = argDate.Substring(0, 2) + "/" + argDate.Substring(2, 4);
                            }
                        }
                        else
                        {
                            argDate = PadMMYYYY(argDate);
                        }
                        string strVal = argDate.Substring(1, argDate.Length - 1);
                        if (CheckMMYYYYDate(strVal))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                }
                else if (argDate.ToUpper() == "VARIOUS" && intTaxSoftwareID == 4 && blnAcceptVAR)
                {
                    return true;
                }
                else if (argDate.ToUpper() == "VARIOUS" && intTaxSoftwareID == 5 && blnAcceptVAR)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        private bool CheckVarDateMMYY(string argDate, int intTaxSoftwareID, string argDWPCode, bool blnAcceptVAR)
        {
            if (argDate.Length != 0)
            {
                if (argDate.ToUpper() == "VAR" && intTaxSoftwareID == 1 && blnAcceptVAR)
                {
                    return true;
                }
                else if (argDate == "99/99" && (intTaxSoftwareID == 3 || intTaxSoftwareID == 6) && blnAcceptVAR)
                {
                    return true;
                }
                else if (intTaxSoftwareID == 2)
                {
                    if (argDate[0] == '-')
                    {
                        if (double.TryParse(argDate, out _))
                        {
                            if (argDate.Length == 5 && argDate[0] == '-')
                            {
                                argDate = argDate.Substring(0, 2) + "/" + argDate.Substring(2, 2);
                            }
                        }
                        else
                        {
                            argDate = PadMMYY(argDate);
                        }
                        string strVal = argDate.Substring(1, argDate.Length - 1);
                        if (CheckMMYYDate(strVal, "YY"))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                }
                else if (argDate.ToUpper() == "VARIOUS" && intTaxSoftwareID == 4 && blnAcceptVAR)
                {
                    return true;
                }
                else if (argDate.ToUpper() == "VARIOUS" && intTaxSoftwareID == 5 && blnAcceptVAR)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private string DoDropDownValidation(ref string sender, string DWPCode, DataView argDataView)
        {
            try
            {
                sender = sender.Trim();
                string strValue = sender;
                strValue = strValue.Replace("'", "''");
                if (strValue.Length != 0)
                {
                    DataView dvDataView = argDataView;
                    dvDataView.RowFilter = "ParameterID='" + DWPCode + "' And (ParameterDetailValue='" + strValue.ToUpper() + "' or PWCDisplayName='" + strValue.ToUpper() + "')";
                    if (dvDataView.Count <= 0)
                    {
                        sender = "";
                        return "1011";
                    }
                    else
                    {
                        sender = dvDataView[0]["ParameterDetailValueStateAbbrivation"].ToString();
                        if (dvDataView[0]["PWCDisplayName"].ToString() == "")
                        {
                            sender = dvDataView[0]["ParameterDetailvalue"].ToString();
                        }
                        else
                        {
                            sender = dvDataView[0]["PWCDisplayName"].ToString();
                        }
                    }
                    dvDataView = null;
                }
                strValue = null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return string.Empty;
        }

        private string DoDescValidation(IOCRReference ocrRef, int argMinLength, int argMaxLength)
        {
            string strValue = ocrRef.OCRValue.Trim();
            if (strValue.Length != 0)
            {
                if (argMinLength > 0 && strValue.Length < argMinLength)
                {
                    ocrRef.OCRValue = "";
                    return "1012";
                }
                if (argMaxLength > 0 && strValue.Length > argMaxLength)
                {
                    ocrRef.OCRValue = "";
                    return "1013";
                }
            }
            return string.Empty;
        }

        private string DoIntegerValidation(IOCRReference ocrRef, int maxLength = 0)
        {
            string theVal = ocrRef.OCRValue.Trim();
            theVal = theVal.Replace(" ", "");
            if (theVal.Length != 0)
            {
                if (theVal[0] == '-')
                {
                    ocrRef.OCRValue = "";
                    return "1014";
                }
                try
                {
                    long.Parse(theVal);
                    if (maxLength > 0 && theVal.Length > maxLength)
                    {
                        ocrRef.OCRValue = "";
                        return "1016";
                    }
                }
                catch (Exception)
                {
                    ocrRef.OCRValue = "";
                    return "1014";
                }
            }
            theVal = null;
            return string.Empty;
        }

        private string PadMMYYYY(string argVal)
        {
            string strVal = argVal;
            if (argVal.Length <= 7 && argVal.IndexOf("/") > -1)
            {
                string[] arrDate = argVal.Split('/');
                for (int intDateCount = 0; intDateCount < arrDate.Length; intDateCount++)
                {
                    if (arrDate[intDateCount].Length == 1)
                    {
                        arrDate[intDateCount] = "0" + arrDate[intDateCount];
                    }
                }
                strVal = arrDate[0] + "/" + arrDate[1];
            }
            return strVal;
        }

        private string PadMMYY(string argVal)
        {
            string strVal = argVal;
            if (argVal.Length <= 5 && argVal.IndexOf("/") > -1)
            {
                string[] arrDate = argVal.Split('/');
                for (int intDateCount = 0; intDateCount < arrDate.Length; intDateCount++)
                {
                    if (arrDate[intDateCount].Length == 1)
                    {
                        arrDate[intDateCount] = "0" + arrDate[intDateCount];
                    }
                }
                strVal = arrDate[0] + "/" + arrDate[1];
            }
            return strVal;
        }

        private string ConvDateMMYYYY(string argVal, short Yeardigit)
        {
            if (argVal.Length == 0)
            {
                return "";
            }
            else
            {
                DateTime strVal = DateTime.Parse(argVal);
                string strTemp = "";
                string strTempMonth = strVal.Month.ToString();
                switch (Yeardigit)
                {
                    case 4:
                        strTemp = (strTempMonth.Length == 1 ? "0" + strTempMonth : strVal.Month.ToString()) + "/" + strVal.Year;
                        break;
                    case 2:
                        strTemp = (strVal.Month.ToString().Length == 1 ? "0" + strVal.Month.ToString() : strVal.Month.ToString()) + "/" + strVal.Year.ToString().Substring(2, 2);
                        break;
                    case 0:
                        strTemp = (strVal.Month.ToString().Length == 1 ? "0" + strVal.Month.ToString() : strVal.Month.ToString()) + "/" + (strVal.Day.ToString().Length == 1 ? "0" + strVal.Day.ToString() : strVal.Day.ToString());
                        break;
                }

                return strTemp;
            }
        }

        private bool PercentageCheck(string value, int intTaxSoftwareid = 0)
        {
            try
            {
                string pattern = @"^\d*\.?\d*$";
                if (value == "")
                {
                    return true;
                }
                if (double.TryParse(value, out _))
                {
                    value = (double.Parse(value) / 100).ToString();
                }
                if (Regex.IsMatch(value, pattern))
                {
                    return true;
                }
                else
                {
                    if (intTaxSoftwareid == 2)
                    {
                        if (value == "1" || value == "-1")
                        {
                            return true;
                        }
                    }
                    else if (intTaxSoftwareid == 1 || intTaxSoftwareid == 3)
                    {
                        if (value == "1")
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
