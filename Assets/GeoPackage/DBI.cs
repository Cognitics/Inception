using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
using SQLiteDataReader = Mono.Data.Sqlite.SqliteDataReader;
using SQLiteParameter = Mono.Data.Sqlite.SqliteParameter;
//using SQLiteConnection = System.Data.SQLite.SQLiteConnection;
//using SQLiteCommand = System.Data.SQLite.SQLiteCommand;
//using SQLiteDataReader = System.Data.SQLite.SQLiteDataReader;
//using SQLiteParameter = System.Data.SQLite.SQLiteParameter;

namespace Cognitics.DBI
{
    public class Connection : IDisposable
    {
        public readonly SQLiteConnection Database;

        public Connection(string filename)
        {
            Database = new SQLiteConnection("Data Source=" + filename + ";Version=3;Mode=ReadOnly;");
            Database.Open();
        }

        public Statement Prepare(string query) => new Statement(Database.CreateCommand(), query);

        public Statement Execute(string query)
        {
            var result = Prepare(query);
            result.Execute();
            return result;
        }

        public IEnumerable<Tuple<string, string>> Fields(string table)
        {
            using (var tableSchema = Database.GetSchema("Columns", new string[] { null, null, table }))
                foreach(DataRow row in tableSchema.Rows)
                    yield return new Tuple<string, string>(row["COLUMN_NAME"].ToString(), row["DATA_TYPE"].ToString());
        }


        ~Connection()
        {
            /*
            Database.Close();
            //This necessary because SQLite doesn't seem to totally close the
            //Database until the finalizer is called.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            */
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Database.Close();
                    //This necessary because SQLite doesn't seem to totally close the
                    //Database until the finalizer is called.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                disposedValue = true;
            }
        }

        

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

        }
        #endregion


    }

    public class Statement : IDisposable
    {
        public SQLiteCommand Command;
        public SQLiteDataReader Reader;

        public void Dispose() => Command.Dispose();

        public Statement(SQLiteCommand command, string query)
        {
            Command = command;
            Command.CommandType = CommandType.Text;
            Command.CommandText = query;
        }

        public void AddParameter<T>(string key, T value) => Command.Parameters.Add(new SQLiteParameter(key, value));
        public int FieldCount => Reader.FieldCount;
        public void Execute() => Reader = Command.ExecuteReader();
        public bool Next() => Reader.Read();
        public int Ordinal(string key) => Reader.GetOrdinal(key);
        public string Key(int ordinal) => Reader.GetName(ordinal);
        public object Value(string key) => Value(Ordinal(key));

        public object Value(int ordinal)
        {
            try
            {
                return Reader.GetValue(ordinal);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public T Value<T>(int ordinal, T defaultValue)
        {
            if (Reader.IsDBNull(ordinal))
                return defaultValue;
            try
            {
                return Reader.GetFieldValue<T>(ordinal);
            }
            catch (FormatException)
            {
                return defaultValue;
            }
        }

        public T Value<T>(string key, T defaultValue) => Value(Ordinal(key), defaultValue);
        public System.IO.Stream Stream(int ordinal) => Reader.GetStream(ordinal);
        public System.IO.Stream Stream(string key) => Reader.GetStream(Ordinal(key));

    }

}
