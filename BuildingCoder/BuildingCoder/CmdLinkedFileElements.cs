#region Header
//
// CmdLinkedFileElements.cs - list elements in linked files
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
#endregion // Namespaces

namespace BuildingCoder
{
  public class ElementData
  {
    string _document;
    string _elementName;
    int _id;
    double _x;
    double _y;
    double _z;
    string _uniqueId;
    string _folder;

    public ElementData(
      string path,
      string elementName,
      int id,
      double x,
      double y,
      double z,
      string uniqueId )
    {
      int i = path.LastIndexOf( "\\" );
      _document = path.Substring( i + 1 );
      _elementName = elementName;
      _id = id;
      _x = x;
      _y = y;
      _z = z;
      _uniqueId = uniqueId;
      _folder = path.Substring( 0, i );
    }

    public string Document {
      get { return _document; }
    }
    public string Element {
      get { return _elementName; }
    }
    public int Id {
      get { return _id; }
    }
    public string X {
      get { return Util.RealString( _x ); }
    }
    public string Y {
      get { return Util.RealString( _y ); }
    }
    public string Z {
      get { return Util.RealString( _z ); }
    }
    public string UniqueId {
      get { return _uniqueId; }
    }
    public string Folder {
      get { return _folder; }
    }
  }

  [Transaction( TransactionMode.ReadOnly )]
  class CmdLinkedFileElements : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet highlightElements )
    {
      /*

      // retrieve all link elements:

      Document doc = app.ActiveUIDocument.Document;
      List<Element> links = GetElements(
        BuiltInCategory.OST_RvtLinks,
        typeof( Instance ), app, doc );

      // determine the link paths:

      DocumentSet docs = app.Documents;
      int n = docs.Size;
      Dictionary<string, string> paths
        = new Dictionary<string, string>( n );

      foreach( Document d in docs )
      {
        string path = d.PathName;
        int i = path.LastIndexOf( "\\" ) + 1;
        string name = path.Substring( i );
        paths.Add( name, path );
      }
      */


      // retrieve lighting fixture element
      // data from linked documents:

      List<ElementData> data = new List<ElementData>();
      UIApplication app = commandData.Application;
      DocumentSet docs = app.Application.Documents;

      foreach( Document doc in docs )
      {
        FilteredElementCollector a
          = Util.GetElementsOfType( doc,
          typeof( FamilyInstance ),
          BuiltInCategory.OST_LightingFixtures );

        foreach( FamilyInstance e in a )
        {
          string name = e.Name;
          LocationPoint lp = e.Location as LocationPoint;
          if( null != lp )
          {
            XYZ p = lp.Point;
            data.Add( new ElementData( doc.PathName, e.Name,
              e.Id.IntegerValue, p.X, p.Y, p.Z, e.UniqueId ) );
          }
        }
      }

      // display data:

      using( CmdLinkedFileElementsForm dlg = new CmdLinkedFileElementsForm( data ) )
      {
        dlg.ShowDialog();
      }

      // this command does not modify the Revit document:

      return Result.Cancelled;
    }
  }
}
