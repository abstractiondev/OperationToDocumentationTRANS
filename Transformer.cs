using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Documentation_v1_0;
using Operation_v1_0;

namespace OperationToDocumentationTRANS
{
    public class Transformer
    {
        private static string CurrentXmlFileName;

        T LoadXml<T>(string xmlFileName)
        {
            using (FileStream fStream = File.OpenRead(xmlFileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                T result = (T)serializer.Deserialize(fStream);
                fStream.Close();
                return result;
            }
        }



	    public Tuple<string, string>[] GetGeneratorContent(params string[] xmlFileNames)
	    {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            foreach(string xmlFileName in xmlFileNames)
            {
                CurrentXmlFileName = xmlFileName;
                OperationAbstractionType operationAbs = LoadXml<OperationAbstractionType>(xmlFileName);
                DocumentationAbstractionType docAbs = TransformAbstraction(operationAbs);
                string xmlContent = WriteToXmlString(docAbs);
                FileInfo fInfo = new FileInfo(xmlFileName);
                string contentFileName = "DocFrom" + fInfo.Name;
                result.Add(Tuple.Create(contentFileName, xmlContent));
            }
	        return result.ToArray();
	    }

        private string WriteToXmlString(DocumentationAbstractionType docAbs)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DocumentationAbstractionType));
            MemoryStream memoryStream = new MemoryStream();
            serializer.Serialize(memoryStream, docAbs);
            byte[] data = memoryStream.ToArray();
            string result = System.Text.Encoding.UTF8.GetString(data);
            return result;
        }

        public static DocumentationAbstractionType TransformAbstraction(OperationAbstractionType operationAbstraction)
        {
            DocumentType document = GetDocument(operationAbstraction);
            return new DocumentationAbstractionType
                       {
                           Documentations = new DocumentationsType
                                                {
                                                    Documents = new[]
                                                                    {
                                                                        GetDocument(operationAbstraction)

                                                                    }
                                                }
                       };
        }

        private static DocumentType GetDocument(OperationAbstractionType operationAbstraction)
        {
            DocumentType document = new DocumentType
                                        {
                                            title = "Operations (" + Path.GetFileNameWithoutExtension(CurrentXmlFileName) + ")",
                                            name =
                                                "Operations (" + Path.GetFileNameWithoutExtension(CurrentXmlFileName) + ")",
                                        };

            List<HeaderType> contents = new List<HeaderType>();
            if (operationAbstraction.Drafts != null)
            {
                HeaderType drafts = new HeaderType()
                                        {
                                            level = 1,
                                            text = "Draft Estimates"
                                        };
                List<HeaderType> draftContent = new List<HeaderType>();
                draftContent.AddRange(GetDraftSummaries(operationAbstraction.Drafts));
                foreach (var draft in draftContent)
                    drafts.AddSubHeader(draft);
                contents.Add(drafts);
            }
            if (operationAbstraction.Operations != null)
            {
                contents.AddRange(
                    operationAbstraction.Operations.SelectMany(opers => opers.Operation).OrderBy(oper => oper.name).
                        Select(
                            GetOperationContent).ToArray());
            }

            document.Content = contents.ToArray();
            return document;
        }

        private static HeaderType[] GetDraftSummaries(DraftsType[] drafts)
        {
            //Debugger.Break();
            var allDrafts = drafts.SelectMany(draftSet => draftSet.DraftOperation).ToArray();
            var noParameterDrafts = allDrafts.Where(NoDraftParameters);
            var draftsWithOneOrTwoParameters = allDrafts.Where(draft => NoDraftParameters(draft) == false &&
                                                                        draft.DraftParameters.Length <= 2);
            var draftsWithMoreThanTwoParameters = allDrafts.Where(draft => draft.DraftParameters != null &&
                                                                           draft.DraftParameters.Length > 2);
            var draftsWithForLoop =
                allDrafts.Where(
                    draft => draft.DraftExecution != null &&
                             draft.DraftExecution.Count(exec => exec is ForLoopType) > 0);
            var draftsWithIfCondition = allDrafts.Where(
                    draft => draft.DraftExecution != null &&
                             draft.DraftExecution.Count(exec => exec is IfConditionType) > 0);
            var draftsWithReturnValueType = allDrafts.Where(draft => String.IsNullOrEmpty(draft.returnValueDataType) == false && draft.returnValueDataType.Trim() != "void");
            var draftsWithOperationCall= allDrafts.Where(
                    draft => draft.DraftExecution != null &&
                             draft.DraftExecution.Count(exec => exec is DraftMethodCallType) > 0);
            List<HeaderType> draftHeaders = new List<HeaderType>();
            draftHeaders.Add(GetHeaderedDrafts(allDrafts, "All Operations"));
            draftHeaders.Add(GetHeaderedDrafts(draftsWithOperationCall, "Containing Call To Other Method"));
            draftHeaders.Add(GetHeaderedDrafts(noParameterDrafts, "No Input Parameters"));
            draftHeaders.Add(GetHeaderedDrafts(draftsWithOneOrTwoParameters, "One or Two Input Parameters"));
            draftHeaders.Add(GetHeaderedDrafts(draftsWithMoreThanTwoParameters, "More Than Two Input Parameters"));
            draftHeaders.Add(GetHeaderedDrafts(draftsWithIfCondition, "With At Least One If Condition"));
            draftHeaders.Add(GetHeaderedDrafts(draftsWithForLoop, "With At Least One For Loop"));
            draftHeaders.Add(GetHeaderedDrafts(draftsWithReturnValueType, "With Some Return Value"));
            return draftHeaders.ToArray();
        }

        private static bool NoDraftParameters(DraftOperationType draft)
        {
            return draft.DraftParameters == null ||
                   (draft.DraftParameters.Length == 1 && draft.DraftParameters[0].name == "");
        }

        private static HeaderType GetHeaderedDrafts(IEnumerable<DraftOperationType> draftset, string headerName)
        {
            HeaderType header = new HeaderType()
                                    {
                                        text = headerName + " (Amount of methods: " + draftset.Count() + ", Lines of code: " + draftset.Sum(draft => draft.workEstimateInDays) + ")",
                                        level = 2,
                                    };
            foreach (var draft in draftset.OrderBy(dr => dr.name))
            {
                header.AddSubHeader(new HeaderType()
                                        {
                                            text = draft.name + " (Lines of code " + draft.workEstimateInDays + ")"
                                        });
            }
            return header;
        }

        private static HeaderType GetOperationContent(OperationType operation)
        {
            string parameterExt = "";
            if(operation.Parameters != null)
                parameterExt = " (" + String.Join(", ",operation.Parameters.Parameter.Select(item => item.name).ToArray()) + ")";
            string rootPrefix = operation.isRootOperation ? "(R) " : "";
            string headerText = rootPrefix + operation.name + parameterExt;
            HeaderType header = new HeaderType
                                    {
                                        text = headerText,
                                        level = 1,
                                    };
            List<HeaderType> subHeaders = new List<HeaderType>();
            subHeaders.Add(GetOperationSpecifications(operation.OperationSpec));
            if (operation.Parameters != null && operation.Parameters.Parameter[0].name != "")
            {
                subHeaders.Add(GetVariablesHeaderedTable("Parameters", "Parameter", operation.Parameters.Parameter));
                if(operation.Parameters.Items != null)
                    subHeaders.AddRange(operation.Parameters.Items.Select(GetValidationModificationContent));
            }
            //subHeaders.AddRange(
            //    operation.Parameters.Parameter.Select(GetParameterContent));
            subHeaders.AddRange(
                operation.Execution.SequentialExecution.Select(GetExecutionContent));
            if(operation.OperationReturnValues != null)
            {
                bool isBogus = operation.OperationReturnValues.ReturnValue[0].dataType.Trim() == "void" ||
                               String.IsNullOrWhiteSpace(operation.OperationReturnValues.ReturnValue[0].dataType);
                if(!isBogus)
                    subHeaders.Add(GetReturnValuesContent(operation.name, operation.OperationReturnValues));
            }
            subHeaders.ForEach(subHeader => header.AddSubHeader(subHeader));
            return header;
        }

        private static HeaderType GetOperationSpecifications(OperationSpecType operationSpec)
        {
            HeaderType header = new HeaderType
                                    {
                                        text = "Specifications",
                                        level = 2
                                    };
            header.AddHeaderTextContent(null, operationSpec.Description);
            if(operationSpec.Requirements != null)
                header.AddSubHeaderTableContent("Requirements", GetRequirementsTable(operationSpec.Requirements));
            if(operationSpec.UseCases != null)
                header.AddSubHeaderTableContent("Use Cases", GetUseCasesTable(operationSpec.UseCases));
            return header;
        }

        private static TableType GetUseCasesTable(UseCaseType[] useCases)
        {
            TableType table = new TableType
            {
                Columns = new[]
                                                    {
                                                        new ColumnType {name = "Use Case Name"},
                                                        new ColumnType {name = "Location"}
                                                    }
            };
            List<TextType[]> rows = new List<TextType[]>();
            rows.AddRange(useCases.Select(uc => new TextType[]
                                                       {
                                                           new TextType {TextContent = uc.name },
                                                           new TextType {TextContent = uc.locationUrl },
                                                       }));
            table.Rows = rows.ToArray();
            return table;
        }

        private static TableType GetRequirementsTable(RequirementType[] requirements)
        {
            TableType table = new TableType
            {
                Columns = new[]
                                                    {
                                                        new ColumnType {name = "Requirement"},
                                                        new ColumnType {name = "Category"},
                                                        new ColumnType {name = "Description/Data"},
                                                    }
            };
            List<TextType[]> rows = new List<TextType[]>();
            rows.AddRange(requirements.Select(req => new TextType[]
                                                       {
                                                           new TextType {TextContent = req.name },
                                                           new TextType {TextContent = req.category.ToString() },
                                                           new TextType { TextContent = GetRequirementDescriptionData(req.Item) }
                                                       }));
            table.Rows = rows.ToArray();
            return table;
        }

        private static string GetRequirementDescriptionData(object item)
        {
            if (item == null)
                return null;
            string textReq = item as string;
            RequirementTypePerformance performanceReq = item as RequirementTypePerformance;
            if (textReq != null)
                return textReq;
            else if(performanceReq != null)
                return GetPerformanceRequirementDescriptionData(performanceReq);
            else
                throw new NotSupportedException("Requirement data type: " + item.GetType().Name);

        }

        private static string GetPerformanceRequirementDescriptionData(RequirementTypePerformance performanceReq)
        {
            List<string> allReqs = new List<string>();
            if(performanceReq.maxCPUTimeMsSpecified)
                allReqs.Add(string.Format("Max CPU Time: {0} ms", performanceReq.maxCPUTimeMs));
            if (performanceReq.maxFileIOBytesSpecified)
                allReqs.Add(string.Format("Max File I/O Bytes: {0}", performanceReq.maxFileIOBytes));
            if (performanceReq.maxFileIOCountSpecified)
                allReqs.Add(string.Format("Max File I/O Count: {0}", performanceReq.maxFileIOCount));
            if (performanceReq.maxMemoryBytesSpecified)
                allReqs.Add(string.Format("Max Memory Bytes: {0}", performanceReq.maxMemoryBytes));
            if (performanceReq.maxTotalTimeMsSpecified)
                allReqs.Add(string.Format("Max Total Time {0} ms", performanceReq.maxTotalTimeMs));
            return String.Join(", ", allReqs.ToArray());
        }

        private static HeaderType GetReturnValuesContent(string operationName, OperationReturnValuesType operationReturnValues)
        {
            string returnValueName = operationName + "ReturnValue";
            string targetAndParamExt = "";
            string[] paramNames =
                (operationReturnValues.Parameter ?? new TargetType[0]).Select(target => target.name).ToArray();
            string[] targetNames =
                (operationReturnValues.Target ?? new TargetType[0]).Select(target => target.name).ToArray();
            targetAndParamExt = getParameterExtensionString(paramNames.Union(targetNames));

            string headerText = "Return Value : " + returnValueName + targetAndParamExt;
            HeaderType returnValueHeader = new HeaderType
                                               {
                                                   text = headerText
                                               };
            returnValueHeader.AddHeaderTableContent(GetVariableTable("Return Value", operationReturnValues.ReturnValue));
            return returnValueHeader;
        }

        private static HeaderType GetVariablesHeaderedTable(string headerText, string itemName, VariableType[] variables)
        {
            HeaderType paramHeader = new HeaderType();
            paramHeader.text = headerText;
            paramHeader.Paragraph = new ParagraphType[]
                                        {
                                            new ParagraphType { Item = GetVariableTable(itemName, variables)  }
                                        };
            return paramHeader;
        }

        private static TableType GetVariableTable(string itemName, VariableType[] parameters)
        {
            TableType table = new TableType
                                  {
                                      Columns = new[]
                                                    {
                                                        new ColumnType {name = itemName},
                                                        new ColumnType {name = "DataType"},
                                                        new ColumnType {name = "Description"}
                                                    }
                                  };
            List<TextType[]> rows = new List<TextType[]>();
            rows.AddRange(parameters.Select(par => new TextType[]
                                                       {
                                                           new TextType {TextContent = par.name, 
                                                               styleRef = GetStyleName(par.state.ToString())},
                                                           new TextType {TextContent = par.dataType,
                                                               styleRef = GetStyleName(par.state.ToString())},
                                                           new TextType {TextContent = par.designDesc,
                                                               styleRef = GetStyleName(par.state.ToString())}
                                                       }));
            table.Rows = rows.ToArray();
            return table;
        }

        private static HeaderType GetExecutionContent(object execItem)
        {
            dynamic dynObj = execItem;
            if (dynObj.name == "###")
                return null;
            TargetType[] parameters = dynObj.Parameter ?? new TargetType[0];
            TargetType[] targets = dynObj.Target ?? new TargetType[0];
            string targetExt = getParameterExtensionString(targets.Union(parameters).Select(target => target.name));

            string headerTypeString = "Execution: ";
            if (execItem.GetType() == typeof(TargetDefinitionType))
                headerTypeString = "Target Definition: ";
            else if (execItem.GetType() == typeof(MethodExecuteType))
                headerTypeString = "Method Call: ";
            else if (execItem.GetType() == typeof (OperationExecuteType))
                headerTypeString = "Operation Call: ";
            string headerText = headerTypeString + dynObj.name + targetExt;
            HeaderType execHeader = new HeaderType
                                        {
                                            text = headerText,
                                        };
            string styleName = GetStyleName(dynObj.state.ToString());
            string content = dynObj.designDesc;
            execHeader.AddHeaderTextContent(styleName, content);

            if(execItem is MethodExecuteType || execItem is OperationExecuteType)
            {
                VariableType[] returnValues = dynObj.ReturnValue;
                if (returnValues != null)
                    execHeader.AddHeaderTableContent(GetVariableTable("Output value field", returnValues));
            }
            return execHeader;
        }

        private static HeaderType GetValidationModificationContent(object validationModification)
        {
            ValidationType validation = validationModification as ValidationType;
            ModificationType modification = validationModification as ModificationType;
            if (validation != null)
                return GetValidationContent(validation);
            else if (modification != null)
                return GetModificationContent(modification);
            else
                throw new NotSupportedException("ValidationModification type: " + validationModification.GetType().Name);
        }

        static string getParameterExtensionString(IEnumerable<string> parameters)
        {
            if (parameters == null || parameters.Count() == 0)
                return "";
            return " ( " + String.Join(", ", parameters) + " )";
        }

        private static HeaderType GetModificationContent(ModificationType modification)
        {
            if (modification.name == "####")
                return null;
            string targetExt = "";
            if (modification.Target != null)
                targetExt = getParameterExtensionString(modification.Target.Select(target => target.name));
            HeaderType header = new HeaderType {text = "Modification: " + modification.name + targetExt };
            string styleName = GetStyleName(modification.state.ToString());
            header.SetHeaderTextContent(styleName, modification.designDesc);
            return header;
        }

        private static HeaderType GetValidationContent(ValidationType validation)
        {
            if (validation.name == "####")
                return null;
            string targetExt = "";
            if (validation.Target != null)
                targetExt = getParameterExtensionString(validation.Target.Select(target => target.name));
            HeaderType header = new HeaderType { text = "Validation: " + validation.name + targetExt };
            string styleName = GetStyleName(validation.state.ToString());
            header.SetHeaderTextContent(styleName, validation.designDesc);
            return header;
        }

        private static string GetStyleName(string stateString)
        {
            switch (stateString)
            {
                case "implemented":
                    return null;
                case "designApproved":
                    return "color:blue;font-weight:bold;font-style:italic";
                case "underDesign":
                    return "color:red;font-weight:bold;text-decoration:underline";
                default:
                    throw new NotSupportedException("State string value: " + stateString);
            }
        }
    }
}
