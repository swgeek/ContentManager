using ContentManagerCore;
using DbInterface;
using MpvUtilities;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace AddFilesV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool stopProcessing = false;
        FileList filelist;
        //string sourceDirName;
        string depotRootPath;
        string dbFileName;
        DbHelper databaseHelper;

        public MainWindow()
        {
            InitializeComponent();

            string dbsetting = AddFilesV2.Properties.Settings.Default.DatabaseFilePath;
            if ((dbsetting != null) && (dbsetting != String.Empty))
                dbFileTextBlock.Text = dbsetting;

            string storeSetting = AddFilesV2.Properties.Settings.Default.ObjectStoreRootPath;
            if ((dbsetting != null) && (dbsetting != String.Empty))
                destinationDirectoryTextBlock.Text = storeSetting;


        }

        private void OnSourceDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                sourceDirectoryTextBlock.Text = dirname;
        }

        private void OnDestinationDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                destinationDirectoryTextBlock.Text = dirname;

            if (dirname != depotRootPath)
            {
                AddFilesV2.Properties.Settings.Default.ObjectStoreRootPath = dirname;
                AddFilesV2.Properties.Settings.Default.Save();
            }
        }

        // todo: make sure file extension is correct
        private void OnDbFileButtonClick(object sender, RoutedEventArgs e)
        {
            string filename = FilePickerUtility.PickFile();
            if ((filename != null) && (filename != String.Empty))
                dbFileTextBlock.Text = filename;
        }

        private void OnGetFilesButtonClick(object sender, RoutedEventArgs e)
        {
            
            string sourceDirName = sourceDirectoryTextBlock.Text;
            depotRootPath = destinationDirectoryTextBlock.Text;
            dbFileName = dbFileTextBlock.Text;

            databaseHelper = new DbHelper(dbFileName);

            if (sourceDirName == String.Empty || depotRootPath == String.Empty || dbFileName == String.Empty)
                return;

            
            if (Directory.Exists(sourceDirName) && Directory.Exists(depotRootPath))
            {
                getFilesButton.Visibility = System.Windows.Visibility.Collapsed;
                hashFilesbutton.Visibility = System.Windows.Visibility.Visible;

                // could add this list to db so saves state between runs, will decide later...
                // otoh, may not want to save state.
                filelist = TraverseDir.GetAllFilesInDir(sourceDirName);
                int count = filelist.Count;
                countRemainingTextBlock.Text = count.ToString();
            }
            else
            {
                // error handling
            }

        }

        async private void hashFilesbutton_Click(object sender, RoutedEventArgs e)
        {
            stopProcessing = false;

            while ((stopProcessing == false) && (filelist != null) && (filelist.Count > 0))
            {
                string currentFile = filelist.CurrentFile();

                // should do this part when building filelist, not now.
                if (System.IO.Directory.Exists(currentFile))
                {
                    // handle directory
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

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnSaveStateAndExitButtonPress(object sender, RoutedEventArgs e)
        {

        }

        private async Task HashFile(string filePath)
        {
            await Task.Run(() =>
            {
                string hashValue = SH1HashUtilities.HashFile(filePath);
                string objectStoreFileName = DepotPathUtilities.GetHashFilePathV2(depotRootPath, hashValue);
                if (!databaseHelper.FileAlreadyInDatabase(hashValue))
                {
                    CopyFile(filePath, hashValue);

                    FileInfo fileInfo = new FileInfo(filePath);
                    // TODO: add location, size, type, maybe modified date to db under hash value
                    // TODO: add hashvalue to directory object in db. How to make directory key unique? Maybe add date or time of addition? not sure,
                    // think this one through...

                    databaseHelper.AddFile(hashValue, fileInfo.Length);
                }
                // always add directory info even if file is in db already, as may be a different copy and name

                databaseHelper.AddFileDirectoryLocation(hashValue, filePath);
            });
        }

        public void CopyFile(string filePath, string hashValue)
        {
            string objectStoreFileName = DepotPathUtilities.GetHashFilePathV2(depotRootPath, hashValue);

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
    }
}
