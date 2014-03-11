using ContentManagerCore;
using DbInterface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomTasksUI
{
    class MiscTasks
    {
        public delegate void LogInfo(string logInfo);

        public static  void RestoreObjectStore(DbHelper databaseHelper, string objectStorePath, LogInfo logStuff)
        {
            if (objectStorePath == String.Empty)
                return;

            if (!Directory.Exists(objectStorePath))
                return;

            int objectStoreId = databaseHelper.CheckObjectStoreExistsAndInsertIfNot(objectStorePath);

            string[] directoryList = Directory.GetDirectories(objectStorePath);

            foreach (string directory in directoryList)
            {
                ProcessFilesFromDirectory(databaseHelper, directory, objectStoreId);
                if (logStuff != null)
                    logStuff("directory " + directory + "done");
            }

            logStuff("Number of new locations: " + databaseHelper.NumOfNewFileLocations);
            logStuff("Number of duplicate locations: " + databaseHelper.NumOfDuplicateFileLocations);
            if (logStuff != null)
                logStuff("finished adding files from store to db!");

            DataSet filesFromObjectStore = databaseHelper.GetFilesFromObjectStore(objectStoreId);
            int numOfFiles = filesFromObjectStore.Tables[0].Rows.Count;
            int numDeletedFromDb = 0;
            foreach (DataRow row in filesFromObjectStore.Tables[0].Rows)
            {
                string filehash = row[0].ToString();
                // if file does not exist in object store, remove location from database
                if (DepotPathUtilities.GetExistingFilePath(objectStorePath, filehash) == null)
                {
                    databaseHelper.ReplaceFileLocation(filehash, objectStoreId, null);
                    numDeletedFromDb++;
                }
            }

            String logString = String.Format("Found {0} files with this locations" + Environment.NewLine +
                "Deleted {1} references as not found on disk " + Environment.NewLine +
                " Finished cleaning up references from this objectstore", numOfFiles, numDeletedFromDb);
            if (logStuff != null)
                logStuff(logString);

        }

        static void ProcessFilesFromDirectory(DbHelper databaseHelper, string subdirPath, int objectStoreId)
        {
            foreach (string filePath in Directory.EnumerateFiles(subdirPath))
            {
                // object files should not have an extension. Sanity check
                if (System.IO.Path.HasExtension(filePath))
                    throw new Exception(filePath + "has extension, should not happen");

                // could do a sanity check and check that filesize matches that in db, but will skip for now
                // already in object store, things should be ok

                string hashValue = System.IO.Path.GetFileName(filePath);
                databaseHelper.AddFileLocation(hashValue, objectStoreId);
            }
        }
    }
}
