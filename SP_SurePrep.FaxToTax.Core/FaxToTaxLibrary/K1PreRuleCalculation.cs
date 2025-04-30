using FaxToTaxLibrary.Classes;
using FaxToTaxLibrary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Configuration;
using FaxToTaxDataValidations;

namespace FaxToTaxLibrary
{
public class K1PreRuleCalculation
    {
        private List<TemplateDefinitions> TemplateDefinitionCollection;
        private List<OCRField> OCRFieldsCollection;
        private List<Results> OCRFieldIdRuleCollection;
        private List<EngagementFaxForm> EngOCRFieldCollection;
        private int EngId;

        public K1PreRuleCalculation(string configConnectionKey)
        {
            OCRFieldsCollection = new List<OCRField>();
            OCRFieldIdRuleCollection = new List<Results>();
            CommonCode.ConfigConnectionKey = configConnectionKey;
        }

        #region Business Rule

        /// <summary>
        /// Main function call from Fax2Tax agent.
        /// </summary>
        public bool EvaluatePreRule(int engagementId, int taxYear = 0, int engagementTypeId = 0)
        {
            try
            {
                EngOCRFieldCollection = GetOCRFieldCollection(engagementId);

                if (EngOCRFieldCollection == null || EngOCRFieldCollection.Count == 0)
                {
                   ////LogEntry(CommonCode.Severity.Low, "DDPAgent", "No Record Found For Supplement K1 Rule Match", 708, 1, engagementId, CommonCode.TraceType.InfoLog);
                   //CommonCode.WriteDBLog("No Record Found For Supplement K1 Rule Match", 708, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, engagementId);
                    return true;
                }

                TemplateDefinitionCollection = GetTemplateDefinitionCollection(engagementId, taxYear, engagementTypeId);
                if (TemplateDefinitionCollection == null || TemplateDefinitionCollection.Count == 0)
                {
                   ////LogEntry(CommonCode.Severity.Low, "DDPAgent", "No Rule Found For Supplement K1 Rule Match", 709, 1, engagementId, CommonCode.TraceType.InfoLog);
                   //CommonCode.WriteDBLog("No Rule Found For Supplement K1 Rule Match", 709, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, engagementId);
                    return true;
                }

               ////LogEntry(CommonCode.Severity.Low, "DDPAgent", $"Start MatchTheRule function with parameter {engagementId} : {taxYear} : {engagementTypeId}", 710, 1, engagementId, CommonCode.TraceType.InfoLog);
               //CommonCode.WriteDBLog($"Start MatchTheRule function with parameter {engagementId} : {taxYear} : {engagementTypeId}", 710, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, engagementId);

                MatchTheRule(EngOCRFieldCollection, TemplateDefinitionCollection);

               //CommonCode.WriteDBLog($"End MatchTheRule function with parameter {engagementId} : {taxYear} : {engagementTypeId}", 711, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, engagementId);
               ////LogEntry(CommonCode.Severity.Low, "DDPAgent", $"End MatchTheRule function with parameter {engagementId} : {taxYear} : {engagementTypeId}", 711, 1, engagementId, CommonCode.TraceType.InfoLog);

                UpdateEngagementOCRFieldValue(engagementId);
                return true;
            }
            catch (Exception ex)
            {
               ////LogEntry(CommonCode.Severity.Low, "DDPAgent", $"EvaluatePreRule - Error Occurred: {ex.Message} engagementId = {engagementId} taxYear = {taxYear} engagementTypeId = {engagementTypeId}", 919, 1, engagementId, CommonCode.TraceType.ErrorLog);
                throw;
            }
        }

