#region Header

//
// JtNamedGuidStorage.cs - implement named Guid storage, e.g. for a globally unique project identifier
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

#endregion // Namespaces

namespace BuildingCoder
{
    internal class JtNamedGuidStorage
    {
        /// <summary>
        ///     Retrieve an existing named Guid
        ///     in the specified Revit document or
        ///     optionally create and return a new
        ///     one if it does not yet exist.
        /// </summary>
        public static bool Get(
            Document doc,
            string name,
            out Guid guid,
            bool create = true)
        {
            var rc = false;

            guid = Guid.Empty;

            // Retrieve a DataStorage element with our
            // extensible storage entity attached to it
            // and the specified element name. Only zero
            // or one should exist.

            var f
                = new ExtensibleStorageFilter(
                    JtNamedGuidStorageSchema.SchemaGuid);

            if (new FilteredElementCollector(doc)
                .OfClass(typeof(DataStorage))
                .WherePasses(f)
                .Where(e => name.Equals(e.Name))
                .FirstOrDefault() is not DataStorage dataStorage)
            {
                if (create)
                {
                    using var t = new Transaction(
                        doc, "Create named Guid storage");
                    t.Start();

                    // Create named data storage element

                    dataStorage = DataStorage.Create(doc);
                    dataStorage.Name = name;

                    // Create entity to store the Guid data

                    var entity = new Entity(
                        JtNamedGuidStorageSchema.GetSchema());

                    entity.Set("Guid", guid = Guid.NewGuid());

                    // Set entity to the data storage element

                    dataStorage.SetEntity(entity);

                    t.Commit();

                    rc = true;
                }
            }
            else
            {
                // Retrieve entity from the data storage element.

                var entity = dataStorage.GetEntity(
                    JtNamedGuidStorageSchema.GetSchema(false));

                Debug.Assert(entity.IsValid(),
                    "expected a valid extensible storage entity");

                if (entity.IsValid())
                {
                    guid = entity.Get<Guid>("Guid");

                    rc = true;
                }
            }

            return rc;
        }

        /// <summary>
        ///     The extensible storage schema,
        ///     containing one single Guid field.
        /// </summary>
        public static class JtNamedGuidStorageSchema
        {
            /// <summary>
            ///     Always create your own, new, unique GUID
            ///     before making use of this class!
            ///     E.g., Visual Studio > Tools > Create GUID.
            /// </summary>
            public static readonly Guid SchemaGuid = new(
                "{5F374308-9C59-42AE-ACC3-A77EF45EC146}");

            /// <summary>
            ///     Retrieve our extensible storage schema
            ///     or optionally create a new one if it does
            ///     not yet exist.
            /// </summary>
            public static Schema GetSchema(
                bool create = true)
            {
                var schema = Schema.Lookup(SchemaGuid);

                if (create && null == schema)
                {
                    var schemaBuilder =
                        new SchemaBuilder(SchemaGuid);

                    schemaBuilder.SetSchemaName(
                        "JtNamedGuiStorage");

                    schemaBuilder.AddSimpleField(
                        "Guid", typeof(Guid));

                    schema = schemaBuilder.Finish();
                }

                return schema;
            }
        }
    }
}