#region Header

//
// CmdNewDuctSystem.cs - create a new duct system via the NewMechanicalSystem API call
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewDuctSystem : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            using var tx = new Transaction(doc);
            tx.Start("New Duct System");

            var connectorSet = new ConnectorSet();

            Connector baseConnector = null;

            ConnectorSetIterator csi;

            // select a Parallel Fan Powered VAV
            // and some Supply Diffusers prior to running
            // this command

            //ElementSet selection = uidoc.Selection.Elements; // 2014

            foreach (var id in uidoc.Selection.GetElementIds()) // 2015
            {
                var e = doc.GetElement(id);

                if (e is FamilyInstance fi)
                {
                    var family = fi.Symbol.Family;

                    // assume the selected Mechanical Equipment
                    // is the base equipment for new system:

                    switch (family.FamilyCategory.Name)
                    {
                        case "Mechanical Equipment":
                        {
                            // find the "Out" and "SupplyAir" connectors
                            // on the base equipment

                            if (null != fi.MEPModel)
                            {
                                csi = fi.MEPModel.ConnectorManager
                                    .Connectors.ForwardIterator();

                                while (csi.MoveNext())
                                {
                                    var conn = csi.Current as Connector;

                                    if (conn.Direction == FlowDirectionType.Out
                                        && conn.DuctSystemType == DuctSystemType.SupplyAir)
                                    {
                                        baseConnector = conn;
                                        break;
                                    }
                                }
                            }

                            break;
                        }
                        case "Air Terminals":
                            // add selected Air Terminals to
                            // connector set for new mechanical system

                            csi = fi.MEPModel.ConnectorManager
                                .Connectors.ForwardIterator();

                            csi.MoveNext();

                            connectorSet.Insert(csi.Current as Connector);
                            break;
                    }
                }
            }

            // create a new SupplyAir mechanical system

            var ductSystem = doc.Create.NewMechanicalSystem(
                baseConnector, connectorSet, DuctSystemType.SupplyAir);

            tx.Commit();

            return Result.Succeeded;
        }
    }
}