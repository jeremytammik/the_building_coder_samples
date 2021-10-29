#region Header

//
// CmdWindowHandle.cs - determine Revit
// application main window handle.
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Wrapper class for converting IntPtr to IWin32Window.
    /// </summary>
    public class WindowHandle : IWin32Window
    {
        public WindowHandle(IntPtr h)
        {
            Debug.Assert(IntPtr.Zero != h,
                "expected non-null window handle");

            Handle = h;
        }

        public IntPtr Handle { get; }
    }

    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdWindowHandle : IExternalCommand
    {
        private const string _prompt
            = "Please select some elements.";

        private static WindowHandle _hWndRevit;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            if (null == _hWndRevit)
            {
                //Process[] processes
                //  = Process.GetProcessesByName( "Revit" );

                //if( 0 < processes.Length )
                //{
                //  IntPtr h = processes[0].MainWindowHandle;
                //  _hWndRevit = new WindowHandle( h );
                //}

                var process
                    = Process.GetCurrentProcess();

                var h = process.MainWindowHandle;
                _hWndRevit = new WindowHandle(h);
            }

            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var sel = uidoc.Selection;

            using var f
                = new CmdWindowHandleForm();
            f.Show(_hWndRevit);
            var go = true;
            while (go)
            {
                //SelElementSet ss = sel.Elements; // 2014
                //int n = ss.Size;
                var ids = sel.GetElementIds(); // 2015
                var n = ids.Count;

                var s = $"{n} element{Util.PluralSuffix(n)} selected{(0 == n ? $";\n{_prompt}" : ":")}";

                foreach (var id in ids)
                {
                    s += "\n";
                    s += Util.ElementDescription(
                        doc.GetElement(id));
                }

                f.LabelText = s;

#if _2010
          sel.StatusbarTip = _prompt;
          go = sel.PickOne();
#endif // _2010

                var r = uidoc.Selection.PickObject(
                    ObjectType.Element, _prompt);

                go = null != r;

                Debug.Print($"go = {go}");
            }

            return Result.Failed;
        }

        /// <summary>
        ///     Modified version with changes to use WindowSelect, by Tao (Tau) Yang, Autodesk.
        /// </summary>
        public Result Execute2(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            if (null == _hWndRevit)
            {
                var process
                    = Process.GetCurrentProcess();

                var h = process.MainWindowHandle;
                _hWndRevit = new WindowHandle(h);
            }

            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var sel = uidoc.Selection;

            using var f
                = new CmdWindowHandleForm();
            f.Show(_hWndRevit);
            var go = true;
            while (go)
            {
#if _2010
          sel.StatusbarTip = _prompt;
          go = sel.WindowSelect();
#endif // _2010

                var a = sel.PickElementsByRectangle(_prompt);
                go = 0 < a.Count;

                //SelElementSet ss = sel.Elements; // 2014
                //int n = ss.Size;
                var ids = sel.GetElementIds(); // 2015
                var n = ids.Count;

                var s = $"{n} element{Util.PluralSuffix(n)} selected{(0 == n ? $";\n{_prompt}" : ":")}";

                //foreach( Element e in ss )
                foreach (var id in ids)
                {
                    s += "\n";
                    s += Util.ElementDescription(
                        doc.GetElement(id));
                }

                f.LabelText = s;
                Debug.Print($"go = {go}");
            }

            return Result.Failed;
        }
    }
}