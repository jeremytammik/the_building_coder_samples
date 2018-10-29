using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;

using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;
using System.Windows.Forms;

namespace TestRebar
{
  [TransactionAttribute( TransactionMode.Manual )]
  public class TestRebar : IExternalCommand
  {
    UIApplication m_uiApp;
    Document m_doc;

    ElementId elementId = ElementId.InvalidElementId;

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      try
      {
        initStuff( commandData );
        if( m_doc == null )
          return Result.Failed;

        Element host = getRebarHost( commandData );
        if( host == null )
        {
          MessageBox.Show( "Null host" );
          return Result.Succeeded;
        }

        RebarShape shape = getRebarShape();
        if( shape == null )
        {
          MessageBox.Show( "Null shape" );
          return Result.Succeeded;
        }

        RebarBarType rebarType = getRebarBarType();
        if( rebarType == null )
        {
          MessageBox.Show( "Null rebarType" );
          return Result.Succeeded;
        }

        GeometryElement geometryElement 
          = host.get_Geometry( new Options() );

        // this will get the edges in family coordinates 
        // not the global coordinates

        IList<Curve> edges = getFaceEdges( geometryElement ); 

        Transform trf = Transform.Identity;
        FamilyInstance famInst = host as FamilyInstance;
        if( famInst != null )
          trf = famInst.GetTransform();

        // SOLUTION 1

        {
          XYZ origin, xAxisDir, yAxisDir, xAxisBox, yAxisBox;
          getOriginXandYvecFromFaceEdges( edges,
            out origin, out xAxisDir, out yAxisDir, 
            out xAxisBox, out yAxisBox );

          // we obtained origin, xAxis, yAxis in family 
          // coordinates. Now we will transform them in 
          // global coordinates

          origin = trf.OfPoint( origin );
          xAxisDir = trf.OfVector( xAxisDir );
          yAxisDir = trf.OfVector( yAxisDir );

          xAxisBox = trf.OfVector( xAxisBox );
          yAxisBox = trf.OfVector( yAxisBox );

          using( Transaction tr = new Transaction( m_doc ) )
          {
            tr.Start( "Create Rebar" );
            Rebar createdStirrupRebar = Rebar.CreateFromRebarShape(
              m_doc, shape, rebarType, host, origin, xAxisDir, yAxisDir );

            RebarShapeDrivenAccessor rebarStirrupShapeDrivenAccessor
              = createdStirrupRebar.GetShapeDrivenAccessor();
            rebarStirrupShapeDrivenAccessor.SetLayoutAsFixedNumber(
              5, 10, true, true, true );
            rebarStirrupShapeDrivenAccessor.ScaleToBox(
              origin, xAxisBox, yAxisBox );

            tr.Commit();
          }
        }

        // SOLUTION 2

        {
          IList<Curve> rebarSegments = new List<Curve>();

          // transform the edges in global coordinates
          IList<Curve> rebarSegms = new List<Curve>();
          foreach( Curve curve in edges )
            rebarSegms.Add( curve.CreateTransformed( trf ) );

          // Here you can also offset the curves to respect the cover.

          using( Transaction tr = new Transaction( m_doc ) )
          {
            tr.Start( "Create Rebar from Curves" );
            Rebar createdStirrupRebar = Rebar.CreateFromCurves(
              m_doc, RebarStyle.StirrupTie, rebarType, null, null,
              host, -XYZ.BasisX, rebarSegms, RebarHookOrientation.Left,
              RebarHookOrientation.Left, true, false );
            RebarShapeDrivenAccessor rebarStirrupShapeDrivenAccessor
              = createdStirrupRebar.GetShapeDrivenAccessor();
            rebarStirrupShapeDrivenAccessor.SetLayoutAsFixedNumber(
              10, 10, true, true, true );
            tr.Commit();
          }
        }
      }
      catch( Exception e )
      {
        TaskDialog.Show( "exception", e.Message );
        return Result.Failed;
      }

      return Result.Succeeded;
    }

