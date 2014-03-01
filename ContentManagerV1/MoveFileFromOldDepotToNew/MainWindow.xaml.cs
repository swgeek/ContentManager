using ContentManagerCore;
using DbInterface;
using MpvUtilities;
using System;
using System.IO;
using System.Windows;

namespace MoveFileFromOldDepotToNew
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DbHelper databaseHelper = null;
        int filesMoved = 0;
        int filesAlreadyInStore = 0;
        int fileCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            string dbFileName = Properties.Settings.Default.DatabaseFilePath;
            if ((dbFileName != null) && (dbFileName != String.Empty))
                databaseHelper = new DbHelper(dbFileName);
        }

        private void OnSourceDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                sourceDirectoryTextBlock.Text = dirname;
        }

        private void OnDestDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                destDirectoryTextBlock.Text = dirname;
        }

        private void OnLogsDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                logsDirectoryTextBlock.Text = dirname;
        }

        private void OnProcessFilesButtonClick(object sender, RoutedEventArgs e)
        {
            databaseHelper.OpenConnection();
            string sourceDirectory = sourceDirectoryTextBlock.Text;
            string destDirectory = destDirectoryTextBlock.Text;
            string logsDirName = logsDirectoryTextBlock.Text;

            if (sourceDirectory == String.Empty || destDirectory == String.Empty || logsDirName == String.Empty)
                return;

            if (Directory.Exists(sourceDirectory) && Directory.Exists(destDirectory) && Directory.Exists(logsDirName))
            {
                databaseHelper.CheckObjectStoreExistsAndInsertIfNot(destDirectory);

                string[] directoryList = Directory.GetDirectories(sourceDirectory);

                foreach (string directory in directoryList)
                {
                    
                    ProcessFilesFromDir(directory, destDirectory, logsDirName);
                    statusTextBlock.Text = directory + " done";
                }

                databaseHelper.CloseConnection();

                string logfileName = System.IO.Path.Combine(logsDirName, "finished.txt");
                string logText = directoryList.Length.ToString() + " directories with " + fileCount + " processed, FINISHED!" + Environment.NewLine;
                logText += "Files moved: " + filesMoved + Environment.NewLine;
                logText += "Files not moved as already in object store: " + filesAlreadyInStore + Environment.NewLine;
                logText += "Files added to database: " + databaseHelper.NumOfNewFiles + Environment.NewLine;
                logText += "Files not added as already in database: " + databaseHelper.NumOfDuplicateFiles + Environment.NewLine;
                logText += "file locations added to database: " + databaseHelper.NumOfNewFileLocations + Environment.NewLine;
                logText += "locations not added as already in database: " + databaseHelper.NumOfDuplicateFileLocations + Environment.NewLine;
                File.WriteAllText(logfileName, logText);
                statusTextBlock.Text = "FINISHED!";
            }
        }

        private void ProcessFilesFromDir(string sourceSubDir, string objectStoreRoot, string logsDirName)
        {
            string[] fileList = Directory.GetFiles(sourceSubDir);

            foreach (string filePath in fileList)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                long filesize = fileInfo.Length;
                string fileName = fileInfo.Name;
                string sanityCheck = System.IO.Path.GetFileName(filePath);

                string destinationPath = DepotPathUtilities.GetHashFilePathV2(objectStoreRoot, fileName); 

                if (File.Exists(destinationPath))
                {
                    filesAlreadyInStore++;
                    FileInfo existingFileInfo = new FileInfo(destinationPath);
                    if (existingFileInfo.Length != filesize)
                        throw new Exception("file already exists, but different size!");
                }
                else
                {
                    filesMoved++;
                    File.Move(filePath, destinationPath);
                }

                bool exists = databaseHelper.FileAlreadyInDatabase(fileName, filesize);
 
                if (!exists)
                {
                    databaseHelper.AddFile(fileName, filesize);
                }

                // add file location to database
                databaseHelper.AddFileLocation(fileName, objectStoreRoot);
            }

            // log results
            string sourceDirNameOnly = System.IO.Path.GetFileName(sourceSubDir);
            string logfileName = System.IO.Path.Combine(logsDirName, sourceDirNameOnly + ".txt");
            File.WriteAllText(logfileName, fileList.Length.ToString() + " files processed from " + sourceDirNameOnly);
            fileCount += fileList.Length;
        }
    }
}
