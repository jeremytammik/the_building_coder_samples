#region Header
//
// CmdFamilyParamValue.cs - list family parameter values
// defined on the types in a family document
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
using Autodesk.Revit.UI;
//using Autodesk.Revit.Collections;
using Autodesk.Revit.UI.Selection;


#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdFamilyParamValue : IExternalCommand
  {
    static string FamilyParamValueString(
      FamilyType t,
      FamilyParameter fp,
      Document doc )
    {
      string value = t.AsValueString( fp );
      switch( fp.StorageType )
      {
        case StorageType.Double:
          value = Util.RealString(
            ( double ) t.AsDouble( fp ) )
            + " (double)";
          break;

        case StorageType.ElementId:
          ElementId id = t.AsElementId( fp );
          Element e = doc.GetElement( id );
          value = id.IntegerValue.ToString() + " ("
            + Util.ElementDescription( e ) + ")";
          break;

        case StorageType.Integer:
          value = t.AsInteger( fp ).ToString()
            + " (int)";
          break;

        case StorageType.String:
          value = "'" + t.AsString( fp )
            + "' (string)";
          break;
      }
      return value;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;
      if( !doc.IsFamilyDocument )
      {
        message =
          "Please run this command in a family document.";
      }
      else
      {
        FamilyManager mgr = doc.FamilyManager;

        int n = mgr.Parameters.Size;

        Debug.Print(
          "\nFamily {0} has {1} parameter{2}.",
          doc.Title, n, Util.PluralSuffix( n ) );

        Dictionary<string, FamilyParameter> fps
          = new Dictionary<string, FamilyParameter>( n );

        foreach( FamilyParameter fp in mgr.Parameters )
        {
          string name = fp.Definition.Name;
          fps.Add( name, fp );

          #region Look at associated parameters
#if LOOK_AT_ASSOCIATED_PARAMETERS
          ParameterSet ps = fp.AssociatedParameters;
          n = ps.Size;

          string values = string.Empty;
          foreach( Parameter p in ps )
          {
            if( 0 == values.Length )
            {
              values = " ";
            }
            else
            {
              values += ", ";
            }
            values += p.AsValueString();
          }

          Debug.Print(
            "Parameter {0} has {1} associated parameter{2}{3}{4}.",
            name,
            n,
            PluralSuffix( n ),
            ( 0 < n ? ":" : "" ),
            values );
#endif // LOOK_AT_ASSOCIATED_PARAMETERS
          #endregion // Look at associated parameters

        }
        List<string> keys = new List<string>( fps.Keys );
        keys.Sort();

        n = mgr.Types.Size;

        Debug.Print(
          "Family {0} has {1} type{2}{3}",
          doc.Title,
          n,
          Util.PluralSuffix( n ),
          Util.DotOrColon( n ) );

        foreach( FamilyType t in mgr.Types )
        {
          string name = t.Name;
          Debug.Print( "  {0}:", name );
          foreach( string key in keys )
          {
            FamilyParameter fp = fps[key];
            if( t.HasValue( fp ) )
            {
              string value
                = FamilyParamValueString( t, fp, doc );

              Debug.Print( "    {0} = {1}", key, value );
            }
          }
        }
      }

      #region Exercise ExtractPartAtomFromFamilyFile

      // by the way, here is a completely different way to
      // get at all the parameter values, and all the other
      // family information as well:

      bool exercise_this_method = false;

      if( doc.IsFamilyDocument && exercise_this_method )
      {
        string path = doc.PathName;
        if( 0 < path.Length )
        {
          app.Application.ExtractPartAtomFromFamilyFile(
            path, path + ".xml" );
        }
      }
      #endregion // Exercise ExtractPartAtomFromFamilyFile

      return Result.Failed;
    }
  }
}
