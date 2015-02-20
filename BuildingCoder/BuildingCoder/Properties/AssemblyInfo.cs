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
[assembly: AssemblyCopyright( "Copyright © 2008-2015 by Jeremy Tammik, Autodesk, Inc." )]
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
// 2015-02-20 2015.0.118.0 updated CmdListMarks to use TransactionMode.Manual instead of Automatic
// 
[assembly: AssemblyVersion( "2015.0.118.0" )]
[assembly: AssemblyFileVersion( "2015.0.118.0" )]
