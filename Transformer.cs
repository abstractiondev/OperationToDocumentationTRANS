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
            HeaderType header = new HeaderType
                                    {
                                        text = operation.name,
                                        level = 1,
                                    };
            List<HeaderType> subHeaders = new List<HeaderType>();
            subHeaders.Add(GetParametersHeaderedTable(operation.Parameters.Parameter));
            //subHeaders.AddRange(
            //    operation.Parameters.Parameter.Select(GetParameterContent));
            subHeaders.AddRange(
                operation.Parameters.Items.Select(GetValidationModificationContent));
            subHeaders.ForEach(subHeader => header.AddSubHeader(subHeader));
            return header;
        }

        private static HeaderType GetParametersHeaderedTable(VariableType[] parameters)
        {
            HeaderType paramHeader = new HeaderType();
            paramHeader.text = "Parameters";
            paramHeader.Paragraph = new ParagraphType[]
                                        {
                                            new ParagraphType { Item = GetParameterTable(parameters)  }
                                        };
            return paramHeader;
        }

        private static TableType GetParameterTable(VariableType[] parameters)
        {
            TableType table = new TableType
                                  {
                                      Columns = new[]
                                                    {
                                                        new ColumnType {name = "Parameter"},
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
            string styleName = modification.state.ToString();
            header.SetHeaderTextContent(styleName, modification.designDesc);
            foreach (var target in modification.Target ?? new TargetType[0])
                header.AddHeaderTextContent(styleName, "Target: " + target.name);
            return header;
        }

        private static HeaderType GetValidationContent(ValidationType validation)
        {
            HeaderType header = new HeaderType {text = "Validation: " + validation.name};
            string styleName = validation.state.ToString();
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
            return stateString;
        }
    }
}
