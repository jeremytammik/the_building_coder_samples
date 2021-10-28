#region Header

//
// CmdElectricalLoad.cs - Retrieve electrical load
//
// Copyright (C) 2019-2020 by Alexander Ignatovich and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    public class CmdElectricalLoad : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;

            var familyInstance
                = SelectFamilyInstanceWithApparentLoad(
                    uidoc);

            if (familyInstance == null)
                return Result.Cancelled;

            var electricalApparentLoadFactory
                = new ElectricalApparentLoadFactory();

            var apparentLoads = electricalApparentLoadFactory
                .Create(familyInstance);

            TaskDialog.Show("CmdElectricalLoad",
                string.Join("\n", apparentLoads));

            return Result.Succeeded;
        }

        private static FamilyInstance
            SelectFamilyInstanceWithApparentLoad(
                UIDocument uidoc)
        {
            var electricalApparentLoadFactory
                = new ElectricalApparentLoadFactory();

            var selectionFilter
                = new FamilyInstanceWithApparentLoadSelectionFilter(
                    electricalApparentLoadFactory);

            try
            {
                return (FamilyInstance) uidoc.Document.GetElement(
                    uidoc.Selection.PickObject(ObjectType.Element,
                        selectionFilter));
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private class ElectricalApparentLoad
        {
            public ElectricalApparentLoad(
                ElectricalSystemType electricalSystemType,
                int connectorId,
                double apparentLoad)
            {
                ElectricalSystemType = electricalSystemType;
                ConnectorId = connectorId;
                ApparentLoad = apparentLoad;
            }

            public ElectricalSystemType ElectricalSystemType { get; }

            public int ConnectorId { get; }

            public double ApparentLoad { get; }

            public override string ToString()
            {
                return $"{ElectricalSystemType}: {ConnectorId} - {{ApparentLoad}} V*A";
            }
        }

        private class ElectricalApparentLoadFactory
        {
            public IEnumerable<ElectricalApparentLoad>
                Create(FamilyInstance familyInstance)
            {
                return familyInstance.MEPModel
                    .ConnectorManager
                    .Connectors
                    .Cast<Connector>()
                    .Select(Create)
                    .Where(x => x != null);
            }

            private static ElectricalApparentLoad Create(
                Connector connector)
            {
                if (connector.Domain != Domain.DomainElectrical)
                    return null;

                var mepConnectorInfo
                    = connector.GetMEPConnectorInfo()
                        as MEPFamilyConnectorInfo;

                if (mepConnectorInfo
                    ?.GetConnectorParameterValue(
                        new ElementId(
                            BuiltInParameter.RBS_ELEC_APPARENT_LOAD)) is not DoubleParameterValue parameterValue)
                    return null;

                //var load = UnitUtils.ConvertFromInternalUnits(
                //  parameterValue.Value,
                //  DisplayUnitType.DUT_VOLT_AMPERES ); // 2020

                var load = UnitUtils.ConvertFromInternalUnits(
                    parameterValue.Value,
                    UnitTypeId.VoltAmperes); // 2021

                return new ElectricalApparentLoad(
                    connector.ElectricalSystemType,
                    connector.Id, load);
            }
        }

        private class FamilyInstanceWithApparentLoadSelectionFilter
            : ISelectionFilter
        {
            private readonly ElectricalApparentLoadFactory
                electricalApparentLoadFactory;

            public FamilyInstanceWithApparentLoadSelectionFilter(
                ElectricalApparentLoadFactory
                    electricalApparentLoadFactory)
            {
                this.electricalApparentLoadFactory
                    = electricalApparentLoadFactory;
            }

            public bool AllowElement(Element elem)
            {
                if (elem is not FamilyInstance familyInstance)
                    return false;

                return electricalApparentLoadFactory
                    .Create(familyInstance)
                    .Any();
            }

            public bool AllowReference(Reference r, XYZ p)
            {
                return false;
            }
        }
    }
}