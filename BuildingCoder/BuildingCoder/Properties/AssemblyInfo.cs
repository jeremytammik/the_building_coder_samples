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
[assembly: AssemblyCopyright( "Copyright © 2008-2014 by Jeremy Tammik, Autodesk, Inc." )]
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
// 
[assembly: AssemblyVersion( "2015.0.114.0" )]
[assembly: AssemblyFileVersion( "2015.0.114.0" )]
