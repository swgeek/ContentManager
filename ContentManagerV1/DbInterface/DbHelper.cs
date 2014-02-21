using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// interface to database. Abstract out so can swap databases without changing code elsewhere
namespace DbInterface
{
    public class DbHelper
    {
        // TODO: should I keep this open all the time? Or open as close as needed?
        private SQLiteConnection dbConnection;

        public DbHelper(string databaseFilePathName)
        {
            if (System.IO.File.Exists(databaseFilePathName))
            {
                string connectionString = String.Format("Data Source={0};Version=3;", databaseFilePathName);
                dbConnection = new SQLiteConnection(connectionString);


                // TODO: open database, check tables exist
 
            }
            else
            {
                CreateDb(databaseFilePathName);
            }
        }

        private void CreateDb(string dbFilePath)
        {
            SQLiteConnection.CreateFile(dbFilePath);
            string connectionString = String.Format("Data Source={0};Version=3;", dbFilePath);
            dbConnection = new SQLiteConnection(connectionString);

            dbConnection.Open();

            string sql = "create table files (hash char(40) PRIMARY KEY, filesize int)";
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            command.ExecuteNonQuery();

            dbConnection.Close();
        }

        public void AddFile(string hash, long filesize)
        {
            dbConnection.Open();
            string commandString = string.Format("insert into files (hash, filesize) values (\"{0}\", {1})", hash, filesize);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            command.ExecuteNonQuery();
            dbConnection.Close();
        }

        // maybe should also check filesize as a sanity check?
        public bool FileAlreadyInDatabase(string hash)
        {
            bool exists = false;
            dbConnection.Open();

            // probably not optimal, but get it working, find best way later.
            string commandString = String.Format("select * from files where hash = \"{0}\"", hash);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                exists = true;

            dbConnection.Close();

            return exists;
        }

        public void AddFileDirectoryLocation(string hashValue, string dirPath)
        {
            bool alreadyExists = false;
            dbConnection.Open();

            // definitely not optimal, but get it working, find best way later.
            string commandString = String.Format("select * from DirectoryListForEachFile where hash = \"{0}\" and directoryPath = \"{1}\"", hashValue, dirPath);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                alreadyExists = true;

            if (!alreadyExists)
            {
                string insertCommandString = string.Format("insert into DirectoryListForEachFile (hash, directoryPath) values (\"{0}\", \"{1}\")", hashValue, dirPath);
                SQLiteCommand insertCommand = new SQLiteCommand(insertCommandString, dbConnection);
                insertCommand.ExecuteNonQuery();
            }

            dbConnection.Close();

        }
    }
}
