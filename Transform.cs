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
        public DocumentationAbstractionType TransformAbstraction(OperationAbstractionType operationAbstraction)
        {
            DocumentType document = GetDocument(operationAbstraction);
            return new DocumentationAbstractionType()
                       {
                           Documentations = new DocumentationsType()
                                                {
                                                    Documents = new DocumentType[]
                                                                    {
                                                                        GetDocument(operationAbstraction)

                                                                    }
                                                }
                       };
        }

        private static DocumentType GetDocument(OperationAbstractionType operationAbstraction)
        {
            DocumentType document = new DocumentType();
            int stackLevel = 1;
            document.Content = operationAbstraction.Operations.Operation.Select(
                operation => GetOperationContent(operation, stackLevel)).ToArray();
            return document;
        }

        private static HeaderType GetOperationContent(OperationType operation, int stackLevel)
        {
            HeaderType header = new HeaderType
                                    {
                                        text = operation.name,
                                        level = stackLevel,
                                    };
            List<HeaderType> subHeaders = new List<HeaderType>();
            subHeaders.AddRange(
                operation.Parameters.Parameter.Select(
                    param => GetParameterContent(param, stackLevel + 1)));
            subHeaders.AddRange(
                operation.Parameters.Items.Select(valmod => GetValidationModificationContent(valmod,
                    stackLevel + 1))
                );

            return header;
        }

        private static HeaderType GetValidationModificationContent(object validationModification, int stackLevel)
        {
            ValidationType validation = validationModification as ValidationType;
            ModificationType modification = validationModification as ModificationType;
            if (validation != null)
                return GetValidationContent(validation, stackLevel);
            else if (modification != null)
                return GetModificationContent(modification, stackLevel);
            else
                throw new NotSupportedException("ValidationModification type: " + validationModification.GetType().Name);
        }

        private static HeaderType GetModificationContent(ModificationType modification, int stackLevel)
        {
            HeaderType header = new HeaderType();
            header.text = "Modification: " + modification.name;
            header.level = stackLevel;
            string styleName = modification.state.ToString();
            header.SetHeaderTextContent(styleName, modification.designDesc);
            foreach (var target in modification.Target ?? new TargetType[0])
                header.AddHeaderTextContent(styleName, "Target: " + target.name);
            return header;
        }

        private static HeaderType GetValidationContent(ValidationType validation, int stackLevel)
        {
            HeaderType header = new HeaderType();
            header.text = "Validation: " + validation.name;
            header.level = stackLevel;
            string styleName = validation.state.ToString();
            header.SetHeaderTextContent(styleName, validation.designDesc);
            foreach (var target in validation.Target ?? new TargetType[0])
                header.AddHeaderTextContent(styleName, "Target: " + target.name);
            return header;
        }

        private static HeaderType GetParameterContent(VariableType parameter, int stackLevel)
        {
            HeaderType header = new HeaderType
                                    {
                                        text = parameter.name + " (" + parameter.dataType + ")",
                                        level = stackLevel,
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