        /// <summary>
        /// Match the rule one by one from Collection of Rule template.
        /// </summary>
        private bool MatchTheRule(List<EngagementFaxForm> ocrFieldCollection, List<TemplateDefinitions> templateDefinitionList)
        {
            string logInfo = string.Empty;
            try
            {
                int engagementFormFieldCounter = 1;
                List<OCRField> ocrcollec;

                foreach (var ocrfields in ocrFieldCollection)
                {
                    foreach (var template in templateDefinitionList)
                    {
                        if (template.FaxFormFieldID_Code1 == 0 && template.FaxFormFieldID_Amount1 == 0) continue;

                        logInfo = $" -> FaxDWPCode: {template.FaxDWPCode_Code1}";

                        if (template.FaxFormID_1 == template.FaxFormID_2)
                        {
                            ocrcollec = ocrfields.EngagementOCRFieldCollection
                                .Where(x => x.FaxFormFieldID == template.FaxFormFieldID_Code1 ||
                                            x.FaxFormFieldID == template.FaxFormFieldID_Amount1 ||
                                            x.FaxFormFieldID == template.FaxFormFieldID_Code2 ||
                                            x.FaxFormFieldID == template.FaxFormFieldID_Amount2)
                                .ToList();
                        }
                        else
                        {
                            if (engagementFormFieldCounter > 1) continue;

                            ocrcollec = OCRFieldsCollection
                                .Where(x => x.FaxFormFieldID == template.FaxFormFieldID_Code1 ||
                                            x.FaxFormFieldID == template.FaxFormFieldID_Amount1 ||
                                            x.FaxFormFieldID == template.FaxFormFieldID_Code2 ||
                                            x.FaxFormFieldID == template.FaxFormFieldID_Amount2)
                                .ToList();
                        }

                        var ParentPageFields = ocrcollec
                            .Where(x => x.FaxFormFieldID == template.FaxFormFieldID_Code1 || x.FaxFormFieldID == template.FaxFormFieldID_Amount1)
                            .ToList();

                        var SupplementPageFields = ocrcollec
                            .Where(y => y.FaxFormFieldID == template.FaxFormFieldID_Code2 || y.FaxFormFieldID == template.FaxFormFieldID_Amount2)
                            .ToList();

                        if (!(ParentPageFields.Count > 0 && SupplementPageFields.Count > 0)) continue;

                        if (template.FaxFormFieldID_Code2 == 0 && template.FaxFormFieldID_Code1 == 0)
                        {
                            MatchAmountWithoutCode(ParentPageFields, SupplementPageFields, template);
                        }
                        else if (template.FaxFormFieldID_Code1 > 0 && template.FaxFormFieldID_Code2 > 0)
                        {
                            MatchAmountAgainstCode(ParentPageFields, SupplementPageFields, template);
                        }
                        else if (template.FaxFormFieldID_Code1 > 0 && template.FaxFormFieldID_Code2 == 0)
                        {
                            MatchAmountIgnoreParentCode(ParentPageFields, SupplementPageFields, template);
                        }
                    }

                    engagementFormFieldCounter++;
                }

                return true;
            }
            catch (Exception ex)
            {
               ////LogEntry(CommonCode.Severity.Low, "DDPAgent", $"MatchTheRule - Error Occurred: {ex.Message} {logInfo} -> {DateTime.Now}", 920, 1, EngId, CommonCode.TraceType.ErrorLog);
                throw;
            }
        }
        /// <summary>
        /// Match the amount irrespective of Code (Either K1 or Supplement) both side rule
        /// </summary>
        private void MatchAmountWithoutCode(List<OCRField> K1ParentFieldCode, List<OCRField> supplementPageFields, TemplateDefinitions rulesTemplate)
        {
            string logInfo = string.Empty;
            try
            {
                List<int> tempFieldIds = new List<int>();
                if (rulesTemplate != null)
                    logInfo = " -> FaxFormFieldID: " + rulesTemplate.FaxFormFieldID_Amount1.ToString();

                var ParentAmountList = K1ParentFieldCode
                    .Where(PAL => PAL.FaxFormFieldID == rulesTemplate.FaxFormFieldID_Amount1 && IsNumeric(PAL.OCRValue))
                    .ToList();

                var SupplimentAmountsList = supplementPageFields
                    .Where(SAL => SAL.FaxFormFieldID == rulesTemplate.FaxFormFieldID_Amount2 && IsNumeric(SAL.OCRValue))
                    .ToList();

                int parentAmount = 0;
                int supplementAmount = 0;

                foreach (var k1 in ParentAmountList)
                {
                    parentAmount += Convert.ToInt32(k1.OCRValue);
                    tempFieldIds.Add(k1.EngagementOCRFieldID);
                }

                foreach (var supplement in SupplimentAmountsList)
                {
                    supplementAmount += Convert.ToInt32(supplement.OCRValue);
                    tempFieldIds.Add(supplement.EngagementOCRFieldID);
                }

                if (parentAmount == supplementAmount)
                {
                    foreach (var id in tempFieldIds)
                    {
                        OCRFieldIdRuleCollection.Add(new Results
                        {
                            RuleId = rulesTemplate.SPFaxPreRuleFieldId,
                            EngagementOCRFieldId = id
                        });
                    }
                }
            }
            catch (Exception ex)
            {
               ////LogEntry(CommonCode.Severity.Low, "DDPAgent", $"MatchAmountWithoutCode - Error Occurred: {ex.Message} {logInfo} -> {DateTime.Now}", 921, 1, EngId, CommonCode.TraceType.ErrorLog);
                throw;
            }
        }

