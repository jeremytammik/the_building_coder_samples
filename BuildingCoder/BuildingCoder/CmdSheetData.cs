#region Header
//
// CmdSheetData.cs - export sheet data to XML file
//
// Copyright (C) 2010-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Xml;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Arbitrary sheet data container.
  /// </summary>
  class SheetData
  {
    public bool IsPlaceholder { get; set; }
    public string Name { get; set; }
    public string SheetNumber { get; set; }

    public SheetData( ViewSheet v )
    {
      IsPlaceholder = v.IsPlaceholder;
      Name = v.Name;
      SheetNumber = v.SheetNumber;
    }
  }

  /// <summary>
  /// Gather sheet data from document
  /// and export to XML file.
  /// </summary>
  [Transaction( TransactionMode.ReadOnly )]
  class CmdSheetData : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      // retrieve all sheets

      FilteredElementCollector a
        = new FilteredElementCollector( doc );

      a.OfCategory( BuiltInCategory.OST_Sheets );
      a.OfClass( typeof( ViewSheet ) );

      // create a collection of all relevant data

      List<SheetData> data = new List<SheetData>();

      foreach( ViewSheet v in a )
      {
        // create some data for each sheet and add
        // to some serializable collection called Data
        SheetData item = new SheetData( v );
        data.Add( item );
      }

      // write out data collection to xml

      XmlTextWriter w = new XmlTextWriter(
        "C:/SheetData.xml", null );

      w.Formatting = Formatting.Indented;
      w.WriteStartDocument();
      w.WriteComment( string.Format(
        " SheetData from {0} on {1} by Jeremy ",
        doc.PathName, DateTime.Now ) );

      w.WriteStartElement( "ViewSheets" );

      foreach( SheetData item in data )
      {
        w.WriteStartElement( "ViewSheet" );

        w.WriteElementString( "IsPlaceholder",
          item.IsPlaceholder.ToString() );

        w.WriteElementString( "Name", item.Name );

        w.WriteElementString( "SheetNumber",
          item.SheetNumber );

        w.WriteEndElement();
      }
      w.WriteEndElement();
      w.WriteEndDocument();
      w.Close();

      return Result.Succeeded;
    }
  }
}
