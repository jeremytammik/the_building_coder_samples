#region Header

//
// CmdDemoCheck.cs - Check whether the running Revit application is a demo version
//
// Copyright (C) 2013-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
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
using UIFrameworkServices;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdDemoCheck : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var revitHandle = Process
                .GetCurrentProcess().MainWindowHandle;

            var s = GetWindowTextUsingWinApi(
                revitHandle);

            // Much simpler direct access:

            s = Process.GetCurrentProcess().MainWindowTitle;

            // My system returns:
            // "Autodesk Revit 2013 - Not For Resale Version 
            // - [Floor Plan: Level 1 - rac_empty.rvt]"

            var isDemo = s.Contains("VIEWER");

            // Language independent serial number check:

            var serial_number = InfoCenterService.ProductSerialNumber;

            isDemo = serial_number.Equals("000-00000000");

            var sDemo = isDemo ? "Demo" : "Production";

            TaskDialog.Show(
                "Serial Number and Demo Version Check",
                $"Serial number: {serial_number} : {sDemo} version.");

            return Result.Succeeded;
        }

        [DllImport("user32.dll", SetLastError = true,
            CharSet = CharSet.Auto)]
        private static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder lpString,
            int nMaxCount);

        [DllImport("user32.dll", SetLastError = true,
            CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(
            IntPtr hWnd);

        private static StringBuilder GetStatusTextMadmed(
            IntPtr mainWindow)
        {
            var s = new StringBuilder();
            if (mainWindow != IntPtr.Zero)
            {
                var length = GetWindowTextLength(mainWindow);
                var sb = new StringBuilder(length + 1);
                GetWindowText(mainWindow, sb, sb.Capacity);
                sb.Replace("Autodesk Revit Architecture 2013 - ", "");
                return sb;
            }

            return s;
        }

        private static string GetWindowTextUsingWinApi(IntPtr mainWindow)
        {
            if (IntPtr.Zero == mainWindow)
                throw new ArgumentException(
                    "Expected valid window handle.");
            var len = GetWindowTextLength(mainWindow);
            var sb = new StringBuilder(len + 1);
            GetWindowText(mainWindow, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetWindowTextUsingNet()
        {
            return Process.GetCurrentProcess().MainWindowTitle;
        }
    }
}