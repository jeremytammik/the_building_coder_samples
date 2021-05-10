#region Header
//
// CmdBim360Links.cs - retrieve and list BIM360 linked models
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdBim360Links : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Obtain all external resource references 
      // (saying BIM360 Cloud references and local 
      // file references this time)

      ISet<ElementId> xrefs = ExternalResourceUtils
        .GetAllExternalResourceReferences( doc );

      string caption = "BIM360 Links";

      try
      {
        int n = 0;
        var msg = string.Empty;

        foreach( ElementId eid in xrefs )
        {
          var elem = doc.GetElement( eid );
          if( elem == null ) continue;

          // Get RVT document links only this time

          var link = elem as RevitLinkType;
          if( link == null ) continue;

          var map = link.GetExternalResourceReferences();
          var keys = map.Keys;

          foreach( var key in keys )
          {
            var reference = map[key];

            // Contains Forge BIM360 ProjectId 
            // (i.e., LinkedModelModelId) and 
            // ModelId (i.e., LinkedModelModelId) 
            // if it's from BIM360 Docs. 
            // They can be used in calls to
            // ModelPathUtils.ConvertCloudGUIDsToCloudPath.

            var dictinfo = reference.GetReferenceInformation();

            // Link Name shown on the Manage Links dialog

            var displayName = reference.GetResourceShortDisplayName();
            var path = reference.InSessionPath;
          }

          try
          {
            // Load model temporarily to get the model 
            // path of the cloud link

            var result = link.Load();

            // Link ModelPath for Revit internal use

            var mdPath = result.GetModelName();

            link.Unload( null );

            // Convert model path to user visible path, 
            // i.e., saved Path shown on the Manage Links 
            // dialog

            var path = ModelPathUtils
              .ConvertModelPathToUserVisiblePath( mdPath );

            // Reference Type shown on the Manage Links dialog

            var refType = link.AttachmentType;

            msg += string.Format( "{0} {1}\r\n", 
              link.AttachmentType, path );

            ++n;
          }
          catch( Exception ex ) // never catch all exceptions!
          {
            TaskDialog.Show( caption, ex.Message );
          }
        }

        caption = string.Format( "{0} BIM360 Link{1}", 
          n, Util.PluralSuffix( n ) );

        TaskDialog.Show( caption, msg );
      }
      catch( Exception ex )
      {
        TaskDialog.Show( caption, ex.Message );
      }
      return Result.Succeeded;
    }

    #region Determine cloud model local cache file path
    string GetCloudModelLocalCacheFilepath( 
      Document doc, 
      string version_number )
    {
      string title = doc.Title;
      string ext = Path.GetExtension( doc.PathName );
      string localRevitFile = null;

      if( doc.IsModelInCloud )
      {
        ModelPath modelPath = doc.GetCloudModelPath();
        string guid = modelPath.GetModelGUID().ToString();

        string folder = "C:\\Users\\" + Environment.UserName 
          + "\\AppData\\Local\\Autodesk\\Revit\\Autodesk Revit " 
          + version_number + "\\CollaborationCache";

        string revitFile = guid + ext;

        string[] files = Directory
          .GetFiles( folder, revitFile, SearchOption.AllDirectories )
          .Where( c => !c.Contains( "CentralCache" ) )
          .ToArray();

        if( 0 < files.Length )
        {
          localRevitFile = files[ 0 ];
        }
        else
        {
          Debug.Print( "Unable to find local rvt for: " + doc.PathName );
        }
      }
      else
      {
        localRevitFile = doc.PathName;
      }
      return localRevitFile;
    }
    #endregion // Determine cloud model local cache file path 


  }
}
