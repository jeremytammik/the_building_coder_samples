#region Header
//
// CmdNewColumnTypeInstance.cs - create a new
// column type and insert an instance of it
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdNewColumnTypeInstance : IExternalCommand
  {
    const string _family_name
      = "M_Rectangular Column";

    const string _extension
      = ".rfa";

    //const string _directory
    //  = "C:/Documents and Settings/All Users"
    //  + "/Application Data/Autodesk/RAC 2009"
    //  + "/Metric Library/Columns/";

    const string _directory
      = "C:/ProgramData/Autodesk/RAC 2012"
      + "/Libraries/US Metric/Columns/";

    const string _path
      = _directory + _family_name + _extension;

    StructuralType nonStructural
      = StructuralType.NonStructural;

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Result rc
        = Result.Failed;

      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      // check whether the family we are
      // interested in is loaded:

#if _2010
      List<Element> symbols = new List<Element>();

      Filter filter = app.Create.Filter.NewFamilyFilter(
        _family_name );

      doc.get_Elements( filter, symbols );

      // the family filter returns both the
      // symbols and the family itself:

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
          Debug.Print(
            "Family name={0}, symbol name={1}",
            s.Family.Name, s.Name );
        }
      }
#endif

      Family f = Util.GetFirstElementOfTypeNamed(
        doc, typeof( Family ), _family_name ) as Family;


      // if the family was not already loaded, then do so:

      if( null == f )
      {
        if( !doc.LoadFamily( _path, out f ) )
        {
          message = "Unable to load '" + _path + "'.";
        }
      }

      if( null != f )
      {
        Debug.Print( "Family name={0}", f.Name );

        // pick a symbol for duplication, any one will do,
        // we select the first:

        FamilySymbol s = null;
        foreach( FamilySymbol s2 in f.Symbols )
        {
          s = s2;
          break;
        }
        Debug.Assert( null != s,
          "expected at least one symbol"
          + " to be defined in family" );

        // duplicate the existing symbol:

        ElementType s1 = s.Duplicate( "Nuovo simbolo" );
        s = s1 as FamilySymbol;

        // analyse the symbol parameters:

        foreach( Parameter param in s.Parameters )
        {
          Debug.Print(
            "Parameter name={0}, value={1}",
            param.Definition.Name,
            param.AsValueString() );
        }

        // define new dimensions for our new type;
        // the specified parameter name is case sensitive:

        s.get_Parameter( "Width" ).Set(
          Util.MmToFoot( 500 ) );

        s.get_Parameter( "Depth" ).Set(
          Util.MmToFoot( 1000 ) );

        // we can change the symbol name at any time:

        s.Name = "Nuovo simbolo due";

        // insert an instance of our new symbol:

        XYZ p = XYZ.Zero;
        doc.Create.NewFamilyInstance(
          p, s, nonStructural );

        // for a column, the reference direction is ignored:

        //XYZ normal = new XYZ( 1, 2, 3 );
        //doc.Create.NewFamilyInstance(
        //  p, s, normal, null, nonStructural );
        rc = Result.Succeeded;
      }
      return rc;
    }
  }
}
