using System;
using System.Diagnostics;
using Autodesk.Revit.DB;

namespace BuildingCoder
{
  /// <summary>
  /// A Revision parameter wrapper class by Max.
  /// </summary>
  class JtRevision
  {
    /// <summary>
    /// The BIM element.
    /// </summary>
    Element _e;

    /// <summary>
    /// Internal access to the named parameter. 
    /// </summary>
    Parameter _p( string parameter_name )
    {
      //return _e.get_Parameter( parameter_name ); // 2014

      Debug.Assert( 
        1 == _e.GetParameters( parameter_name ).Count,
        string.Format( 
          "expected only one parameters named '{0}'", 
          parameter_name ) );

      return _e.LookupParameter( parameter_name ); // 2015
    }

    /// <summary>
    /// Create a Revision parameter accessor 
    /// for the given BIM element.
    /// </summary>
    public JtRevision( Element e )
    {
      _e = e;
    }

    public string Date
    {
      get { return _p( "Revision Date" ).AsString(); }
      set { _p( "Revision Date" ).Set( value ); }
    }

    public string IssuedTo
    {
      get { return _p( "Issued to" ).AsString(); }
      set { _p( "Issued to" ).Set( value ); }
    }

    public string Number
    {
      get { return _p( "Revision Number" ).AsString(); }
      set { _p( "Revision Number" ).Set( value ); }
    }

    public int Issued
    {
      get { return _p( "Issued" ).AsInteger(); }
      set { _p( "Issued" ).Set( value ); }
    }

    public int Numbering
    {
      get { return _p( "Numbering" ).AsInteger(); }
      set { _p( "Numbering" ).Set( value ); }
    }

    public int Sequence
    {
      get { return _p( "Revision Sequence" ).AsInteger(); }
      set { _p( "Revision Sequence" ).Set( value ); }
    }

    public string Description
    {
      get { return _p( "Revision Description" ).AsString(); }
      set { _p( "Revision Description" ).Set( value ); }
    }

    public string IssuedBy
    {
      get { return _p( "Issued by" ).AsString(); }
      set { _p( "Issued by" ).Set( value ); }
    }
  }
}
