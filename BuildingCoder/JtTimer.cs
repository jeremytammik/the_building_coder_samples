#region Header

//
// JtTimer.cs - performance profiling timer
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Performance timer for profiling purposes.
    ///     For a full description, please refer to
    ///     http://thebuildingcoder.typepad.com/blog/2010/03/performance-profiling.html
    /// </summary>
    public class JtTimer : IDisposable
    {
        private double _duration;

        private string _key;
        private Stopwatch _timer;

        /// <summary>
        ///     Performance timer
        /// </summary>
        /// <param name="what_are_we_testing_here">
        ///     Key describing code to be timed
        /// </param>
        public JtTimer(string what_are_we_testing_here)
        {
            Restart(what_are_we_testing_here);
        }

        /// <summary>
        ///     Automatic disposal when the the using statement
        ///     block finishes: the timer is stopped and the
        ///     time is registered.
        /// </summary>
        void IDisposable.Dispose()
        {
            _timer.Stop();
            _duration = _timer.Elapsed.TotalSeconds;
            TimeRegistry.AddTime(_key, _duration);
        }

        /// <summary>
        ///     Write and display a report of the timing
        ///     results in a text file.
        /// </summary>
        public void Report(string description)
        {
            TimeRegistry.WriteResults(description, _duration);
        }

        /// <summary>
        ///     Restart the measurement from scratch.
        /// </summary>
        public void Restart(string what_are_we_testing_here)
        {
            _key = what_are_we_testing_here;
            _timer = Stopwatch.StartNew();
        }

        #region Internal TimeRegistry class

        private class TimeRegistry
        {
            /// <summary>
            ///     Add new duration for specified key.
            /// </summary>
            public static void AddTime(string key, double duration)
            {
                Entry e;
                if (_collection.ContainsKey(key))
                {
                    e = _collection[key];
                }
                else
                {
                    e = new Entry();
                    _collection.Add(key, e);
                }

                e.Time += duration;
                ++e.Calls;
            }

            /// <summary>
            ///     Write the report of the results to a text file.
            /// </summary>
            public static void WriteResults(
                string description,
                double totalTime)
            {
                // Set up text file path:

                var strReportPath = Path.Combine(Path.GetTempPath(), "PerformanceReport.txt");
                var fs = new FileStream(strReportPath, FileMode.OpenOrCreate, FileAccess.Write);
                var streamWriter = new StreamWriter(fs);
                streamWriter.BaseStream.Seek(0, SeekOrigin.End);

                // Sort output by percentage of total time used:

                var lines = new List<string>(_collection.Count);
                foreach (var pair in _collection)
                {
                    var e = pair.Value;
                    lines.Add($"{GetPercent(e.Time, totalTime),10:0.00}%{Math.Round(e.Time, 2),10:0.00}{e.Calls,8}   {pair.Key}");
                }

                lines.Sort();

                var header = " Percentage   Seconds   Calls   Process";
                var n = Math.Max(header.Length, lines.Max(x => x.Length));
                if (description is {Length: > 0})
                {
                    n = Math.Max(n, description.Length);
                    header = $"{description}\r\n{header}";
                }

                var separator = "-";
                while (0 < n--) separator += "-";
                streamWriter.WriteLine(separator);
                streamWriter.WriteLine(header);
                streamWriter.WriteLine(separator);

                foreach (var line in lines) streamWriter.WriteLine(line);
                streamWriter.WriteLine($"{separator}\r\n");
                streamWriter.Close();
                fs.Close();
                Process.Start(strReportPath);
                _collection.Clear();
            }

            #region Internal data and helper methods

            private class Entry
            {
                public double Time { get; set; }
                public int Calls { get; set; }
            }

            private static readonly Dictionary<string, Entry> _collection = new();

            /// <summary>
            ///     Return the percentage based on total time.
            /// </summary>
            /// <param name="value">value</param>
            /// <param name="totalTime">total time</param>
            /// <returns></returns>
            private static double GetPercent(double value, double totalTime)
            {
                return 0 == totalTime
                    ? 0
                    : Math.Round(value * 100 / totalTime, 2);
            }

            #endregion // Private internal data and helper methods
        }

        #endregion // Internal TimeRegistry class
    }
}