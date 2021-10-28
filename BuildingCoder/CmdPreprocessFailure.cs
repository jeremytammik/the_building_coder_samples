#region Header

//
// CmdPreprocessFailure.cs - suppress warning message by implementing the IFailuresPreprocessor interface
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Here is some code that is in the RevitAPI.chm
    ///     as a snippet for IFailuresPreprocessor Interface).
    ///     It creates an unbounded room and suppresses the
    ///     warning ("Room is not in a properly enclosed region")
    ///     that would otherwise be given.
    ///     The duration for this implementation is only for
    ///     the transaction in the external command, so after
    ///     the command is executed manually placed unbounded
    ///     rooms do result in the warning.
    ///     However, it is also possible with the new failure
    ///     API to suppress warnings for the entire Revit session.
    ///     By Harry Mattison.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    internal class CmdPreprocessFailure : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var doc = commandData.Application
                .ActiveUIDocument.Document;

            using var t = new Transaction(doc);
            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(Level));
            var level = collector.FirstElement() as Level;

            t.Start("Create unbounded room");

            var failOpt
                = t.GetFailureHandlingOptions();

            failOpt.SetFailuresPreprocessor(
                new RoomWarningSwallower());

            t.SetFailureHandlingOptions(failOpt);

            doc.Create.NewRoom(level, new UV(0, 0));

            t.Commit();

            return Result.Succeeded;
        }

        #region General Warning Swallower

        private FailureProcessingResult PreprocessFailures(
            FailuresAccessor a)
        {
            var failures
                = a.GetFailureMessages();

            foreach (var f in failures)
            {
                var fseverity = a.GetSeverity();

                if (fseverity == FailureSeverity.Warning)
                {
                    a.DeleteWarning(f);
                }
                else
                {
                    a.ResolveFailure(f);
                    return FailureProcessingResult.ProceedWithCommit;
                }
            }

            return FailureProcessingResult.Continue;
        }

        #endregion // General Warning Swallower

        public class RoomWarningSwallower : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(
                FailuresAccessor a)
            {
                // inside event handler, get all warnings

                var failures
                    = a.GetFailureMessages();

                foreach (var f in failures)
                {
                    // check failure definition ids
                    // against ones to dismiss:

                    var id
                        = f.GetFailureDefinitionId();

                    if (BuiltInFailures.RoomFailures.RoomNotEnclosed
                        == id)
                        a.DeleteWarning(f);
                }

                return FailureProcessingResult.Continue;
            }
        }
    }
}