#region Header

//
// CmdLinq.cs - test linq.
//
// Copyright (C) 2009-2020 by Joel Karr and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdLinq : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(FamilyInstance));

            var instanceDataList
                = new List<InstanceData>();

            foreach (var e in collector)
                instanceDataList.Add(
                    new InstanceData(e));

            var s = "value1";
            var b = true;
            var i = 42;

            var found = from instance in instanceDataList
                where
                    instance.Param1.Equals(s)
                    && b == instance.Param2
                    && i < instance.Param3
                select instance;

            foreach (var instance in found)
            {
                // Do whatever you would like
            }

            return Result.Failed;
        }

        private class InstanceData
        {
            #region Constructor

            public InstanceData(Element instance)
            {
                Instance = instance;

                var m = Instance.ParametersMap;

                var p = m.get_Item("Param1");
                Param1 = p == null ? string.Empty : p.AsString();

                p = m.get_Item("Param2");
                Param2 = p == null ? false : 0 != p.AsInteger();

                p = m.get_Item("Param3");
                Param3 = p == null ? 0 : p.AsInteger();
            }

            #endregion

            #region Properties

            // auto-implemented properties, cf.
            // http://msdn.microsoft.com/en-us/library/bb384054.aspx

            public Element Instance { get; }
            public string Param1 { get; }
            public bool Param2 { get; }
            public int Param3 { get; }

            #endregion
        }
    }
}