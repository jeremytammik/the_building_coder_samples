#region Header

//
// CmdNewBeamTypeInstance.cs - create a new
// beam type and insert an instance of it
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewBeamTypeInstance : IExternalCommand
    {
        private const string _family_name
            = "M_Concrete-Rectangular Beam";

        private const string _extension
            = ".rfa";

        private const string _directory
            = "C:/Documents and Settings/All Users"
              + "/Application Data/Autodesk/RAC 2009"
              + "/Metric Library/Structural/Framing"
              + "/Concrete/";

        private const string _path
            = _directory + _family_name + _extension;

        private readonly StructuralType stBeam
            = StructuralType.Beam;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var rc = Result.Failed;
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            // Check whether the family we are interested in is loaded:

#if _2010
      List<Element> symbols = new List<Element>();
      Filter filterFamily = creApp.Filter.NewFamilyFilter( _family_name );
      doc.get_Elements( filterFamily, symbols );

      // the family filter returns both the symbols and the family itself:

      Family f = null;
      foreach( Element e in symbols )
      {
        if( e is Family )
        {
          f = e as Family;
        }
        else if( e is FamilySymbol )
        {
          FamilySymbol s = e as FamilySymbol;
          Debug.Print( "Family name={0}, symbol name={1}", s.Family.Name, s.Name );
        }
      }
#endif // _2010

            var f = Util.GetFirstElementOfTypeNamed(
                doc, typeof(Family), _family_name) as Family;

            using var t = new Transaction(doc);
            t.Start("Create Beam Type and Instance");
            // If the family was not already loaded, then do so:

            if (null == f)
                if (!doc.LoadFamily(_path, out f))
                    message = $"Unable to load '{_path}'.";

            if (null != f)
            {
                Debug.Print("Family name={0}", f.Name);

                // Pick a symbol for duplication, any one will do,
                // we select the first:

                FamilySymbol s = null;

                //foreach( FamilySymbol s2 in f.Symbols ) // 2014

                foreach (var id in f.GetFamilySymbolIds()) // 2015
                {
                    s = doc.GetElement(id) as FamilySymbol;
                    break;
                }

                Debug.Assert(null != s, "expected at least one symbol to be defined in family");

                // Duplicate the existing symbol:

                var s1 = s.Duplicate("Nuovo simbolo");
                s = s1 as FamilySymbol;

                // Analyse the symbol parameters:

                foreach (Parameter param in s.Parameters) Debug.Print("Parameter name={0}, value={1}", param.Definition.Name, param.AsValueString());

                // Define new dimensions for our new type;
                // the specified parameter name is case sensitive:

                //s.get_Parameter( "b" ).Set( Util.MmToFoot( 500 ) ); // 2014
                //s.get_Parameter( "h" ).Set( Util.MmToFoot( 1000 ) ); // 2014

                s.LookupParameter("b").Set(Util.MmToFoot(500)); // 2015
                s.LookupParameter("h").Set(Util.MmToFoot(1000)); // 2015

                // we can change the symbol name at any time:

                s.Name = "Nuovo simbolo due";

                // insert an instance of our new symbol:

                var creApp = app.Application.Create;
                var creDoc = doc.Create;

                // It is possible to insert a beam,
                // which normally uses a location line,
                // by specifying only a location point:

                //XYZ p = XYZ.Zero;
                //doc.Create.NewFamilyInstance( p, s, nonStructural );

                var p = XYZ.Zero;
                var q = creApp.NewXYZ(30, 20, 20); // feet
                var line = Line.CreateBound(p, q);

                // Specifying a non-structural type here means no beam
                // is created, and results in a null family instance:

                var fi = creDoc.NewFamilyInstance(
                    line, s, null, stBeam);

                // This creates a visible family instance,
                // but the resulting beam has no location line
                // and behaves strangely, e.g. cannot be selected:
                //FamilyInstance fi = doc.Create.NewFamilyInstance(
                //  p, s, q, null, nonStructural );

                //List<Element> levels = new List<Element>();
                //doc.get_Elements( typeof( Level ), levels );
                //Debug.Assert( 0 < levels.Count,
                //  "expected at least one level in model" );
                //Level level = levels[0] as Level;
                //fi = doc.Create.NewFamilyInstance(
                //  line, s, level, nonStructural );

                rc = Result.Succeeded;
            }

            t.Commit();

            return rc;
        }
    }
}