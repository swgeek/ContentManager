using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MpvUtilities;

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

        private const string FilesTable = "Files";
        private const string OriginalDirectoriesForFileTable = "OriginalDirectoriesForFileV5";
        private const string OldOriginalDirectoriesForFileTable = "OriginalDirectoriesForFileV2";
        private const string OriginalDirectoriesTable = "originalDirectoriesV2";
        private const string ObjectStoresTable = "objectStores";

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
                OriginalDirectoriesForFileTable);
            ExecuteNonQuerySql(createODFFTTableSqlString);

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
            //string createTableSqlString = String.Format(
            //    "create table {0} (filehash char(40), filename varchar(300), "
            //    + "dirPathHash char(40), extension varchar(30), PRIMARY KEY (filehash, filename, dirPathHash))",
            //    OriginalDirectoriesForFileTable);
            //ExecuteNonQuerySql(createTableSqlString);

            // move data from old to new
            string getFileDirInfoSqlString = String.Format("select hash, directoryPath from {0};", OldOriginalDirectoriesForFileTable);
            SQLiteDataReader reader = GetDataReaderForSqlQuery(getFileDirInfoSqlString);
            while (reader.Read())
            {
                string fileHash = reader.GetString(0);
                string filePath = reader.GetString(1);

                AddOriginalFileLocation(fileHash, filePath);
            }
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
                if ((filesize != -1) && (filesize != filesizeFromDb))
                {
                    if (filesizeFromDb == -1)
                    {
                        // filesize was previously unknown, update db
                        string updateFilesizeSql = String.Format("update Files set filesize = {0} where filehash = \"{1}\"; ", filesize, hash);
                        ExecuteNonQuerySql(updateFilesizeSql);
                    }
                    else
                        throw new Exception("filesizes do not match");
                }
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

        public void AddFileDirectoryLocationOld(string hashValue, string dirPath)
        {
            string insertCommandString = string.Format("insert into OriginalDirectoriesForFileV2 (hash, directoryPath) values (\"{0}\", \"{1}\")", hashValue, dirPath);
                SQLiteCommand insertCommand = new SQLiteCommand(insertCommandString, dbConnection);
                insertCommand.ExecuteNonQuery();
                NumOfNewDirectoryMappings++;
        }

        bool FileOriginalLocationAlreadyInDatabase(string hashValue, string filename, string dirHash)
        {
            bool exists = false;

            string queryString = String.Format("select * from {0} where filehash = \"{1}\" and filename = \"{2}\" and dirPathHash = \"{3}\";", 
                OriginalDirectoriesForFileTable, hashValue, filename, dirHash);
            SQLiteDataReader reader = GetDataReaderForSqlQuery(queryString);
            if (reader.Read())
            {
                    exists = true;
            }
            return exists;
        }

        public void AddOriginalFileLocation(string hashValue, string filePath)
        {
            string filename = System.IO.Path.GetFileName(filePath);
            string extension = System.IO.Path.GetExtension(filePath);
            string directory = System.IO.Path.GetDirectoryName(filePath);
            string dirHash = SH1HashUtilities.HashString(directory);

            if (!DirectoryAlreadyInDatabase(dirHash))
                addDirectory(dirHash, directory);

            if (! FileOriginalLocationAlreadyInDatabase(hashValue, filename, dirHash))
            {
                string addOriginalFileLocationSqlString = String.Format(
                    "insert into {0} (filehash, filename, dirPathHash, extension) values (\"{1}\", \"{2}\", \"{3}\", \"{4}\");", 
                    OriginalDirectoriesForFileTable, hashValue, filename, dirHash, extension);

                ExecuteNonQuerySql(addOriginalFileLocationSqlString);
            }
        }

        public bool DirectoryAlreadyInDatabase(string dirPathHash)
        {
            bool exists = false;

            // probably not optimal, but get it working, find best way later.
            string commandString = String.Format("select * from {0} where dirPathHash = \"{1}\"", OriginalDirectoriesTable, dirPathHash);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                exists = true;
                NumOfDuplicateDirs++;
            }

            return exists;
        }

        // removes fileinfo from db completely, used when added in error, e.g. xml files added early on
        // for now just two tables, but will be more thorough later on...
        public void RemoveFileCompletely(string filename)
        {
            string countFileEntriesSql = String.Format( "select count(*) from Files where hash = \"{0}\";", filename);
            int countBefore = (int)ExecuteSqlQueryReturningSingleInt(countFileEntriesSql);
            if (countBefore > 0)
            {
                string deleteFileSql = String.Format("delete from Files where hash = \"{0}\";", filename);
                ExecuteNonQuerySql(deleteFileSql);
                int countAfter = (int)ExecuteSqlQueryReturningSingleInt(countFileEntriesSql);

                if (countAfter < countBefore)
                    Console.WriteLine("filename + Deleted from Files");
                else
                    Console.WriteLine(filename + "not deleted");
            }

            string countLocationEntriesSql = String.Format("select count(*) from fileLocations where filehash = \"{0}\";", filename);
            countBefore = (int)ExecuteSqlQueryReturningSingleInt(countLocationEntriesSql);
            if (countBefore > 0)
            {
                string deleteLocationSql = String.Format("delete from fileLocations where filehash = \"{0}\";", filename);
                ExecuteNonQuerySql(deleteLocationSql);
                int countAfter = (int)ExecuteSqlQueryReturningSingleInt(countLocationEntriesSql);

                if (countAfter < countBefore)
                    Console.WriteLine("filename + Deleted from fileLocations");
                else
                    Console.WriteLine(filename + "not deleted");
            }
        }


        public void addDirectory(string dirPathHash, string dirPath)
        {
            string insertCommandString =
                string.Format("insert into {0} (dirPathHash, dirPath) values (\"{1}\", \"{2}\")", OriginalDirectoriesTable, dirPathHash, dirPath);
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
            string commandString = String.Format( "select hash from {0} where status = \"todo\" order by filesize desc limit 10", FilesTable);
            return GetDatasetForSqlQuery(commandString) ;
        }

        public DataSet GetOriginalDirectoriesForFile(string fileHash)
        {
            string commandString = String.Format("select dirPath, dirPathHash, filename from {0} join {1} using (dirPathHash) where filehash = \"{2}\" limit 100;", 
                OriginalDirectoriesForFileTable, OriginalDirectoriesTable, fileHash);
            return GetDatasetForSqlQuery(commandString);
        }

        public DataSet GetListOfFilesInOriginalDirectory(string dirPathHash)
        {
            string commandString = String.Format("select filename, filehash from {0} where dirPathHash = \"{1}\" ;", OriginalDirectoriesForFileTable, dirPathHash);
            return GetDatasetForSqlQuery(commandString);
        }

        // for now, just the hash filenames, will return more info later. Maybe.
        // NOT USED OR TESTED YET!
        public List<string> GetLargestFilesToDo(int count)
        {
            List<string> fileList = new List<string>();

            string commandString = String.Format("select hash from Files where status == \"todo\" order by filesize desc limit {0};", count);
            SQLiteCommand command = new SQLiteCommand(commandString, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                fileList.Add(reader.GetString(0));
            return fileList;
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

        public List<string> GetFileLocationPaths(string fileHash)
        {
            List<int> locationList = GetFileLocations(fileHash);

            List<string> locationPaths = new List<string>();
            
            foreach (int location in locationList)
            {
                string commandString = String.Format("select dirPath from {0} where id = {1}", ObjectStoresTable, location);
                SQLiteDataReader reader = GetDataReaderForSqlQuery(commandString);
                if (reader.Read())
                    locationPaths.Add(reader.GetString(0));
            }
            return locationPaths;
        }

        public void SetToDelete(string fileHash)
        {
            string sqlCommand = String.Format("update Files set status = \"todelete\" where hash = \"{0}\";", fileHash);
            ExecuteNonQuerySql(sqlCommand);
        }
    }
}
