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
        CViweer viweerHelper;
        bool stopProcessing = false;

        string currentFileHash; // temporary

        FileList filelist;

        public MainWindow()
        {
            InitializeComponent();
            string dbFileName = Viweer.Properties.Settings.Default.DatabaseFilePath;
            viweerHelper = new CViweer(dbFileName);
            this.WindowState = System.Windows.WindowState.Maximized;
        }



        static public string PickDirAndUpdateTextBlock(System.Windows.Controls.TextBlock textBlock)
        {
            string dirPath = MpvUtilities.FilePickerUtility.PickDirectory();
            if (string.IsNullOrEmpty(dirPath))
                return null;

            textBlock.Text = dirPath;
            return dirPath;
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

            DataView directoriesView = viweerHelper.OriginalDirectoriesForFile(value);
            string firstFileName = directoriesView.Table.Rows[0]["filename"].ToString();
            dirList.DataContext = directoriesView;
        }

        private void ListDirectoresForFile()
        {
            string filehash = chosenFileTextBlock.Text;

            if (string.IsNullOrEmpty(filehash))
                return;

            DataView directoriesView = viweerHelper.OriginalDirectoriesForFile(filehash);
            dirList.DataContext = directoriesView;
        }


        // maybe todo: create a dirlist and filelist type, so can use those directly instead of datatables.
        private void listDirectory(string dirhash)
        {
            filesInDir.DataContext = viweerHelper.FilesInOriginalDirectory(dirhash);
            DirNameTextBlock.Text = viweerHelper.OriginalDirectoryPathForDirHash(dirhash);
            subdirsInDir.DataContext = viweerHelper.SubdirectoriesInOriginalDirectory(dirhash);
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
            viweerHelper.cleanup();
        }

        private void OnExtractFile(object sender, RoutedEventArgs e)
        {
            string destinationDir = extractDirectoryTextBlock.Text;

            if (string.IsNullOrEmpty(destinationDir))
                return;

            const long minExtraSpaceToLeave = 10000000000;
            string drivePath = System.IO.Path.GetPathRoot(destinationDir);
            DriveInfo driveInfo = new DriveInfo(drivePath);

            bool spaceAvailable = driveInfo.AvailableFreeSpace > minExtraSpaceToLeave;

            foreach (var selectedItem in fileList.SelectedItems)
            {
                if (! spaceAvailable)
                {
                    MessageBox.Show("Problem: not enough space, change minimum or clear space", "out of space", MessageBoxButton.OK);
                    return;
                }
                DataRowView trythis = selectedItem as DataRowView;
                string filehash = trythis.Row.ItemArray[0] as string;
                string filename = viweerHelper.GetFirstFilename(filehash);
                viweerHelper.ExtractFile(filehash, filename, destinationDir);
            }

            Process.Start(extractDirectoryTextBlock.Text);
        }

        private void OnDeleteFile(object sender, RoutedEventArgs e)
        {

            foreach (var selectedItem in fileList.SelectedItems)
            {
                DataRowView trythis = selectedItem as DataRowView;
                string filehash = trythis.Row.ItemArray[0] as string;
                viweerHelper.DeleteFile(filehash);
            }

            currentFileHash = null;

            filesInDir.DataContext = null;
            dirList.DataContext = null;
            fileList.Items.Refresh();
            // clear directory list
            // reset file list
        }


        private void goButton_Click(object sender, RoutedEventArgs e)
        {
            bool todoFiles = todoFilesChoice.IsChecked == true ? true: false;
            bool todeleteFiles = todeleteFilesChoice.IsChecked == true ? true : false; 
            bool todolaterFiles = todoLaterFilesChoice.IsChecked == true ? true : false; 
            bool deletedFiles = deletedFilesChoice.IsChecked == true ? true : false;

            string inputExtensions = extensionsTextBox.Text;
            string extensionString = FormatExtensionString(inputExtensions);

            string inputSearchTerm = filenameSearchTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputSearchTerm))
                inputSearchTerm = null;
            string searchTermString = FormatSearchTerms(inputSearchTerm);

            string statusString = FormatStatusString(todoFiles, todolaterFiles, todeleteFiles, deletedFiles);

            fileList.DataContext = viweerHelper.GetFileList(statusString, extensionString, searchTermString);
        }

        // move this and others to helper, should not be here
        private string FormatStatusString(bool todoFiles, bool todolaterFiles, bool todeleteFiles, bool deletedFiles)
        {
            string statusString = "";
            if (todoFiles)
                statusString += "\'todo\', ";
            if (todolaterFiles)
                statusString += "\'todoLater\', ";
            if (todeleteFiles)
                statusString += "\'todelete\', ";
            if (deletedFiles)
                statusString += "\'deleted\', ";

            statusString = statusString.TrimEnd(',', ' ');

            if (statusString.Equals(""))
                return null;
            else
                return statusString;
        }

        private string FormatExtensionString(string inputExtensions)
        {
            string[] extensions = inputExtensions.Split(new Char[] { ' ', ',', ';', ':', '\t' });
            List<string> extensionList = new List<string>();
            foreach (string ext in extensions)
            {
                if (string.IsNullOrWhiteSpace(ext))
                    continue;

                string extension = ext.Trim();

                if (!extension.StartsWith("."))
                    extension = "." + extension;

                extension = "\"" + extension + "\"";

                extensionList.Add(extension);
            }

            if ((extensionList.Count == 0) || (extensionList.Contains("\".*\"")))
            {
                return null;
            }

            string finalExtensionString = string.Join(",", extensionList);
            return finalExtensionString;
        }

        private string FormatSearchTerms(string inputSearchString)
        {
            // for now only handle one search term
            string newSearchString = inputSearchString.Trim();
            if (newSearchString.Equals("*"))
                return null;

           // newSearchString = "\'" + newSearchString + "\'";
            return newSearchString;

            //string[] searchTerms = inputSearchString.Split(new Char[] { ' ', ',', '\t' });
            //List<string> searchTermList = new List<string>();
            //foreach (string ext in searchTerms)
            //{
            //    if (string.IsNullOrWhiteSpace(ext))
            //        continue;

            //    string term = ext.Trim();

            //    term = "\"" + term + "\"";

            //    searchTermList.Add(term);
            //}

            //if ((searchTermList.Count == 0) || (searchTermList.Contains("\"*\"")))
            //{
            //    return null;
            //}

            //string finalSearchTermString = string.Join(",", searchTermList);
            //return finalSearchTermString;
        }

        private void FillObjectStoreListBox()
        {
            DataTable storeData = viweerHelper.ObjectStores();
            objectStoreListBox.DataContext = storeData;
            //objectStoreListBox.Items.Clear();
            //for (int i=0; i<storeData.Rows.Count; i++)
            //{
            //    string location = storeData.Rows[i][1].ToString();
            //    ListBoxItem locationItem = new ListBoxItem();
            //    locationItem.Content = location;
            //    if (!Directory.Exists(location))
            //        locationItem.IsEnabled = false;
            //    objectStoreListBox.Items.Add(locationItem);
            //}
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
            viweerHelper.SetTodoLater(currentFileHash);
        }

        private void deleteDirectoryMenuItemClicked(object sender, RoutedEventArgs e)
        {
            var selectedItem = dirList.SelectedItem;
            Console.WriteLine(selectedItem.ToString());
            DataRowView chosenRowData = selectedItem as DataRowView;
            string dirpath = chosenRowData.Row.ItemArray[0] as string;

            MessageBoxResult choice =  MessageBox.Show("Delete dir " + dirpath + "?", "delete?", MessageBoxButton.OKCancel);
            if (choice != MessageBoxResult.OK)
                return;

            string dirpathHash = chosenRowData.Row.ItemArray[1] as string;

            if (!viweerHelper.ChangeDirectoryStatus(dirpathHash, dirpath, "todelete"))
                MessageBox.Show("Problem: could not delete directory " + dirpath, "Problem", MessageBoxButton.OK);
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            DataTable storeTable = viweerHelper.ObjectStores();
            List<string> objectStorePaths = new List<string>();

            for (int i = 0; i < storeTable.Rows.Count; i++ )
            {
                string location = storeTable.Rows[i]["dirPath"].ToString();
                if (Directory.Exists(location))
                    objectStorePaths.Add(location);
            }

            objectStoreComboBox.ItemsSource = objectStorePaths;
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

        async private void importFiles(string depotRoot, bool setAsLink, bool moveInsteadOfCopy)
        {
            string rootDir = directoryToImportTextBlock.Text;

            List<string> directoryList = new List<string>();

            // Should I check if there is space in the object store and give a message? Not sure...

            if (!Directory.Exists(rootDir))
                return;

            viweerHelper.AddOriginalRootDirectory(rootDir);
            directoryList.Add(rootDir);

            while ((stopProcessing == false) && (filelist != null) && (filelist.Count > 0))
            {
                string currentPath = filelist.CurrentFile();

                if (System.IO.Directory.Exists(currentPath))
                {
                    if (!currentPath.Equals(rootDir))
                    {
                        viweerHelper.AddOriginalSubDirectory(currentPath);
                        directoryList.Add(currentPath);
                        // also have to update subdirListingForDir and fileListingForDir. How to do that?
                    }
                }
                else if (System.IO.File.Exists(currentPath))
                {
                    await viweerHelper.HashFile(currentPath, depotRoot, setAsLink, moveInsteadOfCopy);
                }
                else
                {
                    throw new Exception(currentPath + " does not exist!");
                }

                filelist.RemoveCurrentFile();
                countRemainingTextBlock.Text = filelist.Count.ToString();
            }

            // work with directoryList
            foreach (string dirPath in directoryList)
            {
                viweerHelper.UpdateDirListing(dirPath);
            }

            reportTextBox.Text = viweerHelper.LogMessage();
        }

        private void startImportButton_Click(object sender, RoutedEventArgs e)
        {

            bool moveInsteadOfCopy = false;

            if (moveCheckBox.IsChecked == true)
                moveInsteadOfCopy = true; 
            
            string depotRoot = objectStoreComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(depotRoot) || !Directory.Exists(depotRoot))
                return;

            importFiles(depotRoot, false, moveInsteadOfCopy);
        }

        private void startLinkImportButton_Click(object sender, RoutedEventArgs e)
        {
            // Most of this is common code, move elsewhere
            // maybe get rid of the extra button and use a checkbox instead, so just have an import button
            bool moveInsteadOfCopy = false;
            if (moveCheckBox.IsChecked == true)
                moveInsteadOfCopy = true;

            string depotRoot = objectStoreComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(depotRoot) || !Directory.Exists(depotRoot))
                return;

            importFiles(depotRoot, true, moveInsteadOfCopy);

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

            // this is horrible, we have the hashvalue and the path, should be able to simply set instead of querying
            DataTable dirTable = viweerHelper.DirectoryWithDirPath(dirpath);
            dirList.DataContext = dirTable;

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
                string status = row["status"] as string;
                //if ((status != "deleted") && (status != "replacedByLink"))
                if (status == "todo")
                    viweerHelper.ExtractFile(filehash, filename, destinationDir);

            }

            // TODO: subdirectories

            Process.Start(destinationDir);

        }

        //   private void processDeleteDirButton_Click(object sender, RoutedEventArgs e)
        //{
        //    viweerHelper.MarkFilesInToDeleteDirectories();
        //}

           private void objectStoreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
           {

           }

           private void deleteMenuItem_Click(object sender, RoutedEventArgs e)
           {
               foreach (var selectedItem in fileList.SelectedItems)
               {
                   DataRowView trythis = selectedItem as DataRowView;
                   string filehash = trythis.Row.ItemArray[0] as string;
                   viweerHelper.DeleteFile(filehash);
               }
           }

           private void markToDoLaterMenuItem_Click(object sender, RoutedEventArgs e)
           {
               foreach (var selectedItem in fileList.SelectedItems)
               {
                   DataRowView trythis = selectedItem as DataRowView;
                   string filehash = trythis.Row.ItemArray[0] as string;
                   viweerHelper.RemoveCompletelyFile(filehash);
               }
           }

           private void removeTracesMenuItem_Click(object sender, RoutedEventArgs e)
           {

           }

           private void OnExtractDirButtonClick(object sender, RoutedEventArgs e)
           {
               string destinationDir = MpvUtilities.FilePickerUtility.PickDirectory();

               if (string.IsNullOrEmpty(destinationDir) || !Directory.Exists(destinationDir))
                   return;

               extractDirectoryTextBlock.Text = destinationDir;

           }

           private void viewFileLocationsMenuItemClicked(object sender, RoutedEventArgs e)
           {
               var selectedItem = filesInDir.SelectedItem;
               Console.WriteLine(selectedItem.ToString());
               DataRowView chosenRowData = selectedItem as DataRowView;
               string filehash = chosenRowData.Row["filehash"].ToString();
               chosenFileTextBlock.Text = filehash;
               ListDirectoresForFile();

           }

           private void updateObjectStorePathMenuItem_Click(object sender, RoutedEventArgs e)
           {

               string objectStorePath = objectStoreListBox.SelectedValue as string;

               string newPath = MpvUtilities.FilePickerUtility.PickDirectory();

               if (string.IsNullOrEmpty(newPath) || ! Directory.Exists(newPath))
                   return;

               viweerHelper.updateObjectStoreLocation(objectStorePath, newPath);
               
           }

           private void extractWithHashNameButton_Click(object sender, RoutedEventArgs e)
           {
               string destinationDir = extractDirectoryTextBlock.Text;

               if (string.IsNullOrEmpty(destinationDir))
                   return;

               foreach (var selectedItem in fileList.SelectedItems)
               {
                   DataRowView trythis = selectedItem as DataRowView;
                   string filehash = trythis.Row.ItemArray[0] as string;
                   string filename = viweerHelper.GetFirstFilename(filehash);
                   string extension = System.IO.Path.GetExtension(filename);
                   string extractFilename = filehash + extension;
                   viweerHelper.ExtractFile(filehash, extractFilename, destinationDir);
               }

               Process.Start(extractDirectoryTextBlock.Text);
           }

           private void startDeleteCorrespondingButton_Click(object sender, RoutedEventArgs e)
           {
               MessageBoxResult choice = MessageBox.Show("Delete these files?", "delete?", MessageBoxButton.OKCancel);
               if (choice != MessageBoxResult.OK)
                   return;

               updateStatusForCorrespondingFiles("todelete");
           }


           private void getRootDirsButton_Click(object sender, RoutedEventArgs e)
           {
               rootDirsListBox.IsEnabled = true;
               rootDirsListBox.DataContext = viweerHelper.GetRootDirectories();
           }

           private void rootDirsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
           {
               string selectedItem = rootDirsListBox.SelectedItem.ToString();
               Console.WriteLine(selectedItem.ToString());
               DataTable dirTable = viweerHelper.DirectoryWithDirPath(selectedItem);
               dirList.DataContext = dirTable;
           }

           private void todoLaterDirectoryMenuItemClicked(object sender, RoutedEventArgs e)
           {
               var selectedItem = dirList.SelectedItem;
               Console.WriteLine(selectedItem.ToString());
               DataRowView chosenRowData = selectedItem as DataRowView;
               string dirpath = chosenRowData.Row.ItemArray[0] as string;

               MessageBoxResult choice = MessageBox.Show("mark dir " + dirpath + " todo later?", "todolater?", MessageBoxButton.OKCancel);
               if (choice != MessageBoxResult.OK)
                   return;

               string dirpathHash = chosenRowData.Row.ItemArray[1] as string;

               if (!viweerHelper.ChangeDirectoryStatus(dirpathHash, dirpath, "toSetToTodoLater"))
                   MessageBox.Show("Problem: could not update directory " + dirpath, "Problem", MessageBoxButton.OK);
           }



           private void undeleteDirectoryMenuItemClicked(object sender, RoutedEventArgs e)
           {
               var selectedItem = dirList.SelectedItem;
               Console.WriteLine(selectedItem.ToString());
               DataRowView chosenRowData = selectedItem as DataRowView;
               string dirpath = chosenRowData.Row.ItemArray[0] as string;

               MessageBoxResult choice = MessageBox.Show("undelete dir " + dirpath + "?", "undelete?", MessageBoxButton.OKCancel);
               if (choice != MessageBoxResult.OK)
                   return;

               string dirpathHash = chosenRowData.Row.ItemArray[1] as string;

               if (!viweerHelper.ChangeDirectoryStatus(dirpathHash, dirpath, "tryToUndelete"))
                   MessageBox.Show("Problem: could not update directory " + dirpath, "Problem", MessageBoxButton.OK);

           }

           private void setToLaterCorrespondingButton_Click(object sender, RoutedEventArgs e)
           {

               MessageBoxResult choice = MessageBox.Show("set these files todo Later?", "todolater?", MessageBoxButton.OKCancel);
               if (choice != MessageBoxResult.OK)
                   return;

               updateStatusForCorrespondingFiles("todoLater");
           }

           async private void updateStatusForCorrespondingFiles(string newStatus)
           {
               string rootDir = directoryToImportTextBlock.Text;

               if (!Directory.Exists(rootDir))
                   return;

               while ((stopProcessing == false) && (filelist != null) && (filelist.Count > 0))
               {
                   string currentPath = filelist.CurrentFile();

                   // just deleting files, not directories
                   if (System.IO.File.Exists(currentPath))
                   {
                       await viweerHelper.UpdateStatusForCorrespondingFile(currentPath, newStatus);
                   }

                   filelist.RemoveCurrentFile();
                   countRemainingTextBlock.Text = filelist.Count.ToString();
               }
               reportTextBox.Text = viweerHelper.LogMessage();
           }

           private void gotoParentDirectoryMenuItemClicked(object sender, RoutedEventArgs e)
           {
               var selectedItem = dirList.SelectedItem;
               Console.WriteLine(selectedItem.ToString());
               DataRowView chosenRowData = selectedItem as DataRowView;
               string dirpath = chosenRowData.Row.ItemArray[0] as string;
               Console.WriteLine("selected dir: " + dirpath);
               string dirHash = chosenRowData.Row["dirPathHash"].ToString();

               string parentDirectory = Directory.GetParent(dirpath).FullName;

               DataTable dirTable = viweerHelper.DirectoryWithDirPath(parentDirectory);
               dirList.DataContext = dirTable;
           }

           private void newDatabaseButton_Click(object sender, RoutedEventArgs e)
           {
               if (PickDirAndUpdateTextBlock(databasePathTextBlock) != null)
               {
                   string databaseFilePath = System.IO.Path.Combine(databasePathTextBlock.Text, "db.sqlite");
                   if (File.Exists(databaseFilePath))
                   {
                       MessageBox.Show("database file already exists here, pick new location", "cannot create here", MessageBoxButton.OK);
                       return;
                   }

                    databasePathTextBlock.Text = databaseFilePath;
                    createDatabaseButton.Visibility = System.Windows.Visibility.Visible;
               }

           }

           private void performCreateDatabaseButton_Click(object sender, RoutedEventArgs e)
           {
               createDatabaseButton.Visibility = System.Windows.Visibility.Collapsed;
               //string dbFileName = Viweer.Properties.Settings.Default.DatabaseFilePath;
               string newDbFilePath = databasePathTextBlock.Text;
               if (File.Exists(newDbFilePath))
               {
                   MessageBox.Show("database file already exists here, pick new location", "cannot create here", MessageBoxButton.OK);
                   return;
               }

               viweerHelper.CreateNewDatabase(newDbFilePath);
           }

           private void browseRootDirsButton_Click(object sender, RoutedEventArgs e)
           {
               browseRootDirsButton.Visibility = System.Windows.Visibility.Collapsed;
               List<string> dirList = viweerHelper.GetRootDirectories();
               foreach (string rootDir in dirList)
                   dirTreeView.Items.Add(CreateDirectoryTreeItem(rootDir, rootDir));

           }


           private void dirTreeViewItem_Expanded(object sender, RoutedEventArgs e)
           {
               TreeViewItem item = e.Source as TreeViewItem;

               if (item == null)
                   return;

               // for now, always refresh after expanding. May change that later...

               item.Items.Clear();
               string dirpath = item.Tag as string;
               if (dirpath == null)
                   return;

               //returns dirpath, dirhash, status, I think
               DataView trythis = viweerHelper.SubdirectoriesInDirPath(dirpath);

               foreach (DataRow row in trythis.Table.Rows)
               {
                   string subdirPath = row["dirPath"] as string;
                   string subdirName = System.IO.Path.GetFileName(subdirPath);
                   item.Items.Add(CreateDirectoryTreeItem(subdirPath, subdirName));
               }
           }

           private void dirTreeViewItem_Selected(object sender, RoutedEventArgs e)
           {
               TreeViewItem item = e.Source as TreeViewItem;

               if (item == null)
                   return;

               string dirpath = item.Tag as string;
               if (dirpath == null)
                   return;

               dirNameTextBlock.Text = dirpath;
               filesInChosenDir.DataContext = viweerHelper.FilesInOriginalDirectoryGivenDirPath(dirpath);
           }

           private TreeViewItem CreateDirectoryTreeItem(string dirPath, string dirName)
           {
               // could use newDir as tag, may make life easier
               // postpone for now...
               // DirViewModel newDir = new DirViewModel(dirName, dirPath, hash, status);
               TreeViewItem treeViewItem = new TreeViewItem();
               treeViewItem.Header = dirName;
               treeViewItem.Tag = dirPath;
               treeViewItem.Items.Add("...");
               return treeViewItem;
           }

           private void viewFileLocationsForFileMenuItemClicked(object sender, RoutedEventArgs e)
           {
           }

           private void markDuplicatesForDirMenuItemClick(object sender, RoutedEventArgs e)
           {

           }

           private void removeDirFromDbCompletelyMenuItemClick(object sender, RoutedEventArgs e)
           {
               var sd = (sender as MenuItem);
               var sd2 = sd.Tag;

               TreeViewItem item = dirTreeView.SelectedItem as TreeViewItem;

               if (item == null)
                   return;

               string dirpath = item.Tag as string;
               if (dirpath == null)
                   return;

               dirNameTextBlock.Text = String.Format("removing: dirpath");

           }

           private void dirTreeView_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
           {

               var sd = e.Source as TreeViewItem;
               var sd2 = sd.Tag;
               sd.IsSelected = true;



           }





      }
}
