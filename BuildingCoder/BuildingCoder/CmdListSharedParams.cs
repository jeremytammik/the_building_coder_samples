#region Header
//
// CmdListSharedParams.cs - list all shared parameters
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
//using Autodesk.Revit.Collections;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdListSharedParams : IExternalCommand
  {
    /// <summary>
    /// Get GUID for a given shared param name.
    /// </summary>
    /// <param name="app">Revit application</param>
    /// <param name="defGroup">Definition group name</param>
    /// <param name="defName">Definition name</param>
    /// <returns>GUID</returns>
    static Guid SharedParamGuid( Application app, string defGroup, string defName )
    {
      Guid guid = Guid.Empty;
      try
      {
        DefinitionFile file = app.OpenSharedParameterFile();
        DefinitionGroup group = file.Groups.get_Item( defGroup );
        Definition definition = group.Definitions.get_Item( defName );
        ExternalDefinition externalDefinition = definition as ExternalDefinition;
        guid = externalDefinition.GUID;
      }
      catch( Exception )
      {
      }
      return guid;
    }


    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      BindingMap bindings = doc.ParameterBindings;
      //Dictionary<string, Guid> guids = new Dictionary<string, Guid>();
      Dictionary<Definition, object> mapDefToGuid = new Dictionary<Definition, object>();

      int n = bindings.Size;
      Debug.Print( "{0} shared parementer{1} defined{2}",
        n, Util.PluralSuffix( n ), Util.DotOrColon( n ) );

      if( 0 < n )
      {
        DefinitionBindingMapIterator it
          = bindings.ForwardIterator();

        while( it.MoveNext() )
        {
          Definition d = it.Key as Definition;
          Binding b = it.Current as Binding;
          if( d is ExternalDefinition )
          {
            Guid g = ( ( ExternalDefinition ) d ).GUID;
            Debug.Print( d.Name + ": " + g.ToString() );
            mapDefToGuid.Add( d, g );
          }
          else
          {
            Debug.Assert( d is InternalDefinition );

            // this built-in parameter is INVALID:

            BuiltInParameter bip = (( InternalDefinition ) d ).BuiltInParameter;
            Debug.Print( d.Name + ": " + bip.ToString() );

            // if have a definition file and group name, we can still determine the GUID:

            //Guid g = SharedParamGuid( app, "Identity data", d.Name );

            mapDefToGuid.Add( d, null );
          }
        }
      }

      List<Element> walls = new List<Element>();
      if( !Util.GetSelectedElementsOrAll(
        walls, uidoc, typeof( Wall ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.Elements.Size )
          ? "Please select some wall elements."
          : "No wall elements found.";
      }
      else
      {
        //List<string> keys = new List<string>( mapDefToGuid.Keys );
        //keys.Sort();

        foreach( Wall wall in walls )
        {
          Debug.Print( Util.ElementDescription( wall ) );

          foreach( Definition d in mapDefToGuid.Keys )
          {
            object o = mapDefToGuid[d];

            Parameter p = (null == o)
              ? wall.get_Parameter( d )
              : wall.get_Parameter( ( Guid ) o );

            string s = (null == p)
              ? "<null>"
              : p.AsValueString();

            Debug.Print( d.Name + ": " + s );
          }
        }
      }
      return Result.Failed;
    }
  }
}
