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

        public int NumOfNewFileLocations { get; private set; }
        public int NumOfDuplicateFileLocations { get; private set; }

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

        // Maybe a good idea to create methods to perform queries. That way can add timers etc. when debugging.
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

            string objectStoreSqlString = "create table objectStores (id INTEGER PRIMARY KEY AUTOINCREMENT,  dirPath varchar(500))";
            SQLiteCommand objectStoreSqlCommand = new SQLiteCommand(objectStoreSqlString, dbConnection);
            objectStoreSqlCommand.ExecuteNonQuery();

            string locationSqlString = "create table fileLocations (filehash char(40) PRIMARY KEY, objectStore1 int, objectStore2 int, "
                   + "objectStore3 int, FOREIGN KEY (objectStore1) REFERENCES objectStores(id), FOREIGN KEY (objectStore2) REFERENCES objectStores(id), "
                   + "FOREIGN KEY (objectStore3) REFERENCES objectStores(id) );";
            SQLiteCommand locationSqlCommand = new SQLiteCommand(locationSqlString, dbConnection);
            locationSqlCommand.ExecuteNonQuery();

            dbConnectionForCreate.Close();
        }

        // temporary code to create new version of a particular table. Leave code here for now in case need to do something similar in the
        // future
        public void CreateNewTable()
        {
            dbConnection.Open();

            string createTableSqlString = "create table originalRootDirectories (rootdir char(500) PRIMARY KEY);";
            SQLiteCommand sqlCommand = new SQLiteCommand(createTableSqlString, dbConnection);
            sqlCommand.ExecuteNonQuery();

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

        public int? GetObjectStoreId(string objectStorePath)
        {
            int? depotID = null;

            string commandString = String.Format("select id from objectStores where dirPath = \"{0}\"", objectStorePath);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                depotID = reader.GetInt32(0);
            }

            return depotID;
        }

        public void CheckObjectStoreExistsAndInsertIfNot(string objectStorePath)
        {
            if (GetObjectStoreId(objectStorePath) == null)
            {
                string insertCommandString =
                    string.Format("insert into objectStores (dirPath) values (\"{0}\")", objectStorePath);
                SQLiteCommand insertCommand = new SQLiteCommand(insertCommandString, dbConnection);
                insertCommand.ExecuteNonQuery();
            }  
        }

        private void doInsertForAddFreshLocation(string filename, int objectStoreID)
        {
            string insertCommandString =
                    string.Format("insert into fileLocations (filehash, objectStore1) values (\"{0}\", {1})", filename, objectStoreID);

            SQLiteCommand insertCommand = new SQLiteCommand(insertCommandString, dbConnection);
                insertCommand.ExecuteNonQuery();
        }

        private void InsertAdditionalFileLocation(string filename, int objectStoreID)
        {
            string insertSqlString;

            string locationCommandString = String.Format("select objectStore1, objectStore2, objectStore3 from fileLocations where filehash = \"{0}\"", filename);
            SQLiteCommand locationCommand = new SQLiteCommand(locationCommandString, dbConnection);
            SQLiteDataReader locationReader = locationCommand.ExecuteReader();
            if (locationReader.Read())
            {
                if (locationReader.IsDBNull(0))
                    insertSqlString = String.Format("update fileLocations set objectStore1 = {0} where filehash = \"{1}\";", objectStoreID, filename);
                else if (locationReader.IsDBNull(1))
                    insertSqlString = String.Format("update fileLocations set objectStore2 = {0} where filehash = \"{1}\";", objectStoreID, filename);
                else if (locationReader.IsDBNull(2))
                    insertSqlString = String.Format("update fileLocations set objectStore3 = {0} where filehash = \"{1}\";", objectStoreID, filename);
                else
                    throw new Exception("non of the entries were null");

                SQLiteCommand insertCommand = new SQLiteCommand(insertSqlString, dbConnection);
                insertCommand.ExecuteNonQuery();

            }
        }


        public List<int> GetFileLocations(string filename)
        {
            List<int> locationList = new List<int>();

            string locationCommandString = String.Format("select objectStore1, objectStore2, objectStore3 from fileLocations where filehash = \"{0}\"", filename);
            SQLiteCommand locationCommand = new SQLiteCommand(locationCommandString, dbConnection);
            SQLiteDataReader locationReader = locationCommand.ExecuteReader();
            if (locationReader.Read())
            {
                if (!locationReader.IsDBNull(0))
                    locationList.Add(locationReader.GetInt32(0));
                if (!locationReader.IsDBNull(1))
                    locationList.Add(locationReader.GetInt32(1));
                if (!locationReader.IsDBNull(2))
                    locationList.Add(locationReader.GetInt32(2));
            }

            return locationList;
        }

        public void AddFileLocation(string filename, string objectStoreRoot)
        {
            CheckObjectStoreExistsAndInsertIfNot(objectStoreRoot);

            int depotId = (int)GetObjectStoreId(objectStoreRoot);

            // find any locations that exist already for this file
            List<int> existingLocations = GetFileLocations(filename);

            // if location already in existing locations, nothing to do, just return
            if (existingLocations.Contains(depotId))
            {
                NumOfDuplicateFileLocations++;
                return;
            }

            NumOfNewFileLocations++;
            // if no locations yet, simple insert
            if (existingLocations.Count == 0)
            {
                doInsertForAddFreshLocation(filename, depotId);
                return;
            }

            // a location exists, but not this one, so insert it
            if (existingLocations.Count == 3)
                throw new Exception("already reached max number of locations, cannot add another");
            // TODO: in future will have an additional table to handle more than three location, maybe even more than two.
            else
                InsertAdditionalFileLocation(filename, depotId);
        }

        public void AddOriginalRootDirectoryIfNotInDb(string dirPath)
        {
            string commandString = String.Format("select count(*) from originalRootDirectories where rootdir = \"{0}\"", dirPath);
            int count = -1;
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                count = reader.GetInt32(0);
            }

            if (count == 0)
            {
                string insertCommandString =
                    string.Format("insert into originalRootDirectories (rootdir) values (\"{0}\")", dirPath);
                SQLiteCommand insertCommand = new SQLiteCommand(insertCommandString, dbConnection);
                insertCommand.ExecuteNonQuery();
            }  
        }
    }
}
