using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;

namespace DbInterface
{
    class DbInterface
    {
        // TODO: should I keep this open all the time? Or open as close as needed?
        private SQLiteConnection dbConnection;

        public DbInterface(string databaseFilePathName)
        {
            if (! System.IO.File.Exists(databaseFilePathName))
                CreateEmptyDbFile(databaseFilePathName);

            string connectionString = String.Format("Data Source={0};Version=3; Journal Mode=Off;", databaseFilePathName);
            dbConnection = new SQLiteConnection(connectionString);
       }

        private void CreateEmptyDbFile(string dbFilePath)
        {
            if (System.IO.File.Exists(dbFilePath))
            {
                throw new Exception(String.Format("Cannot create database file {0}. File already exists!", dbFilePath));
            }

            SQLiteConnection.CreateFile(dbFilePath);
        }

        // TODO: create two types of connection: journal mode on and off, use on for create table etc., off for efficient stuff
        public void OpenConnection()
        {
            dbConnection.Open();
        }

        public void CloseConnection()
        {
            dbConnection.Close();
        }

        public void ExecuteNonQuerySql(string sqlStatement)
        {
            bool toClose = false;
            if (dbConnection.State == ConnectionState.Closed)
            {
                dbConnection.Open();
                toClose = true;
            }

            SQLiteCommand command = new SQLiteCommand(sqlStatement, dbConnection);
            command.ExecuteNonQuery();

            if (toClose)
                dbConnection.Close();
        }

        public int? ExecuteSqlQueryReturningSingleInt(string sqlStatement)
        {
            int? value = null;
            SQLiteCommand command = new SQLiteCommand(sqlStatement, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                if (!reader.IsDBNull(0))
                    value = reader.GetInt32(0);
            }
            return value;
        }

        public List<String> ExecuteSqlQueryForStrings(string sqlQueryString)
        {
            List<String> results = new List<string>();

            SQLiteCommand command = new SQLiteCommand(sqlQueryString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                    results.Add(reader.GetString(0));
            }
            return results;
        }

        public DataSet GetDatasetForSqlQuery(string sqlQueryString)
        {
            // TODO: use datareader instead of dataset? Less overhead?

            // for debugging: List<string> trythis = ExecuteSqlQueryForStrings(sqlQueryString);

            SQLiteDataAdapter dataAdaptor = new SQLiteDataAdapter();
            SQLiteCommand command = new SQLiteCommand();
            command.CommandText = sqlQueryString;
            dataAdaptor.SelectCommand = command;
            command.Connection = dbConnection;
            DataSet dataset = new DataSet();
            dataAdaptor.Fill(dataset);

            // for debugging:
            //int resultCount = dataset.Tables[0].Rows.Count;
            //for (int i = 0; i < resultCount; i++)
            //{
            //    Console.WriteLine(dataset.Tables[0].Rows[i][0].ToString());
            //}

            return dataset;
        }

        // maybe pass in a callback or anonymous method to handle each field?
        public SQLiteDataReader GetDataReaderForSqlQuery(string sqlQueryString)
        {
            // probably not optimal, but get it working, find best way later.
            SQLiteCommand command = new SQLiteCommand(sqlQueryString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            return reader;
        }
















        // TEMPORARY:: THIS NEEDS TO MOVE TO DBHELPER!
        public void CreateTables()
        {
            OpenConnection();

            // TODO: CHANGE TO LONG!
            string sql = "create table Files (hash char(40) PRIMARY KEY, filesize int, status varchar(60))";
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            command.ExecuteNonQuery();


            string dirSqlString = "create table originalDirectoriesV2 (dirPathHash char(40) PRIMARY KEY, dirPath varchar(500), status varchar(60))";
            SQLiteCommand dirCommand = new SQLiteCommand(dirSqlString, dbConnection);
            dirCommand.ExecuteNonQuery();

            string dirToSubdirSqlString =
                "create table originalDirToSubdir (dirPathHash char(40), subdirPathHash char(40), PRIMARY KEY (dirPathHash, subdirPathHash))";
            SQLiteCommand dirToSubdirCommand = new SQLiteCommand(dirToSubdirSqlString, dbConnection);
            dirToSubdirCommand.ExecuteNonQuery();

            string objectStoreSqlString = "create table objectStores (id INTEGER PRIMARY KEY AUTOINCREMENT,  dirPath varchar(500))";
            SQLiteCommand objectStoreSqlCommand = new SQLiteCommand(objectStoreSqlString, dbConnection);
            objectStoreSqlCommand.ExecuteNonQuery();

            string locationSqlString = "create table fileLocations (filehash char(40) PRIMARY KEY, objectStore1 int, objectStore2 int, "
                   + "objectStore3 int, FOREIGN KEY (objectStore1) REFERENCES objectStores(id), FOREIGN KEY (objectStore2) REFERENCES objectStores(id), "
                   + "FOREIGN KEY (objectStore3) REFERENCES objectStores(id) );";
            SQLiteCommand locationSqlCommand = new SQLiteCommand(locationSqlString, dbConnection);
            locationSqlCommand.ExecuteNonQuery();

            string createODFFTTableSqlString = String.Format(
                "create table {0} (filehash char(40), filename varchar(300), "
                + "dirPathHash char(40), extension varchar(30), PRIMARY KEY (filehash, filename, dirPathHash))",
                "OriginalDirectoriesForFileV5");
            ExecuteNonQuerySql(createODFFTTableSqlString);

            CloseConnection();
        }



    }
}
