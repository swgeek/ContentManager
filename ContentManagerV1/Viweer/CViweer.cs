﻿using ContentManagerCore;
using DbInterface;
using MpvUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viweer
{
    class CViweer
    {
        DbHelper databaseHelper = null;
        //int currentDepotId = -1;
        //string currentDepotRootPath = null;


        public CViweer(string dbFileName)
        {
            if ((dbFileName != null) && (dbFileName != String.Empty))
                databaseHelper = new DbHelper(dbFileName);

            databaseHelper.OpenConnection();
        }

        public void CreateNewDatabase(string newDbFilePath)
        {
            DbHelper.CreateDb(newDbFilePath);
        }

        public DataView OriginalDirectoriesForFile(string filehash)
        {
            DataSet dirListData = databaseHelper.GetOriginalDirectoriesForFile(filehash);
            return dirListData.Tables[0].DefaultView;
        }

        // admittedly a bit of a hack to have a query for this as just one row,
        // maybe should build the table here, or set values some other way...
        // leave for now, works well
        public DataTable DirectoryWithDirPath(string dirPath)
        {
            return databaseHelper.GetOriginalDirectoryWithPath(dirPath);
        }

        public DataView FilesInOriginalDirectoryGivenDirPath(string dirPath)
        {
            string hash = SH1HashUtilities.HashString(dirPath);

            DataTable dirListData = databaseHelper.GetListOfFilesInOriginalDirectory(hash);
            return dirListData.DefaultView;
        }

        public DataView FilesInOriginalDirectory(string dirhash)
        {
            DataTable dirListData = databaseHelper.GetListOfFilesInOriginalDirectory(dirhash);
            return dirListData.DefaultView;
        }

        public string OriginalDirectoryPathForDirHash(string dirhash)
        {
            return databaseHelper.GetDirectoryPathForDirHash(dirhash);
        }

        public DataView SubdirectoriesInDirPath(string dirpath)
        {
            string hash = SH1HashUtilities.HashString(dirpath);
            return SubdirectoriesInOriginalDirectory(hash);
        }

        public DataView SubdirectoriesInOriginalDirectory(string dirhash)
        {
            DataTable subdirListData = databaseHelper.GetListOfSubdirectoriesInOriginalDirectory(dirhash);
            return subdirListData.DefaultView;
        }

        public void cleanup()
        {
            // as C# does not have a reliable destructor
            databaseHelper.CloseConnection();
        }


        public bool ExtractFile(string fileHash, string filename, string destinationDir)
        {
            if (string.IsNullOrEmpty(destinationDir) ||  !Directory.Exists(destinationDir))
                return false;

            List<string> locations = databaseHelper.GetObjectStorePathsForFile(fileHash);

            if (locations == null)
                return false;

            string filePath = null;
            foreach (string location in locations)
            {
                filePath = DepotPathUtilities.GetExistingFilePath(location, fileHash);
                if (filePath != null)
                    break;
            }

            if (filePath == null)
                return false;

            string newPath = System.IO.Path.Combine(destinationDir, filename);
            if (! File.Exists(newPath)) 
                File.Copy(filePath, newPath);

            return true;
        }

        public string GetFirstFilename(string filehash)
        {
            return databaseHelper.getFirstFilenameForFile(filehash);
        }

        public void DeleteFile(string filehash)
        {
            databaseHelper.SetToDelete(filehash);
        }

        public void RemoveCompletelyFile(string filehash)
        {
            databaseHelper.SetToRemoveCompletely(filehash);
        }

        public DataView GetFileList(string statusList, string extensionList, string searchTerm)
        {
           DataSet fileData = databaseHelper.GetLargestFiles(50, statusList, extensionList, searchTerm);
           // DataSet fileData = databaseHelper.GetLargestFilesTodo(30);
           // // will add the other filters in a bit, start with this...
           //DataSet fileData = databaseHelper.GetListOfFilesWithCustomQuery();
           //// DataSet fileData = databaseHelper.GetListOfFilesWithExtensionMatchingSearchString(".jpg", "candids_200");
           return fileData.Tables[0].DefaultView;
        }

       
        public DataTable ObjectStores()
        {
            return databaseHelper.GetObjectStores();
        }

        public void SetTodoLater(string filehash)
        {
            databaseHelper.SetToLater(filehash);
        }

        // should merge with DeleteDirectory, pass status in as a parameter
        public bool ChangeDirectoryStatus(string dirpathHash, string dirPath, string newStatus)
        {
            string pathFromDb = databaseHelper.GetDirectoryPathForDirHash(dirpathHash);

            if (!dirPath.Equals(pathFromDb))
                return false;

            databaseHelper.UpdateStatusForDirectoryAndContents(dirpathHash, newStatus);
            return true;
        }

        public async Task UpdateStatusForCorrespondingFile(string filePath, string newStatus)
        {
            await Task.Run(() =>
            {
                string hashValue = SH1HashUtilities.HashFile(filePath);
                FileInfo fileInfo = new FileInfo(filePath);

                if (databaseHelper.FileAlreadyInDatabase(hashValue, fileInfo.Length))
                {
                    databaseHelper.SetNewStatusIfNotDeleted(hashValue, newStatus);
                }
            }
                );
        }

        public async Task HashFile(string originalFilePath, string depotRoot, bool setAsLink, bool moveInsteadOfCopy)
        {
            await Task.Run(() =>
            {
                string hashValue = SH1HashUtilities.HashFile(originalFilePath);
                FileInfo fileInfo = new FileInfo(originalFilePath);

                if (!databaseHelper.FileAlreadyInDatabase(hashValue, fileInfo.Length))
                {
                    string objectStoreFileName = DepotPathUtilities.GetHashFilePathV2(depotRoot, hashValue);

                    if (File.Exists(objectStoreFileName))
                        // technically this should not happen - we already checked the database. maybe throw an exception?
                        throw new Exception(String.Format("File {0} already exists ", objectStoreFileName));

                    if (moveInsteadOfCopy)
                        File.Move(originalFilePath, objectStoreFileName);
                    else
                        File.Copy(originalFilePath, objectStoreFileName);

                    databaseHelper.AddFile(hashValue, fileInfo.Length);
                    databaseHelper.AddFileLocation(hashValue, depotRoot);
                    
                    // TEMPORARY~~~
                    //databaseHelper.AddOriginalFileLocation(hashValue, originalFilePath);                
                }
                else
                {
                    Console.WriteLine("TEMPORARY DEBUG OUTPUT: file already in database!");
                }

                // link file even if file in db already, as may be linking to existing file
                if (setAsLink)
                {
                    string originalFileHash = System.IO.Path.GetFileNameWithoutExtension(originalFilePath).Substring(0, 40);
                    databaseHelper.SetLink(originalFileHash, hashValue);
                }

                // always add directory info even if file is in db already, as may be a different copy and name
                // check this is correct call, have made some changes
                databaseHelper.AddOriginalFileLocation(hashValue, originalFilePath);
            });
        }


        public void SetDirectoryDeleteState(string dirpathHash)
        {
            string status = databaseHelper.GetStatusOfDirectory(dirpathHash);
            if (status.Equals("deleted"))
                return;

            bool canMarkDirectoryAsDeleted = true;

            // first do subdirectories
            string[] subDirs = databaseHelper.GetSubdirectories(dirpathHash);

            foreach (string subDirPathHash in subDirs)
            {
                SetDirectoryDeleteState(subDirPathHash);

                string newStatus = databaseHelper.GetStatusOfDirectory(subDirPathHash);
                if (!newStatus.Equals("deleted"))
                    canMarkDirectoryAsDeleted = false;
            }

            string[] files = databaseHelper.GetFileListForDirectory(dirpathHash);

            foreach (string filehash in files)
            {
                string fileStatus = databaseHelper.GetStatusOfFile(filehash);
                if (!fileStatus.Equals("deleted"))
                {
                    canMarkDirectoryAsDeleted = false;

                    if (!fileStatus.Equals("todelete"))
                    {
                        databaseHelper.setFileStatus(filehash, "todelete");
                    }
                }
            }

            if (canMarkDirectoryAsDeleted)
                databaseHelper.setDirectoryStatus(dirpathHash, "deleted");

        }

        public bool updateObjectStoreLocation(string oldPath, string newPath)
        {
            int? id = databaseHelper.GetObjectStoreId(oldPath);

            if (id == null)
                return false;

            databaseHelper.UpdateObjectStore((int)id, newPath);

            if (databaseHelper.GetObjectStoreId(newPath) != id)
                return false;

            return true;
        }

        public void AddOriginalRootDirectory(string dirPath)
        {
            databaseHelper.AddOriginalRootDirectoryIfNotInDb(dirPath);
        }

        public void UpdateDirListing(string dirPath)
        {
            string hashValue = SH1HashUtilities.HashString(dirPath);
            databaseHelper.UpdateFileAndSubDirListForDir(hashValue);

        }

        public void AddOriginalSubDirectory(string dirPath)
        {
            string parentDir = Directory.GetParent(dirPath).FullName;

            string hashValue = SH1HashUtilities.HashString(dirPath);
            string parentDirhashValue = SH1HashUtilities.HashString(parentDir);

            if (!databaseHelper.DirectoryAlreadyInDatabase(hashValue))
            {
                databaseHelper.addDirectory(hashValue, dirPath);
            }

            if (!databaseHelper.DirectoryAlreadyInDatabase(parentDirhashValue))
            {
                databaseHelper.addDirectory(parentDirhashValue, parentDir);
            }

            if (!databaseHelper.DirSubdirMappingExists(parentDirhashValue, hashValue))
                databaseHelper.AddDirSubdirMapping(parentDirhashValue, hashValue);

        }

        public List<string> GetRootDirectories()
        {
            return databaseHelper.GetRootDirectories();
        }


        public string LogMessage()
        {
            string logText = "";
            logText += "Files added to database: " + databaseHelper.NumOfNewFiles + Environment.NewLine;
            logText += "Files not added as already in database: " + databaseHelper.NumOfDuplicateFiles + Environment.NewLine;
            logText += "file locations added to database: " + databaseHelper.NumOfNewFileLocations + Environment.NewLine;
            logText += "locations not added as already in database: " + databaseHelper.NumOfDuplicateFileLocations + Environment.NewLine;
            logText += "Num of files with status change: " + databaseHelper.NumOfFilesWithStatusChange + Environment.NewLine;
            logText += "Num of files already deleted: " + databaseHelper.NumOfFilesAlreadyDeleted + Environment.NewLine;
            return logText;
        }
    }
}
