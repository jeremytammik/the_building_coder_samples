#region Header

//
// CmdCategorySupportsTypeParameter.cs - Boolean predicate to 
// determine whether a given category supports type parameter
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Boolean predicate to determine whether a
    ///     given category supports type parameter.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class CmdCategorySupportsTypeParameter : IExternalCommand
    {
        // How does an enumeration compare with a
        // dictionary performance-wise? Unclear to me, cf.
        // http://stackoverflow.com/questions/3256713/enum-and-performance

        // The two enumerations of built-in categories 
        // supporting instance and type parameters were
        // provided by Todd Jacobs in the Revit API 
        // discussion forum thread "Determine if Category 
        // supports Type parameter binding", cf.
        // http://forums.autodesk.com/t5/Revit-API/Determine-if-Category-supports-Type-parameter-binding/m-p/4918068

        #region Built-in categories supporting type parameters

        public static BuiltInCategory[]
            _bicAllowsBoundParametersAsType
                =
                {
                    ///<summary>Analytical Links</summary>
                    BuiltInCategory.OST_LinksAnalytical,
                    ///<summary>Structural Connections</summary>
                    BuiltInCategory.OST_StructConnections,
                    ///<summary>Structural Fabric Areas</summary>
                    BuiltInCategory.OST_FabricAreas,
                    ///<summary>Structural Fabric Reinforcement</summary>
                    BuiltInCategory.OST_FabricReinforcement,
                    ///<summary>Rebar Shape</summary>
                    BuiltInCategory.OST_RebarShape,
                    ///<summary>Structural Path Reinforcement</summary>
                    BuiltInCategory.OST_PathRein,
                    ///<summary>Structural Area Reinforcement</summary>
                    BuiltInCategory.OST_AreaRein,
                    ///<summary>Structural Rebar</summary>
                    BuiltInCategory.OST_Rebar,
                    ///<summary>Pipe Placeholders</summary>
                    BuiltInCategory.OST_PlaceHolderPipes,
                    ///<summary>Duct Placeholders</summary>
                    BuiltInCategory.OST_PlaceHolderDucts,
                    ///<summary>Cable Tray Runs</summary>
                    BuiltInCategory.OST_CableTrayRun,
                    ///<summary>Conduit Runs</summary>
                    BuiltInCategory.OST_ConduitRun,
                    ///<summary>Conduits</summary>
                    BuiltInCategory.OST_Conduit,
                    ///<summary>Cable Trays</summary>
                    BuiltInCategory.OST_CableTray,
                    ///<summary>Conduit Fittings</summary>
                    BuiltInCategory.OST_ConduitFitting,
                    ///<summary>Cable Tray Fittings</summary>
                    BuiltInCategory.OST_CableTrayFitting,
                    ///<summary>Duct Linings</summary>
                    BuiltInCategory.OST_DuctLinings,
                    ///<summary>Duct Insulations</summary>
                    BuiltInCategory.OST_DuctInsulations,
                    ///<summary>Pipe Insulations</summary>
                    BuiltInCategory.OST_PipeInsulations,
                    ///<summary>Switch System</summary>
                    BuiltInCategory.OST_SwitchSystem,
                    ///<summary>Sprinklers</summary>
                    BuiltInCategory.OST_Sprinklers,
                    ///<summary>Lighting Devices</summary>
                    BuiltInCategory.OST_LightingDevices,
                    ///<summary>Fire Alarm Devices</summary>
                    BuiltInCategory.OST_FireAlarmDevices,
                    ///<summary>Data Devices</summary>
                    BuiltInCategory.OST_DataDevices,
                    ///<summary>Communication Devices</summary>
                    BuiltInCategory.OST_CommunicationDevices,
                    ///<summary>Security Devices</summary>
                    BuiltInCategory.OST_SecurityDevices,
                    ///<summary>Nurse Call Devices</summary>
                    BuiltInCategory.OST_NurseCallDevices,
                    ///<summary>Telephone Devices</summary>
                    BuiltInCategory.OST_TelephoneDevices,
                    ///<summary>Pipe Accessories</summary>
                    BuiltInCategory.OST_PipeAccessory,
                    ///<summary>Flex Pipes</summary>
                    BuiltInCategory.OST_FlexPipeCurves,
                    ///<summary>Pipe Fittings</summary>
                    BuiltInCategory.OST_PipeFitting,
                    ///<summary>Pipes</summary>
                    BuiltInCategory.OST_PipeCurves,
                    ///<summary>Piping Systems</summary>
                    BuiltInCategory.OST_PipingSystem,
                    ///<summary>Wires</summary>
                    BuiltInCategory.OST_Wire,
                    ///<summary>Flex Ducts</summary>
                    BuiltInCategory.OST_FlexDuctCurves,
                    ///<summary>Duct Accessories</summary>
                    BuiltInCategory.OST_DuctAccessory,
                    ///<summary>Duct Systems</summary>
                    BuiltInCategory.OST_DuctSystem,
                    ///<summary>Air Terminals</summary>
                    BuiltInCategory.OST_DuctTerminal,
                    ///<summary>Duct Fittings</summary>
                    BuiltInCategory.OST_DuctFitting,
                    ///<summary>Ducts</summary>
                    BuiltInCategory.OST_DuctCurves,
                    ///<summary>Mass</summary>
                    BuiltInCategory.OST_Mass,
                    ///<summary>Detail Items</summary>
                    BuiltInCategory.OST_DetailComponents,
                    ///<summary>Floors.Slab Edges</summary>
                    BuiltInCategory.OST_EdgeSlab,
                    ///<summary>Roofs.Gutters</summary>
                    BuiltInCategory.OST_Gutter,
                    ///<summary>Roofs.Fascias</summary>
                    BuiltInCategory.OST_Fascia,
                    ///<summary>Planting</summary>
                    BuiltInCategory.OST_Planting,
                    ///<summary>Structural Stiffeners</summary>
                    BuiltInCategory.OST_StructuralStiffener,
                    ///<summary>Specialty Equipment</summary>
                    BuiltInCategory.OST_SpecialityEquipment,
                    ///<summary>Topography</summary>
                    BuiltInCategory.OST_Topography,
                    ///<summary>Structural Trusses</summary>
                    BuiltInCategory.OST_StructuralTruss,
                    ///<summary>Structural Columns</summary>
                    BuiltInCategory.OST_StructuralColumns,
                    ///<summary>Structural Beam Systems</summary>
                    BuiltInCategory.OST_StructuralFramingSystem,
                    ///<summary>Structural Framing</summary>
                    BuiltInCategory.OST_StructuralFraming,
                    ///<summary>Structural Foundations</summary>
                    BuiltInCategory.OST_StructuralFoundation,
                    ///<summary>Site.Property Line Segments</summary>
                    BuiltInCategory.OST_SitePropertyLineSegment,
                    ///<summary>Site.Property Lines</summary>
                    BuiltInCategory.OST_SiteProperty,
                    ///<summary>Site.Pads</summary>
                    BuiltInCategory.OST_BuildingPad,
                    ///<summary>Site</summary>
                    BuiltInCategory.OST_Site,
                    ///<summary>Parking</summary>
                    BuiltInCategory.OST_Parking,
                    ///<summary>Plumbing Fixtures</summary>
                    BuiltInCategory.OST_PlumbingFixtures,
                    ///<summary>Mechanical Equipment</summary>
                    BuiltInCategory.OST_MechanicalEquipment,
                    ///<summary>Lighting Fixtures</summary>
                    BuiltInCategory.OST_LightingFixtures,
                    ///<summary>Furniture Systems</summary>
                    BuiltInCategory.OST_FurnitureSystems,
                    ///<summary>Electrical Fixtures</summary>
                    BuiltInCategory.OST_ElectricalFixtures,
                    ///<summary>Electrical Equipment</summary>
                    BuiltInCategory.OST_ElectricalEquipment,
                    ///<summary>Casework</summary>
                    BuiltInCategory.OST_Casework,
                    ///<summary>Railings.Terminations</summary>
                    BuiltInCategory.OST_RailingTermination,
                    ///<summary>Railings.Supports</summary>
                    BuiltInCategory.OST_RailingSupport,
                    ///<summary>Railings.Handrails</summary>
                    BuiltInCategory.OST_RailingHandRail,
                    ///<summary>Railings.Top Rails</summary>
                    BuiltInCategory.OST_RailingTopRail,
                    ///<summary>Stairs.Landings</summary>
                    BuiltInCategory.OST_StairsLandings,
                    ///<summary>Stairs.Runs</summary>
                    BuiltInCategory.OST_StairsRuns,
                    ///<summary>Curtain Systems</summary>
                    BuiltInCategory.OST_CurtaSystem,
                    ///<summary>Assemblies</summary>
                    BuiltInCategory.OST_Assemblies,
                    ///<summary>Levels</summary>
                    BuiltInCategory.OST_Levels,
                    ///<summary>Grids</summary>
                    BuiltInCategory.OST_Grids,
                    ///<summary>Walls.Wall Sweeps</summary>
                    BuiltInCategory.OST_Cornices,
                    ///<summary>Ramps</summary>
                    BuiltInCategory.OST_Ramps,
                    ///<summary>Curtain Wall Mullions</summary>
                    BuiltInCategory.OST_CurtainWallMullions,
                    ///<summary>Curtain Panels</summary>
                    BuiltInCategory.OST_CurtainWallPanels,
                    ///<summary>Generic Models</summary>
                    BuiltInCategory.OST_GenericModel,
                    ///<summary>Railings</summary>
                    BuiltInCategory.OST_StairsRailing,
                    ///<summary>Stairs</summary>
                    BuiltInCategory.OST_Stairs,
                    ///<summary>Columns</summary>
                    BuiltInCategory.OST_Columns,
                    ///<summary>Furniture</summary>
                    BuiltInCategory.OST_Furniture,
                    ///<summary>Ceilings</summary>
                    BuiltInCategory.OST_Ceilings,
                    ///<summary>Roofs</summary>
                    BuiltInCategory.OST_Roofs,
                    ///<summary>Floors</summary>
                    BuiltInCategory.OST_Floors,
                    ///<summary>Doors</summary>
                    BuiltInCategory.OST_Doors,
                    ///<summary>Windows</summary>
                    BuiltInCategory.OST_Windows,
                    ///<summary>Walls</summary>
                    BuiltInCategory.OST_Walls
                };

        #endregion // Built-in categories supporting type parameters

        #region Built-in categories supporting instance parameters

        public static BuiltInCategory[]
            _bicAllowsBoundParametersAsInstance
                =
                {
                    ///<summary>Analytical Links</summary>
                    BuiltInCategory.OST_LinksAnalytical,
                    ///<summary>Analytical Nodes</summary>
                    BuiltInCategory.OST_AnalyticalNodes,
                    ///<summary>Analytical Foundation Slabs</summary>
                    BuiltInCategory.OST_FoundationSlabAnalytical,
                    ///<summary>Analytical Wall Foundations</summary>
                    BuiltInCategory.OST_WallFoundationAnalytical,
                    ///<summary>Analytical Isolated Foundations</summary>
                    BuiltInCategory.OST_IsolatedFoundationAnalytical,
                    ///<summary>Analytical Walls</summary>
                    BuiltInCategory.OST_WallAnalytical,
                    ///<summary>Analytical Floors</summary>
                    BuiltInCategory.OST_FloorAnalytical,
                    ///<summary>Analytical Columns</summary>
                    BuiltInCategory.OST_ColumnAnalytical,
                    ///<summary>Analytical Braces</summary>
                    BuiltInCategory.OST_BraceAnalytical,
                    ///<summary>Analytical Beams</summary>
                    BuiltInCategory.OST_BeamAnalytical,
                    ///<summary>Structural Connections</summary>
                    BuiltInCategory.OST_StructConnections,
                    ///<summary>Structural Fabric Areas</summary>
                    BuiltInCategory.OST_FabricAreas,
                    ///<summary>Structural Fabric Reinforcement</summary>
                    BuiltInCategory.OST_FabricReinforcement,
                    ///<summary>Rebar Shape</summary>
                    BuiltInCategory.OST_RebarShape,
                    ///<summary>Structural Path Reinforcement</summary>
                    BuiltInCategory.OST_PathRein,
                    ///<summary>Structural Area Reinforcement</summary>
                    BuiltInCategory.OST_AreaRein,
                    ///<summary>Structural Rebar</summary>
                    BuiltInCategory.OST_Rebar,
                    ///<summary>Analytical Spaces</summary>
                    BuiltInCategory.OST_AnalyticSpaces,
                    ///<summary>Pipe Placeholders</summary>
                    BuiltInCategory.OST_PlaceHolderPipes,
                    ///<summary>Duct Placeholders</summary>
                    BuiltInCategory.OST_PlaceHolderDucts,
                    ///<summary>Cable Tray Runs</summary>
                    BuiltInCategory.OST_CableTrayRun,
                    ///<summary>Conduit Runs</summary>
                    BuiltInCategory.OST_ConduitRun,
                    ///<summary>Conduits</summary>
                    BuiltInCategory.OST_Conduit,
                    ///<summary>Cable Trays</summary>
                    BuiltInCategory.OST_CableTray,
                    ///<summary>Conduit Fittings</summary>
                    BuiltInCategory.OST_ConduitFitting,
                    ///<summary>Cable Tray Fittings</summary>
                    BuiltInCategory.OST_CableTrayFitting,
                    ///<summary>Duct Linings</summary>
                    BuiltInCategory.OST_DuctLinings,
                    ///<summary>Duct Insulations</summary>
                    BuiltInCategory.OST_DuctInsulations,
                    ///<summary>Pipe Insulations</summary>
                    BuiltInCategory.OST_PipeInsulations,
                    ///<summary>HVAC Zones</summary>
                    BuiltInCategory.OST_HVAC_Zones,
                    ///<summary>Switch System</summary>
                    BuiltInCategory.OST_SwitchSystem,
                    ///<summary>Sprinklers</summary>
                    BuiltInCategory.OST_Sprinklers,
                    ///<summary>Analytical Surfaces</summary>
                    BuiltInCategory.OST_GbXMLFaces,
                    ///<summary>Lighting Devices</summary>
                    BuiltInCategory.OST_LightingDevices,
                    ///<summary>Fire Alarm Devices</summary>
                    BuiltInCategory.OST_FireAlarmDevices,
                    ///<summary>Data Devices</summary>
                    BuiltInCategory.OST_DataDevices,
                    ///<summary>Communication Devices</summary>
                    BuiltInCategory.OST_CommunicationDevices,
                    ///<summary>Security Devices</summary>
                    BuiltInCategory.OST_SecurityDevices,
                    ///<summary>Nurse Call Devices</summary>
                    BuiltInCategory.OST_NurseCallDevices,
                    ///<summary>Telephone Devices</summary>
                    BuiltInCategory.OST_TelephoneDevices,
                    ///<summary>Pipe Accessories</summary>
                    BuiltInCategory.OST_PipeAccessory,
                    ///<summary>Flex Pipes</summary>
                    BuiltInCategory.OST_FlexPipeCurves,
                    ///<summary>Pipe Fittings</summary>
                    BuiltInCategory.OST_PipeFitting,
                    ///<summary>Pipes</summary>
                    BuiltInCategory.OST_PipeCurves,
                    ///<summary>Piping Systems</summary>
                    BuiltInCategory.OST_PipingSystem,
                    ///<summary>Wires</summary>
                    BuiltInCategory.OST_Wire,
                    ///<summary>Electrical Circuits</summary>
                    BuiltInCategory.OST_ElectricalCircuit,
                    ///<summary>Flex Ducts</summary>
                    BuiltInCategory.OST_FlexDuctCurves,
                    ///<summary>Duct Accessories</summary>
                    BuiltInCategory.OST_DuctAccessory,
                    ///<summary>Duct Systems</summary>
                    BuiltInCategory.OST_DuctSystem,
                    ///<summary>Air Terminals</summary>
                    BuiltInCategory.OST_DuctTerminal,
                    ///<summary>Duct Fittings</summary>
                    BuiltInCategory.OST_DuctFitting,
                    ///<summary>Ducts</summary>
                    BuiltInCategory.OST_DuctCurves,
                    ///<summary>Structural Internal Loads.Internal Area Loads</summary>
                    BuiltInCategory.OST_InternalAreaLoads,
                    ///<summary>Structural Internal Loads.Internal Line Loads</summary>
                    BuiltInCategory.OST_InternalLineLoads,
                    ///<summary>Structural Internal Loads.Internal Point Loads</summary>
                    BuiltInCategory.OST_InternalPointLoads,
                    ///<summary>Structural Loads.Area Loads</summary>
                    BuiltInCategory.OST_AreaLoads,
                    ///<summary>Structural Loads.Line Loads</summary>
                    BuiltInCategory.OST_LineLoads,
                    ///<summary>Structural Loads.Point Loads</summary>
                    BuiltInCategory.OST_PointLoads,
                    ///<summary>Spaces</summary>
                    BuiltInCategory.OST_MEPSpaces,
                    ///<summary>Mass.Mass Opening</summary>
                    BuiltInCategory.OST_MassOpening,
                    ///<summary>Mass.Mass Skylight</summary>
                    BuiltInCategory.OST_MassSkylights,
                    ///<summary>Mass.Mass Glazing</summary>
                    //BuiltInCategory.OST_MassWindow, // jeremy - not available in Revit 2015
                    ///<summary>Mass.Mass Roof</summary>
                    BuiltInCategory.OST_MassRoof,
                    ///<summary>Mass.Mass Exterior Wall</summary>
                    BuiltInCategory.OST_MassExteriorWall,
                    ///<summary>Mass.Mass Interior Wall</summary>
                    BuiltInCategory.OST_MassInteriorWall,
                    ///<summary>Mass.Mass Zone</summary>
                    BuiltInCategory.OST_MassZone,
                    ///<summary>Mass.Mass Floor</summary>
                    BuiltInCategory.OST_MassFloor,
                    ///<summary>Mass</summary>
                    BuiltInCategory.OST_Mass,
                    ///<summary>Areas</summary>
                    BuiltInCategory.OST_Areas,
                    ///<summary>Project Information</summary>
                    BuiltInCategory.OST_ProjectInformation,
                    ///<summary>Sheets</summary>
                    BuiltInCategory.OST_Sheets,
                    ///<summary>Detail Items</summary>
                    BuiltInCategory.OST_DetailComponents,
                    ///<summary>Floors.Slab Edges</summary>
                    BuiltInCategory.OST_EdgeSlab,
                    ///<summary>Roofs.Gutters</summary>
                    BuiltInCategory.OST_Gutter,
                    ///<summary>Roofs.Fascias</summary>
                    BuiltInCategory.OST_Fascia,
                    ///<summary>Planting</summary>
                    BuiltInCategory.OST_Planting,
                    ///<summary>Structural Stiffeners</summary>
                    BuiltInCategory.OST_StructuralStiffener,
                    ///<summary>Specialty Equipment</summary>
                    BuiltInCategory.OST_SpecialityEquipment,
                    ///<summary>Topography</summary>
                    BuiltInCategory.OST_Topography,
                    ///<summary>Structural Trusses</summary>
                    BuiltInCategory.OST_StructuralTruss,
                    ///<summary>Structural Columns</summary>
                    BuiltInCategory.OST_StructuralColumns,
                    ///<summary>Structural Beam Systems</summary>
                    BuiltInCategory.OST_StructuralFramingSystem,
                    ///<summary>Structural Framing</summary>
                    BuiltInCategory.OST_StructuralFraming,
                    ///<summary>Structural Foundations</summary>
                    BuiltInCategory.OST_StructuralFoundation,
                    ///<summary>Site.Property Line Segments</summary>
                    BuiltInCategory.OST_SitePropertyLineSegment,
                    ///<summary>Site.Property Lines</summary>
                    BuiltInCategory.OST_SiteProperty,
                    ///<summary>Site.Pads</summary>
                    BuiltInCategory.OST_BuildingPad,
                    ///<summary>Site</summary>
                    BuiltInCategory.OST_Site,
                    ///<summary>Roads</summary>
                    BuiltInCategory.OST_Roads,
                    ///<summary>Parking</summary>
                    BuiltInCategory.OST_Parking,
                    ///<summary>Plumbing Fixtures</summary>
                    BuiltInCategory.OST_PlumbingFixtures,
                    ///<summary>Mechanical Equipment</summary>
                    BuiltInCategory.OST_MechanicalEquipment,
                    ///<summary>Lighting Fixtures</summary>
                    BuiltInCategory.OST_LightingFixtures,
                    ///<summary>Furniture Systems</summary>
                    BuiltInCategory.OST_FurnitureSystems,
                    ///<summary>Electrical Fixtures</summary>
                    BuiltInCategory.OST_ElectricalFixtures,
                    ///<summary>Electrical Equipment</summary>
                    BuiltInCategory.OST_ElectricalEquipment,
                    ///<summary>Casework</summary>
                    BuiltInCategory.OST_Casework,
                    ///<summary>Shaft Openings</summary>
                    BuiltInCategory.OST_ShaftOpening,
                    ///<summary>Railings.Terminations</summary>
                    BuiltInCategory.OST_RailingTermination,
                    ///<summary>Railings.Supports</summary>
                    BuiltInCategory.OST_RailingSupport,
                    ///<summary>Railings.Handrails</summary>
                    BuiltInCategory.OST_RailingHandRail,
                    ///<summary>Railings.Top Rails</summary>
                    BuiltInCategory.OST_RailingTopRail,
                    ///<summary>Stairs.Landings</summary>
                    BuiltInCategory.OST_StairsLandings,
                    ///<summary>Stairs.Runs</summary>
                    BuiltInCategory.OST_StairsRuns,
                    ///<summary>Materials</summary>
                    BuiltInCategory.OST_Materials,
                    ///<summary>Curtain Systems</summary>
                    BuiltInCategory.OST_CurtaSystem,
                    ///<summary>Views</summary>
                    BuiltInCategory.OST_Views,
                    ///<summary>Parts</summary>
                    BuiltInCategory.OST_Parts,
                    ///<summary>Assemblies</summary>
                    BuiltInCategory.OST_Assemblies,
                    ///<summary>Levels</summary>
                    BuiltInCategory.OST_Levels,
                    ///<summary>Grids</summary>
                    BuiltInCategory.OST_Grids,
                    ///<summary>Walls.Wall Sweeps</summary>
                    BuiltInCategory.OST_Cornices,
                    ///<summary>Ramps</summary>
                    BuiltInCategory.OST_Ramps,
                    ///<summary>Curtain Wall Mullions</summary>
                    BuiltInCategory.OST_CurtainWallMullions,
                    ///<summary>Curtain Panels</summary>
                    BuiltInCategory.OST_CurtainWallPanels,
                    ///<summary>Rooms</summary>
                    BuiltInCategory.OST_Rooms,
                    ///<summary>Generic Models</summary>
                    BuiltInCategory.OST_GenericModel,
                    ///<summary>Railings</summary>
                    BuiltInCategory.OST_StairsRailing,
                    ///<summary>Stairs</summary>
                    BuiltInCategory.OST_Stairs,
                    ///<summary>Columns</summary>
                    BuiltInCategory.OST_Columns,
                    ///<summary>Furniture</summary>
                    BuiltInCategory.OST_Furniture,
                    ///<summary>Ceilings</summary>
                    BuiltInCategory.OST_Ceilings,
                    ///<summary>Roofs</summary>
                    BuiltInCategory.OST_Roofs,
                    ///<summary>Floors</summary>
                    BuiltInCategory.OST_Floors,
                    ///<summary>Doors</summary>
                    BuiltInCategory.OST_Doors,
                    ///<summary>Windows</summary>
                    BuiltInCategory.OST_Windows,
                    ///<summary>Walls</summary>
                    BuiltInCategory.OST_Walls
                };

        #endregion // Built-in categories supporting instance parameters

        private static readonly Dictionary<BuiltInCategory, BuiltInCategory>
            _bicSupportsTypeParameters
                = _bicAllowsBoundParametersAsType
                    .ToDictionary(
                        c => c);

        private static readonly Dictionary<BuiltInCategory, BuiltInCategory>
            _bicSupportsInstanceParameters
                = _bicAllowsBoundParametersAsInstance
                    .ToDictionary(
                        c => c);

        public Result Execute(
            ExternalCommandData revit,
            ref string message,
            ElementSet elements)
        {
            var uiapp = revit.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var nCategories = 0;
            var nSupportType = 0;
            var nSupportInstance = 0;
            bool bType, bInstance;

            foreach (BuiltInCategory bic in
                Enum.GetValues(typeof(BuiltInCategory)))
            {
                bType = BicSupportsTypeParameters(bic);
                bInstance = BicSupportsInstanceParameters(bic);

                ++nCategories;
                nSupportType += bType ? 1 : 0;
                nSupportInstance += bInstance ? 1 : 0;

                Debug.Print("{0} {1} instance and {2} type parameters",
                    bic,
                    SupportsOrNotString(bInstance),
                    SupportsOrNotString(bType));
            }

            var caption = "Categories supporting type "
                          + "and instance parameters";

            var msg = $"Tested {nCategories} built-in categories in total, {nSupportInstance} supporting instance and {nSupportType} supporting type parameters.";

            Debug.Print($"\n{caption}:\n{msg}");

            TaskDialog.Show(caption, msg);

            return Result.Succeeded;
        }

        #region Todd's sample code snippet

        private void f(Document doc)
        {
            var app = doc.Application;

            var isInstanceBinding = false; // true = Instance, false = Type
            var bStatus = true;
            var szBIC_Name = "OST_MechanicalEquipment";
            var catSet = app.Create.NewCategorySet();

            if (isInstanceBinding)
            {
                BIC_AllowsBoundParametersAsInstance BIC_ToInsert;

                if (Enum.TryParse(szBIC_Name, out BIC_ToInsert))
                    catSet.Insert(doc.Settings.Categories.get_Item((BuiltInCategory) BIC_ToInsert));
                else
                    // Invalid Category
                    bStatus = false;
                //else ?
                {
                    BIC_AllowsBoundParametersAsType BIC_ToInsert2;

                    if (Enum.TryParse(szBIC_Name, out BIC_ToInsert2))
                        catSet.Insert(doc.Settings.Categories.get_Item((BuiltInCategory) BIC_ToInsert2));
                    else
                        // Invalid Category
                        bStatus = false;
                }

                if (bStatus)
                {
                    var binding = isInstanceBinding
                        ? app.Create.NewTypeBinding(catSet)
                        : app.Create.NewInstanceBinding(catSet) as Binding;

                    //BuiltInParameterGroup parameterGroup;
                    Definition def = null;

                    doc.ParameterBindings.Insert(def, binding);
                }

                // test for enum equlity

                var szMessage = string.Empty;

                // Without cast
                if (BuiltInCategory.OST_MechanicalEquipment.Equals(BIC_AllowsBoundParametersAsType.OST_MechanicalEquipment))
                    szMessage = "We are Equal";
                else
                    // we get here
                    szMessage = "We are not Equal";
                //MessageBox.Show( szMessage );

                // With cast
                if (BuiltInCategory.OST_MechanicalEquipment.Equals((BuiltInCategory) BIC_AllowsBoundParametersAsType.OST_MechanicalEquipment))
                    // we get here
                    szMessage = "We are Equal";
                else
                    szMessage = "We are not Equal";
                //MessageBox.Show( szMessage );
            }
        }

        #endregion // Todd's sample code snippet

        /// <summary>
        ///     Return true if the given built-in
        ///     category supports type parameters.
        /// </summary>
        private static bool BicSupportsTypeParameters(
            BuiltInCategory bic)
        {
            return _bicSupportsTypeParameters.ContainsKey(
                bic);
        }

        /// <summary>
        ///     Return true if the given built-in
        ///     category supports instance parameters.
        /// </summary>
        private static bool BicSupportsInstanceParameters(
            BuiltInCategory bic)
        {
            return _bicSupportsInstanceParameters.ContainsKey(
                bic);
        }

        private static string SupportsOrNotString(bool b)
        {
            return b
                ? "supports"
                : "does not support";
        }

        #region Todd's enums

        public enum BIC_AllowsBoundParametersAsType
        {
            ///<summary>Analytical Links</summary>
            OST_LinksAnalytical = BuiltInCategory.OST_LinksAnalytical,

            ///<summary>Structural Connections</summary>
            OST_StructConnections = BuiltInCategory.OST_StructConnections,

            ///<summary>Structural Fabric Areas</summary>
            OST_FabricAreas = BuiltInCategory.OST_FabricAreas,

            ///<summary>Structural Fabric Reinforcement</summary>
            OST_FabricReinforcement = BuiltInCategory.OST_FabricReinforcement,

            ///<summary>Rebar Shape</summary>
            OST_RebarShape = BuiltInCategory.OST_RebarShape,

            ///<summary>Structural Path Reinforcement</summary>
            OST_PathRein = BuiltInCategory.OST_PathRein,

            ///<summary>Structural Area Reinforcement</summary>
            OST_AreaRein = BuiltInCategory.OST_AreaRein,

            ///<summary>Structural Rebar</summary>
            OST_Rebar = BuiltInCategory.OST_Rebar,

            ///<summary>Pipe Placeholders</summary>
            OST_PlaceHolderPipes = BuiltInCategory.OST_PlaceHolderPipes,

            ///<summary>Duct Placeholders</summary>
            OST_PlaceHolderDucts = BuiltInCategory.OST_PlaceHolderDucts,

            ///<summary>Cable Tray Runs</summary>
            OST_CableTrayRun = BuiltInCategory.OST_CableTrayRun,

            ///<summary>Conduit Runs</summary>
            OST_ConduitRun = BuiltInCategory.OST_ConduitRun,

            ///<summary>Conduits</summary>
            OST_Conduit = BuiltInCategory.OST_Conduit,

            ///<summary>Cable Trays</summary>
            OST_CableTray = BuiltInCategory.OST_CableTray,

            ///<summary>Conduit Fittings</summary>
            OST_ConduitFitting = BuiltInCategory.OST_ConduitFitting,

            ///<summary>Cable Tray Fittings</summary>
            OST_CableTrayFitting = BuiltInCategory.OST_CableTrayFitting,

            ///<summary>Duct Linings</summary>
            OST_DuctLinings = BuiltInCategory.OST_DuctLinings,

            ///<summary>Duct Insulations</summary>
            OST_DuctInsulations = BuiltInCategory.OST_DuctInsulations,

            ///<summary>Pipe Insulations</summary>
            OST_PipeInsulations = BuiltInCategory.OST_PipeInsulations,

            ///<summary>Switch System</summary>
            OST_SwitchSystem = BuiltInCategory.OST_SwitchSystem,

            ///<summary>Sprinklers</summary>
            OST_Sprinklers = BuiltInCategory.OST_Sprinklers,

            ///<summary>Lighting Devices</summary>
            OST_LightingDevices = BuiltInCategory.OST_LightingDevices,

            ///<summary>Fire Alarm Devices</summary>
            OST_FireAlarmDevices = BuiltInCategory.OST_FireAlarmDevices,

            ///<summary>Data Devices</summary>
            OST_DataDevices = BuiltInCategory.OST_DataDevices,

            ///<summary>Communication Devices</summary>
            OST_CommunicationDevices = BuiltInCategory.OST_CommunicationDevices,

            ///<summary>Security Devices</summary>
            OST_SecurityDevices = BuiltInCategory.OST_SecurityDevices,

            ///<summary>Nurse Call Devices</summary>
            OST_NurseCallDevices = BuiltInCategory.OST_NurseCallDevices,

            ///<summary>Telephone Devices</summary>
            OST_TelephoneDevices = BuiltInCategory.OST_TelephoneDevices,

            ///<summary>Pipe Accessories</summary>
            OST_PipeAccessory = BuiltInCategory.OST_PipeAccessory,

            ///<summary>Flex Pipes</summary>
            OST_FlexPipeCurves = BuiltInCategory.OST_FlexPipeCurves,

            ///<summary>Pipe Fittings</summary>
            OST_PipeFitting = BuiltInCategory.OST_PipeFitting,

            ///<summary>Pipes</summary>
            OST_PipeCurves = BuiltInCategory.OST_PipeCurves,

            ///<summary>Piping Systems</summary>
            OST_PipingSystem = BuiltInCategory.OST_PipingSystem,

            ///<summary>Wires</summary>
            OST_Wire = BuiltInCategory.OST_Wire,

            ///<summary>Flex Ducts</summary>
            OST_FlexDuctCurves = BuiltInCategory.OST_FlexDuctCurves,

            ///<summary>Duct Accessories</summary>
            OST_DuctAccessory = BuiltInCategory.OST_DuctAccessory,

            ///<summary>Duct Systems</summary>
            OST_DuctSystem = BuiltInCategory.OST_DuctSystem,

            ///<summary>Air Terminals</summary>
            OST_DuctTerminal = BuiltInCategory.OST_DuctTerminal,

            ///<summary>Duct Fittings</summary>
            OST_DuctFitting = BuiltInCategory.OST_DuctFitting,

            ///<summary>Ducts</summary>
            OST_DuctCurves = BuiltInCategory.OST_DuctCurves,

            ///<summary>Mass</summary>
            OST_Mass = BuiltInCategory.OST_Mass,

            ///<summary>Detail Items</summary>
            OST_DetailComponents = BuiltInCategory.OST_DetailComponents,

            ///<summary>Floors.Slab Edges</summary>
            OST_EdgeSlab = BuiltInCategory.OST_EdgeSlab,

            ///<summary>Roofs.Gutters</summary>
            OST_Gutter = BuiltInCategory.OST_Gutter,

            ///<summary>Roofs.Fascias</summary>
            OST_Fascia = BuiltInCategory.OST_Fascia,

            ///<summary>Planting</summary>
            OST_Planting = BuiltInCategory.OST_Planting,

            ///<summary>Structural Stiffeners</summary>
            OST_StructuralStiffener = BuiltInCategory.OST_StructuralStiffener,

            ///<summary>Specialty Equipment</summary>
            OST_SpecialityEquipment = BuiltInCategory.OST_SpecialityEquipment,

            ///<summary>Topography</summary>
            OST_Topography = BuiltInCategory.OST_Topography,

            ///<summary>Structural Trusses</summary>
            OST_StructuralTruss = BuiltInCategory.OST_StructuralTruss,

            ///<summary>Structural Columns</summary>
            OST_StructuralColumns = BuiltInCategory.OST_StructuralColumns,

            ///<summary>Structural Beam Systems</summary>
            OST_StructuralFramingSystem = BuiltInCategory.OST_StructuralFramingSystem,

            ///<summary>Structural Framing</summary>
            OST_StructuralFraming = BuiltInCategory.OST_StructuralFraming,

            ///<summary>Structural Foundations</summary>
            OST_StructuralFoundation = BuiltInCategory.OST_StructuralFoundation,

            ///<summary>Site.Property Line Segments</summary>
            OST_SitePropertyLineSegment = BuiltInCategory.OST_SitePropertyLineSegment,

            ///<summary>Site.Property Lines</summary>
            OST_SiteProperty = BuiltInCategory.OST_SiteProperty,

            ///<summary>Site.Pads</summary>
            OST_BuildingPad = BuiltInCategory.OST_BuildingPad,

            ///<summary>Site</summary>
            OST_Site = BuiltInCategory.OST_Site,

            ///<summary>Parking</summary>
            OST_Parking = BuiltInCategory.OST_Parking,

            ///<summary>Plumbing Fixtures</summary>
            OST_PlumbingFixtures = BuiltInCategory.OST_PlumbingFixtures,

            ///<summary>Mechanical Equipment</summary>
            OST_MechanicalEquipment = BuiltInCategory.OST_MechanicalEquipment,

            ///<summary>Lighting Fixtures</summary>
            OST_LightingFixtures = BuiltInCategory.OST_LightingFixtures,

            ///<summary>Furniture Systems</summary>
            OST_FurnitureSystems = BuiltInCategory.OST_FurnitureSystems,

            ///<summary>Electrical Fixtures</summary>
            OST_ElectricalFixtures = BuiltInCategory.OST_ElectricalFixtures,

            ///<summary>Electrical Equipment</summary>
            OST_ElectricalEquipment = BuiltInCategory.OST_ElectricalEquipment,

            ///<summary>Casework</summary>
            OST_Casework = BuiltInCategory.OST_Casework,

            ///<summary>Railings.Terminations</summary>
            OST_RailingTermination = BuiltInCategory.OST_RailingTermination,

            ///<summary>Railings.Supports</summary>
            OST_RailingSupport = BuiltInCategory.OST_RailingSupport,

            ///<summary>Railings.Handrails</summary>
            OST_RailingHandRail = BuiltInCategory.OST_RailingHandRail,

            ///<summary>Railings.Top Rails</summary>
            OST_RailingTopRail = BuiltInCategory.OST_RailingTopRail,

            ///<summary>Stairs.Landings</summary>
            OST_StairsLandings = BuiltInCategory.OST_StairsLandings,

            ///<summary>Stairs.Runs</summary>
            OST_StairsRuns = BuiltInCategory.OST_StairsRuns,

            ///<summary>Curtain Systems</summary>
            OST_CurtaSystem = BuiltInCategory.OST_CurtaSystem,

            ///<summary>Assemblies</summary>
            OST_Assemblies = BuiltInCategory.OST_Assemblies,

            ///<summary>Levels</summary>
            OST_Levels = BuiltInCategory.OST_Levels,

            ///<summary>Grids</summary>
            OST_Grids = BuiltInCategory.OST_Grids,

            ///<summary>Walls.Wall Sweeps</summary>
            OST_Cornices = BuiltInCategory.OST_Cornices,

            ///<summary>Ramps</summary>
            OST_Ramps = BuiltInCategory.OST_Ramps,

            ///<summary>Curtain Wall Mullions</summary>
            OST_CurtainWallMullions = BuiltInCategory.OST_CurtainWallMullions,

            ///<summary>Curtain Panels</summary>
            OST_CurtainWallPanels = BuiltInCategory.OST_CurtainWallPanels,

            ///<summary>Generic Models</summary>
            OST_GenericModel = BuiltInCategory.OST_GenericModel,

            ///<summary>Railings</summary>
            OST_StairsRailing = BuiltInCategory.OST_StairsRailing,

            ///<summary>Stairs</summary>
            OST_Stairs = BuiltInCategory.OST_Stairs,

            ///<summary>Columns</summary>
            OST_Columns = BuiltInCategory.OST_Columns,

            ///<summary>Furniture</summary>
            OST_Furniture = BuiltInCategory.OST_Furniture,

            ///<summary>Ceilings</summary>
            OST_Ceilings = BuiltInCategory.OST_Ceilings,

            ///<summary>Roofs</summary>
            OST_Roofs = BuiltInCategory.OST_Roofs,

            ///<summary>Floors</summary>
            OST_Floors = BuiltInCategory.OST_Floors,

            ///<summary>Doors</summary>
            OST_Doors = BuiltInCategory.OST_Doors,

            ///<summary>Windows</summary>
            OST_Windows = BuiltInCategory.OST_Windows,

            ///<summary>Walls</summary>
            OST_Walls = BuiltInCategory.OST_Walls
        }

        public enum BIC_AllowsBoundParametersAsInstance
        {
            ///<summary>Analytical Links</summary>
            OST_LinksAnalytical = BuiltInCategory.OST_LinksAnalytical,

            ///<summary>Analytical Nodes</summary>
            OST_AnalyticalNodes = BuiltInCategory.OST_AnalyticalNodes,

            ///<summary>Analytical Foundation Slabs</summary>
            OST_FoundationSlabAnalytical = BuiltInCategory.OST_FoundationSlabAnalytical,

            ///<summary>Analytical Wall Foundations</summary>
            OST_WallFoundationAnalytical = BuiltInCategory.OST_WallFoundationAnalytical,

            ///<summary>Analytical Isolated Foundations</summary>
            OST_IsolatedFoundationAnalytical = BuiltInCategory.OST_IsolatedFoundationAnalytical,

            ///<summary>Analytical Walls</summary>
            OST_WallAnalytical = BuiltInCategory.OST_WallAnalytical,

            ///<summary>Analytical Floors</summary>
            OST_FloorAnalytical = BuiltInCategory.OST_FloorAnalytical,

            ///<summary>Analytical Columns</summary>
            OST_ColumnAnalytical = BuiltInCategory.OST_ColumnAnalytical,

            ///<summary>Analytical Braces</summary>
            OST_BraceAnalytical = BuiltInCategory.OST_BraceAnalytical,

            ///<summary>Analytical Beams</summary>
            OST_BeamAnalytical = BuiltInCategory.OST_BeamAnalytical,

            ///<summary>Structural Connections</summary>
            OST_StructConnections = BuiltInCategory.OST_StructConnections,

            ///<summary>Structural Fabric Areas</summary>
            OST_FabricAreas = BuiltInCategory.OST_FabricAreas,

            ///<summary>Structural Fabric Reinforcement</summary>
            OST_FabricReinforcement = BuiltInCategory.OST_FabricReinforcement,

            ///<summary>Rebar Shape</summary>
            OST_RebarShape = BuiltInCategory.OST_RebarShape,

            ///<summary>Structural Path Reinforcement</summary>
            OST_PathRein = BuiltInCategory.OST_PathRein,

            ///<summary>Structural Area Reinforcement</summary>
            OST_AreaRein = BuiltInCategory.OST_AreaRein,

            ///<summary>Structural Rebar</summary>
            OST_Rebar = BuiltInCategory.OST_Rebar,

            ///<summary>Analytical Spaces</summary>
            OST_AnalyticSpaces = BuiltInCategory.OST_AnalyticSpaces,

            ///<summary>Pipe Placeholders</summary>
            OST_PlaceHolderPipes = BuiltInCategory.OST_PlaceHolderPipes,

            ///<summary>Duct Placeholders</summary>
            OST_PlaceHolderDucts = BuiltInCategory.OST_PlaceHolderDucts,

            ///<summary>Cable Tray Runs</summary>
            OST_CableTrayRun = BuiltInCategory.OST_CableTrayRun,

            ///<summary>Conduit Runs</summary>
            OST_ConduitRun = BuiltInCategory.OST_ConduitRun,

            ///<summary>Conduits</summary>
            OST_Conduit = BuiltInCategory.OST_Conduit,

            ///<summary>Cable Trays</summary>
            OST_CableTray = BuiltInCategory.OST_CableTray,

            ///<summary>Conduit Fittings</summary>
            OST_ConduitFitting = BuiltInCategory.OST_ConduitFitting,

            ///<summary>Cable Tray Fittings</summary>
            OST_CableTrayFitting = BuiltInCategory.OST_CableTrayFitting,

            ///<summary>Duct Linings</summary>
            OST_DuctLinings = BuiltInCategory.OST_DuctLinings,

            ///<summary>Duct Insulations</summary>
            OST_DuctInsulations = BuiltInCategory.OST_DuctInsulations,

            ///<summary>Pipe Insulations</summary>
            OST_PipeInsulations = BuiltInCategory.OST_PipeInsulations,

            ///<summary>HVAC Zones</summary>
            OST_HVAC_Zones = BuiltInCategory.OST_HVAC_Zones,

            ///<summary>Switch System</summary>
            OST_SwitchSystem = BuiltInCategory.OST_SwitchSystem,

            ///<summary>Sprinklers</summary>
            OST_Sprinklers = BuiltInCategory.OST_Sprinklers,

            ///<summary>Analytical Surfaces</summary>
            OST_GbXMLFaces = BuiltInCategory.OST_GbXMLFaces,

            ///<summary>Lighting Devices</summary>
            OST_LightingDevices = BuiltInCategory.OST_LightingDevices,

            ///<summary>Fire Alarm Devices</summary>
            OST_FireAlarmDevices = BuiltInCategory.OST_FireAlarmDevices,

            ///<summary>Data Devices</summary>
            OST_DataDevices = BuiltInCategory.OST_DataDevices,

            ///<summary>Communication Devices</summary>
            OST_CommunicationDevices = BuiltInCategory.OST_CommunicationDevices,

            ///<summary>Security Devices</summary>
            OST_SecurityDevices = BuiltInCategory.OST_SecurityDevices,

            ///<summary>Nurse Call Devices</summary>
            OST_NurseCallDevices = BuiltInCategory.OST_NurseCallDevices,

            ///<summary>Telephone Devices</summary>
            OST_TelephoneDevices = BuiltInCategory.OST_TelephoneDevices,

            ///<summary>Pipe Accessories</summary>
            OST_PipeAccessory = BuiltInCategory.OST_PipeAccessory,

            ///<summary>Flex Pipes</summary>
            OST_FlexPipeCurves = BuiltInCategory.OST_FlexPipeCurves,

            ///<summary>Pipe Fittings</summary>
            OST_PipeFitting = BuiltInCategory.OST_PipeFitting,

            ///<summary>Pipes</summary>
            OST_PipeCurves = BuiltInCategory.OST_PipeCurves,

            ///<summary>Piping Systems</summary>
            OST_PipingSystem = BuiltInCategory.OST_PipingSystem,

            ///<summary>Wires</summary>
            OST_Wire = BuiltInCategory.OST_Wire,

            ///<summary>Electrical Circuits</summary>
            OST_ElectricalCircuit = BuiltInCategory.OST_ElectricalCircuit,

            ///<summary>Flex Ducts</summary>
            OST_FlexDuctCurves = BuiltInCategory.OST_FlexDuctCurves,

            ///<summary>Duct Accessories</summary>
            OST_DuctAccessory = BuiltInCategory.OST_DuctAccessory,

            ///<summary>Duct Systems</summary>
            OST_DuctSystem = BuiltInCategory.OST_DuctSystem,

            ///<summary>Air Terminals</summary>
            OST_DuctTerminal = BuiltInCategory.OST_DuctTerminal,

            ///<summary>Duct Fittings</summary>
            OST_DuctFitting = BuiltInCategory.OST_DuctFitting,

            ///<summary>Ducts</summary>
            OST_DuctCurves = BuiltInCategory.OST_DuctCurves,

            ///<summary>Structural Internal Loads.Internal Area Loads</summary>
            OST_InternalAreaLoads = BuiltInCategory.OST_InternalAreaLoads,

            ///<summary>Structural Internal Loads.Internal Line Loads</summary>
            OST_InternalLineLoads = BuiltInCategory.OST_InternalLineLoads,

            ///<summary>Structural Internal Loads.Internal Point Loads</summary>
            OST_InternalPointLoads = BuiltInCategory.OST_InternalPointLoads,

            ///<summary>Structural Loads.Area Loads</summary>
            OST_AreaLoads = BuiltInCategory.OST_AreaLoads,

            ///<summary>Structural Loads.Line Loads</summary>
            OST_LineLoads = BuiltInCategory.OST_LineLoads,

            ///<summary>Structural Loads.Point Loads</summary>
            OST_PointLoads = BuiltInCategory.OST_PointLoads,

            ///<summary>Spaces</summary>
            OST_MEPSpaces = BuiltInCategory.OST_MEPSpaces,

            ///<summary>Mass.Mass Opening</summary>
            OST_MassOpening = BuiltInCategory.OST_MassOpening,

            ///<summary>Mass.Mass Skylight</summary>
            OST_MassSkylights = BuiltInCategory.OST_MassSkylights,

            ///<summary>Mass.Mass Glazing</summary>
            //OST_MassWindow = BuiltInCategory.OST_MassWindow, // jeremy - not available in Revit 2015
            ///<summary>Mass.Mass Roof</summary>
            OST_MassRoof = BuiltInCategory.OST_MassRoof,

            ///<summary>Mass.Mass Exterior Wall</summary>
            OST_MassExteriorWall = BuiltInCategory.OST_MassExteriorWall,

            ///<summary>Mass.Mass Interior Wall</summary>
            OST_MassInteriorWall = BuiltInCategory.OST_MassInteriorWall,

            ///<summary>Mass.Mass Zone</summary>
            OST_MassZone = BuiltInCategory.OST_MassZone,

            ///<summary>Mass.Mass Floor</summary>
            OST_MassFloor = BuiltInCategory.OST_MassFloor,

            ///<summary>Mass</summary>
            OST_Mass = BuiltInCategory.OST_Mass,

            ///<summary>Areas</summary>
            OST_Areas = BuiltInCategory.OST_Areas,

            ///<summary>Project Information</summary>
            OST_ProjectInformation = BuiltInCategory.OST_ProjectInformation,

            ///<summary>Sheets</summary>
            OST_Sheets = BuiltInCategory.OST_Sheets,

            ///<summary>Detail Items</summary>
            OST_DetailComponents = BuiltInCategory.OST_DetailComponents,

            ///<summary>Floors.Slab Edges</summary>
            OST_EdgeSlab = BuiltInCategory.OST_EdgeSlab,

            ///<summary>Roofs.Gutters</summary>
            OST_Gutter = BuiltInCategory.OST_Gutter,

            ///<summary>Roofs.Fascias</summary>
            OST_Fascia = BuiltInCategory.OST_Fascia,

            ///<summary>Planting</summary>
            OST_Planting = BuiltInCategory.OST_Planting,

            ///<summary>Structural Stiffeners</summary>
            OST_StructuralStiffener = BuiltInCategory.OST_StructuralStiffener,

            ///<summary>Specialty Equipment</summary>
            OST_SpecialityEquipment = BuiltInCategory.OST_SpecialityEquipment,

            ///<summary>Topography</summary>
            OST_Topography = BuiltInCategory.OST_Topography,

            ///<summary>Structural Trusses</summary>
            OST_StructuralTruss = BuiltInCategory.OST_StructuralTruss,

            ///<summary>Structural Columns</summary>
            OST_StructuralColumns = BuiltInCategory.OST_StructuralColumns,

            ///<summary>Structural Beam Systems</summary>
            OST_StructuralFramingSystem = BuiltInCategory.OST_StructuralFramingSystem,

            ///<summary>Structural Framing</summary>
            OST_StructuralFraming = BuiltInCategory.OST_StructuralFraming,

            ///<summary>Structural Foundations</summary>
            OST_StructuralFoundation = BuiltInCategory.OST_StructuralFoundation,

            ///<summary>Site.Property Line Segments</summary>
            OST_SitePropertyLineSegment = BuiltInCategory.OST_SitePropertyLineSegment,

            ///<summary>Site.Property Lines</summary>
            OST_SiteProperty = BuiltInCategory.OST_SiteProperty,

            ///<summary>Site.Pads</summary>
            OST_BuildingPad = BuiltInCategory.OST_BuildingPad,

            ///<summary>Site</summary>
            OST_Site = BuiltInCategory.OST_Site,

            ///<summary>Roads</summary>
            OST_Roads = BuiltInCategory.OST_Roads,

            ///<summary>Parking</summary>
            OST_Parking = BuiltInCategory.OST_Parking,

            ///<summary>Plumbing Fixtures</summary>
            OST_PlumbingFixtures = BuiltInCategory.OST_PlumbingFixtures,

            ///<summary>Mechanical Equipment</summary>
            OST_MechanicalEquipment = BuiltInCategory.OST_MechanicalEquipment,

            ///<summary>Lighting Fixtures</summary>
            OST_LightingFixtures = BuiltInCategory.OST_LightingFixtures,

            ///<summary>Furniture Systems</summary>
            OST_FurnitureSystems = BuiltInCategory.OST_FurnitureSystems,

            ///<summary>Electrical Fixtures</summary>
            OST_ElectricalFixtures = BuiltInCategory.OST_ElectricalFixtures,

            ///<summary>Electrical Equipment</summary>
            OST_ElectricalEquipment = BuiltInCategory.OST_ElectricalEquipment,

            ///<summary>Casework</summary>
            OST_Casework = BuiltInCategory.OST_Casework,

            ///<summary>Shaft Openings</summary>
            OST_ShaftOpening = BuiltInCategory.OST_ShaftOpening,

            ///<summary>Railings.Terminations</summary>
            OST_RailingTermination = BuiltInCategory.OST_RailingTermination,

            ///<summary>Railings.Supports</summary>
            OST_RailingSupport = BuiltInCategory.OST_RailingSupport,

            ///<summary>Railings.Handrails</summary>
            OST_RailingHandRail = BuiltInCategory.OST_RailingHandRail,

            ///<summary>Railings.Top Rails</summary>
            OST_RailingTopRail = BuiltInCategory.OST_RailingTopRail,

            ///<summary>Stairs.Landings</summary>
            OST_StairsLandings = BuiltInCategory.OST_StairsLandings,

            ///<summary>Stairs.Runs</summary>
            OST_StairsRuns = BuiltInCategory.OST_StairsRuns,

            ///<summary>Materials</summary>
            OST_Materials = BuiltInCategory.OST_Materials,

            ///<summary>Curtain Systems</summary>
            OST_CurtaSystem = BuiltInCategory.OST_CurtaSystem,

            ///<summary>Views</summary>
            OST_Views = BuiltInCategory.OST_Views,

            ///<summary>Parts</summary>
            OST_Parts = BuiltInCategory.OST_Parts,

            ///<summary>Assemblies</summary>
            OST_Assemblies = BuiltInCategory.OST_Assemblies,

            ///<summary>Levels</summary>
            OST_Levels = BuiltInCategory.OST_Levels,

            ///<summary>Grids</summary>
            OST_Grids = BuiltInCategory.OST_Grids,

            ///<summary>Walls.Wall Sweeps</summary>
            OST_Cornices = BuiltInCategory.OST_Cornices,

            ///<summary>Ramps</summary>
            OST_Ramps = BuiltInCategory.OST_Ramps,

            ///<summary>Curtain Wall Mullions</summary>
            OST_CurtainWallMullions = BuiltInCategory.OST_CurtainWallMullions,

            ///<summary>Curtain Panels</summary>
            OST_CurtainWallPanels = BuiltInCategory.OST_CurtainWallPanels,

            ///<summary>Rooms</summary>
            OST_Rooms = BuiltInCategory.OST_Rooms,

            ///<summary>Generic Models</summary>
            OST_GenericModel = BuiltInCategory.OST_GenericModel,

            ///<summary>Railings</summary>
            OST_StairsRailing = BuiltInCategory.OST_StairsRailing,

            ///<summary>Stairs</summary>
            OST_Stairs = BuiltInCategory.OST_Stairs,

            ///<summary>Columns</summary>
            OST_Columns = BuiltInCategory.OST_Columns,

            ///<summary>Furniture</summary>
            OST_Furniture = BuiltInCategory.OST_Furniture,

            ///<summary>Ceilings</summary>
            OST_Ceilings = BuiltInCategory.OST_Ceilings,

            ///<summary>Roofs</summary>
            OST_Roofs = BuiltInCategory.OST_Roofs,

            ///<summary>Floors</summary>
            OST_Floors = BuiltInCategory.OST_Floors,

            ///<summary>Doors</summary>
            OST_Doors = BuiltInCategory.OST_Doors,

            ///<summary>Windows</summary>
            OST_Windows = BuiltInCategory.OST_Windows,

            ///<summary>Walls</summary>
            OST_Walls = BuiltInCategory.OST_Walls
        }

        #endregion // Todd's enums
    }
}