#region Header
//
// CmdLinkedFiles.cs - retrieve linked files
// in current project
//
// Copyright (C) 2008-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdLinkedFiles : IExternalCommand
  {
    FilteredElementCollector GetLinkedFiles(
      Document doc )
    {
      return Util.GetElementsOfType( doc,
        typeof( Instance ),
        BuiltInCategory.OST_RvtLinks );
    }

    Dictionary<string,string> GetFilePaths(
      Application app,
      bool onlyImportedFiles )
    {
      DocumentSet docs = app.Documents;
      int n = docs.Size;

      Dictionary<string, string> dict
        = new Dictionary<string, string>( n );

      foreach( Document doc in docs )
      {
        if( !onlyImportedFiles
          || ( null == doc.ActiveView ) )
        {
          string path = doc.PathName;
          int i = path.LastIndexOf( "\\" ) + 1;
          string name = path.Substring( i );
          dict.Add( name, path );
        }
      }
      return dict;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      Application app = uiapp.Application;
      Document doc = uiapp.ActiveUIDocument.Document;

      Dictionary<string, string> dict
        = GetFilePaths( app, true );

      IList<Element> links
        = GetLinkedFiles( doc ).ToElements();

      int n = links.Count;
      Debug.Print(
        "There {0} {1} linked Revit model{2}.",
        ( 1 == n ? "is" : "are" ), n,
        Util.PluralSuffix( n ) );

      string name;
      char[] sep = new char[] { ':' };
      string[] a;

      foreach( Element link in links )
      {
        name = link.Name;
        a = name.Split( sep );
        name = a[0].Trim();

        Debug.Print(
          "Link '{0}' full path is '{1}'.",
          name, dict[name] );

        #region Explore Location
        Location loc = link.Location; // unknown content in here
        LocationPoint lp = loc as LocationPoint;
        if( null != lp )
        {
          XYZ p = lp.Point;
        }
        GeometryElement e = link.get_Geometry( new Options() );
        if( null != e ) // no geometry defined
        {
          //GeometryObjectArray objects = e.Objects; // 2012
          //n = objects.Size; // 2012
          n = e.Count<GeometryObject>(); // 2013
        }
        #endregion // Explore Location

        #region Explore Pinning
        if( link is ImportInstance ) // nope, this never happens ...
        {
          ImportInstance i = link as ImportInstance;
          string s = i.Pinned ? "" : "not ";
          Debug.Print( "{1}pinned", s );
          i.Pinned = !i.Pinned;
        }
        #endregion // Explore Pinning
      }
      return Result.Succeeded;
    }
  }
}
