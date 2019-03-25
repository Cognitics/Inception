using System;
using System.Collections.Generic;
using System.Text;

namespace Cognitics.GeoPackage
{
    public class RelatedTablesRelationship
    {
        public string baseTableName;
        public string baseTableColumn;
        public string relatedTableName;
        public string relatedTableColumn;
        public string relationshipName;
        public string mappingTableName;
    }

    public class RelatedTables : IDisposable
    {

        private Database database;

        public Database Database { get => database; set => database = value; }

        public RelatedTables(string fileName)
        {
            database = new Database(fileName);
        }
        public Boolean CreateSchema()
        {
            // First see if the schema already exists.
            using (var statement = database.Connection.Prepare("select * from gpkg_extensions WHERE extension_name='related_tables'"))
            {
                statement.Execute();
                if (!statement.Reader.HasRows)
                {
                    using (var statement2 = database.Connection.Prepare("CREATE TABLE IF NOT EXISTS 'gpkgext_relations' "
                        + "( id INTEGER PRIMARY KEY AUTOINCREMENT, base_table_name TEXT NOT NULL, "
                        + "base_primary_column TEXT NOT NULL DEFAULT 'id', related_table_name TEXT NOT NULL, "
                        + "related_primary_column TEXT NOT NULL DEFAULT 'id', relation_name TEXT NOT NULL, "
                        + "mapping_table_name TEXT NOT NULL UNIQUE )"))
                    {
                        statement2.Execute();
                    }
                }
            }
            return true;
        }
        public void AddMediaTableIfNotExists(String name)
        {
            // Create the mapping table
            string query = "CREATE TABLE IF NOT EXISTS '" + name
                + "' ( id INTEGER PRIMARY KEY AUTOINCREMENT, data BLOB NOT NULL, content_type TEXT NOT NULL )";
            using (var statement = database.Connection.Prepare(query))
            {
                statement.Execute();
            }
        }

        public void ExecuteRawQuery(String query)
        {
            using (var statement = database.Connection.Prepare(query))
            {
                statement.Execute();
            }
        }

        /**
        *
        * @param mediaTable Name of the table to insert into
        * @param blob Binary data to insert into the 'data' column
        * @param contentType The type of content
        * @return the row id of the new row.
        */
        public long AddMedia(String mediaTable, byte[] blob, String contentType)
        {
            //Add the actual media table
            using (var statement = database.Connection.Prepare("INSERT INTO " + mediaTable + " (data,content_type) VALUES(@data,@value)"))
            {
                statement.AddParameter("@data", blob);
                statement.AddParameter("@value", contentType);
                statement.Execute();
            }
            return 0;
        }

        public IEnumerable<long> GetRelatedFeatureIds(string mappingTable, long featureId)
        {
            using (var statement = database.Connection.Prepare("SELECT * FROM " + mappingTable + "where base_id=@base_id"))
            {
                statement.AddParameter("@base_id", featureId);
                statement.Execute();
                while (statement.Next())
                    yield return statement.Value("related_id", 0);
            }
        }

        public IEnumerable<RelatedTablesRelationship> GetRelationships(string layer)
        {
            string query = "select * from gpkgext_relations WHERE base_table_name='" + layer + "'";

            using (var statement = database.Connection.Prepare(query))
            {
                
                statement.Execute();
                while (statement.Next())
                {
                    RelatedTablesRelationship relationship = new RelatedTablesRelationship();
                    relationship.baseTableColumn = statement.Value("base_primary_column", statement.Value<string>("base_primary_column","id"));
                    relationship.relatedTableName = statement.Value("related_table_name", statement.Value<string>("related_table_name", "id"));
                    relationship.relatedTableColumn = statement.Value("related_primary_column", statement.Value<string>("related_primary_column", "id"));
                    relationship.relationshipName = statement.Value("relation_name", statement.Value<string>("relation_name", ""));
                    relationship.mappingTableName = statement.Value("mapping_table_name", statement.Value<string>("base_primary_column", ""));
                    yield return relationship;
                }
            }
        }

        public void AddRelationship(RelatedTablesRelationship relationship)
        {
            
            // Create the mapping table
            string query = "CREATE TABLE IF NOT EXISTS '" + relationship.mappingTableName 
                + "' ( base_id INTEGER NOT NULL, related_id INTEGER NOT NULL )";
            using (var statement = database.Connection.Prepare(query))
            {
                statement.Execute();
            }
            // Add to the gpkgext_relationships table
            query = "INSERT INTO gpkgext_relations (base_table_name, base_primary_column," +
                "related_table_name,related_primary_column,relation_name,mapping_table_name)" +
                "VALUES(@base_table_name,@base_primary_column,@related_table_name," +
                "@related_primary_column,@relation_name,@mapping_table_name)";

            using (var statement = database.Connection.Prepare(query))
            {
                statement.AddParameter("@base_table_name", relationship.baseTableName);
                statement.AddParameter("@base_primary_column", relationship.baseTableColumn);
                statement.AddParameter("@related_table_name", relationship.relatedTableName);
                statement.AddParameter("@related_primary_column", relationship.relatedTableColumn);
                statement.AddParameter("@relation_name", relationship.relationshipName);
                statement.AddParameter("@mapping_table_name", relationship.mappingTableName);
                statement.Execute();
            }
        }

        public void AddFeatureRelationship(RelatedTablesRelationship relationship, long baseFID, long relatedFID)
        {
            string query = "INSERT INTO " + relationship.mappingTableName + " (base_id,related_id) VALUES(@base_id,@related_id)";
            using (var statement = database.Connection.Prepare(query))
            {
                statement.AddParameter("@base_id", baseFID);
                statement.AddParameter("@related_id", relatedFID);
                statement.Execute();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    database.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }


        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
