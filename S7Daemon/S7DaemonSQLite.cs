using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data.SQLite;

namespace S7Console
{
    class S7DaemonSQLite
    {
        // S7Daemon logger
        // Coding by kjurlina. Have a lot of fun
        // Application database master

        private string DboFilePath;
        private string FullDboFilePath;
        private string DboFileName;

        public S7DaemonSQLite(string path, string name)
        {
            // Compose SQLite file path, name and exitension
            DboFilePath = path;
            DboFileName = name;
            FullDboFilePath = DboFilePath + @"\" + DboFileName;


        }

        public void CreateDatabase()
        {

            SQLiteConnection.CreateFile(FullDboFilePath);
        }

        public bool CheckDatabaseExists()
        {
            // Check if SQLite database file exists         
            return File.Exists(FullDboFilePath);
        }

        public void CreateTable(string name, string type)
        {
            // First determine requested data type
            string TagDataType;
            if (type == "1" | type == "2")
            {
                TagDataType = "INTEGER";
            }
            else if (type == "3")
            {
                TagDataType = "REAL";
            }
            else
            {
                TagDataType = "varchar(32)";
            }

            // Create database table
            string CmdString = "CREATE TABLE " + name + " (Timestamp varchar(32), Value " + TagDataType + ")";
            SQLiteConnection Conn = new SQLiteConnection("Data Source =" + FullDboFilePath + ";Version=3;");
            SQLiteCommand Cmd = new SQLiteCommand(CmdString, Conn);

            Conn.Open();
            Cmd.ExecuteNonQuery();
            Conn.Close();
        }

        public bool CheckTableExists(string name)
        {
            // Check if database table exists
            bool TableExists = false;
            string QueryString = "SELECT name FROM sqlite_master WHERE type = 'table'";
            SQLiteConnection Conn = new SQLiteConnection("Data Source =" + FullDboFilePath + ";Version=3;");
            SQLiteCommand Cmd = new SQLiteCommand(QueryString, Conn);

            Conn.Open();
            SQLiteDataReader Reader = Cmd.ExecuteReader();
            while (Reader.Read())
                if (Reader.GetString(0) == name)
                {
                    TableExists = true;
                    break;
                }
            Reader.Close();
            Conn.Close();

            return TableExists;
        }

        public void InsertIntoTable(string tag, object value, DateTime jiffy)
        {
            string QueryString = "INSERT INTO " + tag + "(Timestamp, Value) VALUES('" + jiffy.ToString() + "','" + value.ToString() + "')";

            SQLiteConnection Conn = new SQLiteConnection("Data Source =" + FullDboFilePath + ";Version=3;");
            SQLiteCommand Cmd = new SQLiteCommand(QueryString, Conn);

            Conn.Open();
            Cmd.ExecuteNonQuery();
            Conn.Close();
        }
    }
}
