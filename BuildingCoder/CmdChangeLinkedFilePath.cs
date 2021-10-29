#region Header

//
// CmdChangeLinkedFilePath.cs - change linked RVT file path
//
// Copyright (C) 2011-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
    ///     This command will change the path of all linked
    ///     Revit files the next time the document at the
    ///     given location is opened.
    ///     Please refer to the TransmissionData reference
    ///     for more details.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class CmdChangeLinkedFilePath : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            var location = new FilePath("C:/file.rvt");

            var transData
                = TransmissionData.ReadTransmissionData(
                    location);

            if (null != transData)
            {
                // Collect all (immediate) external
                // references in the model

                var externalReferences
                    = transData.GetAllExternalFileReferenceIds();

                // Find every reference that is a link

                foreach (var refId in externalReferences)
                {
                    var extRef
                        = transData.GetLastSavedReferenceData(
                            refId);

                    if (extRef.ExternalFileReferenceType
                        == ExternalFileReferenceType.RevitLink)
                        // Change the path of the linked file,
                        // leaving everything else unchanged:

                        transData.SetDesiredReferenceData(refId,
                            new FilePath("C:/MyNewPath/cut.rvt"),
                            extRef.PathType, true);
                }

                // Make sure the IsTransmitted property is set

                transData.IsTransmitted = true;

                // Modified transmission data must be saved
                // back to the model

                TransmissionData.WriteTransmissionData(
                    location, transData);
            }
            else
            {
                TaskDialog.Show("Unload Links",
                    "The document does not have"
                    + " any transmission data");
            }

            return Result.Succeeded;
        }
    }
}