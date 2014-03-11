using ContentManagerCore;
using DbInterface;
using MpvUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
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

        private void dirList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            var selectedItem = e.AddedItems[0];
            Console.WriteLine(selectedItem.ToString());
            DataRowView trythis = selectedItem as DataRowView;
            string value = trythis.Row.ItemArray[1] as string;
            Console.WriteLine("selected dir hash: " + value);

            DataSet dirListData = databaseHelper.GetListOfFilesInOriginalDirectory(value);

            //int resultCount = dirListData.Tables[0].Rows.Count;
            //for (int i = 0; i < resultCount; i++)
            //{
            //    Console.WriteLine(dirListData.Tables[0].Rows[i][0].ToString());
            //}

            filesInDir.DataContext = dirListData.Tables[0].DefaultView;

            DataSet subdirListData = databaseHelper.GetListOfSubdirectoriesInOriginalDirectory(value);

            subdirsInDir.DataContext = subdirListData.Tables[0].DefaultView;

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            databaseHelper.CloseConnection();
        }

        private void ExtractFile(string fileHash, string filename, string destinationDir)
        {
            List<string> locations = databaseHelper.GetFileLocationPaths(fileHash);

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
            DataSet fileData = databaseHelper.GetLargestFiles(30);
            //DataSet fileData = databaseHelper.GetLargestFilesTodo(30);
            //DataSet fileData = databaseHelper.GetListOfFilesWithExtensionMatchingSearchString(".mp3", "salsa");
            fileList.DataContext = fileData.Tables[0].DefaultView;

        }

        private void objectStoreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void FillObjectStoreListBox()
        {

            DataSet storeData = databaseHelper.GetObjectStores();
            for (int i=0; i<storeData.Tables[0].Rows.Count; i++)
            {
                string location = storeData.Tables[0].Rows[i][1].ToString();
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

    }
}
