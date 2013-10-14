#region Header
//
// CmdDemoCheck.cs - Check whether the running Revit application is a demo version
//
// Copyright (C) 2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdDemoCheck : IExternalCommand
  {
    [DllImport( "user32.dll", SetLastError = true, 
      CharSet = CharSet.Auto )]
    static extern int GetWindowText( 
      IntPtr hWnd, 
      StringBuilder lpString, 
      int nMaxCount );

    [DllImport( "user32.dll", SetLastError = true, 
      CharSet = CharSet.Auto )]
    static extern int GetWindowTextLength( 
      IntPtr hWnd );

    static StringBuilder GetStatusTextMadmed( 
      IntPtr mainWindow )
    {
      StringBuilder s = new StringBuilder();
      if( mainWindow != IntPtr.Zero )
      {
        int length = GetWindowTextLength( mainWindow );
        StringBuilder sb = new StringBuilder( length + 1 );
        GetWindowText( mainWindow, sb, sb.Capacity );
        sb.Replace( "Autodesk Revit Architecture 2013 - ", "" );
        return sb;
      }
      return s;
    }

    static string GetWindowTextUsingWinApi( IntPtr mainWindow )
    {
      if( IntPtr.Zero == mainWindow )
      {
        throw new ArgumentException(
          "Expected valid window handle." );
      }
      int len = GetWindowTextLength( mainWindow );
      StringBuilder sb = new StringBuilder( len + 1 );
      GetWindowText( mainWindow, sb, sb.Capacity );
      return sb.ToString();
    }

    static string GetWindowTextUsingNet()
    {
      return System.Diagnostics.Process.
        GetCurrentProcess().MainWindowTitle;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      IntPtr revitHandle = System.Diagnostics.Process
        .GetCurrentProcess().MainWindowHandle;

      string s = GetWindowTextUsingWinApi( 
        revitHandle );

      // Much simpler direct access:

      s = System.Diagnostics.Process.
        GetCurrentProcess().MainWindowTitle;

      // My system returns:
      // "Autodesk Revit 2013 - Not For Resale Version 
      // - [Floor Plan: Level 1 - rac_empty.rvt]"

      bool isDemo = s.Contains( "VIEWER" );

      // Language independent serial number check:

      string serial_number = UIFrameworkServices
        .InfoCenterService.ProductSerialNumber;

      isDemo = serial_number.Equals( "000-00000000" );

      string sDemo = isDemo ? "Demo" : "Production";

      TaskDialog.Show( 
        "Serial Number and Demo Version Check",
        string.Format( 
          "Serial number: {0} : {1} version.",
          serial_number, sDemo ) );

      return Result.Succeeded;
    }
  }
}
