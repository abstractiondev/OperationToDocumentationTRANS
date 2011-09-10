using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Documentation_v1_0;
using Operation_v1_0;

namespace OperationToDocumentationTRANS
{
    public class Transform
    {
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
            subHeaders.AddRange(
                operation.Parameters.Parameter.Select(GetParameterContent));
            subHeaders.AddRange(
                operation.Parameters.Items.Select(GetValidationModificationContent));
            subHeaders.ForEach(subHeader => header.AddSubHeader(subHeader));
            return header;
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
            return null;
        }
    }
}
