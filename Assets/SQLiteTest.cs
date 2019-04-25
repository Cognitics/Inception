using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//From https://github.com/rizasif/sqlite-unity-plugin
using Mono.Data.Sqlite;
using System.Data;

public class SQLiteTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string connection = "URI=file:" + Application.persistentDataPath + "/My_Database3.sqlite";
        IDbConnection dbcon = new SqliteConnection(connection);
        dbcon.Open();
        IDbCommand dbcmd;
        IDataReader reader;

        dbcmd = dbcon.CreateCommand();
        string q_createTable =
          "CREATE TABLE IF NOT EXISTS my_table (val INTEGER )";

        dbcmd.CommandText = q_createTable;
        reader = dbcmd.ExecuteReader();
        IDbCommand cmnd = dbcon.CreateCommand();
        cmnd.CommandText = "INSERT INTO my_table (val) VALUES (5)";
        cmnd.ExecuteNonQuery();
        IDbCommand cmnd_read = dbcon.CreateCommand();

        string query = "SELECT * FROM my_table";
        cmnd_read.CommandText = query;
        reader = cmnd_read.ExecuteReader();
        while (reader.Read())
        {
            for (int col = 0; col < reader.FieldCount; col++)
            {
                Debug.Log("val: " + col.ToString() + "=" + reader[col].ToString());
            }
        }
        dbcon.Close();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
