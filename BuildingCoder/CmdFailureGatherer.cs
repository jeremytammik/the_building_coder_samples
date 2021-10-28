#region Header

//
// CmdFailureGatherer.cs - gather and show warning messages with IFailuresPreprocessor
//
// Copyright (C) 2018-2020 by Mastjaso and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Collect all failure message description strings.
    /// </summary>
    internal class MessageDescriptionGatheringPreprocessor : IFailuresPreprocessor
    {
        public MessageDescriptionGatheringPreprocessor()
        {
            FailureList = new List<string>();
        }

        private List<string> FailureList { get; }

        public FailureProcessingResult PreprocessFailures(
            FailuresAccessor failuresAccessor)
        {
            foreach (var fMA
                in failuresAccessor.GetFailureMessages())
            {
                FailureList.Add(fMA.GetDescriptionText());
                var FailDefID
                    = fMA.GetFailureDefinitionId();

                //if (FailDefID == BuiltInFailures
                //  .GeneralFailures.DuplicateValue)
                //    failuresAccessor.DeleteWarning(fMA);
            }

            return FailureProcessingResult.Continue;
        }

        public void ShowDialogue()
        {
            var s = string.Join("\r\n", FailureList);

            TaskDialog.Show("Post Processing Failures:", s);
        }
    }

    [Transaction(TransactionMode.Manual)]
    internal class CmdFailureGatherer : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiApp = commandData.Application;
            var doc = uiApp.ActiveUIDocument.Document;

            var pp
                = new MessageDescriptionGatheringPreprocessor();

            using (var t = new Transaction(doc))
            {
                var ops
                    = t.GetFailureHandlingOptions();

                ops.SetFailuresPreprocessor(pp);
                t.SetFailureHandlingOptions(ops);

                t.Start("Marks");

                // Generate a 'duplicate mark' warning message:

                var specEqu
                    = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_SpecialityEquipment)
                        .WhereElementIsNotElementType()
                        .ToElements();

                if (specEqu.Count >= 2)
                    for (var i = 0; i < 2; i++)
                        specEqu[i].get_Parameter(
                            BuiltInParameter.ALL_MODEL_MARK).Set(
                            "Duplicate Mark");

                // Generate an 'duplicate wall' warning message:

                var level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .FirstElement();

                var line = Line.CreateBound(XYZ.Zero, 10 * XYZ.BasisX);
                var wall1 = Wall.Create(doc, line, level.Id, false);
                var wall2 = Wall.Create(doc, line, level.Id, false);

                t.Commit();
            }

            pp.ShowDialogue();

            return Result.Succeeded;
        }
    }
}