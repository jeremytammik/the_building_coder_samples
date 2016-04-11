using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "BuildingCoder" )]
[assembly: AssemblyDescription( "The Building Coder samples, http://thebuildingcoder.typepad.com" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Autodesk, Inc." )]
[assembly: AssemblyProduct( "BuildingCoder" )]
[assembly: AssemblyCopyright( "Copyright © 2008-2016 by Jeremy Tammik, Autodesk, Inc." )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid( "15d5844d-8236-48dc-ad63-7ce8f99dec10" )]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers
// by using the '*' as shown below:
// 2014-04-11 2014.0.109.0 renamed CmdSlopedFloor.cs to CmdCreateSlopedSlab.cs, implemented CmdCategorySupportsTypeParameter
// 2014-04-14 2015.0.109.0 migrated to Revit 2015
// 2014-05-14 2015.0.110.0 CmdViewsShowingElements
// 2014-07-21 2015.0.110.1 AddFaceBasedFamilyToLinks
// 2014-08-19 2015.0.110.2 removed obsolete API usage: reduced warning count from 71 to 67
// 2014-08-20 2015.0.110.3 added code in CmdUnrotateNorth to determine angle to north from project base point
// 2014-08-20 2015.0.111.0 implemented CmdDocumentVersion
// 2014-08-27 2015.0.111.1 cleaned up PickFaceSetWorkPlaneAndPickPoint, implemented PickPointsForArea, Plane ProjectOnto and ProjectInto extension methods
// 2014-09-01 2015.0.111.2 documented CmdPickPoint3d and added Autodesk.Revit.Exceptions namespace prefix to all uses of OperationCanceledException
// 2014-09-01 2015.0.111.3 added notes on NewFloor refusing floor types that are foundation slab types
// 2014-09-29 2015.0.112.0 implemented CmdNewExtrusionRoof
// 2014-10-01 2015.0.113.0 implemented CmdFaceWall
// 2014-10-01 2015.0.113.1 skip horizontal faces
// 2014-10-06 2015.0.113.2 implemented IsElementHiddenInView
// 2014-10-11 2015.0.114.0 implemented CmdNewTextNote
// 2014-10-11 2015.0.114.1 CmdNewTextNote text width enhancements suggested by Scott Wilson
// 2014-10-15 2015.0.114.2 CmdNewTextNote reimplementation based on Graphics.MeasureString instead of TextRenderer.MeasureText suggested by Scott Wilson
// 2014-10-16 2015.0.114.3 added pipe wall thickness determination to CmdRollingOffset
// 2014-10-11 2015.0.115.0 implemented CmdNewCrossFitting
// 2014-10-11 2015.0.115.1 fixed some of the warnings about deprecated use of Selection.Elements collection
// 2014-11-10 2015.0.115.2 converted CmdNewSweptBlend to use manual transaction mode and implemented CreateNewSweptBlendArc
// 2014-11-11 2015.0.116.0 implemented CmdDimensionInstanceOrigin and JtPairPicker
// 2014-11-12 2015.0.116.1 replace explicit selection code in CmdRollingOffset by new JtPairPicker class
// 2014-11-12 2015.0.116.3 implement generic JtElementsOfClassSelectionFilter template class
// 2014-11-12 2015.0.116.3 replace explicit Wall, CurveElement and Pipe selection filters by JtElementsOfClassSelectionFilter
// 2014-11-13 2015.0.116.4 eliminated obsolete API usage warning in CmdCollectorPerformance.cs: replace get_Parameter(string) by built-in enumeration value
// 2014-11-13 2015.0.116.4 eliminated obsolete API usage warning in CmdCollectorPerformance.cs: use Pipe.Create instead of doc.Create.NewPipe
// 2014-11-13 2015.0.116.4 eliminated obsolete API usage warning in CmdCoordsOfViewOnSheet.cs: use GetAllPlacedViews() instead of Autodesk.Revit.DB.ViewSheet.Views
// 2014-11-13 2015.0.116.4 eliminated various obsolete API usage warnings saying Autodesk.Revit.UI.Selection.Selection.Elements is obsolete: Use GetElementIds instead
// 2014-11-14 2015.0.116.5 implemented GetInstancesIntersectingElement
// 2014-11-14 2015.0.116.5 radical rewrite of SelectSingleElement, GetSingleSelectedElement, GetSelectedElementsOrAll to clean up and eliminate obsolete API usage
// 2014-11-14 2015.0.116.5 radical rewrite of HasRequestedType, which probably never previously worked as intended
// 2014-11-19 2015.0.116.6 implemented GetSortedLevels
// 2015-01-27 2015.0.116.7 added second implementation to CmdWallProfile
// 2015-01-27 2015.0.116.8 updated copyright year to 2015
// 2015-01-27 2015.0.116.9 eliminated obsolete API usage, 15 warnings left
// 2015-01-27 2015.0.117.0 implemented CmdListPipeSizes
// 2015-02-09 2015.0.117.1 implemented mechanism to abort PromptForFamilyInstancePlacement after placing first instance
// 2015-02-10 2015.0.117.2 eliminated all deprecated API usage to compile with zero warnings now
// 2015-02-14 2015.0.117.3 implemented PlaceFamilyInstanceOnFace
// 2015-02-19 2015.0.117.4 updated CmdInstallLocation from Revit 2010 to 2015 and replaced TransactionMode.Automatic by ReadOnly
// 2015-02-20 2015.0.117.5 updated CmdListMarks to use TransactionMode.Manual instead of Automatic
// 2015-02-20 2015.0.118.0 implemented CmdItemExecuted and added reference to PresentationFramework assembly
// 2015-03-02 2015.0.119.0 implemented CmdPostRequestInstancePlacement
// 2015-03-03 2015.0.119.1 implemented HideLightingFixtureHosts
// 2015-03-11 2015.0.119.2 implemented sketch for traversing family instances per level and category
// 2015-03-11 2015.0.119.2 replaced automatic transaction mode by manual in CmdWallProfileArea
// 2015-03-11 2015.0.119.2 replaced automatic transaction mode by manual in CmdWallLayers
// 2015-03-11 2015.0.119.2 replaced automatic transaction mode by read-only in CmdWallDimensions
// 2015-03-11 2015.0.120.0 implemented CmdSelectionChanged
// 2015-03-25 2015.0.120.1 added Revit 2015 API class diagram
// 2015-03-25 2015.0.120.2 renamed class diagram adding Revit.DB namespace
// 2015-03-25 2015.0.120.3 before Alexander Ignatovich ai CmdWallProfile enhancement
// 2015-03-25 2015.0.120.4 integrated Alexander Ignatovich ai CmdWallProfile - first attempt.cs
// 2015-03-25 2015.0.120.5 integrated Alexander Ignatovich ai CmdWallProfile - second attempt.cs
// 2015-03-25 2015.0.120.6 integrated Alexander Ignatovich ai CmdWallProfile - final.cs
// 2015-03-25 2015.0.120.7 eliminated and replaced non-static Creator.CreateModelLine taking XYZ start and end point by static overload taking Document as well
// 2015-04-13 2015.0.120.9 implemented Plane Compare method
// 2015-05-05 2015.0.120.10 cleaned up, tested and fixed a bug in CmdCopyWallType
// 2015-05-15 2015.0.120.11 started integrating version compatibility extension methods by Magson Leone
// 2015-05-18 2015.0.120.12 second attempt at integrating version compatibility extension methods by Magson Leone
// 2015-05-19 2015.0.120.13 third version of version compatibility extension methods by Magson Leone
// 2015-05-20 2016.0.120.0 first successful compilation for the Revit 2016 API
// 2015-05-26 2016.0.120.1 updated BcSamples.txt for Revit 2016 and implemented Util.Intersection method
// 2015-05-27 2016.0.120.2 enhanced Util.Intersection to gracefully handle parallel or coincident lines
// 2015-07-08 2016.0.120.3 added solid sphere and cube creation utility methods
// 2015-07-13 2016.0.120.4 added super simple floor creation code
// 2015-08-13 2016.0.120.5 added SetSiteLocationToCity2
// 2015-08-18 2016.0.120.6 removed obsolete API usage in CmdNewLineLoad.cs
// 2015-08-18 2016.0.120.7 removed obsolete API usage in CmdCollectorPerformance, CmdCreateSlopedSlab, CmdNewTextNote, CmdRoomNeighbours, CmdRoomWallAdjacency, and CmdSpaceAdjacency
// 2015-08-18 2016.0.120.8 replaced obsolete PlanarFace.Normal property by PlanarFace.FaceNormal
// 2015-08-18 2016.0.120.9 eliminated obsolete API calls to ElementTransformUtils.MirrorElements by adding bool mirrorCopies argument
// 2015-09-04 2016.0.120.10 added original EditFilledRegion to show code improvements
// 2015-09-04 2016.0.120.11 in EditFilledRegion: foreach iter, using tx, single tx, XYZ static member
// 2015-09-04 2016.0.120.12 in EditFilledRegion: use MoveElements instead of MoveElement
// 2015-09-14 2016.0.120.14 implemented GetAllModelElements
// 2015-09-14 2016.0.120.15 implemented IterateOverCollector
// 2015-09-28 2016.0.120.16 added rof creation sample code from Revit online help sample
// 2015-10-20 2016.0.121.0 implemented CmdSheetToModel using code provided by Paolo Serra
// 2015-10-22 2016.0.121.1 added miros sample code
// 2015-10-30 2016.0.121.2 implemented Util.NewExternalDefinitionCreationOptions method
// 2015-10-31 2016.0.121.3 implemented SpellingErrorCorrector
// 2015-11-05 2016.0.122.0 implemented CmdSetWallType
// 2015-11-05 2016.0.122.1 implemented GetAllDetailComponentCustomParamValues
// 2015-11-16 2016.0.123.0 implemented CmdFlatten
// 2015-11-29 2016.0.123.1 updated CmdDetailCurves to create detail curve along pre-selected wall centre location curve
// 2015-11-30 2016.0.123.2 YbExporteContext
// 2015-12-16 2016.0.124.0 implemented CmdWallOpenings
// 2015-12-17 2016.0.125.0 integrated CmdProjectParameterGuids by CoderBoy
// 2015-12-17 2016.0.125.1 cleaned up CmdWallOpenings
// 2015-12-18 2016.0.125.2 cleaned up CmdProjectParameterGuids
// 2015-12-22 2016.0.126.0 integrated CmdWallOpeningProfiles by Scott Wilson
// 2015-12-22 2016.0.126.1 cleaned up for publication
// 2016-01-08 2016.0.126.2 incremented copyright year
// 2016-02-24 2016.0.126.3 implemented CmdMepElementShape.GetElementShape4 and break selection loop if element is preselected
// 2016-02-24 2016.0.126.4 implemented CmdMepElementShape.GetProfileTypes returing all duct connector shapes
// 2016-02-24 2016.0.126.5 updated readme and added keywords to all modules to improve global internet search results
// 2016-03-16 2016.0.126.6 implemented SetFamilyParameterValue sample code
// 2016-03-22 2016.0.126.7 implemented GetBeamsIntersectingTwoColumns
// 2016-04-05 2016.0.126.8 implemented DistinguishRooms
// 2016-04-06 2016.0.126.8 updated DistinguishRooms
// 2016-04-08 2016.0.126.9 renamed GetCorners to GetBottomCorners
// 2016-04-11 2016.0.126.10 implemeneted JtNamedGuiStorage skeleton
// 2016-04-11 2016.0.126.10 fleshed out the JtNamedGuiStorage class
// 2016-04-11 2016.0.127.0 implemented CmdNamedGuidStorage skeleton to test JtNamedGuiStorage
// 
[assembly: AssemblyVersion( "2016.0.127.0" )]
[assembly: AssemblyFileVersion( "2016.0.127.0" )]
