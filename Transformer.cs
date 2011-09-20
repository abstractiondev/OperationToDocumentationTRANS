using System;
using System.Collections.Generic;
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
                                            title = "Operations",
                                            name = "Operations",
                                            Content = operationAbstraction.Operations.Operation.Select(
                                                operation => GetOperationContent(operation)).ToArray()
                                        };
            return document;
        }

        private static HeaderType GetOperationContent(OperationType operation)
        {
            string headerText = operation.name + " ("
                                +
                                String.Join(", ", operation.Parameters.Parameter.Select(item => item.name).ToArray())
                                + ")";
            HeaderType header = new HeaderType
                                    {
                                        text = headerText,
                                        level = 1,
                                    };
            List<HeaderType> subHeaders = new List<HeaderType>();
            subHeaders.Add(GetVariablesHeaderedTable("Parameters", "Parameter", operation.Parameters.Parameter));
            //subHeaders.AddRange(
            //    operation.Parameters.Parameter.Select(GetParameterContent));
            subHeaders.AddRange(
                operation.Parameters.Items.Select(GetValidationModificationContent));
            subHeaders.AddRange(
                operation.Execution.SequentialExecution.Select(GetExecutionContent));
            if(operation.OperationReturnValues != null)
                subHeaders.Add(GetReturnValuesContent(operation.OperationReturnValues));
            subHeaders.ForEach(subHeader => header.AddSubHeader(subHeader));
            return header;
        }

        private static HeaderType GetReturnValuesContent(OperationReturnValuesType operationReturnValues)
        {
            string headerText = "Return Values ("
                                +
                                String.Join(", ", operationReturnValues.ReturnValue.Select(item => item.name).ToArray())
                                + ")";
            HeaderType returnValueHeader = new HeaderType
                                               {
                                                   text = headerText
                                               };
            string[] paramNames =
                (operationReturnValues.Parameter ?? new TargetType[0]).Select(target => target.name).ToArray();
            string[] targetNames = 
                (operationReturnValues.Target ?? new TargetType[0]).Select(target => target.name).ToArray();
            if (paramNames.Length > 0 || targetNames.Length > 0)
            {
                string parametersAndTargets = "Parameters and targets: " +
                                              String.Join(", ", paramNames.Union(targetNames).ToArray());
                returnValueHeader.AddHeaderTextContent(null, parametersAndTargets);
            }
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
            string headerText = "Execution: " + dynObj.name;
            HeaderType execHeader = new HeaderType
                                        {
                                            text = headerText,
                                        };
            string styleName = GetStyleName(dynObj.state.ToString());
            string content = dynObj.designDesc;
            execHeader.AddHeaderTextContent(styleName, content);
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

        private static HeaderType GetModificationContent(ModificationType modification)
        {
            HeaderType header = new HeaderType {text = "Modification: " + modification.name};
            string styleName = GetStyleName(modification.state.ToString());
            header.SetHeaderTextContent(styleName, modification.designDesc);
            foreach (var target in modification.Target ?? new TargetType[0])
                header.AddHeaderTextContent(styleName, "Target: " + target.name);
            return header;
        }

        private static HeaderType GetValidationContent(ValidationType validation)
        {
            HeaderType header = new HeaderType {text = "Validation: " + validation.name};
            string styleName = GetStyleName(validation.state.ToString());
            header.SetHeaderTextContent(styleName, validation.designDesc);
            foreach (var target in validation.Target ?? new TargetType[0])
                header.AddHeaderTextContent(styleName, "Target: " + target.name);
            return header;
        }

        private static HeaderType GetParameterContent(VariableType parameter)
        {
            HeaderType header = new HeaderType
                                    {
                                        text = parameter.name + " (" + parameter.dataType + ")",
                                    };
            header.SetHeaderTextContent(parameter.state.ToString(), parameter.designDesc);
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
