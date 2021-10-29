#region Header

//
// CmdRemoveDwfLinks.cs - Remove DWF links
//
// Copyright (C) 2012-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Windows.Forms;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdRemoveDwfLinks : IExternalCommand
    {
        #region MiroReloadLinks test code

        private void MiroReloadLinks(IList<RevitLinkType> fecLinkTypes)
        {
            // Loop all RVT Links

            foreach (var typeLink in fecLinkTypes)
            {
                // ...

                // Skip1 - not IsFromRevitServer

                if (!typeLink.IsFromRevitServer())
                    //…
                    continue;

                // Skip2 - not ExternalFileReference
                // 99% it would already skip above as 
                // RevitServer MUST be ExternalFileReference, 
                // but leave just in case...

                var er = typeLink.GetExternalFileReference();

                if (er == null)
                    // ...

                    continue;

                // If here, we can cache ModelPath related 
                // info and show to user regardless if we skip 
                // on next checks or not....

                var mp = er.GetPath();

                var userVisiblePath = ModelPathUtils
                    .ConvertModelPathToUserVisiblePath(mp);

                // Skip3 - if ModelPath is NOT Server Path 
                // 99% redundant as we already checked raw 
                // RevitLinkType for this, but keep 
                // just in case...

                if (!mp.ServerPath)
                    // ...

                    continue;

                // Skip4 - if NOT "NOT Found" problematic one 
                // there is nothing to fix

                if (er.GetLinkedFileStatus()
                    != LinkedFileStatus.NotFound)
                    // ...

                    continue;

                // Skip5 - if Nested Link (can’t (re)load these!)

                if (typeLink.IsNestedLink)
                    // ...

                    continue;

                // If here, we MUST offer user to "Reload from..."

                // ...

                //RevitLinkLoadResult res = null; // 2017
                LinkLoadResult res = null; // 2018

                try
                {
                    // This fails for problematic Server files 
                    // since it also fails on "Reload" button in 
                    // UI (due to the GUID issue in the answer)

                    //res = typeLink.Reload();

                    // This fails same as above :-(!

                    //res = typeLink.Load();

                    // This WORKS!
                    // Basically, this is the equivalent of UI 
                    // "Reload from..." + browsing to the *same* 
                    // Saved path showing in the manage Links 
                    // dialogue.
                    // ToDo: Check if we need to do anything 
                    // special with WorksetConfiguration? 
                    // In tests, it works fine with the 
                    // default c-tor.

                    var mpForReload = ModelPathUtils
                        .ConvertUserVisiblePathToModelPath(
                            userVisiblePath);

                    res = typeLink.LoadFrom(mpForReload,
                        new WorksetConfiguration());

                    Util.InfoMsg($"Result = {res.LoadResult}");
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            } // foreach typeLink
        }

        #endregion // MiroReloadLinks test code

        #region Andrea Tassera Reload Links Test Code

#if NEED_THIS_SAMPLE_CODE
    // https://forums.autodesk.com/t5/revit-api-forum/reload-revit-links-from/m-p/7722248
    public Result Execute1(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      // Get application and document objects

      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // NO TRANSACTION NEEDS TO BE OPENED

      try
      {
        using( Transaction tx = new Transaction( doc ) )
        {
          // Collect files linked in current project

          FilteredElementCollector collector = new FilteredElementCollector( doc );
          ICollection<Element> linkInstances = collector.OfClass( typeof( RevitLinkType ) ).ToElements();

          // Check which elements are loaded > to be used as filter

          List<bool> loaded = new List<bool>();
          foreach( RevitLinkType i in linkInstances )
          {
            loaded.Add( RevitLinkType.IsLoaded( doc, i.Id ) );
          }

          // Convert ICollection into a list of RevitLinkTypes
          int i1 = 0;

          List<RevitLinkType> revLinkType = new List<RevitLinkType>();
          foreach( RevitLinkType rli in linkInstances )
          {
            if( !loaded[i1++] )
            {
              revLinkType.Add( rli );
            }
          }

          // Put names of linked files into a list of strings
          int i2 = 0;

          List<string> linkNames = new List<string>();
          foreach( Element eli in linkInstances )
          {
            if( !loaded[i2++] )
            {
              linkNames.Add( eli.Name.Split( ' ' )[0] );
            }
          }

          // Prompt user with files selection dialog

          //Start:
          OpenFileDialog openFileDialog1 = new OpenFileDialog();
          openFileDialog1.InitialDirectory = ( @"P:\" );
          openFileDialog1.Filter = "RVT|*.rvt";
          openFileDialog1.Multiselect = true;
          openFileDialog1.RestoreDirectory = true;

          // If you select the files and hit OK (in the file browser)

          if( openFileDialog1.ShowDialog() == DialogResult.OK )
          {
            // Show which files (path + version) has been selected before linking them

            StringBuilder userSelectionWVersion = new StringBuilder();
            foreach( string fp in openFileDialog1.FileNames )
            {
              userSelectionWVersion.AppendLine(
                fp.ToString()
                + " which was created with " +
                BasicFileInfo.Extract( fp ).SavedInVersion.ToString().ToUpper() );
            }

            // Recap the user with his selection + Revit version of the file

            DialogResult linkCorrect = MessageBox.Show(
                          userSelectionWVersion.ToString(),
                          "You selected the files:",
                          MessageBoxButtons.OKCancel );

            // Put paths of files selected by user into a list

            if( linkCorrect == DialogResult.OK )
            {

              List<string> userSelectionNames = new List<string>();
              foreach( string fp in openFileDialog1.FileNames )
              {
                userSelectionNames.Add( fp.ToString() );
              }

              // Check which of the files that the user selected have the same name of the files linked in the project

              IEnumerable<string> elementsToReload = userSelectionNames.Where( a => linkNames.Exists( b => a.Contains( b ) ) );

              // Show which files need to be reloaded

              StringBuilder intersection = new StringBuilder();
              foreach( string fp in elementsToReload )
              {
                intersection.AppendLine( fp.ToString() );
              }
              DialogResult promptToLoad = MessageBox.Show( intersection.ToString(), "The following files need to be roloaded" );

              // Initialize + populate list of ModelPaths > path from where to reload

              List<ModelPath> modPaths = new List<ModelPath>();

              foreach( string fp in elementsToReload )
              {
                FileInfo filePath = new FileInfo( fp );
                ModelPath linkpath = ModelPathUtils.ConvertUserVisiblePathToModelPath( filePath.ToString() );
                modPaths.Add( linkpath );
              }

              // Zip together file (as RevitLinkType) and the corresponding path to be reloaded from > Reload

              foreach( var ab in revLinkType.Zip( modPaths, Tuple.Create ) )
              {
                ab.Item1.LoadFrom( ab.Item2, new WorksetConfiguration() );
              }
            }
            return Result.Succeeded;
          }
        }
      }
      catch( Exception ex )
      {
        // If something went wrong return Result.Failed

        DialogResult genericException = MessageBox.Show( ex.Message, "Oops there was problem!" );

        return Result.Failed;
      }
      return Result.Succeeded;
    }
#endif // NEED_THIS_SAMPLE_CODE

        #endregion // Andrea Tassera Reload Links Test Code

        /// <summary>
        ///     Unpin all of the pinned elements in the list.
        /// </summary>
        private int Unpin(List<ElementId> ids, Document doc)
        {
            var count = 0;

            foreach (var id in ids)
            {
                var e = doc.GetElement(id);
                if (e.Pinned)
                {
                    e.Pinned = false;
                    ++count;
                }
            }

            return count;
        }

        /// <summary>
        ///     Return true if the given element category
        ///     name contains the substring ".dwf".
        /// </summary>
        private bool ElementCategoryContainsDwf(Element e)
        {
            return null != e.Category
                   && e.Category.Name.ToLower()
                       .Contains(".dwf");
        }

        /// <summary>
        ///     Useless non-functional attempt to remove all
        ///     DWF links from the model and return
        ///     the total number of deleted elements.
        ///     This does not work! Instead, use
        ///     RemoveDwfLinkUsingExternalFileUtils.
        /// </summary>
        private int RemoveDwfLinkUsingDelete(Document doc)
        {
            var nDeleted = 0;

            var col
                = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType();

            var ids = new List<ElementId>();

            var pinned = 0;

            foreach (var e in col)
                if (ElementCategoryContainsDwf(e))
                {
                    Debug.Print(Util.ElementDescription(e));
                    pinned += e.Pinned ? 1 : 0;
                    ids.Add(e.Id);
                }

            ICollection<ElementId> idsDeleted = null;
            Transaction t;

            var n = ids.Count;
            var unpinned = 0;

            if (0 < n)
            {
                if (0 < pinned)
                    using (t = new Transaction(doc))
                    {
                        t.Start(
                            "Unpin non-ElementType '.dwf' elements");

                        unpinned = Unpin(ids, doc);

                        t.Commit();
                    }

                using (t = new Transaction(doc))
                {
                    t.Start(
                        "Delete non-ElementType '.dwf' elements");

                    idsDeleted = doc.Delete(ids);

                    t.Commit();
                }
            }

            var m = null == idsDeleted
                ? 0
                : idsDeleted.Count;

            Debug.Print("Selected {0} non-ElementType element{1}, "
                        + "{2} pinned, {3} unpinned, "
                        + "{4} successfully deleted.", n, Util.PluralSuffix(n), pinned, unpinned, m);

            nDeleted += m;

            col = new FilteredElementCollector(doc)
                .WhereElementIsElementType();

            ids.Clear();
            pinned = 0;

            foreach (var e in col)
                if (ElementCategoryContainsDwf(e))
                {
                    Debug.Print(Util.ElementDescription(e));
                    pinned += e.Pinned ? 1 : 0;
                    ids.Add(e.Id);
                }

            n = ids.Count;

            if (0 < n)
            {
                if (0 < pinned)
                    using (t = new Transaction(doc))
                    {
                        t.Start(
                            "Unpin element type '.dwf' elements");

                        unpinned = Unpin(ids, doc);

                        t.Commit();
                    }

                using (t = new Transaction(doc))
                {
                    t.Start("Delete element type '.dwf' elements");

                    idsDeleted = doc.Delete(ids);

                    t.Commit();
                }
            }

            m = null == idsDeleted ? 0 : idsDeleted.Count;

            Debug.Print("Selected {0} element type{1}, "
                        + "{2} pinned, {3} unpinned, "
                        + "{4} successfully deleted.", n, Util.PluralSuffix(n), pinned, unpinned, m);

            nDeleted += m;

            return nDeleted;
        }

        /// <summary>
        ///     Remove DWF links from model and return
        ///     the total number of deleted elements.
        /// </summary>
        private int RemoveDwfLinkUsingExternalFileUtils(
            Document doc)
        {
            var idsToDelete
                = new List<ElementId>();

            var ids = ExternalFileUtils
                .GetAllExternalFileReferences(doc);

            foreach (var id in ids)
            {
                var e = doc.GetElement(id);

                Debug.Print(Util.ElementDescription(e));

                var xr = ExternalFileUtils
                    .GetExternalFileReference(doc, id);

                var xrType
                    = xr.ExternalFileReferenceType;

                if (xrType == ExternalFileReferenceType.DWFMarkup)
                {
                    var xrPath = xr.GetPath();

                    var path = ModelPathUtils
                        .ConvertModelPathToUserVisiblePath(xrPath);

                    if (path.EndsWith(".dwf")
                        || path.EndsWith(".dwfx"))
                        idsToDelete.Add(id);
                }
            }

            var n = idsToDelete.Count;

            ICollection<ElementId> idsDeleted = null;

            if (0 < n)
            {
                using var t = new Transaction(doc);
                t.Start("Delete DWFx Links");

                idsDeleted = doc.Delete(idsToDelete);

                t.Commit();
            }

            var m = null == idsDeleted
                ? 0
                : idsDeleted.Count;

            Debug.Print("Selected {0} DWF external file reference{1}, "
                        + "{2} element{3} successfully deleted.", n, Util.PluralSuffix(n), m, Util.PluralSuffix(m));

            return m;
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            if (doc.IsFamilyDocument)
            {
                Util.ErrorMsg(
                    "This command requires an active document.");

                return Result.Failed;
            }

            var nDeleted = RemoveDwfLinkUsingDelete(doc);

            var nDeleted2 = RemoveDwfLinkUsingExternalFileUtils(doc);

            return Result.Succeeded;
        }
    }
}