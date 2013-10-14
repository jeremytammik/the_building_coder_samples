#region Header
//
// CmdFamilyParamGuid.cs - determine family parameter IsShared and GUID properties using System.Reflection
//
// Copyright (C) 2011-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdFamilyParamGuid : IExternalCommand
  {
    /// <summary>
    /// Get family parameter IsShared
    /// and GUID properties.
    /// </summary>
    /// <returns>True if the family parameter
    /// is shared and has a GUID.</returns>
    bool GetFamilyParamGuid(
      FamilyParameter fp,
      out string guid )
    {
      guid = string.Empty;

      bool isShared = false;

      System.Reflection.FieldInfo fi
        = fp.GetType().GetField( "m_Parameter",
          System.Reflection.BindingFlags.Instance
          | System.Reflection.BindingFlags.NonPublic );

      if( null != fi )
      {
        Parameter p = fi.GetValue( fp ) as Parameter;

        isShared = p.IsShared;

        if( isShared && null != p.GUID )
        {
          guid = p.GUID.ToString();
        }
      }
      return isShared;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      if( !doc.IsFamilyDocument )
      {
        message =
          "Please run this command in a family document.";

        return Result.Failed;
      }
      else
      {
        bool isShared;
        string guid;

        FamilyManager mgr = doc.FamilyManager;

        foreach( FamilyParameter fp in mgr.Parameters )
        {
          // Using GetFamilyParamGuid method, 
          // internally accessing m_Parameter:

          isShared = GetFamilyParamGuid( fp, out guid );

          // Using extension method, internally 
          // accessing getParameter:

          if( fp.IsShared() )
          {
            Guid giud2 = fp.GUID;
          }
        }
        return Result.Succeeded;
      }
    }
  }
}