        /// <summary>
        /// Match the amount irrespective of Code if code available in Parent (Either K1 or Supplement) both side rule
        /// </summary>
        private void MatchAmountIgnoreParentCode(List<OCRField> K1ParentFieldCode, List<OCRField> supplementPageFields, TemplateDefinitions rulesTemplate)
        {
            string logInfo = string.Empty;
            try
            {
                List<int> tempFieldIds = new List<int>();
                if (rulesTemplate != null && !string.IsNullOrWhiteSpace(rulesTemplate.FaxDWPCode_Code1))
                    logInfo = " -> FaxDWPCode_1: " + rulesTemplate.FaxDWPCode_Code1;

                var ParentAmountList = K1ParentFieldCode
                    .Where(PAL => PAL.OCRValue == rulesTemplate.FaxDWPCode_Value1 && IsNumeric(PAL.OCRValue))
                    .ToList();

                var SupplimentAmountsList = supplementPageFields
                    .Where(SAL => SAL.FaxFormFieldID == rulesTemplate.FaxFormFieldID_Amount2 && IsNumeric(SAL.OCRValue))
                    .ToList();

                int parentAmount = 0;
                int supplementAmount = 0;

                var K1list = K1ParentFieldCode
                    .Join(ParentAmountList, K1 => K1.FaxRowNumber, p1 => p1.FaxRowNumber, (K1, p1) => new { K1.OCRValue, K1.EngagementOCRFieldID })
                    .Where(K1 => K1ParentFieldCode.Any(x => x.FaxFormFieldID == rulesTemplate.FaxFormFieldID_Amount1 && IsNumeric(x.OCRValue)))
                    .ToList();

                foreach (var k1 in K1list)
                {
                    parentAmount += Convert.ToInt32(k1.OCRValue);
                    tempFieldIds.Add(k1.EngagementOCRFieldID);
                }

                foreach (var supplement in SupplimentAmountsList)
                {
                    supplementAmount += Convert.ToInt32(supplement.OCRValue);
                    tempFieldIds.Add(supplement.EngagementOCRFieldID);
                }

                if (parentAmount == supplementAmount)
                {
                    foreach (var id in tempFieldIds)
                    {
                        OCRFieldIdRuleCollection.Add(new Results
                        {
                            RuleId = rulesTemplate.SPFaxPreRuleFieldId,
                            EngagementOCRFieldId = id
                        });
                    }
                }
            }
            catch (Exception ex)
            {
               ////LogEntry(CommonCode.Severity.Low, "DDPAgent", $"MatchAmountIgnoreParentCode - Error Occurred: {ex.Message} {logInfo} -> {DateTime.Now}", 922, 1, EngId, CommonCode.TraceType.ErrorLog);
                throw;
            }
        }

