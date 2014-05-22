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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RandomTasksUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string databasePath = @"C:\depot\db.sqlite";
        DbHelper databaseHelper = null;

        public MainWindow()
        {
            InitializeComponent();

            databaseHelper = new DbHelper(databasePath);

            databaseHelper.OpenConnection();
         }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            databaseHelper.CloseConnection();
        }

        public void LogInfo(string logInfo)
        {
            string output = outputTextBox.Text;
            output = output + Environment.NewLine + logInfo;
            outputTextBox.Text = output;
        }

        private void reportTotalsButton_Click(object sender, RoutedEventArgs e)
        {
            int countFiles = databaseHelper.GetNumberOfFiles();
            string output = String.Format("Total number of files: {0}" + Environment.NewLine, countFiles);

            int countOriginalFiles = databaseHelper.GetNumberOfOriginalVersionsOfFiles();
            output = output + String.Format("Original locations of files: {0}" + Environment.NewLine, countOriginalFiles);

            int countDirs = databaseHelper.GetNumberOfDirectories();
            output = output + String.Format("Total number of directories: {0}" + Environment.NewLine, countDirs);

            outputTextBox.Text = output;
        }

        private void extensionListButton_Click(object sender, RoutedEventArgs e)
        {

        }


        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
            {
                string now = DateTime.Now.ToLocalTime().ToString("yyMMHHmm");
                string filename = String.Format( "DbReport_{0}.txt", now);
                string filepath = System.IO.Path.Combine(dirname, filename);

                File.WriteAllText(filepath, outputTextBox.Text);
            }

        }

        private void dirList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void getObjectStoresButton_Click(object sender, RoutedEventArgs e)
        {
            DataTable fileData = databaseHelper.GetObjectStores();
            objectStoreList.DataContext = fileData.DefaultView;
        }

        private void objectStoreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void deleteStoreButton_Click(object sender, RoutedEventArgs e)
        {
            string message = "This will delete the object store " + "" + " from the database (Not the disk files).";

            if (MessageBox.Show(message, "Delete?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                DataRowView selectedRow = objectStoreList.SelectedItem as DataRowView;
                var id = (Int64) selectedRow.Row.ItemArray[0];

                int objectStoreId = (int)id;
                bool deleted = databaseHelper.DeleteObjectStore(objectStoreId);
                if (!deleted)
                    MessageBox.Show("could not delete, some files have that store as their only location", "problem", MessageBoxButton.OK);

                outputTextBox.Text = "Deleted object store " + objectStoreId;
            }

        }

        private void temporaryTaskButton_Click(object sender, RoutedEventArgs e)
        {
            //databaseHelper.DeleteLinkFiles();
            //databaseHelper.createLinkTable();
           // databaseHelper.TransferDataToNewVersionOfTable();

            //databaseHelper.SetErrorState();

            //databaseHelper.TemporarySetLinkStatusForAllFilesInLinkTable();

            TempFixErrorStateStuff();
        }

        public void TempFixErrorStateStuff()
        {
            // Lot of hard coded stuff here. Ok for now, temp fixit code, but change once reuse.
            string objectStoreRoot = @"G:\objectstore1";
            List<string> errorStateFiles = databaseHelper.TempGetErrorStateFiles();
            
            // check if file exists on drive, be safe by actually hashing the file
            // if no, remove location 1 from list
            // if yes, change error state to "todo" and remove location error from list

            foreach (string filehash in errorStateFiles)
            {
                string filepath = DepotPathUtilities.GetExistingFilePath(objectStoreRoot, filehash);

                if (filepath == null)
                {
                    // does not exist in this objectstore, remove location
                    databaseHelper.ReplaceFileLocation(filehash, 1, 23);
                }
                if (File.Exists(filepath))
                {
                    string sanitycheckHashValue = MpvUtilities.SH1HashUtilities.HashFile(filepath);
                    if (sanitycheckHashValue.Equals(filehash))
                    {


                    }
                    else
                    {
                        throw new Exception("hash values do not match");
                    }
                }
            }
 
        }

        private void repairStoreButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = objectStoreList.SelectedItem as DataRowView;
            string storePath = (string)selectedRow.Row.ItemArray[1];
            MiscTasks.RestoreObjectStore(databaseHelper, storePath, LogInfo);
        }

        private void addStoreButton_Click(object sender, RoutedEventArgs e)
        {
            string objectStorePath = MpvUtilities.FilePickerUtility.PickDirectory();

            if ((objectStorePath != null) && (Directory.Exists(objectStorePath)))
            {
                MiscTasks.RestoreObjectStore(databaseHelper, objectStorePath, LogInfo);
            }
        }

        private void startBackupButton_Click(object sender, RoutedEventArgs e)
        {
            string objectStoreRoot = chosenObjectStorePathTextBlock.Text;
            if ((objectStoreRoot == null) || (!Directory.Exists(objectStoreRoot)))
                return;

            const long minExtraSpaceToLeave = 10000000000;
            int objectStoreID = databaseHelper.CheckObjectStoreExistsAndInsertIfNot(objectStoreRoot);
            string drivePath = System.IO.Path.GetPathRoot(objectStoreRoot);
            DriveInfo driveInfo = new DriveInfo(drivePath);

            bool spaceAvailable = driveInfo.AvailableFreeSpace > minExtraSpaceToLeave;

            while (spaceAvailable)
            {
                // temporary
                List<String> fileList = databaseHelper.GetUndeletedFilesWithOnlyOneLocation(4000);

                if (fileList.Count == 0)
                    break;

                foreach (string filehash in fileList)
                {
                    // get object store path
                    List<string> objectStorePaths = databaseHelper.GetObjectStorePathsForFile(filehash);
                    if (objectStorePaths.Count != 1)
                        throw new Exception("Number of locations should be 1 but is something else for file " + filehash);

                    string filePath = DepotPathUtilities.GetExistingFilePath(objectStorePaths.First(), filehash);

                    // expecting all primary object stores to be online for now, will fix later...
                    if (filePath == null)
                        throw new Exception(filePath + "does not exist");

                    string newFilePath = DepotPathUtilities.GetHashFilePathV2(objectStoreRoot, filehash);
                    FileInfo fileInfo = new FileInfo(filePath);

                    long tempValue = driveInfo.AvailableFreeSpace;
                    // check space
                    if (driveInfo.AvailableFreeSpace < (minExtraSpaceToLeave + fileInfo.Length))
                    {
                        spaceAvailable = false;
                        break;
                    }

                    // copy file. For now, don't overwrite, but really should check filesize!
                    if (!File.Exists(newFilePath))
                    {
                        try
                        {
                            File.Copy(filePath, newFilePath);

                            // update database with new location
                            databaseHelper.AddFileLocation(filehash, objectStoreID);
                        }
                        catch
                        {
                            // error copying file, create error list
                            // for now move to error location in objectStore
                            // mark second location as ERROR for now so does not try to backup again
                            string origObjectStoreRoot = Directory.GetParent(filePath).Parent.FullName;
                            string errorDir = System.IO.Path.Combine(origObjectStoreRoot, "errors");
                            if (!Directory.Exists(errorDir))
                                Directory.CreateDirectory(errorDir);

                            string errorFilePath = System.IO.Path.Combine(errorDir, filehash);
                            File.Move(filePath, errorFilePath);
                            databaseHelper.AddFileLocation(filehash, objectStoreID);

                            // mark error in database
                            string errorObjectStoreString = "ERRORS_FROM_" + origObjectStoreRoot;
                            databaseHelper.MoveFileLocation(filehash, origObjectStoreRoot, errorObjectStoreString);
                            databaseHelper.AddFileLocation(filehash, "ERROR");
                            databaseHelper.setFileStatus(filehash, "error");
                        }
                    }
                }
            }

        }

        private void backupButton_Click(object sender, RoutedEventArgs e)
        {
            string dirName = MpvUtilities.FilePickerUtility.PickDirectory();
            if ((dirName != null) && (Directory.Exists(dirName)))
            {
                chosenObjectStorePathTextBlock.Visibility = System.Windows.Visibility.Visible;
                chosenObjectStorePathTextBlock.Text = dirName;
                startBackupButton.Visibility = System.Windows.Visibility.Visible;
            }

        }

        
        private void PropogateDirectoryStatus(string dirpathHash, string newStatus)
        {
            string status = databaseHelper.GetStatusOfDirectory(dirpathHash);
            if ( (status != null) &&  (status.Equals("deleted") || status.Equals("replacedByLink")) )
                return;

            bool canMarkDirectoryAsDeleted = true;
            bool canMarkDirectoryAsTodoLater = true;

            // first do subdirectories
            string[] subDirs = databaseHelper.GetSubdirectories(dirpathHash);

            foreach (string subDirPathHash in subDirs)
            {
                PropogateDirectoryStatus(subDirPathHash, newStatus);

                string subdirState = databaseHelper.GetStatusOfDirectory(subDirPathHash);
                if ((status != null) &&  !(subdirState.Equals("deleted") || subdirState.Equals("replacedByLink")))
                    canMarkDirectoryAsDeleted = false;

                if ((status != null) &&  !(subdirState.Equals("todoLater") || subdirState.Equals("deleted") || subdirState.Equals("replacedByLink")))
                    canMarkDirectoryAsTodoLater = false;
            }

            string[] files = databaseHelper.GetFileListForDirectory(dirpathHash);

            foreach (string filehash in files)
            {
                string fileStatus = databaseHelper.GetStatusOfFile(filehash);
                if (fileStatus.Equals("deleted") || fileStatus.Equals("replacedByLink"))
                    continue;

                databaseHelper.setFileStatus(filehash, newStatus);

                // get new status
                fileStatus = databaseHelper.GetStatusOfFile(filehash);

                if (! (fileStatus.Equals("deleted") || fileStatus.Equals("replacedByLink")))
                {
                    canMarkDirectoryAsDeleted = false;      

                    if (! fileStatus.Equals("todoLater"))
                        canMarkDirectoryAsTodoLater = false;
                }
            }

            if (canMarkDirectoryAsTodoLater)
                databaseHelper.setDirectoryStatus(dirpathHash, "todoLater");

            if (canMarkDirectoryAsDeleted)
                databaseHelper.setDirectoryStatus(dirpathHash, "deleted");
        }

        private bool PropogateTryUndeleteDirectoryStatus(string dirpathHash)
        {
            bool successfullyUndeleted = true;

            // first do subdirectories
            string[] subDirs = databaseHelper.GetSubdirectories(dirpathHash);

            foreach (string subDirPathHash in subDirs)
            {
                bool success = PropogateTryUndeleteDirectoryStatus(subDirPathHash);
                if (!success)
                    successfullyUndeleted = false;
            }

            string[] files = databaseHelper.GetFileListForDirectory(dirpathHash);

            foreach (string filehash in files)
            {
                string fileStatus = databaseHelper.GetStatusOfFile(filehash);
                if (fileStatus.Equals("deleted"))
                    databaseHelper.setFileStatus(filehash, "tryToUndelete");

                if (fileStatus.Equals("deleted") || fileStatus.Equals("tryToUndelete"))
                    successfullyUndeleted = false;
            }


            if (successfullyUndeleted) 
            {
                string status = databaseHelper.GetStatusOfDirectory(dirpathHash);
                if (status.Equals("tryToUndelete") || status.Equals("deleted"))
                    databaseHelper.setDirectoryStatus(dirpathHash, "todo"); 
            }

            return successfullyUndeleted;
        }

        private void PropogateStatusChangesforDirectories()
        {
            List<string> dirList = databaseHelper.GetDirPathHashListForDirectoriesWithStatus("todelete");

            foreach (string dirpathHash in dirList)
            {
                PropogateDirectoryStatus(dirpathHash, "todelete");
            }

            dirList = databaseHelper.GetDirPathHashListForDirectoriesWithStatus("toSetToTodoLater");

            foreach (string dirpathHash in dirList)
            {
                PropogateDirectoryStatus(dirpathHash, "todoLater");
            }

            dirList = databaseHelper.GetDirPathHashListForDirectoriesWithStatus("tryToUndelete");

            foreach (string dirpathHash in dirList)
            {
                PropogateTryUndeleteDirectoryStatus(dirpathHash);
            }
        }

 

        private void createTableButton_Click(object sender, RoutedEventArgs e)
        {
            databaseHelper.CreateNewMappingTables2();
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            // make sure files in "todelete" subdirectories are marked for delete
            PropogateStatusChangesforDirectories();

            List<string> todeleteFileList = databaseHelper.GetListOfFilesToDelete();

            foreach (string filehash in todeleteFileList)
            {
                List<string> objectStoresForFile = databaseHelper.GetObjectStorePathsForFile(filehash);

                if (objectStoresForFile == null)
                    continue; // just skip for now, but should investigate at some point
                // make a copy so can remove items without causing a problem with my foreach loop
                List<string> objectStoresForFileCopy = new List<string>(objectStoresForFile);

                foreach (string objectStoreRoot in objectStoresForFileCopy)
                {
                    // check object store is available
                    if (Directory.Exists(objectStoreRoot))
                    {
                        string filePath = DepotPathUtilities.GetExistingFilePath(objectStoreRoot, filehash);
                        if (!File.Exists(filePath))
                            // skip for now, but should really find out why
                            continue;
                           // throw new Exception(filePath + " does not exist!");

                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                        int objectStoreId = (int)databaseHelper.GetObjectStoreId(objectStoreRoot);
                        databaseHelper.ReplaceFileLocation(filehash, objectStoreId, null);
                        objectStoresForFile.Remove(objectStoreRoot);
                    }
                }

                // should also check FileLink table! if deleting link then set original to delete. 
                // if deleteing something that has a link then set that to delete as well...
                if (objectStoresForFile.Count == 0)
                {
                    // yay, removed all copies
                    databaseHelper.setFileStatus(filehash, "deleted");
                }
            }
 
            // update directories where all files have been deleted
            PropogateStatusChangesforDirectories();

        }

        private void deleteStoreAndReferencesButton_Click(object sender, RoutedEventArgs e)
        {

            string message = "This will delete the object store " + "" + "  and references";

            if (MessageBox.Show(message, "Delete?", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;


            int objectStoreIdToDelete;
            string objectStoreRootToDelete;
            GetChosenObjectStore(out objectStoreIdToDelete, out objectStoreRootToDelete);

            // make sure files in "todelete" subdirectories are marked for delete
            PropogateStatusChangesforDirectories();

            List<string> todeleteFileList = databaseHelper.GetListOfFilesToDeleteWithLocation(objectStoreIdToDelete);

            foreach (string filehash in todeleteFileList)
            {
                List<string> objectStoresForFile = databaseHelper.GetObjectStorePathsForFile(filehash);
                if (objectStoresForFile == null)
                    continue; // need to research this, or maybe remove, but for now...

                // make a copy so can remove items without causing a problem with my foreach loop
                List<string> objectStoresForFileCopy = new List<string>(objectStoresForFile);

                foreach (string objectStoreRoot in objectStoresForFileCopy)
                {
                    if (objectStoreRoot.Equals(objectStoreRootToDelete))
                    {
                        // remove this location
                        databaseHelper.ReplaceFileLocation(filehash, objectStoreIdToDelete, null);
                        objectStoresForFile.Remove(objectStoreRoot);
                    }
                }

                if (objectStoresForFile.Count == 0)
                {
                    // no other copies
                    databaseHelper.setFileStatus(filehash, "deleted");
                }
            }

            bool deleted = databaseHelper.DeleteObjectStore(objectStoreIdToDelete);
            if (!deleted)
                MessageBox.Show("could not delete, some files have that store as their only location", "problem", MessageBoxButton.OK);
            else
                outputTextBox.Text = "Deleted object store " + objectStoreIdToDelete;
        }

        private void GetChosenObjectStore(out int objectStoreId, out string objectStoreRoot)
        {
            DataRowView selectedRow = objectStoreList.SelectedItem as DataRowView;
            var id = (Int64)selectedRow.Row.ItemArray[0];

            objectStoreId = (int)id;
            objectStoreRoot = selectedRow.Row["dirPath"].ToString();
        }

        private void MoveStoreContentsButton_Click(object sender, RoutedEventArgs e)
        {
            UserChooseObjectStoreRoot();
            startMoveButton.Visibility = System.Windows.Visibility.Visible;
        }


        private void startMoveButton_Click(object sender, RoutedEventArgs e)
        {
            string destinationObjectStoreRoot = chosenObjectStorePathTextBlock.Text;
            if ((destinationObjectStoreRoot == null) || (!Directory.Exists(destinationObjectStoreRoot)))
                return;

            int destinationStoreID = databaseHelper.CheckObjectStoreExistsAndInsertIfNot(destinationObjectStoreRoot);
            string drivePath = System.IO.Path.GetPathRoot(destinationObjectStoreRoot);
            DriveInfo driveInfo = new DriveInfo(drivePath);

            const long minExtraSpaceToLeave = 20000000000;
            bool spaceAvailable = driveInfo.AvailableFreeSpace > minExtraSpaceToLeave;

            if (!spaceAvailable)
                return;

            DataRowView selectedRow = objectStoreList.SelectedItem as DataRowView;
            var id = (Int64)selectedRow.Row.ItemArray[0];

            int sourceStoreId = (int)id;
            string sourceStorePath = selectedRow.Row["dirPath"].ToString();
            DirectoryInfo rootDirInfo = new DirectoryInfo(sourceStorePath);

            foreach (DirectoryInfo subDirInfo in rootDirInfo.GetDirectories() )
            {
                if (! spaceAvailable)
                    break;

                List<FileInfo> fileList = subDirInfo.GetFiles().ToList();

                //// temp, to save time on exceptions
                //if (fileList.Count < 70)
                //    continue;

                foreach (FileInfo fileInfo in fileList)
                {
                     if (driveInfo.AvailableFreeSpace < (minExtraSpaceToLeave + fileInfo.Length))
                        {
                            spaceAvailable = false;
                            break;
                        }

                    string filehash = fileInfo.Name;
                    List<int> fileLocations = databaseHelper.GetFileLocations(filehash);

                    if (fileLocations == null)
                        continue; // not in database, simply ignore this file

                    if (fileLocations.Contains(destinationStoreID))
                    {
                        if (DeleteFile(fileInfo))
                            databaseHelper.ReplaceFileLocation(filehash,sourceStoreId, null);
                    }
                    else
                    {
                        // skip the copies for now, doing the dups first
                        string newFilePath = DepotPathUtilities.GetHashFilePathV2(destinationObjectStoreRoot, filehash);

                        // check space
                        long tempValue = driveInfo.AvailableFreeSpace;                
                        if (driveInfo.AvailableFreeSpace < (minExtraSpaceToLeave + fileInfo.Length))
                        {
                            spaceAvailable = false;
                            break;
                        }

                        // this part needs to be a single transaction, should change sometime. Will risk for now...
                        if (MoveFile(fileInfo, newFilePath))
                            databaseHelper.ReplaceFileLocation(filehash, sourceStoreId, destinationStoreID);
                    }
                }
            }
        }


        public bool MoveFile(FileInfo fileInfo, string newFilePath)
        {
            if (File.Exists(newFilePath))
                throw new Exception("file already exists"); // inconsistency in database!

            try
            {
                fileInfo.MoveTo(newFilePath);
            }
            catch
            {
                // error copying file
                // for now just skip this file
                return false;
            }

            return true;
        }

        public bool DeleteFile(FileInfo fileInfo)
        {
            try
            {
                fileInfo.Delete();
            }
            catch
            {
                // error deleting file
                // for now just skip this file
                return false;
            }

            return true;
        }

        private void findErrorFilesButton_Click(object sender, RoutedEventArgs e)
        {
            UserChooseObjectStoreRoot();
            startFindErrorFilesButton.Visibility = System.Windows.Visibility.Visible;
        }

        private void UserChooseObjectStoreRoot()
        {
            string dirName = MpvUtilities.FilePickerUtility.PickDirectory();
            if ((dirName != null) && (Directory.Exists(dirName)))
            {
                chosenObjectStorePathTextBlock.Visibility = System.Windows.Visibility.Visible;
                chosenObjectStorePathTextBlock.Text = dirName;
            }
        }

        private void startFindErrorFilesButton_Click(object sender, RoutedEventArgs e)
        {
            string backupStoreRoot = chosenObjectStorePathTextBlock.Text;
            int objectStoreId;
            string objectStorePath;
            GetChosenObjectStore(out objectStoreId, out objectStorePath);
            // get list of error files
            // for each file, check if in store
            // if yes, copy to current store and update location
            // if no, just skip, nothing special
            List<string> errorFiles = databaseHelper.TempGetErrorStateFiles();

            foreach (string filehash in errorFiles)
            {
                if (System.IO.Path.HasExtension(filehash))
                {
                    string extension = System.IO.Path.GetExtension(filehash);
                    // should remove from database here...
                    databaseHelper.RemoveFileCompletely(filehash);
                    continue;
                }
                string filepath = DepotPathUtilities.GetExistingFilePath(backupStoreRoot, filehash);
                if (File.Exists(filepath))
                {
                    string objectStoreFileName = DepotPathUtilities.GetHashFilePathV2(objectStorePath, filehash);

                    if (File.Exists(objectStoreFileName))
                        continue;
                        // technically this should not happen - we already checked the database. maybe throw an exception?
                       // throw new Exception(String.Format("File {0} already exists ", objectStoreFileName));

                    File.Copy(filepath, objectStoreFileName);

                    databaseHelper.ReplaceFileLocation(filehash, 23, objectStoreId);
                }
            }
        }

        private void fileLocationlessFilesButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void processDirectoryStatusChangeButton_Click(object sender, RoutedEventArgs e)
        {
            PropogateStatusChangesforDirectories();
        }

        private void undeleteButton_Click(object sender, RoutedEventArgs e)
        {
            UserChooseObjectStoreRoot();
            startTryUndeleteButton.Visibility = System.Windows.Visibility.Visible;
        }

        // similar code to find error files, so maybe combine...
        private void startTryUndeleteButton_Click(object sender, RoutedEventArgs e)
        {
            string backupStoreRoot = chosenObjectStorePathTextBlock.Text;
            int objectStoreId;
            string objectStorePath;
            GetChosenObjectStore(out objectStoreId, out objectStorePath);

            // get list of files to undelete
            // for each file, check if it is in the backup store
            // if yes, copy to current store and update location

            List<string> undeleteFiles = databaseHelper.GetListOfFilesToTryUnDelete();

            foreach (string filehash in undeleteFiles)
            {
                if (System.IO.Path.HasExtension(filehash))
                {
                    // should never happen, will remove this code eventually, but checking first
                    string extension = System.IO.Path.GetExtension(filehash);
                    // should remove from database here...
                    databaseHelper.RemoveFileCompletely(filehash);
                    continue;
                }
                string filepath = DepotPathUtilities.GetExistingFilePath(backupStoreRoot, filehash);
                if (File.Exists(filepath))
                {
                    string objectStoreFileName = DepotPathUtilities.GetHashFilePathV2(objectStorePath, filehash);

                    if (File.Exists(objectStoreFileName))
                        continue;
                    // technically this should not happen - we already checked the database. maybe throw an exception?
                    // throw new Exception(String.Format("File {0} already exists ", objectStoreFileName));

                    File.Copy(filepath, objectStoreFileName);

                    // the next two steps really should be atomic. Figure out how to do that.
                    databaseHelper.setFileStatus(filehash, "todo");
                    databaseHelper.AddFileLocation(filehash, objectStoreId);

                }
            }

        }

        private void DeleteExtraTodoButton_Click(object sender, RoutedEventArgs e)
        {
            // get drive path
            string extraBackupStorePath = MpvUtilities.FilePickerUtility.PickDirectory();

            if ((extraBackupStorePath != null) && (Directory.Exists(extraBackupStorePath)))
            {
                string[] directoryList = Directory.GetDirectories(extraBackupStorePath);
                foreach (string directory in directoryList)
                {
                    // go through each file in directory, if status=todo then delete file
                    string[] fileList = Directory.GetFiles(directory);
                    foreach (string file in fileList)
                    {
                        string extension = System.IO.Path.GetExtension(file);
                        if (extension != null && extension == ".xml")
                        {
                            File.Delete(file);
                            continue;
                        }

                        string filehash = System.IO.Path.GetFileName(file);
                        // if status == todo
                        // ADD SAFETY CHECK THAT THIS IS NOT PRIMARY LOCATION OR A BACKUP!!!
                        string status = databaseHelper.getFileStatus(filehash);
                        if (status.Equals("todo"))
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                        }
                    }
               }
            }
        }
    }
}
