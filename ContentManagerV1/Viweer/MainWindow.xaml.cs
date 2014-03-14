using ContentManagerCore;
using DbInterface;
using MpvUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Viweer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DbHelper databaseHelper = null;
        string currentFileHash = null;
        FileList filelist;
        bool stopProcessing = false;
        int currentDepotId = -1;
        string currentDepotRootPath = null;

        public MainWindow()
        {
            InitializeComponent();

            string dbFileName = Viweer.Properties.Settings.Default.DatabaseFilePath;
            if ((dbFileName != null) && (dbFileName != String.Empty))
                databaseHelper = new DbHelper(dbFileName);

            databaseHelper.OpenConnection();

            this.WindowState = System.Windows.WindowState.Maximized;
        }

        private void fileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            var selectedItem = e.AddedItems[0];
            Console.WriteLine(selectedItem.ToString());
            DataRowView trythis = selectedItem as DataRowView;
            string value = trythis.Row.ItemArray[0] as string;
            Console.WriteLine("selected file: " + value);
            currentFileHash = value;

            DataSet dirListData = databaseHelper.GetOriginalDirectoriesForFile(value);

            //int resultCount = dirListData.Tables[0].Rows.Count;
            //for (int i = 0; i < resultCount; i++)
            //{
            //    Console.WriteLine(dirListData.Tables[0].Rows[i][0].ToString());
            //}

            dirList.DataContext = dirListData.Tables[0].DefaultView;

            // filename could be different in different dirs, 
            // get first filename in dirlist and use that for now
            string filename = dirListData.Tables[0].Rows[0]["filename"].ToString();
            filenameTextBlock.Text = filename;
        }


        // maybe todo: create a dirlist and filelist type, so can use those directly instead of datatables.
        private void listDirectory(string dirhash)
        {
            
            DataTable dirListData = databaseHelper.GetListOfFilesInOriginalDirectory(dirhash);

            string dirPath = databaseHelper.GetDirectoryPathForDirHash(dirhash);
            DirNameTextBlock.Text = dirPath;

            filesInDir.DataContext = dirListData.DefaultView;

            DataTable subdirListData = databaseHelper.GetListOfSubdirectoriesInOriginalDirectory(dirhash);

            subdirsInDir.DataContext = subdirListData.DefaultView;
        }


        private void dirList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            var selectedItem = e.AddedItems[0];
            Console.WriteLine(selectedItem.ToString());
            DataRowView trythis = selectedItem as DataRowView;
            string value = trythis.Row.ItemArray[1] as string;
            Console.WriteLine("selected dir hash: " + value);

            listDirectory(value);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            databaseHelper.CloseConnection();
        }

        private void ExtractFile(string fileHash, string filename, string destinationDir)
        {
            List<string> locations = databaseHelper.GetObjectStorePathsForFile(fileHash);

            string filePath = null;
            foreach (string location in locations)
            {
                filePath = DepotPathUtilities.GetExistingFilePath(location, fileHash);
                if (filePath != null)
                    break;
            }

            if (filePath == null)
            {
                // cannot extract, use msgbox or status field
            }
            else
            {
                string newPath = System.IO.Path.Combine(destinationDir, filename);
                if (! File.Exists(newPath))
                    File.Copy(filePath, newPath);
            }

        }

        private void OnExtractFile(object sender, RoutedEventArgs e)
        {
            if (extractDirectoryTextBlock.Text == String.Empty)
                return;

            string destinationDir = extractDirectoryTextBlock.Text;

            if (!Directory.Exists(destinationDir))
                return;

            foreach (var selectedItem in fileList.SelectedItems)
            {
                Console.WriteLine(selectedItem);
                DataRowView trythis = selectedItem as DataRowView;
                string filehash = trythis.Row.ItemArray[0] as string;
                Console.WriteLine(filehash);
                string filename = databaseHelper.getFirstFilenameForFile(filehash);
                ExtractFile(filehash, filename, destinationDir);
            }

            Process.Start(extractDirectoryTextBlock.Text);
        }

        private void OnExtractDir(object sender, RoutedEventArgs e)
        {

        }

        private void OnExtractDirButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                extractDirectoryTextBlock.Text = dirname;
        }

        private void OnDeleteFile(object sender, RoutedEventArgs e)
        {
            if (currentFileHash == null)
                return;

            databaseHelper.SetToDelete(currentFileHash);
            currentFileHash = null;
            filenameTextBlock.Text = String.Empty;

            filesInDir.DataContext = null;
            dirList.DataContext = null;
            fileList.Items.Refresh();
            // clear directory list
            // reset file list
        }

        private void OnDeleteDir(object sender, RoutedEventArgs e)
        {

        }

        private void OnExtractAllButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private void goButton_Click(object sender, RoutedEventArgs e)
        {
            bool todoFiles = todoFilesChoice.IsChecked == true ? true: false;
            bool todeleteFiles = todeleteFilesChoice.IsChecked == true ? true : false; 
            bool todolaterFiles = todoLaterFilesChoice.IsChecked == true ? true : false; 
            bool deletedFiles = deletedFilesChoice.IsChecked == true ? true : false; 

            DataSet fileData = databaseHelper.GetLargestFiles(30, todoFiles, todolaterFiles, todeleteFiles, deletedFiles);
            //DataSet fileData = databaseHelper.GetLargestFilesTodo(30);
            //DataSet fileData = databaseHelper.GetListOfFilesWithExtensionMatchingSearchString(".mp3", "salsa");
            fileList.DataContext = fileData.Tables[0].DefaultView;

        }

        private void objectStoreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void FillObjectStoreListBox()
        {

            DataTable storeData = databaseHelper.GetObjectStores();
            for (int i=0; i<storeData.Rows.Count; i++)
            {
                string location = storeData.Rows[i][1].ToString();
                ListBoxItem locationItem = new ListBoxItem();
                locationItem.Content = location;
                if (!Directory.Exists(location))
                    locationItem.IsEnabled = false;
                objectStoreListBox.Items.Add(locationItem);
            }
        }

        private void ObjectStoreFilterCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FillObjectStoreListBox();
            objectStoreListBox.IsEnabled = true;
        }

        private void ObjectStoreFilterCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            objectStoreListBox.IsEnabled = false;
        }

        private void OnLaterFile(object sender, RoutedEventArgs e)
        {
            databaseHelper.SetToLater(currentFileHash);
        }

        private void deleteItem(object sender, RoutedEventArgs e)
        {
            var trythis = e.Source;
            var trythis2 = sender;
        }

        private void markItemToDoLater(object sender, RoutedEventArgs e)
        {

        }

        private void deleteDirectoryMenuItemClicked(object sender, RoutedEventArgs e)
        {
            var selectedItem = dirList.SelectedItem;
            Console.WriteLine(selectedItem.ToString());
            DataRowView chosenRowData = selectedItem as DataRowView;
            string dirpath = chosenRowData.Row.ItemArray[0] as string;
            Console.WriteLine("selected dir: " + dirpath);

            MessageBoxResult choice =  MessageBox.Show("Delete dir " + dirpath + "?", "delete?", MessageBoxButton.OKCancel);
            if (choice != MessageBoxResult.OK)
                return;

            string dirpathHash = chosenRowData.Row.ItemArray[1] as string;
            Console.WriteLine("selected dir hashvalue: " + dirpathHash);

            string pathFromDb = databaseHelper.GetDirectoryPathForDirHash(dirpathHash);

            if (! dirpath.Equals(pathFromDb))
            {
                MessageBox.Show("Problem: path from database does not match path selected, not deleted", "Problem", MessageBoxButton.OK);
                return;
            }

            // delete directory and contents
            databaseHelper.DeleteDirectoryAndContents(dirpathHash);
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            DataTable storeTable = databaseHelper.GetObjectStores();
            List<string> objectStorePaths = new List<string>();

            for (int i = 0; i < storeTable.Rows.Count; i++ )
            {
                string location = storeTable.Rows[i]["dirPath"].ToString();
                if (Directory.Exists(location))
                    objectStorePaths.Add(location);
            }

            objectStoreComboBox.ItemsSource = objectStorePaths;
            objectStoreComboBox.SelectedIndex = 0;

        }

        private void pickDirectoryToImportButton_Click(object sender, RoutedEventArgs e)
        {
            string dirPath = MpvUtilities.FilePickerUtility.PickDirectory();
            if ((string.IsNullOrEmpty(dirPath)) || (!Directory.Exists(dirPath)))
                return;

            directoryToImportTextBlock.Text = dirPath;
        }

        private void listImportFilesButton_Click(object sender, RoutedEventArgs e)
        {
            string dirPath = directoryToImportTextBlock.Text;
            if ((string.IsNullOrEmpty(dirPath)) || (!Directory.Exists(dirPath)))
                return;

            // Get file list (with or without filesizes? not sure yet)
            // display file list
            // if not enough space in object store, maybe show a message
            // display start import button
            filelist = TraverseDir.GetAllFilesInDir(dirPath);
            fileListStackPanel.Visibility = System.Windows.Visibility.Visible;
            countRemainingTextBlock.Text = filelist.Count.ToString();

            filesToImportListBox.ItemsSource = filelist.fileList;

        }

        async private void importFiles()
        {
            string rootDir = directoryToImportTextBlock.Text;

            while ((stopProcessing == false) && (filelist != null) && (filelist.Count > 0))
            {
                string currentFile = filelist.CurrentFile();

                // should do this part when building filelist, not now.
                if (System.IO.Directory.Exists(currentFile))
                {
                    if (currentFile.Equals(rootDir))
                        ; // add as originalRootDirectory
                    else
                        ; // add as subdirectory
                }
                else if (System.IO.File.Exists(currentFile))
                {
                    await HashFile(currentFile);
                }
                else
                {
                    throw new Exception(currentFile + " does not exist!");
                }

                filelist.RemoveCurrentFile();
            }
        }

        private void startImportButton_Click(object sender, RoutedEventArgs e)
        {
            currentDepotRootPath = objectStoreComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(currentDepotRootPath) || !Directory.Exists(currentDepotRootPath))
                return;

            importFiles();
        }

        private async Task HashFile(string filePath)
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

        private void listDirectoryMenuItemClicked(object sender, RoutedEventArgs e)
        {
            var selectedItem = subdirsInDir.SelectedItem;
            Console.WriteLine(selectedItem.ToString());
            DataRowView chosenRowData = selectedItem as DataRowView;
            string dirpath = chosenRowData.Row.ItemArray[0] as string;
            Console.WriteLine("selected dir: " + dirpath);
            string dirpathHash = chosenRowData.Row.ItemArray[1] as string;
            Console.WriteLine("selected dir hashvalue: " + dirpathHash);
            listDirectory(dirpathHash);
        }

        private void extractDirectoryMenuItemClicked(object sender, RoutedEventArgs e)
        {
            if (extractDirectoryTextBlock.Text == String.Empty)
                return;

            string destinationDir = extractDirectoryTextBlock.Text;

            if (!Directory.Exists(destinationDir))
                return;

            var selectedItem = dirList.SelectedItem;
            Console.WriteLine(selectedItem.ToString());
            DataRowView chosenRowData = selectedItem as DataRowView;
            string dirpath = chosenRowData.Row.ItemArray[0] as string;
            Console.WriteLine("selected dir: " + dirpath);
            string dirHash = chosenRowData.Row["dirPathHash"].ToString();

            // if dirlist not showing this directory, then show it
            if (! DirNameTextBlock.Text.Equals(dirpath))
                listDirectory(dirHash);

            // get files in chosen directory
            // already have list displayed, so no need to query again
            DataView fileListView = filesInDir.DataContext as DataView;
            foreach  (DataRow row in fileListView.Table.Rows)
            {
                string filehash = row["filehash"] as string;
                string filename = row["filename"] as string;
                ExtractFile(filehash, filename, destinationDir);

            }

            // TODO: subdirectories

            Process.Start(destinationDir);

        }

        private void SetDirectoryDeleteState(string dirpathHash)
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

        private void MarkFilesInToDeleteDirectories()
        {
            List<string> dirList = databaseHelper.GetDirPathHashListForToDeleteDirectories();


            foreach (string dirpathHash in dirList)
            {
                SetDirectoryDeleteState(dirpathHash);
            }
        }

        private void processDeleteDirButton_Click(object sender, RoutedEventArgs e)
        {
            MarkFilesInToDeleteDirectories();
        }
    }
}
