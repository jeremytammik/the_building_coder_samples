#region Header

//
// CmdStatusBar.cs - set the status bar text using Windows API
//
// Copyright (C) 2011-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdStatusBar : IExternalCommand
    {
        [DllImport("user32.dll",
            SetLastError = true,
            CharSet = CharSet.Auto)]
        private static extern int SetWindowText(
            IntPtr hWnd,
            string lpString);

        [DllImport("user32.dll",
            SetLastError = true)]
        private static extern IntPtr FindWindowEx(
            IntPtr hwndParent,
            IntPtr hwndChildAfter,
            string lpszClass,
            string lpszWindow);

        public static void SetStatusText(
            IntPtr mainWindow,
            string text)
        {
            var statusBar = FindWindowEx(
                mainWindow, IntPtr.Zero,
                "msctls_statusbar32", "");

            if (statusBar != IntPtr.Zero) SetWindowText(statusBar, text);
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var revitHandle = Process
                .GetCurrentProcess().MainWindowHandle;

            SetStatusText(revitHandle, "Kilroy was here.");

            return Result.Succeeded;
        }

        #region BalloonTip

#if BALOON_TIP
    public static void ShowBalloonTip(
      string category,
      string title,
      string text )
    {
      Autodesk.Internal.InfoCenter.ResultItem ri
        = new Autodesk.Internal.InfoCenter.ResultItem();

      ri.Category = category;
      ri.Title = title;
      ri.TooltipText = text;

      // Optional: provide a URL, e.g. a 
      // website containing further information.

      ri.Uri = new System.Uri(
        "http://www.yourContextualHelp.de" );

      ri.IsFavorite = true;
      ri.IsNew = true;

      // You also could add a click event.

      ri.ResultClicked += new EventHandler<
        Autodesk.Internal.InfoCenter.ResultClickEventArgs>(
          ri_ResultClicked );

      Autodesk.Windows.ComponentManager
        .InfoCenterPaletteManager.ShowBalloon( ri );
    }

    private static void ri_ResultClicked(
      object sender,
      Autodesk.Internal.InfoCenter.ResultClickEventArgs e )
    {
      // do some stuff...
    }
#endif

        #endregion // BalloonTip
    }
}