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
           // databaseHelper.TransferDataToNewVersionOfTable();
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
            string objectStoreRoot = backupObjectStoreTextBlock.Text;
            if ((objectStoreRoot == null) || (!Directory.Exists(objectStoreRoot)))
                return;

            const long minExtraSpaceToLeave = 10000000000;
            int objectStoreID = databaseHelper.CheckObjectStoreExistsAndInsertIfNot(objectStoreRoot);
            string drivePath = System.IO.Path.GetPathRoot(objectStoreRoot);
            DriveInfo driveInfo = new DriveInfo(drivePath);

            bool spaceAvailable = driveInfo.AvailableFreeSpace > minExtraSpaceToLeave;

            while (spaceAvailable)
            {
                List<String> fileList = databaseHelper.GetFilesWithOnlyOneLocation(1000);
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
                        File.Copy(filePath, newFilePath);

                        // update database with new location
                        databaseHelper.AddFileLocation(filehash, objectStoreID);
                    }
                }
            }

        }

        private void backupButton_Click(object sender, RoutedEventArgs e)
        {
            string dirName = MpvUtilities.FilePickerUtility.PickDirectory();
            if ((dirName != null) && (Directory.Exists(dirName)))
            {
                backupObjectStoreTextBlock.Visibility = System.Windows.Visibility.Visible;
                backupObjectStoreTextBlock.Text = dirName;
                startBackupButton.Visibility = System.Windows.Visibility.Visible;
            }

        }


    }
}
