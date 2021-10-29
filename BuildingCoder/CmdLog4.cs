#region Header

//
// CmdLog4.cs - test using Log4Net in a Revit add-in
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
// https://logging.apache.org
// https://forums.autodesk.com/t5/revit-api-forum/log4net-does-not-log-from-revit/m-p/10572663
// 
// Revit already loads its own version of Log4Net. 
// Check the version of
// C:/Program Files/Autodesk\Revit xxxx/log4net.dll
// Use this version and no other.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

#endregion // Namespaces


namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    public class CmdLog4 : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            if (!Logger.Initialised)
                // In a normal Revit add-in, this initialisation 
                // call might be placed in the OnStartup method.

                Logger.InitMainLogger(typeof(CmdLog4));

            Logger.Log(new Exception("sample exception"));
            Logger.Info("just info");

            return Result.Succeeded;
        }

        public static class Logger
        {
            private static ILog mainlogger;

            public static bool Initialised => null != mainlogger;

            public static void InitMainLogger(Type type)
            {
                var name = type.ToString();
                var repository = LogManager.CreateRepository(name);
                mainlogger = LogManager.GetLogger(name, type);


                var LogFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "Autodesk", "TbcSamples", "Log", "Revit.log");

                var LogFile = new RollingFileAppender();
                LogFile.File = LogFilePath;
                LogFile.MaxSizeRollBackups = 10;
                LogFile.RollingStyle = RollingFileAppender.RollingMode.Size;
                LogFile.DatePattern = "_dd-MM-yyyy";
                LogFile.MaximumFileSize = "10MB";
                LogFile.ActivateOptions();
                LogFile.AppendToFile = true;
                LogFile.Encoding = Encoding.UTF8;
                LogFile.Layout = new XmlLayoutSchemaLog4j();
                LogFile.ActivateOptions();
                BasicConfigurator.Configure(repository, LogFile);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static string GetCurrentMethod()
            {
                var st = new StackTrace();
                var sf = st.GetFrame(1);
                return sf.GetMethod().Name;
            }

            public static void Log(Exception ex)
            {
                mainlogger?.Error("Error", ex);
            }

            public static void Log(string text, Exception ex)
            {
                mainlogger?.Error(text, ex);
            }

            public static void Log(string text)
            {
                mainlogger?.Info(text);
            }

            public static void Error(string text)
            {
                mainlogger?.Error(text);
            }

            public static void Warn(string text)
            {
                mainlogger?.Warn(text);
            }

            public static void Warn(string text, Exception ex)
            {
                mainlogger?.Warn(text, ex);
            }

            public static void Info(string text)
            {
                mainlogger?.Info(text);
            }
        }
    }
}