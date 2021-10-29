#region Header

//
// CmdSteelStairBeams.cs - create a series of connected mitered steel beams for a steel stair
//
// Copyright (C) 2011-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    internal class BeamCreator
    {
        private readonly Document _doc;

        public BeamCreator(Document doc)
        {
            _doc = doc;
        }

        public void Run()
        {
            var view = _doc.ActiveView;

            var level = view.GenLevel;

            if (null == level)
                throw new Exception(
                    "No level associated with view");

            var pt1 = Util.MmToFoot(new XYZ(0, 0, 1000));
            var pt2 = Util.MmToFoot(new XYZ(1000, 0, 1000));
            var pt3 = Util.MmToFoot(new XYZ(2000, 0, 2500));
            var pt4 = Util.MmToFoot(new XYZ(3000, 0, 2500));

            var familySymbol = Util.FindFamilySymbol(
                _doc,
                CmdSteelStairBeams.FamilyName,
                CmdSteelStairBeams.SymbolName);

            if (familySymbol == null) throw new Exception("Beam Family not found");

            CreateBeam(familySymbol, level, pt1, pt2);
            CreateBeam(familySymbol, level, pt2, pt3);
            CreateBeam(familySymbol, level, pt3, pt4);
        }

        private FamilyInstance CreateBeam(
            FamilySymbol familySymbol,
            Level level,
            XYZ startPt,
            XYZ endPt)
        {
            var structuralType
                = StructuralType.Beam;

            //Line line = _doc.Application.Create.NewLineBound( startPt, endPt ); // 2013
            var line = Line.CreateBound(startPt, endPt); // 2014

            var beam = _doc.Create
                .NewFamilyInstance(startPt, familySymbol,
                    level, structuralType);

            if (beam.Location is LocationCurve beamCurve) beamCurve.Curve = line;
            return beam;
        }
    }

    [Transaction(TransactionMode.Manual)]
    internal class CmdSteelStairBeams : IExternalCommand
    {
        public const string FamilyName
            = "RHS-Rectangular Hollow Section";

        public const string SymbolName
            = "160x80x4RHS";

        private const string _extension
            = ".rfa";

        private const string _directory
            = "C:/ProgramData/Autodesk/RAC 2012/Libraries"
              + "/US Metric/Structural/Framing/Steel/";

        private const string _family_path
            = _directory + FamilyName + _extension;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            using var t = new Transaction(doc);
            t.Start("Create Steel Stair Beams");

            // Check whether the required family is loaded:

            var collector
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family));

            // If the family is not already loaded, do so:

            if (!collector.Any(
                e => e.Name.Equals(FamilyName)))
            {
                FamilySymbol symbol;

                if (!doc.LoadFamilySymbol(
                    _family_path, SymbolName, out symbol))
                {
                    message = $"Unable to load '{SymbolName}' from '{_family_path}'.";

                    t.RollBack();

                    return Result.Failed;
                }
            }

            try
            {
                // Create a couple of connected beams:

                var s = new BeamCreator(doc);

                s.Run();

                t.Commit();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                t.RollBack();

                return Result.Failed;
            }
        }
    }
}

// C:\ProgramData\Autodesk\
//
// \RAC 2012\Libraries\US Metric\Structural\Framing\Steel\RHS-Rectangular Hollow Section.rfa
// \RST 2012\Libraries\US Metric\Structural\Framing\Steel\RHS-Rectangular Hollow Section.rfa