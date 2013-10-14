#region Header
//
// CmdMepElementShape.cs - determine element shape, i.e. MEP element cross section
//
// Copyright (C) 2011-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdMepElementShape : IExternalCommand
  {
    #region MEP Element Shape Version 1
    static class MepElementShapeV1
    {
      /// <summary>
      /// Helper class to cache compiled regular expressions.
      /// </summary>
      class RegexCache : Dictionary<string, Regex>
      {
        /// <summary>
        /// Apply regular expression pattern matching
        /// to a given input string. The compiled
        /// regular expression is cached for efficient
        /// future reuse.
        /// </summary>
        /// <param name="pattern">Regular expression pattern</param>
        /// <param name="input">Input string</param>
        /// <returns>True if input matches pattern, else false</returns>
        public bool Match( string pattern, string input )
        {
          if( !ContainsKey( pattern ) )
          {
            Add( pattern, new Regex( pattern ) );
          }
          return this[pattern].IsMatch( input );
        }
      }

      static RegexCache _regexCache = new RegexCache();

      static bool is_element_of_category(
        Element e,
        BuiltInCategory c )
      {
        //return e.Category.Id.Equals(
        //  e.Document.Settings.Categories.get_Item(
        //    c ).Id );

        return e.Category.Id.IntegerValue.Equals(
          (int) c );
      }

      /// <summary>
      /// Determine element shape from
      /// its MEP PartType and Size parameter.
      /// @: maciej szlek
      /// kedziormsz@gmail.com
      /// http://maciejszlek.pl
      /// </summary>
      public static string GetElementShape( Element e )
      {
        if( is_element_of_category( e,
          BuiltInCategory.OST_DuctCurves ) )
        {
          // simple case, no need to use regular expression

          string size = e.get_Parameter( "Size" )
            .AsString();

          if( size.Split( 'x' ).Length == 2 )
            return "rectangular";
          else if( size.Split( '/' ).Length == 2 )
            return "oval";
          else
            return "round";
        }
        else if( is_element_of_category( e,
          BuiltInCategory.OST_DuctFitting ) )
        {
          FamilyInstance fi = e as FamilyInstance;

          if( fi != null && fi.MEPModel is MechanicalFitting )
          {
            string size = e.get_Parameter( "Size" )
              .AsString();

            PartType partType = ( fi.MEPModel as
              MechanicalFitting ).PartType;

            if( PartType.Elbow == partType
              || PartType.Transition == partType )
            {
              // more complex case

              #region Metric only
#if METRIC_ONLY_BEFORE_REGEX_CACHE
            if( size.Split( 'x' ).Length == 3 ) // could use a regex "[0-9]x[0-9]+-[0-9]+/[0-9]+" but splitting is less costly
              return "rectangular2rectangular";
            else if( size.Split( '/' ).Length == 3 )
              return "oval2oval";
            else if(
              new Regex( @"[0-9]+x[0-9]+-[0-9]+/[0-9]+" )
                .IsMatch( size ) )
                  return "rectangular2oval";
            else if(
              new Regex( @"[0-9]+/[0-9]+-[0-9]+x[0-9]+" )
                .IsMatch( size ) )
                  return "oval2rectangular";
            else if(
              new Regex( @"[0-9]+[^0-9]-[0-9]+x[0-9]+" )
                .IsMatch( size ) )
                  return "round2rectangular";
            else if(
              new Regex( @"[0-9]+x[0-9]+-[0-9]+[^0-9]" )
                .IsMatch( size ) )
                  return "rectangular2round";
            else if(
              new Regex( @"[0-9]+[^0-9]-[0-9]+/[0-9]+" )
                .IsMatch( size ) )
                  return "round2oval";
            else if(
              new Regex( @"[0-9]+/[0-9]+-[0-9]+[^0-9]" )
                .IsMatch( size ) )
                  return "oval2round";
            else if(
              new Regex( @"[0-9]+[^0-9]-[0-9]+[^0-9]" )
                .IsMatch( size ) )
                  return "round2round";
            else { return "other case"; }
#endif // METRIC_ONLY_BEFORE_REGEX_CACHE

#if METRIC_ONLY
            if( size.Split( 'x' ).Length == 3 ) // could use a regex "[0-9]x[0-9]+-[0-9]+/[0-9]+" but splitting is less costly
              return "rectangular2rectangular";
            else if( size.Split( '/' ).Length == 3 )
              return "oval2oval";
            else if( _regexCache.Match(
              "[0-9]+x[0-9]+-[0-9]+/[0-9]+", size ) )
                return "rectangular2oval";
            else if( _regexCache.Match(
              "[0-9]+/[0-9]+-[0-9]+x[0-9]+", size ) )
                return "oval2rectangular";
            else if( _regexCache.Match(
              "[0-9]+[^0-9]-[0-9]+x[0-9]+", size ) )
                return "round2rectangular";
            else if( _regexCache.Match(
              "[0-9]+x[0-9]+-[0-9]+[^0-9]", size ) )
                return "rectangular2round";
            else if( _regexCache.Match(
              "[0-9]+[^0-9]-[0-9]+/[0-9]+", size ) )
                return "round2oval";
            else if( _regexCache.Match(
              "[0-9]+/[0-9]+-[0-9]+[^0-9]", size ) )
                return "oval2round";
            else if( _regexCache.Match(
              "[0-9]+[^0-9]-[0-9]+[^0-9]", size ) )
                return "round2round";
#endif // METRIC_ONLY
              #endregion // Metric only

              if( size.Split( 'x' ).Length == 3 ) // or use Regex("[0-9]x[0-9]+-[0-9]+/[0-9]+") but splitting is less costly
                return "rectangular2rectangular";
              else if( size.Split( '/' ).Length == 3 ) // but if in imperial units size is in fractional inches format it has to be replaced by another regular expression
                return "oval2oval";
              else if( _regexCache.Match(
                "[0-9]+\"?x[0-9]+\"?-[0-9]+\"?/[0-9]+\"?", size ) )
                return "rectangular2oval";
              else if( _regexCache.Match(
                "[0-9]+\"?/[0-9]+\"?-[0-9]+\"?x[0-9]+\"?", size ) )
                return "oval2rectangular";
              else if( _regexCache.Match(
                "[0-9]+\"?[^0-9]-[0-9]+\"?x[0-9]+\"?", size ) )
                return "round2rectangular";
              else if( _regexCache.Match(
                "[0-9]+\"?x[0-9]+\"?-[0-9]+\"?[^0-9]", size ) )
                return "rectangular2round";
              else if( _regexCache.Match(
                "[0-9]+\"?[^0-9]-[0-9]+\"?/[0-9]+\"?", size ) )
                return "round2oval";
              else if( _regexCache.Match(
                "[0-9]+\"?/[0-9]+\"?-[0-9]+\"?[^0-9]", size ) )
                return "oval2round";
              else if( _regexCache.Match(
                "[0-9]+\"?[^0-9]-[0-9]+\"?[^0-9]", size ) )
                return "round2round";
              else { return "other case"; }
            }
            // etc (for other part types)
            else
            {
            }
          }
          // etc (for other categories)
          else
          {
          }
        }
        return "unknown";
      }
    }
    #endregion // MEP Element Shape Version 1

    #region MEP Element Shape Version 2
    static class MepElementShapeVersion2
    {
      /// <summary>
      /// Determine element shape from its connectors.
      /// </summary>
      /// <param name="e">Checked element</param>
      /// <param name="pe">Previous element (optional), in case badly-connected MEP system</param>
      /// <param name="ne">Next element (optional), in case you want shape chenge through flow direction only
      /// (for elements with more than one output)</param>
      /// <returns>Element shape changes</returns>
      static public string GetElementShape(
        Element e,
        Element pe = null,
        Element ne = null )
      {
        if( is_element_of_category( e,
          BuiltInCategory.OST_DuctCurves ) )
        {
          // assuming that transition is using to change shape..

          ConnectorManager cm = ( e as MEPCurve )
            .ConnectorManager;

          foreach( Connector c in cm.Connectors )
            return c.Shape.ToString()
              + " 2 " + c.Shape.ToString();
        }
        else if( is_element_of_category( e,
          BuiltInCategory.OST_DuctFitting ) )
        {
          MEPSystem system
            = ExtractMechanicalOrPipingSystem( e );

          FamilyInstance fi = e as FamilyInstance;
          MEPModel mm = fi.MEPModel;

          ConnectorSet connectors
            = mm.ConnectorManager.Connectors;

          if( fi != null && mm is MechanicalFitting )
          {
            PartType partType
              = ( mm as MechanicalFitting ).PartType;

            if( PartType.Elbow == partType )
            {
              // assuming that transition is using to change shape..

              foreach( Connector c in connectors )
              {
                return c.Shape.ToString()
                  + " 2 " + c.Shape.ToString();
              }
            }
            else if( PartType.Transition == partType )
            {
              string[] tmp = new string[2];

              if( system != null )
              {
                foreach( Connector c in connectors )
                {
                  if( c.Direction == FlowDirectionType.In )
                    tmp[0] = c.Shape.ToString();

                  if( c.Direction == FlowDirectionType.Out )
                    tmp[1] = c.Shape.ToString();
                }
                return string.Join( " 2 ", tmp );
              }
              else
              {
                int i = 0;

                foreach( Connector c in connectors )
                {
                  if( pe != null )
                  {
                    if( is_connected_to( c, pe ) )
                      tmp[0] = c.Shape.ToString();
                    else
                      tmp[1] = c.Shape.ToString();
                  }
                  else
                  {
                    tmp[i] = c.Shape.ToString();
                  }
                  ++i;
                }

                if( pe != null )
                  return string.Join( " 2 ", tmp );

                return string.Join( "-", tmp );
              }
            }
            else if( PartType.Tee == partType
              || PartType.Cross == partType
              || PartType.Pants == partType
              || PartType.Wye == partType )
            {
              string from, to;
              from = to = null;
              List<string> unk = new List<string>();

              if( system != null )
              {
                foreach( Connector c in connectors )
                {
                  if( c.Direction == FlowDirectionType.In )
                    from = c.Shape.ToString();
                  else
                    unk.Add( c.Shape.ToString() );

                  if( ne != null && is_connected_to( c, ne ) )
                    to = c.Shape.ToString();
                }

                if( to != null )
                  return from + " 2 " + to;

                return from + " 2 " + string.Join( "-",
                  unk.ToArray() );
              }
              else
              {
                foreach( Connector c in connectors )
                {
                  if( ne != null && is_connected_to(
                    c, ne ) )
                  {
                    to = c.Shape.ToString();
                    continue;
                  }

                  if( pe != null && is_connected_to(
                    c, pe ) )
                  {
                    from = c.Shape.ToString();
                    continue;
                  }

                  unk.Add( c.Shape.ToString() );
                }

                if( to != null )
                  return from + " 2 " + to;

                if( from != null )
                  return from + " 2 "
                    + string.Join( "-", unk.ToArray() );

                return string.Join( "-", unk.ToArray() );
              }
            }
          }
        }
        return "unknown";
      }

      /// <summary>
      /// Check if connector is connected to some connector of the element.
      /// </summary>
      static public bool is_connected_to(
        Connector c,
        Element e )
      {
        ConnectorManager cm = ( e is FamilyInstance )
          ? ( e as FamilyInstance ).MEPModel.ConnectorManager
          : ( e as MEPCurve ).ConnectorManager;

        foreach( Connector c2 in cm.Connectors )
        {
          if( c.IsConnectedTo( c2 ) )
            return true;
        }
        return false;
      }

      /// <summary>
      /// Check if element belongs to the category.
      /// </summary>
      static public bool is_element_of_category(
        Element e,
        BuiltInCategory c )
      {
        //return e.Category.Id.Equals(
        //  e.Document.Settings.Categories.get_Item(
        //    c ).Id );

        return e.Category.Id.IntegerValue.Equals(
          (int) c );
      }

      // copied from sdk - TraverseSystem example
      //
      // (C) Copyright 2003-2010 by Autodesk, Inc.
      //
      /// <summary>
      /// Get the mechanical or piping system
      /// from selected element
      /// </summary>
      /// <param name="selectedElement">Selected element</param>
      /// <returns>The extracted mechanical or piping system,
      /// or null if no expected system is found.</returns>
      static public MEPSystem ExtractMechanicalOrPipingSystem(
        Element selectedElement )
      {
        MEPSystem system = null;

        if( selectedElement is MEPSystem )
        {
          if( selectedElement is MechanicalSystem
            || selectedElement is PipingSystem )
          {
            system = selectedElement as MEPSystem;
            return system;
          }
        }
        else // Selected element is not a system
        {
          FamilyInstance fi = selectedElement
            as FamilyInstance;

          // If selected element is a family instance,
          // iterate its connectors and get the expected system

          if( fi != null )
          {
            MEPModel mepModel = fi.MEPModel;
            ConnectorSet connectors = null;
            try
            {
              connectors = mepModel.ConnectorManager.Connectors;
            }
            catch( System.Exception )
            {
              system = null;
            }
            system = ExtractSystemFromConnectors( connectors );
          }
          else
          {
            // If selected element is a MEPCurve (e.g. pipe or duct),
            // iterate its connectors and get the expected system

            MEPCurve mepCurve = selectedElement as MEPCurve;
            if( mepCurve != null )
            {
              ConnectorSet connectors = null;
              connectors = mepCurve.ConnectorManager.Connectors;
              system = ExtractSystemFromConnectors( connectors );
            }
          }
        }
        return system;
      }

      // copied from sdk - TraverseSystem example
      //
      // (C) Copyright 2003-2010 by Autodesk, Inc.
      //
      /// <summary>
      /// Get the mechanical or piping system
      /// from the connectors of selected element
      /// </summary>
      /// <param name="connectors">Connectors of selected element</param>
      /// <returns>The found mechanical or piping system</returns>
      static public MEPSystem ExtractSystemFromConnectors( ConnectorSet connectors )
      {
        MEPSystem system = null;

        if( connectors == null || connectors.Size == 0 )
        {
          return null;
        }

        // Get well-connected mechanical or
        // piping systems from each connector

        List<MEPSystem> systems = new List<MEPSystem>();
        foreach( Connector connector in connectors )
        {
          MEPSystem tmpSystem = connector.MEPSystem;
          if( tmpSystem == null )
          {
            continue;
          }

          MechanicalSystem ms = tmpSystem as MechanicalSystem;
          if( ms != null )
          {
            if( ms.IsWellConnected )
            {
              systems.Add( tmpSystem );
            }
          }
          else
          {
            PipingSystem ps = tmpSystem as PipingSystem;
            if( ps != null && ps.IsWellConnected )
            {
              systems.Add( tmpSystem );
            }
          }
        }

        // If more than one system is found,
        // get the system contains the most elements

        int countOfSystem = systems.Count;
        if( countOfSystem != 0 )
        {
          int countOfElements = 0;
          foreach( MEPSystem sys in systems )
          {
            if( sys.Elements.Size > countOfElements )
            {
              system = sys;
              countOfElements = sys.Elements.Size;
            }
          }
        }
        return system;
      }
    }
    #endregion // MEP Element Shape Version 2

    #region MEP Element Shape Version 3
    static class MepElementShapeVersion3
    {
      static bool HasInvalidElementIdValue(
        Element e,
        BuiltInParameter bip )
      {
        Parameter p = e.get_Parameter( bip );

        return null != p
          && StorageType.ElementId == p.StorageType
          && ElementId.InvalidElementId == p.AsElementId();
      }

      /// <summary>
      /// Determine element shape from its parameters.
      /// </summary>
      static public string GetElementShape(
        Element e )
      {
        string shape = "unknown";

        ElementId tid = e.GetTypeId();

        if( ElementId.InvalidElementId != tid )
        {
          Document doc = e.Document;

          DuctType dt = doc.GetElement( tid )
            as DuctType;

          if( null != dt )
          {
            if( HasInvalidElementIdValue( e, BuiltInParameter
              .RBS_CURVETYPE_MULTISHAPE_TRANSITION_OVALROUND_PARAM ) )
            {
              shape = "rectangular";
            }
            else if( HasInvalidElementIdValue( e, BuiltInParameter
              .RBS_CURVETYPE_MULTISHAPE_TRANSITION_RECTOVAL_PARAM ) )
            {
              shape = "round";
            }
            else if( HasInvalidElementIdValue( e, BuiltInParameter
              .RBS_CURVETYPE_MULTISHAPE_TRANSITION_PARAM ) )
            {
              shape = "oval";
            }
          }
        }
        return shape;
      }

      #region Rolando Hijar Duct_from_AutoCad method
#if NEED_Duct_from_AutoCad
      public void Duct_from_AutoCad()
      {
        UIDocument uidoc = this.ActiveUIDocument;
        Document doc = uidoc.Document;
        Autodesk.Revit.DB.View currentview = doc.ActiveView;
        if( currentview.ViewType != ViewType.FloorPlan ) { return; }//Only works in floorplans
        try
        {
          puntoref_revit = uidoc.Selection.PickPoint( "Pick origin point in REVIT" );
        }
        catch { return; }
        OpenFileDialog archivofile = new OpenFileDialog();
        archivofile.Title = "Open CAD data file";
        archivofile.CheckFileExists = true;
        archivofile.CheckPathExists = true;
        archivofile.Filter = "Txt|*.txt";
        if( archivofile.ShowDialog() != DialogResult.OK ) { return; }
        string nombrefile = archivofile.FileName;
        string[] lineasarchivo = File.ReadAllLines( nombrefile );
        string lineadata = String.Empty;

        //get Rectangular Duct Type
        FilteredElementCollector collectorductos = new FilteredElementCollector( doc );
        collectorductos.OfClass( typeof( Autodesk.Revit.DB.Mechanical.DuctType ) ).ToElements();

        Autodesk.Revit.DB.Mechanical.DuctType ducttypefinal = null;
        foreach( Element elemw in collectorductos )
        {
          Autodesk.Revit.DB.Mechanical.DuctType duct_type = elemw as Autodesk.Revit.DB.Mechanical.DuctType;
          System.Diagnostics.Debug.Print( duct_type.Name );
          Parameter ovaltoround = duct_type.get_Parameter( BuiltInParameter.RBS_CURVETYPE_MULTISHAPE_TRANSITION_OVALROUND_PARAM );
          Parameter recttooval = duct_type.get_Parameter( BuiltInParameter.RBS_CURVETYPE_MULTISHAPE_TRANSITION_RECTOVAL_PARAM );
          Parameter recttoround = duct_type.get_Parameter( BuiltInParameter.RBS_CURVETYPE_MULTISHAPE_TRANSITION_PARAM );
          int val_ovaltoround = ovaltoround.AsElementId().IntegerValue;
          int val_recttooval = recttooval.AsElementId().IntegerValue;
          int val_recttoround = recttoround.AsElementId().IntegerValue;
          System.Diagnostics.Debug.Print( "Oval to round:" + val_ovaltoround.ToString() );
          System.Diagnostics.Debug.Print( "Rect to oval:" + val_recttooval.ToString() );
          System.Diagnostics.Debug.Print( "Rect to round:" + val_recttoround.ToString() );
          //if val_recttoround is -1 the ducttype is OVAL
          //if val_ovaltoround is -1 the ducttype is RECTANGULAR
          // if val_recttooval is -1 the ducttyoe is ROUND
          if( val_ovaltoround == -1 )
          {
            ducttypefinal = duct_type; break;
          }
        }
        //
        lineadata = lineasarchivo[0];
        string[] datos = lineadata.Split( ';' );
        double x1 = Math.Round( Convert.ToDouble( datos[0] ), 6 ) / 0.3048;
        double y1 = Math.Round( Convert.ToDouble( datos[1] ), 6 ) / 0.3048;
        double z1 = Math.Round( Convert.ToDouble( datos[2] ), 6 ) / 0.3048;
        puntoref_cad = new XYZ( x1, y1, z1 );
        vector_correccion = puntoref_revit - puntoref_cad;
        XYZ puntoCR = new XYZ();
        int Countducts = 0;
        using( Transaction tr = new Transaction( doc, "DuctFromCAD" ) )
        {
          tr.Start();
          for( int ii = 1; ii < lineasarchivo.Length; ii++ )
          {
            lineadata = lineasarchivo[ii];
            datos = lineadata.Split( ';' );
            x1 = Math.Round( Convert.ToDouble( datos[0] ), 6 ) / 0.3048;
            y1 = Math.Round( Convert.ToDouble( datos[1] ), 6 ) / 0.3048;
            z1 = Math.Round( Convert.ToDouble( datos[2] ), 6 ) / 0.3048;
            double x2 = Math.Round( Convert.ToDouble( datos[3] ), 6 ) / 0.3048;
            double y2 = Math.Round( Convert.ToDouble( datos[4] ), 6 ) / 0.3048;
            double z2 = Math.Round( Convert.ToDouble( datos[5] ), 6 ) / 0.3048;
            double ancho = Math.Round( Convert.ToDouble( datos[6] ), 3 );
            string dim_ducto = datos[7];
            if( dim_ducto != null && dim_ducto != "" )
            {
              string[] blanksplit = dim_ducto.Split( ' ' );
              string[] sizeplit = blanksplit[0].Split( 'x' );
              double widthduct = Convert.ToDouble( sizeplit[0] ) / 12;//from inches to feet
              double heightduct = Convert.ToDouble( sizeplit[1] ) / 12;//from inches to feet
              System.Diagnostics.Debug.Print( widthduct.ToString() + "x" + heightduct.ToString() );
              XYZ p1 = new XYZ( x1, y1, z1 ) + vector_correccion;
              XYZ p2 = new XYZ( x2, y2, z2 ) + vector_correccion;
              if( p1.DistanceTo( p2 ) < 1 ) { continue; }//Not less than a feet
              try
              {

                Autodesk.Revit.DB.Mechanical.Duct new_duct = doc.Create.NewDuct( p1, p2, ducttypefinal );
                new_duct.get_Parameter( BuiltInParameter.RBS_CURVE_WIDTH_PARAM ).Set( widthduct );
                new_duct.get_Parameter( BuiltInParameter.RBS_CURVE_HEIGHT_PARAM ).Set( heightduct );
                doc.Create.NewTag( currentview, new_duct, false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.TAG_HORIZONTAL, p1.Add( p2 ).Divide( 2 ) );
                //doc.Create.NewTextNote(currentview, p1.Add(p2).Divide(2.001), XYZ.BasisX, XYZ.BasisZ, 1, TextAlignFlags.TEF_ALIGN_CENTER, sizeplit[0] + "x" + sizeplit[1]);
                Countducts++;
              }
              catch { }
            }
          }
          tr.Commit();
        }
        MessageBox.Show( "Imported " + Countducts.ToString() + " Ducts" );
      }
#endif // NEED_Duct_from_AutoCad
      #endregion // Rolando Hijar Duct_from_AutoCad method
    }
    #endregion // MEP Element Shape Version 3

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      Element e = null;

      while( true )
      {
        try
        {
          e = Util.SelectSingleElementOfType(
            uidoc, typeof( Element ), "an element", true );
        }
        catch( OperationCanceledException )
        {
          message = "No element selected";
          break;
        }
        if( null == e )
        {
          break;
        }

        Util.InfoMsg( string.Format(
          //"{0} is {1} {2} ({3})",
          "{0} is {1} ({2})",
          Util.ElementDescription( e ),
          //MepElementShapeVersion3.GetElementShape( e ),
          MepElementShapeVersion2.GetElementShape( e ),
          MepElementShapeV1.GetElementShape( e ) ) );
      }
      return Result.Succeeded;
    }
  }
}

// Z:\a\doc\revit\blog\zip\tmp\example_revit.rvt
// Z:\j\tmp\rvt\rac_2014_empty.rvt