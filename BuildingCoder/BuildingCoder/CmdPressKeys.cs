#region Header
//
// CmdPressKeys.cs - press keys to launch 'Create Similar' and other Revit commands
//
// Copyright (C) 2010-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  #region Press class: encapsulates PostMessage and provides Keys method
  public class Press
  {
    [DllImport( "USER32.DLL" )]
    public static extern bool PostMessage(
      IntPtr hWnd, uint msg, uint wParam, uint lParam );

    [DllImport( "user32.dll" )]
    static extern uint MapVirtualKey(
      uint uCode, uint uMapType );

    enum WH_KEYBOARD_LPARAM : uint
    {
      KEYDOWN = 0x00000001,
      KEYUP = 0xC0000001
    }

    enum KEYBOARD_MSG : uint
    {
      WM_KEYDOWN = 0x100,
      WM_KEYUP = 0x101
    }

    enum MVK_MAP_TYPE : uint
    {
      VKEY_TO_SCANCODE = 0,
      SCANCODE_TO_VKEY = 1,
      VKEY_TO_CHAR = 2,
      SCANCODE_TO_LR_VKEY = 3
    }

    /// <summary>
    /// Post one single keystroke.
    /// </summary>
    static void OneKey( IntPtr handle, char letter )
    {
      uint scanCode = MapVirtualKey( letter,
        ( uint ) MVK_MAP_TYPE.VKEY_TO_SCANCODE );

      uint keyDownCode = ( uint )
        WH_KEYBOARD_LPARAM.KEYDOWN
        | ( scanCode << 16 );

      uint keyUpCode = ( uint )
        WH_KEYBOARD_LPARAM.KEYUP
        | ( scanCode << 16 );

      PostMessage( handle,
        ( uint ) KEYBOARD_MSG.WM_KEYDOWN,
        letter, keyDownCode );

      PostMessage( handle,
        ( uint ) KEYBOARD_MSG.WM_KEYUP,
        letter, keyUpCode );
    }

    /// <summary>
    /// Post a sequence of keystrokes.
    /// </summary>
    public static void Keys( string command )
    {
      IntPtr revitHandle = System.Diagnostics.Process
        .GetCurrentProcess().MainWindowHandle;

      foreach( char letter in command )
      {
        OneKey( revitHandle, letter );
      }
    }
  }
  #endregion //Press class: encapsulates PostMessage and provides Keys method

  [Transaction( TransactionMode.Manual )]
  class CmdPressKeys : IExternalCommand
  {
    #region GetFirstWallTypeNamed
    /// <summary>
    /// Return the first wall type with the given name.
    /// </summary>
    static WallType GetFirstWallTypeNamed(
      Document doc,
      string name )
    {
      // built-in parameter storing this
      // wall type's name:

      BuiltInParameter bip
        = BuiltInParameter.SYMBOL_NAME_PARAM;

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterStringRuleEvaluator evaluator
        = new FilterStringEquals();

      FilterRule rule = new FilterStringRule(
        provider, evaluator, name, false );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .OfClass( typeof( WallType ) )
          .WherePasses( filter );

      return collector.FirstElement() as WallType;
    }
    #endregion // GetFirstWallTypeNamed

    #region GetFirstWallUsingType
    /// <summary>
    /// Return the first wall found that
    /// uses the given wall type.
    /// </summary>
    static Wall GetFirstWallUsingType(
      Document doc,
      WallType wallType )
    {
      // built-in parameter storing this
      // wall's wall type element id:

      BuiltInParameter bip
        = BuiltInParameter.ELEM_TYPE_PARAM;

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterNumericRuleEvaluator evaluator
        = new FilterNumericEquals();

      FilterRule rule = new FilterElementIdRule(
        provider, evaluator, wallType.Id );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Wall ) )
          .WherePasses( filter );

      return collector.FirstElement() as Wall;
    }
    #endregion // GetFirstWallUsingType

    /// <summary>
    /// Here is a part or our code to start a Revit command.
    /// The aim of the code is to set a wall type current in the Revit property window.
    /// We only start up the wall command with the API and let the user do the drawing of the wall.
    /// This solution can also be used to launch other Revit commands.
    /// </summary>
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // name of target wall type that we want to use:

      string wallTypeName = "Generic - 203";

      WallType wallType = GetFirstWallTypeNamed(
        doc, wallTypeName );

      Wall wall = GetFirstWallUsingType(
        doc, wallType );

      // select the wall in the UI

      uidoc.Selection.Elements.Add( wall );

      if( 0 == uidoc.Selection.Elements.Size )
      {
        // no wall with the correct wall type found

        FilteredElementCollector collector
          = new FilteredElementCollector( doc );

        Level ll = collector
          .OfClass( typeof( Level ) )
          .FirstElement() as Level;

        // place a new wall with the
        // correct wall type in the project

        //Line geomLine = app.Create.NewLineBound( XYZ.Zero, new XYZ( 2, 0, 0 ) ); // 2013
        Line geomLine = Line.CreateBound( XYZ.Zero, new XYZ( 2, 0, 0 ) ); // 2014

        Transaction t = new Transaction(
          doc, "Create dummy wall" );

        t.Start();

        //Wall nw = doc.Create.NewWall( geomLine, // 2012
        //  wallType, ll, 1, 0, false, false );

        Wall nw = Wall.Create( doc, geomLine, // 2013
          wallType.Id, ll.Id, 1, 0, false, false );

        t.Commit();

        // Select the new wall in the project

        uidoc.Selection.Elements.Add( nw );

        // Start command create similar. In the
        // property menu, our wall type is set current

        Press.Keys( "CS" );

        // select the new wall in the project,
        // so we can delete it

        uidoc.Selection.Elements.Add( nw );

        // erase the selected wall (remark:
        // doc.delete(nw) may not be used,
        // this command will undo)

        Press.Keys( "DE" );

        // start up wall command

        Press.Keys( "WA" );
      }
      else
      {
        // the correct wall is already selected:

        Press.Keys( "CS" ); // start "create similar"
      }
      return Result.Succeeded;
    }
  }
}

// "C:\Program Files\Autodesk\Revit Architecture 2011\Program\Samples\rac_basic_sample_project.rvt"
// "C:\Program Files\Autodesk\Revit Architecture 2011\Program\Samples\rac_advanced_sample_project.rvt"
