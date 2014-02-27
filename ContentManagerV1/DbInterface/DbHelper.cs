using System;
using System.Collections.Generic;
using System.Data;
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

        public int NumOfNewFiles { get; private set; }
        public int NumOfNewDirectoryMappings { get; private set; }
        public int NumOfDuplicateFiles { get; private set; }
        public int NumOfDuplicateDirectoryMappings { get; private set; }

        public int NumOfNewDirs { get; private set; }
        public int NumOfNewDirSubDirMappings { get; private set; }
        public int NumOfDuplicateDirs { get; private set; }
        public int NumOfDuplicateDirSubDirMappings { get; private set; }


        public DbHelper(string databaseFilePathName)
        {
            if (! System.IO.File.Exists(databaseFilePathName))
                CreateDb(databaseFilePathName);

            string connectionString = String.Format("Data Source={0};Version=3; Journal Mode=Off;", databaseFilePathName);
            dbConnection = new SQLiteConnection(connectionString);
            ClearCounts();
       }

        public void OpenConnection()
        {
            dbConnection.Open();
        }

        public void CloseConnection()
        {
            dbConnection.Close();
        }

        private void CreateDb(string dbFilePath)
        {
             if (System.IO.File.Exists(dbFilePath))
            {
                throw new Exception(String.Format("Cannot create database file {0}. File already exists!", dbFilePath));
            }

            SQLiteConnection.CreateFile(dbFilePath);
            string connectionString = String.Format("Data Source={0};Version=3;", dbFilePath);
            SQLiteConnection dbConnectionForCreate = new SQLiteConnection(connectionString);

            dbConnectionForCreate.Open();

            // TODO: CHANGE TO LONG!
            string sql = "create table Files (hash char(40) PRIMARY KEY, filesize int, status varchar(60))";
            SQLiteCommand command = new SQLiteCommand(sql, dbConnectionForCreate);
            command.ExecuteNonQuery();

            string filedirSqlString = "create table OriginalDirectoriesForFile (hash char(40), directoryPath varchar(500))";
            SQLiteCommand filedirCommand = new SQLiteCommand(filedirSqlString, dbConnectionForCreate);
            filedirCommand.ExecuteNonQuery();

            string dirSqlString = "create table originalDirectories (dirPathHash char(40) PRIMARY_KEY, dirPath varchar(500))";
            SQLiteCommand dirCommand = new SQLiteCommand(dirSqlString, dbConnection);
            dirCommand.ExecuteNonQuery();

            string dirToSubdirSqlString = "create table originalDirToSubdir (dirPathHash char(40), subdirPathHash char(40), PRIMARY KEY (dirPathHash, subdirPathHash))";
            SQLiteCommand dirToSubdirCommand = new SQLiteCommand(dirToSubdirSqlString, dbConnection);
            dirToSubdirCommand.ExecuteNonQuery();

            dbConnectionForCreate.Close();
        }

        // temporary code to create new version of a particular table. Leave code here for now in case need to do something similar in the future
        public void CreateNewTable()
        {
            dbConnection.Open();

            string dirSqlString = "create table originalDirectories (dirPathHash char(40) PRIMARY KEY, dirPath varchar(500))";
            SQLiteCommand dirCommand = new SQLiteCommand(dirSqlString, dbConnection);
            dirCommand.ExecuteNonQuery();

            dbConnection.Close();
        }

        // temporary code. Leave code here for now in case need to do something similar in the future
        public void DeleteOldTable()
        {
            dbConnection.Open();

            string filedirSqlString = "drop table OriginalDirectoriesForFile";
            SQLiteCommand filedirCommand = new SQLiteCommand(filedirSqlString, dbConnection);
            filedirCommand.ExecuteNonQuery();

            dbConnection.Close();
        }

        // temporary code to copy data to new version of a particular table. Leave code here for now in case need to do something similar in the future
        public void TransferDataToNewVersionOfTable()
        {
            dbConnection.Open();

            string filedirSqlString = "insert into OriginalDirectoriesForFileV2 select * from OriginalDirectoriesForFile";
            SQLiteCommand filedirCommand = new SQLiteCommand(filedirSqlString, dbConnection);
            filedirCommand.ExecuteNonQuery();

            dbConnection.Close();

        }

        public void AddFile(string hash, long filesize)
        {

            string commandString = string.Format("insert into files (hash, filesize, status) values (\"{0}\", {1}, \"todo\")", hash, filesize);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            command.ExecuteNonQuery();

            NumOfNewFiles++;
        }

        public bool FileAlreadyInDatabase(string hash, long filesize)
        {
            bool exists = false;

            // probably not optimal, but get it working, find best way later.
            string commandString = String.Format("select filesize from files where hash = \"{0}\"", hash);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {  
                exists = true;
                long filesizeFromDb = reader.GetInt64(0);
                if (filesize != filesizeFromDb)
                    throw new Exception("filesizes do not match");
                NumOfDuplicateFiles++;
            }

            return exists;
        }

        public bool FileAlreadyInDatabaseUnknownSize(string hash)
        {
            bool exists = false;

            // probably not optimal, but get it working, find best way later.
            string commandString = String.Format("select * from files where hash = \"{0}\"", hash);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                exists = true;
                NumOfDuplicateFiles++;
            }

            return exists;
        }

        public bool FileDirectoryLocationExists(string hashValue, string dirPath)
        {
            bool alreadyExists = false;

            // definitely not optimal, but get it working, find best way later.
            string commandString = String.Format("select * from OriginalDirectoriesForFileV2 where hash = \"{0}\" and directoryPath = \"{1}\"", hashValue, dirPath);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                alreadyExists = true;
                NumOfDuplicateDirectoryMappings++;
                break;
            }

            return alreadyExists;
        }

        public void AddFileDirectoryLocation(string hashValue, string dirPath)
        {
            string insertCommandString = string.Format("insert into OriginalDirectoriesForFileV2 (hash, directoryPath) values (\"{0}\", \"{1}\")", hashValue, dirPath);
                SQLiteCommand insertCommand = new SQLiteCommand(insertCommandString, dbConnection);
                insertCommand.ExecuteNonQuery();
                NumOfNewDirectoryMappings++;
        }

        public bool DirectoryAlreadyInDatabase(string dirPathHash)
        {
            bool exists = false;

            // probably not optimal, but get it working, find best way later.
            string commandString = String.Format("select * from originalDirectories where dirPathHash = \"{0}\"", dirPathHash);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                exists = true;
                NumOfDuplicateDirs++;
            }

            return exists;
        }


        public void addDirectory(string dirPathHash, string dirPath)
        {
            string insertCommandString = 
                string.Format("insert into originalDirectories (dirPathHash, dirPath) values (\"{0}\", \"{1}\")", dirPathHash, dirPath);
            SQLiteCommand insertCommand = new SQLiteCommand(insertCommandString, dbConnection);
            insertCommand.ExecuteNonQuery();
            NumOfNewDirs++;
        }

        public bool DirSubdirMappingExists(string dirPathHash, string subdirPathHash)
        {
            bool exists = false;

            // probably not optimal, but get it working, find best way later.
            string commandString = String.Format("select * from originalDirToSubdir where dirPathHash = \"{0}\" and subdirPathHash = \"{1}\""
                , dirPathHash, subdirPathHash);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                exists = true;
               NumOfDuplicateDirSubDirMappings++;
            }

            return exists;
        }


       // string dirToSubdirSqlString = "create table originalDirToSubdir (dirPathHash char(40), subdirPathHash char(40), PRIMARY KEY (dirPathHash, subdirPathHash))";
        public void AddDirSubdirMapping(string dirPathHash, string subdirPathHash)
        {
            string insertCommandString =
                string.Format("insert into originalDirToSubdir (dirPathHash, subdirPathHash) values (\"{0}\", \"{1}\")", dirPathHash, subdirPathHash);
            SQLiteCommand insertCommand = new SQLiteCommand(insertCommandString, dbConnection);
            insertCommand.ExecuteNonQuery();
            NumOfNewDirSubDirMappings++;
        }

        public void ClearCounts()
        {
            NumOfNewFiles = 0;
            NumOfNewDirectoryMappings = 0;
            NumOfDuplicateFiles = 0;
            NumOfDuplicateDirectoryMappings = 0;

            NumOfNewDirs = 0;
            NumOfNewDirSubDirMappings  = 0;
            NumOfDuplicateDirs = 0;
            NumOfDuplicateDirSubDirMappings = 0;
        }

        public DataSet TryThis()
        {
            dbConnection.Open();
            SQLiteDataAdapter dataAdaptor = new SQLiteDataAdapter();
            SQLiteCommand command = new SQLiteCommand();
            string commandString = "select hash, filesize from Files limit 100";
            command.CommandText = commandString;
            dataAdaptor.SelectCommand = command;
            command.Connection = dbConnection;
            DataSet dataset = new DataSet();
            dataAdaptor.Fill(dataset);
            return dataset;
        }
    }
}