        /// <summary>
        /// Match the Amount against the Code
        /// </summary>
        private void MatchAmountAgainstCode(List<OCRField> K1ParentFieldCode, List<OCRField> supplementPageFields, TemplateDefinitions rulesTemplate)
        {
            string logInfo = string.Empty;
            try
            {
                List<int> tempFieldIds = new List<int>();
                if (rulesTemplate != null && !string.IsNullOrWhiteSpace(rulesTemplate.FaxDWPCode_Code1))
                {
                    logInfo = " -> FaxDWPCode_1: " + rulesTemplate.FaxDWPCode_Code1;
                }

                List<OCRField> ParentAmountList = K1ParentFieldCode
                    .Where(PAL => PAL.OCRValue == rulesTemplate.FaxDWPCode_Value1 && IsNumeric(PAL.OCRValue))
                    .ToList();

                List<OCRField> SupplimentAmountsList = supplementPageFields
                    .Where(SAL => SAL.OCRValue == rulesTemplate.FaxDWPCode_Value2 && IsNumeric(SAL.OCRValue))
                    .ToList();

                int parentAmount = 0;
                int SupplementAmount = 0;

                if (!(ParentAmountList.Count > 0 && SupplimentAmountsList.Count > 0))
                {
                    return;
                }

                var K1list = K1ParentFieldCode
                    .Join(ParentAmountList,
                        K1 => K1.FaxRowNumber,
                        p1 => p1.FaxRowNumber,
                        (K1, p1) => new { K1.OCRValue, K1.EngagementOCRFieldID })
                    .Where(K1 => K1ParentFieldCode.Any(x => x.FaxFormFieldID == rulesTemplate.FaxFormFieldID_Amount1 && IsNumeric(x.OCRValue)))
                    .ToList();

                var Supplementlist = supplementPageFields
                    .Join(SupplimentAmountsList,
                        K1 => K1.FaxRowNumber,
                        p1 => p1.FaxRowNumber,
                        (K1, p1) => new { K1.OCRValue, K1.EngagementOCRFieldID })
                    .Where(K1 => supplementPageFields.Any(x => x.FaxFormFieldID == rulesTemplate.FaxFormFieldID_Amount2 && IsNumeric(x.OCRValue)))
                    .ToList();

                foreach (var k1 in K1list)
                {
                    parentAmount += Convert.ToInt32(k1.OCRValue);
                    tempFieldIds.Add(k1.EngagementOCRFieldID);
                }

                foreach (var supplement in Supplementlist)
                {
                    SupplementAmount += Convert.ToInt32(supplement.OCRValue);
                    tempFieldIds.Add(supplement.EngagementOCRFieldID);
                }

                if (parentAmount == SupplementAmount)
                {
                    foreach (int id in tempFieldIds)
                    {
                        OCRFieldIdRuleCollection.Add(new Results
                        {
                            RuleId = rulesTemplate.SPFaxPreRuleFieldId,
                            EngagementOCRFieldId = id
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                //LogEntry(CommonCode.Severity.Low, "DDPAgent", $"MatchAmountAgainstCode - Error Occurred: {ex.Message} {logInfo} -> {DateTime.Now}", 923, 1, EngId, CommonCode.TraceType.ErrorLog);
                throw;
            }
        }



        #endregion
        #region Data Functions

        /// <summary>
        /// Populate Pre Rule for K1 Supplement forms.
        /// </summary>
        /// <param name="engagementId"></param>
        /// <param name="taxYear"></param>
        /// <param name="engagementTypeId"></param>
        /// <returns></returns>
        private List<TemplateDefinitions> GetTemplateDefinitionCollection(int engagementId, int taxYear, int engagementTypeId)
        {
            List<TemplateDefinitions> templateCollection = new List<TemplateDefinitions>();
            DataTable templateDatable;
            string logInfo = string.Empty;

            try
            {
                
                    SpParameter[] sqlParam = new SpParameter[2];
                    sqlParam[0] = new SpParameter("@TaxYear", taxYear);
                    sqlParam[1] = new SpParameter("@EngagementTypeID", engagementTypeId);
                    templateDatable = CommonCode.GetEngagementDbCommon(engagementId).GetDataTable("Proc_GetSPFaxPreRule", sqlParam, "Table1", true);
                

                if (templateDatable != null && templateDatable.Rows.Count > 0)
                {
                    foreach (DataRow item in templateDatable.Rows)
                    {
                        TemplateDefinitions template = new TemplateDefinitions
                        {
                            SPFaxPreRuleFieldId = Convert.ToInt32(item["SPFaxPreRuleFieldId"]),
                            EngagementTypeID = Convert.ToInt32(item["EngagementTypeID"]),
                            FaxDWPCode_Amount1 = Convert.ToString(item["FaxDWPCode_Amount1"]),
                            FaxDWPCode_Amount2 = Convert.ToString(item["FaxDWPCode_Amount2"]),
                            FaxDWPCode_Code1 = Convert.ToString(item["FaxDWPCode_Code1"]),
                            FaxDWPCode_Code2 = Convert.ToString(item["FaxDWPCode_Code2"]),
                            FaxDWPCode_Value1 = Convert.ToString(item["FaxDWPCode_Value1"]),
                            FaxDWPCode_Value2 = Convert.ToString(item["FaxDWPCode_Value2"]),
                            FaxFieldName = Convert.ToString(item["FaxFieldName"]),
                            FaxFormFieldID_Amount1 = Convert.ToInt32(item["FaxFormFieldID_Amount1"]),
                            FaxFormFieldID_Amount2 = Convert.ToInt32(item["FaxFormFieldID_Amount2"]),
                            FaxFormFieldID_Code1 = IsDBNull(item["FaxFormFieldID_Code1"]) ? 0 : Convert.ToInt32(item["FaxFormFieldID_Code1"]),
                            FaxFormFieldID_Code2 = IsDBNull(item["FaxFormFieldID_Code2"]) ? 0 : Convert.ToInt32(item["FaxFormFieldID_Code2"]),
                            FaxFormID_1 = Convert.ToInt32(item["FaxFormID_1"]),
                            FaxFormID_2 = Convert.ToInt32(item["FaxFormID_2"]),
                            TaxYear = Convert.ToInt32(item["TaxYear"])
                        };
                        templateCollection.Add(template);
                    }
                }
                return templateCollection;
            }
            catch (Exception ex)
            {
                //LogEntry(CommonCode.Severity.Low, "DDPAgent", $"GetTemplateDefinitionCollection - Error Occurred: {ex.Message} {logInfo} engagementId = {engagementId} taxYear = {taxYear} engagementTypeId = {engagementTypeId}", 924, 1, engagementId, CommonCode.TraceType.ErrorLog);
                throw;
            }
        }

        private static bool IsDBNull(object expression)
        {
            return expression is DBNull || expression == null || string.IsNullOrEmpty(expression.ToString());
        }

        /// <summary>
        /// Populate the OCR Field collection.
        /// </summary>
        /// <param name="engagementId"></param>
        /// <returns></returns>
        private List<EngagementFaxForm> GetOCRFieldCollection(int engagementId)
        {
            string logInfo = string.Empty;

            try
            {
                int currEngagementFaxFormId;
                int prevEngagementFaxFormId = -1;
                List<EngagementFaxForm> engFormFormCollection = new List<EngagementFaxForm>();
                List<OCRField> ocrFieldCollection = new List<OCRField>();
                DataTable ocrField;

                
                    SpParameter[] sqlParam = new SpParameter[1];
                    sqlParam[0] = new SpParameter("@EngagementID", engagementId);
                    ocrField = CommonCode.GetEngagementDbCommon(engagementId).GetDataTable("Proc_GetSPEngagementOCRFields", sqlParam, "DataTable", true);
                

                if (ocrField != null && ocrField.Rows.Count > 0)
                {
                    foreach (DataRow item in ocrField.Rows)
                    {
                        logInfo = $" -> EngagementOCRFieldID: {item["EngagementOCRFieldID"]}";
                        AddOCRFieldInCollection(item, OCRFieldsCollection);
                        currEngagementFaxFormId = Convert.ToInt32(item["EngagementFaxFormID"]);

                        if (prevEngagementFaxFormId == currEngagementFaxFormId)
                        {
                            AddOCRFieldInCollection(item, ocrFieldCollection);
                        }
                        else
                        {
                            ocrFieldCollection = new List<OCRField>();
                            AddOCRFieldInCollection(item, ocrFieldCollection);
                            engFormFormCollection.Add(new EngagementFaxForm
                            {
                                EngagementFaxFormId = currEngagementFaxFormId,
                                EngagementOCRFieldCollection = ocrFieldCollection
                            });
                        }
                        prevEngagementFaxFormId = currEngagementFaxFormId;
                    }
                }
                return engFormFormCollection;
            }
            catch (Exception ex)
            {
                //LogEntry(CommonCode.Severity.Low, "DDPAgent", $"GetOCRFieldCollection - Error Occurred: {ex.Message} {logInfo} engagementId = {engagementId}", 925, 1, engagementId, CommonCode.TraceType.ErrorLog);
                throw;
            }
        }

        /// <summary>
        /// Add row into OCRField collection.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="ocrFieldCollection"></param>
        private void AddOCRFieldInCollection(DataRow row, List<OCRField> ocrFieldCollection)
        {
            OCRField ocr = new OCRField
            {
                FaxRowNumber = IsDBNull(row["FaxRowNumber"]) ? 0 : Convert.ToInt32(row["FaxRowNumber"]),
                EngagementID = IsDBNull(row["EngagementID"]) ? 0 : Convert.ToInt32(row["EngagementID"]),
                EngagementFaxFormID = IsDBNull(row["EngagementFaxFormID"]) ? 0 : Convert.ToInt32(row["EngagementFaxFormID"]),
                EngagementOCRFieldID = IsDBNull(row["EngagementOCRFieldID"]) ? 0 : Convert.ToInt32(row["EngagementOCRFieldID"]),
                EngagementPageID = IsDBNull(row["EngagementPageID"]) ? 0 : Convert.ToInt32(row["EngagementPageID"]),
                FaxFormFieldID = IsDBNull(row["FaxFormFieldID"]) ? 0 : Convert.ToInt32(row["FaxFormFieldID"]),
                FaxFormID = IsDBNull(row["FaxFormID"]) ? 0 : Convert.ToInt32(row["FaxFormID"]),
                OCRValue = IsDBNull(row["OCRValue"]) ? string.Empty : Convert.ToString(row["OCRValue"]),
                OCRVerifiedValue = IsDBNull(row["OCRVerifiedValue"]) ? string.Empty : Convert.ToString(row["OCRVerifiedValue"]),
                AutoVerified = IsDBNull(row["AutoVerified"]) ? string.Empty : Convert.ToString(row["AutoVerified"]),
                UnCertainChar = IsDBNull(row["UnCertainChar"]) ? string.Empty : Convert.ToString(row["UnCertainChar"])
            };

            ocrFieldCollection.Add(ocr);
        }

        /// <summary>
        /// Update the OCR fields Autoverify and Uncertain column against the EngagementOCRFieldId.
        /// </summary>
        /// <param name="engagementId"></param>
        private void UpdateEngagementOCRFieldValue(int engagementId)
        {
            if (OCRFieldIdRuleCollection != null && OCRFieldIdRuleCollection.Count > 0)
            {
               CommonCode.WriteDBLog("Start UpdateEngagementOCRFieldValue: Matching Rules Found", 712, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, engagementId);
                Console.WriteLine("Start UpdateEngagementOCRFieldValue: Matching Rules Found");
                //LogEntry(CommonCode.Severity.Low, "DDPAgent", "Start UpdateEngagementOCRFieldValue: Matching Rules Found", 712, 1, engagementId, CommonCode.TraceType.InfoLog);

                DataTable ocrFieldIds = ConvertToDataTable(OCRFieldIdRuleCollection);

               
                    SpParameter[] sqlParam = new SpParameter[2];
                    sqlParam[0] = new SpParameter("@EngagementID", engagementId);
                    sqlParam[1] = new SpParameter("@UDT_EngagementFaxFormFieldID", ocrFieldIds);
                    CommonCode.GetEngagementDbCommon(engagementId).AddUpdateOrDelete("Proc_UpdateEngagementOCRField_AutoVerified", sqlParam, true, false);
                

               CommonCode.WriteDBLog("End UpdateEngagementOCRFieldValue: Matching Rules Found", 713, 1, CommonCode.Severity.Low, CommonCode.TraceType.InfoLog, engagementId);
                Console.WriteLine("End UpdateEngagementOCRFieldValue: Matching Rules Found");
                //LogEntry(CommonCode.Severity.Low, "DDPAgent", "End UpdateEngagementOCRFieldValue: Matching Rules Found", 713, 1, engagementId, CommonCode.TraceType.InfoLog);
            }
        }
        #endregion


        #region Utility Code

        private DataTable ConvertToDataTable<T>(IList<T> list)
        {
            var entityType = typeof(T);
            var table = new DataTable();
            var properties = TypeDescriptor.GetProperties(entityType);

            foreach (PropertyDescriptor prop in properties)
            {
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (var item in list)
            {
                var row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }

            return table;
        }
        private static bool IsNumeric(string value)
        {
            return int.TryParse(value, out _);
        }

        #endregion
    }

}

