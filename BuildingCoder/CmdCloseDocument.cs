#region Header

//
// CmdCloseDocument.cs - close active document by sending Windows message
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdCloseDocument : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            ThreadPool.QueueUserWorkItem(
                CloseDocProc);

            return Result.Succeeded;
        }

        private static void CloseDocProc(object stateInfo)
        {
            try
            {
                // maybe we need some checks for the right
                // document, but this is a simple sample...

                SendKeys.SendWait("^{F4}");
            }
            catch (Exception ex)
            {
                Util.ErrorMsg(ex.Message);
            }
        }

        private static void CloseDocByCommand(UIApplication uiapp)
        {
            var closeDoc
                = RevitCommandId.LookupPostableCommandId(
                    PostableCommand.Close);

            uiapp.PostCommand(closeDoc);
        }

        #region PostCommand + SendKeys

        // from https://forums.autodesk.com/t5/revit-api-forum/twinmotion-dynamic-link-export-fbx-automatically/m-p/10028748
        private void OnDialogBoxShowing(
            object sender,
            DialogBoxShowingEventArgs args)
        {
            //DialogBoxShowingEventArgs args
            var e2 = args
                as TaskDialogShowingEventArgs;

            e2.OverrideResult((int) TaskDialogResult.Ok);
        }

        private static async void RunCommands(
            UIApplication uiapp,
            RevitCommandId id_addin)
        {
            uiapp.PostCommand(id_addin);
            await Task.Delay(400);
            SendKeys.Send("{ENTER}");
            await Task.Delay(400);
            SendKeys.Send("{ENTER}");
            await Task.Delay(400);
            SendKeys.Send("{ENTER}");
            await Task.Delay(400);
            SendKeys.Send("{ESCAPE}");
            await Task.Delay(400);
            SendKeys.Send("{ESCAPE}");
        }

        public void TwinMotionExportFbx(Document doc)
        {
            //Document doc = this.ActiveUIDocument.Document;
            var app = doc.Application;
            var uiapp = new UIApplication(app);

            try
            {
                var id = RevitCommandId
                    .LookupPostableCommandId(
                        PostableCommand.PlaceAComponent);

                var name = "CustomCtrl_%CustomCtrl_%"
                           + "Twinmotion 2020%Twinmotion Direct Link%"
                           + "ExportButton";

                var id_addin = RevitCommandId
                    .LookupCommandId(name);

                if (id_addin != null)
                {
                    uiapp.DialogBoxShowing += OnDialogBoxShowing;

                    RunCommands(uiapp, id_addin);
                }
            }

            catch
            {
                TaskDialog.Show("Test", "error");
            }
            finally
            {
                uiapp.DialogBoxShowing
                    -= OnDialogBoxShowing;
            }
        }

        #endregion // PostCommand + SendKeys
    }
}