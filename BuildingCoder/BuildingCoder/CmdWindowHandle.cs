#region Header
//
// CmdWindowHandle.cs - determine Revit
// application main window handle.
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
using Autodesk.Revit.UI.Selection;
using IWin32Window
  = System.Windows.Forms.IWin32Window;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Wrapper class for converting IntPtr to IWin32Window.
  /// </summary>
  public class WindowHandle : IWin32Window
  {
    IntPtr _hwnd;

    public WindowHandle( IntPtr h )
    {
      Debug.Assert( IntPtr.Zero != h,
        "expected non-null window handle" );

      _hwnd = h;
    }

    public IntPtr Handle
    {
      get
      {
        return _hwnd;
      }
    }
  }

  [Transaction( TransactionMode.ReadOnly )]
  class CmdWindowHandle : IExternalCommand
  {
    const string _prompt
      = "Please select some elements.";

    static WindowHandle _hWndRevit = null;

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      if( null == _hWndRevit )
      {
        //Process[] processes
        //  = Process.GetProcessesByName( "Revit" );

        //if( 0 < processes.Length )
        //{
        //  IntPtr h = processes[0].MainWindowHandle;
        //  _hWndRevit = new WindowHandle( h );
        //}

        Process process
          = Process.GetCurrentProcess();

        IntPtr h = process.MainWindowHandle;
        _hWndRevit = new WindowHandle( h );
      }

      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Selection sel = uidoc.Selection;

      using( CmdWindowHandleForm f
        = new CmdWindowHandleForm() )
      {
        f.Show( _hWndRevit );
        bool go = true;
        while( go )
        {
          SelElementSet ss = sel.Elements;
          int n = ss.Size;

          string s = string.Format(
            "{0} element{1} selected{2}",
            n, Util.PluralSuffix( n ),
            ((0 == n)
              ? ";\n" + _prompt
              : ":" ) );

          foreach( Element e in ss )
          {
            s += "\n";
            s += Util.ElementDescription( e );
          }
          f.LabelText = s;

#if _2010
          sel.StatusbarTip = _prompt;
          go = sel.PickOne();
#endif // _2010

          Reference r = uidoc.Selection.PickObject(
            ObjectType.Element, _prompt );

          go = null != r;

          Debug.Print( "go = " + go.ToString() );
        }
      }
      return Result.Failed;
    }

    /// <summary>
    /// Modified version with changes to use WindowSelect, by Tao (Tau) Yang, Autodesk.
    /// </summary>
    public Result Execute2(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      if( null == _hWndRevit )
      {
        Process process
          = Process.GetCurrentProcess();

        IntPtr h = process.MainWindowHandle;
        _hWndRevit = new WindowHandle( h );
      }

      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Selection sel = uidoc.Selection;

      using( CmdWindowHandleForm f
        = new CmdWindowHandleForm() )
      {
        f.Show( _hWndRevit );
        bool go = true;
        while( go )
        {

#if _2010
          sel.StatusbarTip = _prompt;
          go = sel.WindowSelect();
#endif // _2010

          IList<Element> a = sel.PickElementsByRectangle( _prompt );
          go = 0 < a.Count;

          SelElementSet ss = sel.Elements;
          int n = ss.Size;

          string s = string.Format(
            "{0} element{1} selected{2}",
            n, Util.PluralSuffix( n ),
            ( ( 0 == n )
              ? ";\n" + _prompt
              : ":" ) );

          foreach( Element e in ss )
          {
            s += "\n";
            s += Util.ElementDescription( e );
          }
          f.LabelText = s;
          Debug.Print( "go = " + go.ToString() );
        }
      }
      return Result.Failed;
    }
  }
}
