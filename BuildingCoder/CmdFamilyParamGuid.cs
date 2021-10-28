#region Header

//
// CmdFamilyParamGuid.cs - determine family parameter IsShared and GUID properties using System.Reflection
//
// Copyright (C) 2011-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdFamilyParamGuid : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            if (!doc.IsFamilyDocument)
            {
                message =
                    "Please run this command in a family document.";

                return Result.Failed;
            }

            bool isShared;
            string guid;

            var mgr = doc.FamilyManager;

            foreach (FamilyParameter fp in mgr.Parameters)
            {
                // Using GetFamilyParamGuid method, 
                // internally accessing m_Parameter:

                isShared = GetFamilyParamGuid(fp, out guid);

                // Using extension method, internally 
                // accessing getParameter:

                if (fp.IsShared())
                {
                    var giud2 = fp.GUID;
                }
            }

            return Result.Succeeded;
        }

        /// <summary>
        ///     Get family parameter IsShared
        ///     and GUID properties.
        /// </summary>
        /// <returns>
        ///     True if the family parameter
        ///     is shared and has a GUID.
        /// </returns>
        private bool GetFamilyParamGuid(
            FamilyParameter fp,
            out string guid)
        {
            guid = string.Empty;

            var isShared = false;

            var fi
                = fp.GetType().GetField("m_Parameter",
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            if (null != fi)
            {
                var p = fi.GetValue(fp) as Parameter;

                isShared = p.IsShared;

                if (isShared && null != p.GUID) guid = p.GUID.ToString();
            }

            return isShared;
        }
    }
}