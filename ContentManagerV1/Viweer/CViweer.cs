using ContentManagerCore;
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
        string currentFileHash = null;
        int currentDepotId = -1;
        string currentDepotRootPath = null;


        public CViweer(string dbFileName)
        {
            if ((dbFileName != null) && (dbFileName != String.Empty))
                databaseHelper = new DbHelper(dbFileName);

            databaseHelper.OpenConnection();
        }

        public DataView OriginalDirectoriesForFile(string filehash)
        {
            DataSet dirListData = databaseHelper.GetOriginalDirectoriesForFile(filehash);
            return dirListData.Tables[0].DefaultView;
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
            databaseHelper.SetToDelete(currentFileHash);
        }

        public DataView GetLargestFiles(bool todoFiles, bool todolaterFiles, bool todeleteFiles, bool deletedFiles)
        {
            DataSet fileData = databaseHelper.GetLargestFiles(30, todoFiles, todolaterFiles, todeleteFiles, deletedFiles);
            //DataSet fileData = databaseHelper.GetLargestFilesTodo(30);
            //DataSet fileData = databaseHelper.GetListOfFilesWithExtensionMatchingSearchString(".mp3", "salsa");
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

        public bool DeleteDirectory(string dirpathHash, string dirPath)
        {
            string pathFromDb = databaseHelper.GetDirectoryPathForDirHash(dirpathHash);

            if (! dirPath.Equals(pathFromDb))
            return false;

            databaseHelper.DeleteDirectoryAndContents(dirpathHash);
            return true;
        }

        public async Task HashFile(string filePath)
        {
            await Task.Run(() =>
            {
                string hashValue = SH1HashUtilities.HashFile(filePath);
                string objectStoreFileName = DepotPathUtilities.GetHashFilePathV2(currentDepotRootPath, hashValue);
                FileInfo fileInfo = new FileInfo(filePath);

                if (!databaseHelper.FileAlreadyInDatabase(hashValue, fileInfo.Length))
                {
                    CopyFile(filePath, hashValue);
                    // TODO: add location, size, type, maybe modified date to db under hash value
                    // TODO: add hashvalue to directory object in db. How to make directory key unique? Maybe add date or time of addition? not sure,
                    // think this one through...

                    databaseHelper.AddFile(hashValue, fileInfo.Length);
                }
                // always add directory info even if file is in db already, as may be a different copy and name

                // check this is correct call, have made some changes
                databaseHelper.AddOriginalFileLocation(hashValue, filePath);
            });
        }

        public void CopyFile(string filePath, string hashValue)
        {
            string objectStoreFileName = DepotPathUtilities.GetHashFilePathV2(currentDepotRootPath, hashValue);

            if (File.Exists(objectStoreFileName))
            {
                // technically this should not happen - we already checked the database. maybe throw an exception?
                throw new Exception(String.Format("File {0} already exists ", objectStoreFileName));
            }
            else
            {
                File.Copy(filePath, objectStoreFileName);
            }
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

        public void MarkFilesInToDeleteDirectories()
        {
            List<string> dirList = databaseHelper.GetDirPathHashListForToDeleteDirectories();


            foreach (string dirpathHash in dirList)
            {
                SetDirectoryDeleteState(dirpathHash);
            }
        }

    }
}
