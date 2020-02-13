#region Header
//
// CmdRollingOffset.cs - calculate a rolling offset pipe segment between two existing pipes and hook them up
//
// Copyright (C) 2013-2020 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using StructuralType = Autodesk.Revit.DB.Structure.StructuralType;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdRollingOffset : IExternalCommand
  {
    /// <summary>
    /// This command can place either a model line
    /// to represent the rolling offset calculation
    /// result, or insert a real pipe segment and the 
    /// associated fittings.
    /// </summary>
    static bool _place_model_line = false;

    /// <summary>
    /// Place the two 45 degree fittings and connect 
    /// them instead of explicitly placing the 
    /// rolling offset pipe segment.
    /// </summary>
    static bool _place_fittings = false;

    /// <summary>
    /// Switch between the new static Pipe.Create
    /// method and the obsolete 
    /// Document.Create.NewPipe.
    /// </summary>
    static bool _use_static_pipe_create = true;

    const string _prompt
      = "Please run this in a model containing "
      + "exactly two parallel offset pipe elements, "
      + "and they will be automatically selected. "
      + "Alternatively, pre-select two pipe elements "
      + "before launching this command, or post-select "
      + "them when prompted.";

    const BuiltInParameter bipDiameter
      = BuiltInParameter.RBS_PIPE_DIAMETER_PARAM;

    #region Determine Pipe Wall Thickness
    const BuiltInParameter bipDiameterInner
      = BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM;

    const BuiltInParameter bipDiameterOuter
      = BuiltInParameter.RBS_PIPE_OUTER_DIAMETER;

    static double GetWallThickness( Pipe pipe )
    {
      double dinner = pipe.get_Parameter(
        bipDiameterInner ).AsDouble();

      double douter = pipe.get_Parameter(
        bipDiameterOuter ).AsDouble();

      return 0.5 * ( douter - dinner );
    }
    #endregion // Determine Pipe Wall Thickness

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      //// Select all pipes in the entire model.

      //List<Pipe> pipes = new List<Pipe>(
      //  new FilteredElementCollector( doc )
      //    .OfClass( typeof( Pipe ) )
      //    .ToElements()
      //    .Cast<Pipe>() );

      //int n = pipes.Count;

      //// If there are less than two, 
      //// there is nothing we can do.

      //if( 2 > n )
      //{
      //  message = _prompt;
      //  return Result.Failed;
      //}

      //// If there are exactly two, pick those.

      //if( 2 < n )
      //{
      //  // Else, check for a pre-selection.

      //  pipes.Clear();

      //  Selection sel = uidoc.Selection;

      //  //n = sel.Elements.Size; // 2014

      //  ICollection<ElementId> ids
      //    = sel.GetElementIds(); // 2015

      //  n = ids.Count; // 2015

      //  Debug.Print( "{0} pre-selected elements.",
      //    n );

      //  // If two or more model pipes were pre-
      //  // selected, use the first two encountered.

      //  if( 1 < n )
      //  {
      //    //foreach( Element e in sel.Elements ) // 2014

      //    foreach( ElementId id in ids ) // 2015
      //    {
      //      Pipe c = doc.GetElement( id ) as Pipe;

      //      if( null != c )
      //      {
      //        pipes.Add( c );

      //        if( 2 == pipes.Count )
      //        {
      //          Debug.Print( "Found two model pipes, "
      //            + "ignoring everything else." );

      //          break;
      //        }
      //      }
      //    }
      //  }

      //  // Else, prompt for an 
      //  // interactive post-selection.

      //  if( 2 != pipes.Count )
      //  {
      //    pipes.Clear();

      //    try
      //    {
      //      Reference r = sel.PickObject(
      //        ObjectType.Element,
      //        new PipeElementSelectionFilter(),
      //        "Please pick first pipe." );

      //      pipes.Add( doc.GetElement( r.ElementId )
      //        as Pipe );
      //    }
      //    catch( Autodesk.Revit.Exceptions
      //      .OperationCanceledException )
      //    {
      //      return Result.Cancelled;
      //    }

      //    try
      //    {
      //      Reference r = sel.PickObject(
      //        ObjectType.Element,
      //        new PipeElementSelectionFilter(),
      //        "Please pick second pipe." );

      //      pipes.Add( doc.GetElement( r.ElementId )
      //        as Pipe );
      //    }
      //    catch( Autodesk.Revit.Exceptions
      //      .OperationCanceledException )
      //    {
      //      return Result.Cancelled;
      //    }
      //  }
      //}

      JtPairPicker<Pipe> picker
        = new JtPairPicker<Pipe>( uidoc );

      Result rc = picker.Pick();

      if( Result.Failed == rc )
      {
        message = _prompt;
      }

      if( Result.Succeeded != rc )
      {
        return rc;
      }

      IList<Pipe> pipes = picker.Selected;

      // Check for same pipe system type.

      ElementId systemTypeId
        = pipes[0].MEPSystem.GetTypeId();

      Debug.Assert( pipes[1].MEPSystem.GetTypeId()
        .IntegerValue.Equals(
          systemTypeId.IntegerValue ),
        "expected two similar pipes" );

      // Check for same pipe level.

      ElementId levelId = pipes[0].LevelId;

      Debug.Assert( 
        pipes[1].LevelId.IntegerValue.Equals(
          levelId.IntegerValue ),
        "expected two pipes on same level" );

      // Extract data from the two selected pipes.

      double wall_thickness = GetWallThickness( pipes[0] );

      Debug.Print( "{0} has wall thickness {1}",
        Util.ElementDescription( pipes[0] ),
        Util.RealString( wall_thickness ) );

      Curve c0 = pipes[0].GetCurve();
      Curve c1 = pipes[1].GetCurve();

      if( !( c0 is Line ) || !( c1 is Line ) )
      {
        message = _prompt
          + " Expected straight pipes.";

        return Result.Failed;
      }

      XYZ p00 = c0.GetEndPoint( 0 );
      XYZ p01 = c0.GetEndPoint( 1 );

      XYZ p10 = c1.GetEndPoint( 0 );
      XYZ p11 = c1.GetEndPoint( 1 );

      XYZ v0 = p01 - p00;
      XYZ v1 = p11 - p10;

      if( !Util.IsParallel( v0, v1 ) )
      {
        message = _prompt
          + " Expected parallel pipes.";

        return Result.Failed;
      }

      // Select the two pipe endpoints
      // that are farthest apart.

      XYZ p0 = p00.DistanceTo( p10 ) > p01.DistanceTo( p10 )
        ? p00
        : p01;

      XYZ p1 = p10.DistanceTo( p0 ) > p11.DistanceTo( p0 )
        ? p10
        : p11;

      XYZ pm = 0.5 * ( p0 + p1 );

      XYZ v = p1 - p0;

      if( Util.IsParallel( v, v0 ) )
      {
        message = "The selected pipes are colinear.";
        return Result.Failed;
      }

      // Normal vector of the plane defined by the
      // two parallel and offset pipes, which is
      // the plane hosting the rolling offset

      XYZ z = v.CrossProduct( v1 );

      // Vector perpendicular to v0 and v0 and
      // z, i.e. vector pointing from the first pipe
      // to the second in the cross sectional view.

      XYZ w = z.CrossProduct( v1 ).Normalize();

      // Offset distance perpendicular to pipe direction

      double distanceAcross = Math.Abs(
        v.DotProduct( w ) );

      // Distance between endpoints parallel 
      // to pipe direction

      double distanceAlong = Math.Abs(
        v.DotProduct( v1.Normalize() ) );

      Debug.Assert( Util.IsEqual( v.GetLength(),
        Math.Sqrt( distanceAcross * distanceAcross
          + distanceAlong * distanceAlong ) ),
        "expected Pythagorean equality here" );

      // The required offset pipe angle.

      double angle = 45 * Math.PI / 180.0;

      // The angle on the other side.

      double angle2 = 0.5 * Math.PI - angle;

      double length = distanceAcross * Math.Tan( angle2 );

      double halfLength = 0.5 * length;

      // How long should the pipe stubs become?

      double remainingPipeLength
        = 0.5 * ( distanceAlong - length );

      if( 0 > v1.DotProduct( v ) )
      {
        v1.Negate();
      }

      v1 = v1.Normalize();

      XYZ q0 = p0 + remainingPipeLength * v1;

      XYZ q1 = p1 - remainingPipeLength * v1;

      using( Transaction tx = new Transaction( doc ) )
      {
        // Determine pipe diameter for creating 
        // matching pipes and fittings

        Pipe pipe = pipes[0];

        double diameter = pipe
          .get_Parameter( bipDiameter ) // "Diameter"
          .AsDouble();

        // Pipe type for calls to doc.Create.NewPipe

        PipeType pipe_type_standard
          = new FilteredElementCollector( doc )
            .OfClass( typeof( PipeType ) )
            .Cast<PipeType>()
            .Where<PipeType>( e
              => e.Name.Equals( "Standard" ) )
            .FirstOrDefault<PipeType>();

        Debug.Assert(
          pipe_type_standard.Id.IntegerValue.Equals(
            pipe.PipeType.Id.IntegerValue ),
          "expected all pipes in this simple "
          + "model to use the same pipe type" );

        tx.Start( "Rolling Offset" );

        if( _place_model_line )
        {
          // Trim or extend existing pipes

          ( pipes[0].Location as LocationCurve ).Curve
            = Line.CreateBound( p0, q0 );

          ( pipes[1].Location as LocationCurve ).Curve
            = Line.CreateBound( p1, q1 );

          // Add a model line for the rolling offset pipe

          Creator creator = new Creator( doc );

          Line line = Line.CreateBound( q0, q1 );

          creator.CreateModelCurve( line );

          pipe = null;
        }
        else if( _place_fittings )
        {
          // Set active work plane to the rolling 
          // offset plane... removed again, since
          // this has no effect at all on the 
          // fitting placement or rotation.
          //
          //Plane plane = new Plane( z, q0 );
          //
          //SketchPlane sp = SketchPlane.Create( 
          //  doc, plane );
          //
          //uidoc.ActiveView.SketchPlane = sp;
          //uidoc.ActiveView.ShowActiveWorkPlane();

          FamilySymbol symbol
            = new FilteredElementCollector( doc )
              .OfClass( typeof( FamilySymbol ) )
              .OfCategory( BuiltInCategory.OST_PipeFitting )
              .Cast<FamilySymbol>()
              .Where<FamilySymbol>( e
                => e.Family.Name.Contains( "Elbow - Generic" ) )
              .FirstOrDefault<FamilySymbol>();

          // Set up first 45 degree elbow fitting

          FamilyInstance fitting0 = doc.Create
            .NewFamilyInstance( q0, symbol,
              StructuralType.NonStructural );

          fitting0.LookupParameter( "Angle" ).Set(
            45.0 * Math.PI / 180.0 );

          //fitting0.get_Parameter( bipDiameter ) // does not exist
          //  .Set( diameter );

          fitting0.LookupParameter( "Nominal Radius" )
            .Set( 0.5 * diameter );

          Line axis = Line.CreateBound( p0, q0 );
          angle = z.AngleTo( XYZ.BasisZ );

          ElementTransformUtils.RotateElement(
            doc, fitting0.Id, axis, Math.PI - angle );

          Connector con0 = Util.GetConnectorClosestTo(
            fitting0, p0 );

          // Trim or extend existing pipe

          ( pipes[0].Location as LocationCurve ).Curve
            = Line.CreateBound( p0, con0.Origin );

          // Connect pipe to fitting

          Util.Connect( con0.Origin, pipe, fitting0 );

          // Set up second 45 degree elbow fitting

          FamilyInstance fitting1 = doc.Create
            .NewFamilyInstance( q1, symbol,
              StructuralType.NonStructural );

          //fitting1.get_Parameter( "Angle" ).Set( 45.0 * Math.PI / 180.0 ); // 2014
          //fitting1.get_Parameter( "Nominal Radius" ).Set( 0.5 * diameter ); // 2014

          fitting1.LookupParameter( "Angle" ).Set( 45.0 * Math.PI / 180.0 ); // 2015
          fitting1.LookupParameter( "Nominal Radius" ).Set( 0.5 * diameter ); // 2015

          axis = Line.CreateBound(
            q1, q1 + XYZ.BasisZ );

          ElementTransformUtils.RotateElement(
            doc, fitting1.Id, axis, Math.PI );

          axis = Line.CreateBound( q1, p1 );

          ElementTransformUtils.RotateElement(
            doc, fitting1.Id, axis, Math.PI - angle );

          Connector con1 = Util.GetConnectorClosestTo(
            fitting1, p1 );

          ( pipes[1].Location as LocationCurve ).Curve
            = Line.CreateBound( con1.Origin, p1 );

          Util.Connect( con1.Origin, fitting1, pipes[1] );

          con0 = Util.GetConnectorClosestTo(
            fitting0, pm );

          con1 = Util.GetConnectorClosestTo(
            fitting1, pm );

          // Connecting one fitting to the other does
          // not insert a pipe in between. If the 
          // system is edited later, however, the two 
          // fittings snap together.
          //
          //con0.ConnectTo( con1 );

          // Create rolling offset pipe segment

          //pipe = doc.Create.NewPipe( con0.Origin, // 2014
          //  con1.Origin, pipe_type_standard );

          pipe = Pipe.Create( doc, 
            pipe_type_standard.Id, levelId, con0, con1 ); // 2015

          pipe.get_Parameter( bipDiameter )
            .Set( diameter );

          // Connect rolling offset pipe segment
          // with elbow fittings at each end

          Util.Connect( con0.Origin, fitting0, pipe );
          Util.Connect( con1.Origin, pipe, fitting1 );
        }
        else
        {
          if( _use_static_pipe_create )
          {
            // Element id arguments to Pipe.Create.

            ElementId idSystem;
            ElementId idType;
            ElementId idLevel;

            // All these values are invalid for idSystem:

            ElementId idSystem1 = pipe.MEPSystem.Id;
            ElementId idSystem2 = ElementId.InvalidElementId;
            ElementId idSystem3 = PipingSystem.Create(
              doc, pipe.MEPSystem.GetTypeId(), "Tbc" )
                .Id;

            // This throws an argument exception saying
            // The systemTypeId is not valid piping system type.
            // Parameter name: systemTypeId

            //pipe = Pipe.Create( doc, idSystem,
            //  idType, idLevel, q0, q1 );

            // Retrieve pipe system type, e.g. 
            // hydronic supply.

            PipingSystemType pipingSystemType
              = new FilteredElementCollector( doc )
                .OfClass( typeof( PipingSystemType ) )
                .OfType<PipingSystemType>()
                .FirstOrDefault( st
                  => st.SystemClassification
                    == MEPSystemClassification
                      .SupplyHydronic );

            if( null == pipingSystemType )
            {
              message = "Could not find hydronic supply piping system type";
              return Result.Failed;
            }

            idSystem = pipingSystemType.Id;

            Debug.Assert( pipe.get_Parameter(
              BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM )
                .AsElementId().IntegerValue.Equals(
                  idSystem.IntegerValue ),
                "expected same piping system element id" );

            // Retrieve the PipeType.

            PipeType pipeType =
              new FilteredElementCollector( doc )
                .OfClass( typeof( PipeType ) )
                .OfType<PipeType>()
                .FirstOrDefault();

            if( null == pipeType )
            {
              message = "Could not find pipe type";
              return Result.Failed;
            }

            idType = pipeType.Id;

            Debug.Assert( pipe.get_Parameter(
              BuiltInParameter.ELEM_TYPE_PARAM )
                .AsElementId().IntegerValue.Equals(
                  idType.IntegerValue ),
              "expected same pipe type element id" );

            Debug.Assert( pipe.PipeType.Id.IntegerValue
              .Equals( idType.IntegerValue ),
              "expected same pipe type element id" );

            // Retrieve the reference level.
            // pipe.LevelId is not the correct source!

            idLevel = pipe.get_Parameter(
              BuiltInParameter.RBS_START_LEVEL_PARAM )
                .AsElementId();

            // Create the rolling offset pipe.

            pipe = Pipe.Create( doc,
              idSystem, idType, idLevel, q0, q1 );
          }
          else
          {
            //pipe = doc.Create.NewPipe( q0, q1, pipe_type_standard ); // 2014

            pipe = Pipe.Create( doc, systemTypeId, 
              pipe_type_standard.Id, levelId, q0, q1 ); // 2015
          }

          pipe.get_Parameter( bipDiameter )
            .Set( diameter );

          // Connect rolling offset pipe segment
          // directly with the neighbouring original
          // pipes
          //
          //Util.Connect( q0, pipes[0], pipe );
          //Util.Connect( q1, pipe, pipes[1] );

          // NewElbowFitting performs the following:
          // - select appropriate fitting family and type
          // - place and orient a family instance
          // - set its parameters appropriately
          // - connect it with its neighbours

          Connector con0 = Util.GetConnectorClosestTo(
            pipes[0], q0 );

          Connector con = Util.GetConnectorClosestTo(
            pipe, q0 );

          doc.Create.NewElbowFitting( con0, con );

          Connector con1 = Util.GetConnectorClosestTo(
            pipes[1], q1 );

          con = Util.GetConnectorClosestTo(
            pipe, q1 );

          doc.Create.NewElbowFitting( con, con1 );
        }

        tx.Commit();
      }
      return Result.Succeeded;
    }

    #region Victor's Code
    Result f(
      UIDocument uidoc,
      Document doc )
    {
      string message = string.Empty;

      // Extract all pipe system types

      var mepSystemTypes
        = new FilteredElementCollector( doc )
          .OfClass( typeof( PipingSystemType ) )
          .OfType<PipingSystemType>()
          .ToList();

      // Get the Domestic hot water type

      var domesticHotWaterSystemType =
        mepSystemTypes.FirstOrDefault(
          st => st.SystemClassification ==
            MEPSystemClassification.DomesticHotWater );

      if( domesticHotWaterSystemType == null )
      {
        message = "Could not find Domestic Hot Water System Type";
        return Result.Failed;
      }

      // Looking for the PipeType

      var pipeTypes =
        new FilteredElementCollector( doc )
          .OfClass( typeof( PipeType ) )
          .OfType<PipeType>()
          .ToList();

      // Get the first type from the collection

      var firstPipeType =
          pipeTypes.FirstOrDefault();

      if( firstPipeType == null )
      {
        message = "Could not find Pipe Type";
        return Result.Failed;
      }

      var level = uidoc.ActiveView.GenLevel;

      if( level == null )
      {
        message = "Wrong Active View";
        return Result.Failed;
      }

      var startPoint = XYZ.Zero;

      var endPoint = new XYZ( 100, 0, 0 );

      using( var t = new Transaction( doc ) )
      {
        t.Start( "Create pipe using Pipe.Create" );

        var pipe = Pipe.Create( doc,
          domesticHotWaterSystemType.Id,
          firstPipeType.Id,
          level.Id,
          startPoint,
          endPoint );

        t.Commit();
      }
      Debug.Print( message );
      return Result.Succeeded;
    }
    #endregion // Victor's Code
  }
}

// Z:\a\rvt\rolling_offset.rvt
// /a/j/adn/case/bsd/1264642/attach/PipeTest.cs
