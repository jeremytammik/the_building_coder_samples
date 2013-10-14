#region Header
//
// CmdRectDuctCorners.cs - determine the corners of a rectangular duct
//
// Copyright (C) 2010-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdRectDuctCorners : IExternalCommand
  {
    static XYZ Test1( Connector connector )
    {
      XYZ p = connector.CoordinateSystem.OfPoint(
        new XYZ( connector.Width / 2,
          connector.Height / 2, 0 ) );

      return p;
    }

    static XYZ Test2( Connector connector )
    {
      XYZ p = connector.CoordinateSystem.OfPoint(
        new XYZ( connector.Height / 2,
          connector.Width / 2, 0 ) );

      return p;
    }

    /// <summary>
    /// Return the first rectangular connector of the given duct element.
    /// </summary>
    static bool GetFirstRectangularConnector(
      Duct duct,
      out Connector c1 )
    {
      c1 = null;

      ConnectorSet connectors
        = duct.ConnectorManager.Connectors;

      if( 0 < connectors.Size )
      {
        foreach( Connector c in connectors )
        {
          if( ConnectorProfileType.Rectangular
            == c.Shape )
          {
            c1 = c;
            break;
          }
          else
          {
            Trace.WriteLine( "Connector shape: "
              + c.Shape );
          }
        }
      }
      return null != c1;
    }

    /// <summary>
    /// Return true if the given face contains the given connector.
    /// </summary>
    static bool FaceContainsConnector(
      Face face,
      Connector c )
    {
      XYZ p = c.Origin;

      IntersectionResult result = face.Project( p );

      return null != result
        && Math.Abs( result.Distance ) < 1e-9;
    }

    /// <summary>
    /// Analyse the given duct element:
    /// determine its first rectangular connector,
    /// retrieve its solid,
    /// find the face containing the connector,
    /// and list its four vertices.
    /// </summary>
    static bool AnalyseDuct( Duct duct )
    {
      bool rc = false;

      Connector c1;
      if( !GetFirstRectangularConnector( duct, out c1 ) )
      {
        Trace.TraceError( "The duct is not rectangular!" );
      }
      else
      {
        Options opt = new Options();
        opt.DetailLevel = ViewDetailLevel.Fine;
        GeometryElement geoElement = duct.get_Geometry( opt );

        //foreach( GeometryObject obj in geoElement.Objects ) // 2012

        foreach( GeometryObject obj in geoElement ) // 2013
        {
          Solid solid = obj as Solid;
          if( solid != null )
          {
            bool foundFace = false;
            foreach( Face face in solid.Faces )
            {
              foundFace = FaceContainsConnector( face, c1 );
              if( foundFace )
              {
                Trace.WriteLine( "==> Four face corners:" );

                EdgeArray a = face.EdgeLoops.get_Item( 0 );

                foreach( Edge e in a )
                {
                  XYZ p = e.Evaluate( 0.0 );

                  Trace.WriteLine( "Point = "
                    + Util.PointString( p ) );
                }
                rc = true;
                break;
              }
            }
            if( !foundFace )
            {
              Trace.WriteLine( "[Error] Face not found" );
            }
          }
        }
      }
      return rc;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      if( ProductType.MEP != app.Application.Product )
      {
        message = "Please run this command in Revit MEP.";
        return Result.Failed;
      }

      SelElementSet sel = uidoc.Selection.Elements;

      if( 0 == sel.Size )
      {
        message = "Please select some rectangular ducts.";
        return Result.Failed;
      }

      // set up log file:

      string log = Assembly.GetExecutingAssembly().Location
        + "." + DateTime.Now.ToString( "yyyyMMdd" )
        + ".log";

      if( File.Exists( log ) )
      {
        File.Delete( log );
      }

      TraceListener listener
        = new TextWriterTraceListener( log );

      Trace.Listeners.Add( listener );

      try
      {
        Trace.WriteLine( "Begin" );

        // loop over all selected ducts:

        foreach( Duct duct in sel )
        {
          if( null == duct )
          {
            Trace.TraceError( "The selection is not a duct!" );
          }
          else
          {
            // process each duct:

            Trace.WriteLine( "========================" );
            Trace.WriteLine( "Duct: Id = " + duct.Id.IntegerValue );

            AnalyseDuct( duct );
          }
        }
      }
      catch( Exception ex )
      {
        Trace.WriteLine( ex.ToString() );
      }
      finally
      {
        Trace.Flush();
        listener.Close();
        Trace.Close();
        Trace.Listeners.Remove( listener );
      }
      return Result.Failed;
    }
  }
}
