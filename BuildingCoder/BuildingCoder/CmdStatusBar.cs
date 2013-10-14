#region Header
//
// CmdStatusBar.cs - set the status bar text using Windows API
//
// Copyright (C) 2011-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Runtime.InteropServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdStatusBar : IExternalCommand
  {
    [DllImport( "user32.dll",
      SetLastError = true,
      CharSet = CharSet.Auto )]
    static extern int SetWindowText(
      IntPtr hWnd,
      string lpString );

    [DllImport( "user32.dll",
      SetLastError = true )]
    static extern IntPtr FindWindowEx(
      IntPtr hwndParent,
      IntPtr hwndChildAfter,
      string lpszClass,
      string lpszWindow );

    public static void SetStatusText(
      IntPtr mainWindow,
      string text )
    {
      IntPtr statusBar = FindWindowEx(
        mainWindow, IntPtr.Zero,
        "msctls_statusbar32", "" );

      if( statusBar != IntPtr.Zero )
      {
        SetWindowText( statusBar, text );
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      IntPtr revitHandle = System.Diagnostics.Process
        .GetCurrentProcess().MainWindowHandle;

      SetStatusText( revitHandle, "Kilroy was here." );

      return Result.Succeeded;
    }
  }
}
