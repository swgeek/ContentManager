﻿using ContentManagerCore;
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

            //DataSet dirListData = databaseHelper.GetOriginalDirectoriesForFile(value);
            //dirList.DataContext = dirListData.Tables[0].DefaultView;

            //// filename could be different in different dirs, 
            //// get first filename in dirlist and use that for now
            //string filename = dirListData.Tables[0].Rows[0]["filename"].ToString();
            //filenameTextBlock.Text = filename;

            DataView directoriesView = viweerHelper.OriginalDirectoriesForFile(value);
            string firstFileName = directoriesView.Table.Rows[0]["filename"].ToString();
            dirList.DataContext = directoriesView;
            filenameTextBlock.Text = firstFileName;
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

            foreach (var selectedItem in fileList.SelectedItems)
            {
                DataRowView trythis = selectedItem as DataRowView;
                string filehash = trythis.Row.ItemArray[0] as string;
                string filename = viweerHelper.GetFirstFilename(filehash);
                viweerHelper.ExtractFile(filehash, filename, destinationDir);
            }

            Process.Start(extractDirectoryTextBlock.Text);
        }

        private void OnDeleteFile(object sender, RoutedEventArgs e)
        {
            if (currentFileHash == null)
                return;

            viweerHelper.DeleteFile(currentFileHash);

            currentFileHash = null;
            filenameTextBlock.Text = String.Empty;

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
            fileList.DataContext = viweerHelper.GetLargestFiles(todoFiles, todolaterFiles, todeleteFiles, deletedFiles);
        }

        private void FillObjectStoreListBox()
        {
            objectStoreListBox.Items.Clear();
            DataTable storeData = viweerHelper.ObjectStores();
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

            if (! viweerHelper.DeleteDirectory(dirpathHash, dirpath))
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
                    await viweerHelper.HashFile(currentFile);
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
            string currentDepotRootPath = objectStoreComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(currentDepotRootPath) || !Directory.Exists(currentDepotRootPath))
                return;

            importFiles();
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
                viweerHelper.ExtractFile(filehash, filename, destinationDir);

            }

            // TODO: subdirectories

            Process.Start(destinationDir);

        }

           private void processDeleteDirButton_Click(object sender, RoutedEventArgs e)
        {
            viweerHelper.MarkFilesInToDeleteDirectories();
        }

           private void objectStoreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
           {

           }

           private void deleteMenuItem_Click(object sender, RoutedEventArgs e)
           {

           }

           private void markToDoLaterMenuItem_Click(object sender, RoutedEventArgs e)
           {

           }

           private void OnExtractDirButtonClick(object sender, RoutedEventArgs e)
           {
               string destinationDir = MpvUtilities.FilePickerUtility.PickDirectory();

               if (string.IsNullOrEmpty(destinationDir) || !Directory.Exists(destinationDir))
                   return;

               extractDirectoryTextBlock.Text = destinationDir;

           }
    }
}
