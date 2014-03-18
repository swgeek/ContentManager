using MpvUtilities;
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
        //private SQLiteConnection dbConnection;
        private DbInterface db;

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

        private const string FilesTable = "FilesV2";
        private const string OriginalDirectoriesForFileTable = "OriginalDirectoriesForFileV5";
        private const string OldOriginalDirectoriesForFileTable = "OriginalDirectoriesForFileV2";
        private const string OriginalDirectoriesTable = "originalDirectoriesV2";
        private const string ObjectStoresTable = "objectStores";
        private const string FileLocationsTable = "fileLocations";
        private const string FileListingForDirTable = "FileListingForDir";
        private const string SubdirListingForDirTable = "SubdirListingForDir";

        public DbHelper(string databaseFilePathName)
        {
            db = new DbInterface(databaseFilePathName);
            ClearCounts();
       }

        public void OpenConnection()
        {
            db.OpenConnection();
        }

        public void CloseConnection()
        {
            db.CloseConnection();
        }





        public void CreateDb(string dbFilePath)
        {
            if (System.IO.File.Exists(dbFilePath))
                throw new Exception("File already exists!");

            SQLiteConnection.CreateFile(dbFilePath);
            string connectionString = String.Format("Data Source={0};Version=3;", dbFilePath);
            SQLiteConnection dbConnection = new SQLiteConnection(connectionString);
            dbConnection.Open();

            // create tables

            dbConnection.Close();
        }







        private void CreateTables()
        {
            db.CreateTables(); // TEMPORARY, the sql needs to reside here, not in db
        }

        // temporary code to create new version of a particular table. Leave code here for now in case need to do something similar in the
        // future
        public void CreateNewTable()
        {
            db.OpenConnection();

            string createTableCommand = "create table FilesV2 (filehash char(40) PRIMARY KEY, filesize int, status varchar(60));";
            db.ExecuteNonQuerySql(createTableCommand);

            string transferDataCommand = "insert into FilesV2 (filehash, filesize, status) select hash, filesize, status from Files;";
            db.ExecuteNonQuerySql(transferDataCommand);

            db.CloseConnection();
        }

        public void CreateNewMappingTables1()
        {
            db.OpenConnection();

            //string createTableCommand = "create table FileListingForDir (dirPathHash char(40) PRIMARY KEY, files varchar(64000));";
            //db.ExecuteNonQuerySql(createTableCommand);

            // transfer data
            string transferCommand = String.Format("insert into {0} (dirPathHash, files) select dirPathHash, group_concat(filehash, ';') " + 
                " from {1} group by dirPathHash;", FileListingForDirTable, OriginalDirectoriesForFileTable);
            db.ExecuteNonQuerySql(transferCommand);

            db.CloseConnection();

            //string subdircommand = "select dirPathHash, group_concat(subdirPathHash, ';') from originalDirToSubdir group by dirPathHash";
        }

        public void CreateNewMappingTables2()
        {
            db.OpenConnection();

            string createTableCommand = String.Format("create table {0} (dirPathHash char(40) PRIMARY KEY, subdirs varchar(64000));", 
                SubdirListingForDirTable);
            db.ExecuteNonQuerySql(createTableCommand);

            // transfer data
            string transferCommand = String.Format("insert into {0} (dirPathHash, subdirs) select dirPathHash, group_concat(subdirPathHash, ';') " +
                " from {1} group by dirPathHash;", SubdirListingForDirTable, "originalDirToSubdir");
            db.ExecuteNonQuerySql(transferCommand);

            db.CloseConnection();

            //string subdircommand = "select dirPathHash, group_concat(subdirPathHash, ';') from originalDirToSubdir group by dirPathHash";
        }
        // temporary code to copy data to new version of a particular table. Leave code here for now in case need to do something similar in the future
        public void TransferDataToNewVersionOfTable()
        {
            db.OpenConnection();

            // create Tables
            string createTableCommand = "create table fileLocation1 (filehash char(40) PRIMARY KEY, locationId int);";
            db.ExecuteNonQuerySql(createTableCommand);

            createTableCommand = "create table fileLocation2 (filehash char(40) PRIMARY KEY, locationId int);";
            db.ExecuteNonQuerySql(createTableCommand);

            createTableCommand = "create table fileLocation3 (filehash char(40) PRIMARY KEY, locationId int);";
            db.ExecuteNonQuerySql(createTableCommand);

            // move data from old to new
            string copyDataCommand = "insert into fileLocation1 (filehash, locationId) select filehash, objectStore1 from fileLocations " +
                " where objectStore1 is not null;";
            db.ExecuteNonQuerySql(copyDataCommand);

            copyDataCommand = "insert into fileLocation2 (filehash, locationId) select filehash, objectStore2 from fileLocations " +
                " where objectStore2 is not null;";
            db.ExecuteNonQuerySql(copyDataCommand);

            // nothing in 3 yet, no need to transfer

            db.CloseConnection();
        }

        public void AddFile(string hash, long filesize)
        {
            string commandString = string.Format("insert into {0} (filehash, filesize, status) values (\"{1}\", {2}, \"todo\")",FilesTable, hash, filesize);
            db.ExecuteNonQuerySql(commandString);
            NumOfNewFiles++;
        }

        public bool FileAlreadyInDatabase(string hash, long filesize)
        {
            bool exists = false;

            // probably not optimal, but get it working, find best way later.
            string commandString = String.Format("select filesize from {0} where filehash = \"{1}\"", FilesTable, hash);
            SQLiteDataReader reader = db.GetDataReaderForSqlQuery(commandString);
            while (reader.Read())
            {  
                exists = true;
                long filesizeFromDb = reader.GetInt64(0);
                if ((filesize != -1) && (filesize != filesizeFromDb))
                {
                    if (filesizeFromDb == -1)
                    {
                        // filesize was previously unknown, update db
                        string updateFilesizeSql = String.Format("update {0} set filesize = {1} where filehash = \"{2}\"; ", FilesTable, filesize, hash);
                        db.ExecuteNonQuerySql(updateFilesizeSql);
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
            SQLiteDataReader reader = db.GetDataReaderForSqlQuery(commandString);
            while (reader.Read())
            {
                exists = true;
                NumOfDuplicateFiles++;
            }

            return exists;
        }

        public bool FileDirectoryLocationExists(string hashValue, string filePath)
        {
            bool alreadyExists = false;

            string filename = System.IO.Path.GetFileName(filePath);
            string directory = System.IO.Path.GetDirectoryName(filePath);
            string dirHash = SH1HashUtilities.HashString(directory);

            // sanity check, can remove this if need more speed
            string command = String.Format("select dirPathHash from {0} where dirPath = \"{1}\"", OriginalDirectoriesTable, directory);
            string dirPathHash = db.ExecuteSqlQueryForSingleString(command);

            if (dirPathHash == null)
                return false;

            if (! dirPathHash.Equals(dirHash))
                throw new Exception("hash in table does not match hash of directory path");

            alreadyExists = FileOriginalLocationAlreadyInDatabase(hashValue, filename, dirHash);

            return alreadyExists;
        }

        bool FileOriginalLocationAlreadyInDatabase(string hashValue, string filename, string dirHash)
        {
            bool exists = false;

            string queryString = String.Format("select * from {0} where filehash = \"{1}\" and filename = \"{2}\" and dirPathHash = \"{3}\";", 
                OriginalDirectoriesForFileTable, hashValue, filename, dirHash);
            SQLiteDataReader reader = db.GetDataReaderForSqlQuery(queryString);
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

                db.ExecuteNonQuerySql(addOriginalFileLocationSqlString);
            }
        }

        public bool DirectoryAlreadyInDatabase(string dirPathHash)
        {
            bool exists = false;

            // probably not optimal, but get it working, find best way later.
            string commandString = String.Format("select * from {0} where dirPathHash = \"{1}\"", OriginalDirectoriesTable, dirPathHash);
            SQLiteDataReader reader = db.GetDataReaderForSqlQuery(commandString);
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
            string countFileEntriesSql = String.Format( "select count(*) from {0} where filehash = \"{1}\";",FilesTable, filename);
            int countBefore = (int)db.ExecuteSqlQueryReturningSingleInt(countFileEntriesSql);
            if (countBefore > 0)
            {
                string deleteFileSql = String.Format("delete from {0} where filehash = \"{1}\";", FilesTable, filename);
                db.ExecuteNonQuerySql(deleteFileSql);
                int countAfter = (int)db.ExecuteSqlQueryReturningSingleInt(countFileEntriesSql);

                if (countAfter < countBefore)
                    Console.WriteLine(filename + "Deleted from Files");
                else
                    Console.WriteLine(filename + "not deleted");
            }

            string countLocationEntriesSql = String.Format("select count(*) from {0} where filehash = \"{1}\";", FileLocationsTable, filename);
            countBefore = (int)db.ExecuteSqlQueryReturningSingleInt(countLocationEntriesSql);
            if (countBefore > 0)
            {
                string deleteLocationSql = String.Format("delete from {0} where filehash = \"{1}\";",FileLocationsTable, filename);
                db.ExecuteNonQuerySql(deleteLocationSql);
                int countAfter = (int)db.ExecuteSqlQueryReturningSingleInt(countLocationEntriesSql);

                if (countAfter < countBefore)
                    Console.WriteLine(filename + " Deleted from fileLocations");
                else
                    Console.WriteLine(filename + "not deleted");
            }
        }


        public void addDirectory(string dirPathHash, string dirPath)
        {
            string insertCommandString =
                string.Format("insert into {0} (dirPathHash, dirPath) values (\"{1}\", \"{2}\")", OriginalDirectoriesTable, dirPathHash, dirPath);
            db.ExecuteNonQuerySql(insertCommandString);
            NumOfNewDirs++;
        }

        public bool DirSubdirMappingExists(string dirPathHash, string subdirPathHash)
        {
            bool exists = false;

            // probably not optimal, but get it working, find best way later.
            string commandString = String.Format("select * from originalDirToSubdir where dirPathHash = \"{0}\" and subdirPathHash = \"{1}\""
                , dirPathHash, subdirPathHash);
            SQLiteDataReader reader = db.GetDataReaderForSqlQuery(commandString);
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
            db.ExecuteNonQuerySql(insertCommandString);
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

        public DataSet GetLargestFilesTodo(int numOfFiles)
        {
            string commandString = String.Format( "select filehash from {0} where status = \"todo\" order by filesize desc limit {1}", FilesTable, numOfFiles);
            return db.GetDatasetForSqlQuery(commandString) ;
        }

        //public List<String> GetFilesWithOnlyOneLocation(int numOfFiles)
        //{
        //    string locationsCommand = String.Format("select filehash from (select filehash, count(*) as K from " +
        //        " (select filehash, objectStore1 as o from {0} where o is not null " +
        //        " union select filehash, objectStore2 as o from {0} where o is not null " +
        //        " union select filehash, objectStore3 as o from {0} where o is not null " +
        //        " ) group by filehash) where K = 1 limit {1};", FileLocationsTable, numOfFiles);

        //    return db.ExecuteSqlQueryForStrings(locationsCommand);
        //}

        public List<String> GetUndeletedFilesWithOnlyOneLocation(int numOfFiles)
        {
            string locationsCommand = String.Format("select filehash from (select filehash from (select filehash, count(*) as K from " +
                " (select filehash, objectStore1 as o from {0} where o is not null " +
                " union select filehash, objectStore2 as o from {0} where o is not null " +
                " union select filehash, objectStore3 as o from {0} where o is not null " +
                " ) group by filehash) where K = 1) join {1} using (filehash) " +
                "where status <> \"deleted\" and status <> \"todelete\" limit {2};", 
                FileLocationsTable, FilesTable, numOfFiles);

            return db.ExecuteSqlQueryForStrings(locationsCommand);
        }

        public DataSet GetLargestFiles(int numOfFiles, bool includeTodo, bool includeTodoLater, bool includeToDelete, bool includeDeleted)
        {
            string commandString;

            if (includeTodo && includeTodoLater && includeToDelete && includeDeleted)
            {
                // everything chosen, simplify the query
                commandString = String.Format("select filehash, status from {0} order by filesize desc limit {1}", FilesTable, numOfFiles);
            }
            else
            {
                // using the "filehash not null" just to simplify code to add to the query, can always use "or" after this 
                commandString = String.Format("select filehash, status from {0} where filehash is null", FilesTable);

                if (includeTodo)
                    commandString = commandString + " or status = \"todo\"";

                if (includeTodoLater)
                    commandString = commandString + " or status = \"todoLater\"";

                if (includeToDelete)
                    commandString = commandString + " or status = \"todelete\"";

                if (includeDeleted)
                    commandString = commandString + " or status = \"deleted\"";

                commandString = commandString + " order by filesize desc limit " + numOfFiles.ToString() + ";";


            }
            return db.GetDatasetForSqlQuery(commandString);
        }

        // should find a way to roll these queries into one routine, but for now...
        public List<string> GetListOfFilesToDelete()
        {
            string commandString = String.Format("select filehash from {0} where status = \'todelete\';", FilesTable);
            return db.ExecuteSqlQueryForStrings(commandString);
        }

        public DataSet GetFilesFromObjectStore(int objectStoreID)
        {
            string commandString = String.Format("select filehash from {0} where objectStore1 = {1} or objectStore2 = {1} or objectStore3 = {1};",
                FileLocationsTable, objectStoreID);
            return db.GetDatasetForSqlQuery(commandString);
        }

        public DataTable GetObjectStores()
        {
            string commandString = String.Format("select id, dirPath from {0};", ObjectStoresTable);
            return db.GetDatasetForSqlQuery(commandString).Tables[0];
        }

        // combine this with method above
        private int? numOfFilesWithOnlyOneLocation(int objectStoreID, string mainField, string secondfield, string thirdField)
        {
            // check if any file has specified location in main field but does not have another location
            string checkLocationcommand = String.Format("select count(*) from {0} where {2} = {1} and " +
                " {3} is null and {4} is null;", FileLocationsTable, objectStoreID, mainField, secondfield, thirdField);
            return db.ExecuteSqlQueryReturningSingleInt(checkLocationcommand);
        }

        public bool DeleteObjectStore(int objectStoreID)
        {
            // inefficient to do it in multiple queries. May fix later, get it working first.
            // also, instead of passing in table names should get from schema. Find out how.

            int? count = numOfFilesWithOnlyOneLocation(objectStoreID, "objectStore1", "objectStore2", "objectStore3");

            // have files where the only location is the specified one, cannot delete that location or will lose track of file
            if (count != 0)
                return false;

            count = numOfFilesWithOnlyOneLocation(objectStoreID, "objectStore2", "objectStore1", "objectStore3");
            if (count != 0)
                return false;

            count = numOfFilesWithOnlyOneLocation(objectStoreID, "objectStore3", "objectStore1", "objectStore2");
            if (count != 0)
                return false;

            // can delete!

            // delete from 1
            string deleteLocationCommand = String.Format("update {0} set objectStore1 = null where objectStore1 = {1};", 
                FileLocationsTable, objectStoreID);
             db.ExecuteNonQuerySql(deleteLocationCommand);

             // delete from 2
             deleteLocationCommand = String.Format("update {0} set objectStore2 = null where objectStore2 = {1};",
                FileLocationsTable, objectStoreID);
             db.ExecuteNonQuerySql(deleteLocationCommand);

             // delete from 3
             deleteLocationCommand = String.Format("update {0} set objectStore3 = null where objectStore3 = {1};",
                FileLocationsTable, objectStoreID);
             db.ExecuteNonQuerySql(deleteLocationCommand);

            // next delete the object store from the object store table
            string deleteStore = String.Format("delete from {0} where id = {1}", ObjectStoresTable, objectStoreID);
            db.ExecuteNonQuerySql(deleteStore);

            return true;
        }

        public void UpdateObjectStore(int objectStoreId, string newPath)
        {
            string commandString =
                string.Format("update {0} set dirPath = \"{1}\" where id = {2}", ObjectStoresTable, newPath, objectStoreId);
            db.ExecuteNonQuerySql(commandString);

        }

        public string getFirstFilenameForFile(string filehash)
        {
            string commandString = String.Format("select filename from {0} where filehash = \"{1}\" limit 1;",
                OriginalDirectoriesForFileTable, filehash);
            return db.ExecuteSqlQueryForSingleString(commandString);
        }

        public DataSet GetOriginalDirectoriesForFile(string fileHash)
        {
            string commandString = String.Format("select dirPath, dirPathHash, filename from {0} join {1} using (dirPathHash) where filehash = \"{2}\" limit 100;", 
                OriginalDirectoriesForFileTable, OriginalDirectoriesTable, fileHash);
            return db.GetDatasetForSqlQuery(commandString);
        }

        public string GetDirectoryPathForDirHash(string dirHash)
        {
            string commandString = String.Format("select dirPath from {0} where dirPathHash = \"{1}\";", OriginalDirectoriesTable, dirHash);
            return db.ExecuteSqlQueryForSingleString(commandString);
        }

        public void DeleteDirectoryAndContents(string dirHash)
        {
            // just mark directory to be deleted, delete the contents later...
            // delete files within directory
            
            //// first delete files
            //string deleteFilesCommand = String.Format("update {0} set status = \"todelete\" where status <> \"deleted\" and " +
            //    " filehash in (select filehash from {1} where dirPathHash = \"{2}\");", FilesTable, OriginalDirectoriesForFileTable, dirHash);
            //db.ExecuteNonQuerySql(deleteFilesCommand);

            // delete directory
            string deleteDirCommand = String.Format("update {0} set status = \"todelete\" where dirPathHash = \"{1}\" and " +
                "status <> \"deleted\";", OriginalDirectoriesTable, dirHash);
            db.ExecuteNonQuerySql(deleteDirCommand);
        }

        public DataTable GetListOfFilesInOriginalDirectory(string dirPathHash)
        {
            string commandString = String.Format("select filename, filehash, status from {0} join {1} using (filehash) where dirPathHash = \"{2}\" ;", 
                OriginalDirectoriesForFileTable, FilesTable, dirPathHash);
            return db.GetDatasetForSqlQuery(commandString).Tables[0];
        }

        public string[] GetFileListForDirectory(string dirPathHash)
        {
            string commandString = String.Format("select files from {0} where dirPathHash = \'{1}\';", FileListingForDirTable, dirPathHash);
            string fileListString = db.ExecuteSqlQueryForSingleString(commandString);
            if (string.IsNullOrEmpty(fileListString))
                return new string[0];
            return fileListString.Split(';');

        }

        public string GetStatusOfDirectory(string dirPathHash)
        {
            string command = String.Format("select status from {0} where dirPathHash = \"{1}\";", OriginalDirectoriesTable, dirPathHash);
            return db.ExecuteSqlQueryForSingleString(command);
        }

        public string GetStatusOfFile(string filehash)
        {
            string command = String.Format("select status from {0} where filehash = \"{1}\";", FilesTable, filehash);
            return db.ExecuteSqlQueryForSingleString(command);
        }
        public DataTable GetListOfSubdirectoriesInOriginalDirectory(string dirPathHash)
        {
            string commandString = String.Format(
                "select dirPath, {1}.dirPathHash, status  from {0}, {1} where {0}.dirPathHash = \"{2}\" and {0}.subdirPathHash = {1}.dirPathHash ",
                "originalDirToSubdir", "OriginalDirectoriesV2", dirPathHash);
            return db.GetDatasetForSqlQuery(commandString).Tables[0];
        }

        public string[] GetSubdirectories(string dirPathHash)
        {
            string commandString = String.Format("select subdirs from {0} where dirPathHash = \'{1}\';", SubdirListingForDirTable, dirPathHash);
            string subDirListString = db.ExecuteSqlQueryForSingleString(commandString);
            if (string.IsNullOrEmpty(subDirListString))
                return new string[0];
            return subDirListString.Split(';');
        }

        // for now, just the hash filenames, will return more info later. Maybe.
        // NOT USED OR TESTED YET!
        public List<string> GetLargestFilesToDo(int count)
        {
            List<string> fileList = new List<string>();

            string commandString = String.Format("select hash from Files where status == \"todo\" order by filesize desc limit {0};", count);
            SQLiteDataReader reader = db.GetDataReaderForSqlQuery(commandString);
            while (reader.Read())
                fileList.Add(reader.GetString(0));
            return fileList;
        }

        public int? GetObjectStoreId(string objectStorePath)
        {
            int? depotID = null;

            string commandString = String.Format("select id from objectStores where dirPath = \"{0}\"", objectStorePath);
            SQLiteDataReader reader = db.GetDataReaderForSqlQuery(commandString);
            while (reader.Read())
            {
                depotID = reader.GetInt32(0);
            }

            return depotID;
        }

        public int CheckObjectStoreExistsAndInsertIfNot(string objectStorePath)
        {
            int? objectStoreId = GetObjectStoreId(objectStorePath);
            if (objectStoreId == null)
            {
                string insertCommandString =
                    string.Format("insert into objectStores (dirPath) values (\"{0}\")", objectStorePath);
                db.ExecuteNonQuerySql(insertCommandString);
            }
            return (int)GetObjectStoreId(objectStorePath);
        }

        private void doInsertForAddFreshLocation(string filehash, int objectStoreID)
        {
            string insertCommandString =
                    string.Format("insert into fileLocations (filehash, objectStore1) values (\"{0}\", {1})", filehash, objectStoreID);
            db.ExecuteNonQuerySql(insertCommandString);
        }

        private void InsertAdditionalFileLocation(string filename, int objectStoreID)
        {
            string insertSqlString;

            string locationCommandString = String.Format("select objectStore1, objectStore2, objectStore3 from fileLocations where filehash = \"{0}\"", filename);
            using (SQLiteDataReader locationReader = db.GetDataReaderForSqlQuery(locationCommandString))
            {
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

                    db.ExecuteNonQuerySql(insertSqlString);
                }
            }
        }

        public void ReplaceFileLocation(string filehash, int oldObjectStoreID, int? newObjectStoreId)
        {
            string sqlCommand;

            string locationCommandString = String.Format("select objectStore1, objectStore2, objectStore3 from fileLocations where filehash = \"{0}\"", filehash);
            SQLiteDataReader locationReader = db.GetDataReaderForSqlQuery(locationCommandString);
            if (locationReader.Read())
            {
                string newObjectStoreIdString = newObjectStoreId == null ? "null" : newObjectStoreId.ToString();
                if ( (!locationReader.IsDBNull(0)) && (locationReader.GetInt32(0) == oldObjectStoreID))
                    sqlCommand = String.Format("update fileLocations set objectStore1 = {0} where filehash = \"{1}\";", newObjectStoreIdString, filehash);
                else if ( (!locationReader.IsDBNull(1)) && (locationReader.GetInt32(1) == oldObjectStoreID))
                    sqlCommand = String.Format("update fileLocations set objectStore2 = {0} where filehash = \"{1}\";", newObjectStoreIdString, filehash);
                else if ( (!locationReader.IsDBNull(2)) && (locationReader.GetInt32(2) == oldObjectStoreID))
                    sqlCommand = String.Format("update fileLocations set objectStore3 = {0} where filehash = \"{1}\";", newObjectStoreIdString, filehash);
                else
                    throw new Exception("non of the entries were null");

                db.ExecuteNonQuerySql(sqlCommand);
            }
        }


        public List<int> GetFileLocations(string filehash)
        {
            List<int> locationList = new List<int>();

            string locationCommandString = String.Format(
                "select objectStore1, objectStore2, objectStore3 from fileLocations where filehash = \"{0}\"", filehash);
            using (SQLiteDataReader locationReader = db.GetDataReaderForSqlQuery(locationCommandString))
            {
                if (locationReader.Read())
                {
                    if (!locationReader.IsDBNull(0))
                        locationList.Add(locationReader.GetInt32(0));
                    if (!locationReader.IsDBNull(1))
                        locationList.Add(locationReader.GetInt32(1));
                    if (!locationReader.IsDBNull(2))
                        locationList.Add(locationReader.GetInt32(2));
                }
                else // file hash not even in table
                    locationList = null;
            }

            return locationList;
        }

        public void AddFileLocation(string filehash, int objectStoreID)
        {
            // find any locations that exist already for this file
            List<int> existingLocations = GetFileLocations(filehash);

            if (existingLocations == null)
            {
                // filehash does not exist in table, insert it
                NumOfNewFileLocations++;
                doInsertForAddFreshLocation(filehash, objectStoreID);
                return;
            }

            // if location already in existing locations, nothing to do, just return
            if (existingLocations.Contains(objectStoreID))
            {
                NumOfDuplicateFileLocations++;
                return;
            }

            // filehash exists in table but this location does not, so insert it
            if (existingLocations.Count == 3)
                throw new Exception("already reached max number of locations, cannot add another");
            // TODO: in future will have an additional table to handle more than three location, maybe even more than two.

            NumOfNewFileLocations++;
            InsertAdditionalFileLocation(filehash, objectStoreID);
        }

        public void AddFileLocation(string filename, string objectStoreRoot)
        {
            int depotId = CheckObjectStoreExistsAndInsertIfNot(objectStoreRoot);
            AddFileLocation(filename, depotId);
        }

        public void AddOriginalRootDirectoryIfNotInDb(string dirPath)
        {
            string commandString = String.Format("select count(*) from originalRootDirectories where rootdir = \"{0}\"", dirPath);
            int count = -1;
            SQLiteDataReader reader = db.GetDataReaderForSqlQuery(commandString);
            if (reader.Read())
            {
                count = reader.GetInt32(0);
            }

            if (count == 0)
            {
                string insertCommandString =
                    string.Format("insert into originalRootDirectories (rootdir) values (\"{0}\")", dirPath);
                db.ExecuteNonQuerySql(insertCommandString);
            }  
        }

        public List<string> GetObjectStorePathsForFile(string fileHash)
        {
            List<int> locationList = GetFileLocations(fileHash);

            List<string> locationPaths = new List<string>();
            
            foreach (int location in locationList)
            {
                string commandString = String.Format("select dirPath from {0} where id = {1}", ObjectStoresTable, location);
                string objectStoreRootPath = db.ExecuteSqlQueryForSingleString(commandString);
                locationPaths.Add(objectStoreRootPath);
            }

            return locationPaths;
        }

        public void setDirectoryStatus(string dirPathHash, string newStatus)
        {
            string sqlCommand = String.Format("update {0} set status = \"{1}\" where dirPathHash = \"{2}\";", OriginalDirectoriesTable, newStatus, dirPathHash);
            db.ExecuteNonQuerySql(sqlCommand);
        }

        public void setFileStatus(string fileHash, string newStatus)
        {
            string sqlCommand = String.Format("update {0} set status = \"{1}\" where filehash = \"{2}\";", FilesTable, newStatus, fileHash);
            db.ExecuteNonQuerySql(sqlCommand);
        }

        public void SetToDelete(string fileHash)
        {
            setFileStatus(fileHash, "todelete");
        }

        public void SetToLater(string fileHash)
        {
            setFileStatus(fileHash, "todoLater");
        }

        public DataTable GetListOfFilesWithExtensionInOneObjectStore(string extension, string objectStorePath)
        {
            // get id of object store
            string commandString = String.Format( "select id from objectStores where dirPath = \"{0}\";", objectStorePath);
            int? rc = db.ExecuteSqlQueryReturningSingleInt(commandString);
            if (rc == null)
                return null;

            int objectStoreId = (int)rc;

            // get list of all files with given extension in that object store
            commandString = String.Format(
                "select distinct filehash from {0} join {1} "
                + "using (filehash) where extension = \"{2}\" COLLATE NOCASE and "
                + "(objectStore1 = {3} or objectStore2 = {3} or objectStore3 = {3});", 
                OriginalDirectoriesForFileTable, "fileLocations", extension, objectStoreId );

            DataSet results = db.GetDatasetForSqlQuery(commandString);
            return results.Tables[0];
        }

        public DataSet GetListOfFilesWithExtensionMatchingSearchString(string extension, string searchString)
        {
            // get list of all files with given extension in that object store
            string commandString = String.Format(
                "select distinct filehash, filename from {0} join {1} using (dirPathHash) "
                + "where extension = \"{2}\" COLLATE NOCASE and "
                + "dirPath like \"%{3}%\"; ",
                OriginalDirectoriesForFileTable, OriginalDirectoriesTable, extension, searchString);

            return db.GetDatasetForSqlQuery(commandString);
        }

        public void MoveFileLocation(string filehash, string oldObjectStore, string newObjectStore)
        {
            int newObjectStoreID = CheckObjectStoreExistsAndInsertIfNot(newObjectStore);

            int? oldObjectStoreID = GetObjectStoreId(oldObjectStore);

            if (oldObjectStoreID != null)
                ReplaceFileLocation(filehash, (int)oldObjectStoreID, newObjectStoreID);
        }

        // Will use this to build up a query, but for now...
        public int GetNumberOfFiles()
        {
            string queryString = String.Format("select count(*) from {0}", FilesTable);
            int count = (int) db.ExecuteSqlQueryReturningSingleInt(queryString);
            return count;
        }

        // Will use this to build up a query, but for now...
        public int GetNumberOfOriginalVersionsOfFiles()
        {
            string queryString = String.Format("select count(*) from {0}", OriginalDirectoriesForFileTable);
            int count = (int)db.ExecuteSqlQueryReturningSingleInt(queryString);
            return count;
        }

        // Will use this to build up a query, but for now...
        public int GetNumberOfDirectories()
        {
            string queryString = String.Format("select count(*) from {0}", OriginalDirectoriesTable);
            int count = (int)db.ExecuteSqlQueryReturningSingleInt(queryString);
            return count;
        }

        public DataSet GetListOfExtensions(bool spaceTakenForEachType)
        {
            //// get list of all files with given extension in that object store
            //string commandString = String.Format(
            //    "select distinct filehash, filename from {0} join {1} using (dirPathHash) "
            //    + "where extension = \"{2}\" COLLATE NOCASE and "
            //    + "dirPath like \"%{3}%\"; ",
            //    OriginalDirectoriesForFileTable, OriginalDirectoriesTable, extension, searchString);

            //return db.GetDatasetForSqlQuery(commandString);

            // count of each extension only - This version does not work, counts duplicates of files
            //string commandString = String.Format("select extension, count(extension) as count from {0} group by extension order by count desc",
            //    OriginalDirectoriesForFileTable);

            string commandString = null;

            if (spaceTakenForEachType)
            {
                //// count and space taken
                // commandString = String.Format("select extension, count(extension) as fileCount, sum(filesize) as totalSpace " + 
                //    "from {0} join {1} using (filehash) group by extension order by count desc",
                //    OriginalDirectoriesForFileTable, FilesTable);
                commandString = String.Format("select extension, count(*) as fileCount, sum(filesize) as totalSize from " + 
                    " (select distinct filehash, extension, filesize from {0} join {1} using (filehash)) " +
                    " group by extension order by totalSize desc",
                    OriginalDirectoriesForFileTable, FilesTable);
            }
            else
            {
                commandString = String.Format("select extension, count(*) as fileCount from (select distinct filehash, extension " +
                    "from {0}) group by extension order by fileCount desc",
                    OriginalDirectoriesForFileTable);
            }

            return db.GetDatasetForSqlQuery(commandString);
        }

        public DataSet GetListOfExtensions(string extension, string searchString, bool countsOfEachType)
        {
            //// get list of all files with given extension in that object store
            //string commandString = String.Format(
            //    "select distinct filehash, filename from {0} join {1} using (dirPathHash) "
            //    + "where extension = \"{2}\" COLLATE NOCASE and "
            //    + "dirPath like \"%{3}%\"; ",
            //    OriginalDirectoriesForFileTable, OriginalDirectoriesTable, extension, searchString);

            //return db.GetDatasetForSqlQuery(commandString);

            // count of each extension only - This version does not work, counts duplicates of files
            //string commandString = String.Format("select extension, count(extension) as count from {0} group by extension order by count desc",
            //    OriginalDirectoriesForFileTable);

            string commandString = String.Format("select extension, count(*) as eCount from (select distinct filehash, extension, filesize " +
                "from {0} join {1} using (filehash)) group by extension order by eCount desc",
                OriginalDirectoriesForFileTable, FilesTable);

            // count and space taken - have to check if duplicates files counted or not, suspect yes
            string commandString2 = String.Format("select extension, count(extension) as count, sum(filesize) from {0} join {1} using (filehash) group by extension order by count desc",
                OriginalDirectoriesForFileTable, FilesTable);
            return null;
        }

        public List<string> GetDirPathHashListForToDeleteDirectories()
        {
            string command = String.Format("select dirPathHash from {0} where status = \"todelete\";", OriginalDirectoriesTable);
            return db.ExecuteSqlQueryForStrings(command);
        }
    }
}
