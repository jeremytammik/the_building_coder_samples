#region Header
//
// JtTimer.cs - performance profiling timer
//
// Copyright (C) 2010-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Performance timer for profiling purposes.
  /// For a full description, please refer to
  /// http://thebuildingcoder.typepad.com/blog/2010/03/performance-profiling.html
  /// </summary>
  public class JtTimer : IDisposable
  {
    #region Internal TimeRegistry class
    class TimeRegistry
    {
      #region Internal data and helper methods
      class Entry
      {
        public double Time { get; set; }
        public int Calls { get; set; }
      }

      static Dictionary<string, Entry> _collection = new Dictionary<string, Entry>();

      /// <summary>
      /// Return the percentage based on total time.
      /// </summary>
      /// <param name="value">value</param>
      /// <param name="totalTime">total time</param>
      /// <returns></returns>
      static double GetPercent( double value, double totalTime )
      {
        return 0 == totalTime
          ? 0
          : Math.Round( value * 100 / totalTime, 2 );
      }
      #endregion // Private internal data and helper methods

      /// <summary>
      /// Add new duration for specified key.
      /// </summary>
      public static void AddTime( string key, double duration )
      {
        Entry e;
        if( _collection.ContainsKey( key ) )
        {
          e = _collection[key];
        }
        else
        {
          e = new Entry();
          _collection.Add( key, e );
        }
        e.Time += duration;
        ++e.Calls;
      }

      /// <summary>
      /// Write the report of the results to a text file.
      /// </summary>
      public static void WriteResults(
        string description,
        double totalTime )
      {
        // Set up text file path:

        string strReportPath = Path.Combine( Path.GetTempPath(), "PerformanceReport.txt" );
        FileStream fs = new FileStream( strReportPath, FileMode.OpenOrCreate, FileAccess.Write );
        StreamWriter streamWriter = new StreamWriter( fs );
        streamWriter.BaseStream.Seek( 0, SeekOrigin.End );

        // Sort output by percentage of total time used:

        List<string> lines = new List<string>( _collection.Count );
        foreach( KeyValuePair<string, Entry> pair in _collection )
        {
          Entry e = pair.Value;
          lines.Add( string.Format( "{0,10:0.00}%{1,10:0.00}{2,8}   {3}",
            GetPercent( e.Time, totalTime ),
            Math.Round( e.Time, 2 ),
            e.Calls,
            pair.Key ) );
        }
        lines.Sort();

        string header = " Percentage   Seconds   Calls   Process";
        int n = Math.Max( header.Length, lines.Max<string>( x => x.Length ) );
        if( null != description && 0 < description.Length )
        {
          n = Math.Max( n, description.Length );
          header = description + "\r\n" + header;
        }
        string separator = "-";
        while( 0 < n-- )
        {
          separator += "-";
        }
        streamWriter.WriteLine( separator );
        streamWriter.WriteLine( header );
        streamWriter.WriteLine( separator );

        foreach( string line in lines )
        {
          streamWriter.WriteLine( line );
        }
        streamWriter.WriteLine( separator + "\r\n" );
        streamWriter.Close();
        fs.Close();
        Process.Start( strReportPath );
        _collection.Clear();
      }
    }
    #endregion // Internal TimeRegistry class

    string _key;
    Stopwatch _timer;
    double _duration = 0;

    /// <summary>
    /// Performance timer
    /// </summary>
    /// <param name="what_are_we_testing_here">
    /// Key describing code to be timed</param>
    public JtTimer( string what_are_we_testing_here )
    {
      Restart( what_are_we_testing_here );
    }

    /// <summary>
    /// Automatic disposal when the the using statement
    /// block finishes: the timer is stopped and the
    /// time is registered.
    /// </summary>
    void IDisposable.Dispose()
    {
      _timer.Stop();
      _duration = _timer.Elapsed.TotalSeconds;
      TimeRegistry.AddTime( _key, _duration );
    }

    /// <summary>
    /// Write and display a report of the timing
    /// results in a text file.
    /// </summary>
    public void Report( string description )
    {
      TimeRegistry.WriteResults( description, _duration );
    }

    /// <summary>
    /// Restart the measurement from scratch.
    /// </summary>
    public void Restart( string what_are_we_testing_here )
    {
      _key = what_are_we_testing_here;
      _timer = Stopwatch.StartNew();
    }
  }
}
