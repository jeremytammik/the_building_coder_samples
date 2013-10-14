#region Header
//
// CmdLandXml.cs - import LandXML data and create TopographySurface
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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using W = System.Windows.Forms;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdLandXml : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      W.OpenFileDialog dlg = new W.OpenFileDialog();

      // select file to open

      dlg.Filter = "LandXML files (*.xml)|*.xml";

      dlg.Title = "Import LandXML and "
        + "Create TopographySurface";

      if( dlg.ShowDialog() != W.DialogResult.OK )
      {
        return Result.Cancelled;
      }

      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.Load( dlg.FileName );

      XmlNodeList pnts
        = xmlDoc.GetElementsByTagName( "Pnts" );

      char[] separator = new char[] { ' ' };
      double x = 0, y = 0, z = 0;

      List<XYZ> pts = new List<XYZ>();

      for( int k = 0; k < pnts.Count; ++k )
      {
        for( int i = 0;
          i < pnts[k].ChildNodes.Count; ++i )
        {
          int j = 1;

          string text = pnts[k].ChildNodes[i].InnerText;
          string[] coords = text.Split( separator );

          foreach( string coord in coords )
          {
            switch( j )
            {
              case 1:
                x = Double.Parse( coord );
                break;
              case 2:
                y = Double.Parse( coord );
                break;
              case 3:
                z = Double.Parse( coord );
                break;
              default:
                break;
            }
            j++;
          }
          pts.Add( new XYZ( x, y, z ) );
        }
      }

      //TopographySurface surface = doc.Create.NewTopographySurface( pntList );

      //TopographySurface surface = doc.Create.NewTopographySurface( pts ); // 2013

      TopographySurface surface = TopographySurface.Create( doc, pts ); // 2014

      return Result.Succeeded;
    }
  }
}

// C:\a\doc\revit\blog\zip\LandXMLfiles\GSG_features_surfaces_with_volumes.xml