    private void getOriginXandYvecFromFaceEdges(
      IList<Curve> edges, out XYZ origin, out XYZ xAxisDir,
      out XYZ yAxisDir, out XYZ xAxisBox, out XYZ yAxisBox )
    {
      origin = new XYZ();
      xAxisDir = new XYZ();
      yAxisDir = new XYZ();
      xAxisBox = new XYZ();
      yAxisBox = new XYZ();

      double minZ = double.MaxValue;

      for( int ii = 0; ii < edges.Count; ii++ )
      {
        Line edgeLine = edges[ii] as Line;
        if( edgeLine == null )
          continue;

        int nextii = ( ii + 1 ) % edges.Count;
        Line edgeLineNext = edges[nextii] as Line;
        if( edgeLineNext == null )
          continue;

        XYZ pntEnd = edgeLine.Evaluate( 1, true );
        if( pntEnd.Z < minZ )
        {
          minZ = pntEnd.Z;
          origin = pntEnd;
          // These two will be used by Rebar.CreateFromRebarShape.
          // For this the length is irrelevant:
          xAxisDir = edgeLine.Direction * -1;
          yAxisDir = edgeLineNext.Direction;
          // These two  will be used at Rebar.ScaleToBox.
          // For this, the length is important, because it
          // will represent the length of box segment.
          xAxisBox = xAxisDir * edgeLine.ApproximateLength;
          yAxisBox = yAxisDir * edgeLineNext.ApproximateLength;

          // Here you can also remove from the length 
          // the value of the cover.
        }
      }
    }

    private IList<Curve> getFaceEdges(
      GeometryElement geometryElement )
    {
      foreach( GeometryObject geometryObject in geometryElement )
      {
        Solid solid = geometryObject as Solid;
        if( solid != null )
        {
          FaceArray faces = solid.Faces;
          foreach( Face face in faces )
          {
            Plane plane = face.GetSurface() as Plane;
            if( AreEqual( plane.Normal.DotProduct( XYZ.BasisX ), -1 ) ) // vecs are parallel 
            {
              // There should be onlu one curve loop.
              // It can be multiple if the face have a hole.
              if( face.GetEdgesAsCurveLoops().Count != 1 )
                return null;

              IList<Curve> edgesArr = new List<Curve>();
              CurveLoopIterator cli = face
                .GetEdgesAsCurveLoops()[0]
                .GetCurveLoopIterator();

              while( cli.MoveNext() )
              {
                Curve edge = cli.Current;
                edgesArr.Add( edge );
              }
              return edgesArr;
            }
          }
        }
        else
        {
          GeometryInstance geometryInstance 
            = geometryObject as GeometryInstance;

          if( geometryInstance != null )
          {
            return getFaceEdges( geometryInstance
              .GetSymbolGeometry() );
          }
        }
      }
      return null;
    }

    private Element getRebarHost( 
      ExternalCommandData commandData )
    {
      m_uiApp = commandData.Application;
      Selection sel = m_uiApp.ActiveUIDocument.Selection;
      Reference refr = sel.PickObject( ObjectType.Element );
      return m_doc.GetElement( refr.ElementId );
    }

    private RebarBarType getRebarBarType()
    {
      FilteredElementCollector fec 
        = new FilteredElementCollector( m_doc )
          .OfClass( typeof( RebarBarType ) );

      return fec.FirstElement() as RebarBarType;
    }

    private RebarShape getRebarShape()
    {
      FilteredElementCollector fec
        = new FilteredElementCollector( m_doc )
          .OfClass( typeof( RebarShape ) );
      string strName = "Bügel Geschlossen";
      IList<Element> shapeElems = fec.ToElements();
      foreach( var shapeElem in shapeElems )
      {
        RebarShape shape = shapeElem as RebarShape;
        if( shape.Name.Contains( strName ) )
          return shape;
      }
      return null;
    }

    public static bool AreEqual(
      double firstValue, double secondValue,
      double tolerance = 1.0e-9 )
    {
      return ( secondValue - tolerance < firstValue
        && firstValue < secondValue + tolerance );
    }

    void initStuff( ExternalCommandData commandData )
    {
      m_uiApp = commandData.Application;
      m_doc = m_uiApp.ActiveUIDocument.Document;
    }
  }
}
