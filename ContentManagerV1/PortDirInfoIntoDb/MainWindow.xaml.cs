using ContentManagerCore;
using DbInterface;
using MpvUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace PortDirInfoIntoDb
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DbHelper databaseHelper = null;
        Stopwatch watch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            string dbFileName = PortDirInfoIntoDb.Properties.Settings.Default.DatabaseFilePath;
            if ((dbFileName != null) && (dbFileName != String.Empty))
                databaseHelper = new DbHelper(dbFileName);
        }

        private void OnDepotRootDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                depotRootDirectoryTextBlock.Text = dirname;
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

            string depotRoot = depotRootDirectoryTextBlock.Text;
            string logsDirName = logsDirectoryTextBlock.Text;

            if (depotRoot == String.Empty || logsDirName == String.Empty)
                return;

            string logText = "Working on: " + depotRoot + Environment.NewLine;

            if (Directory.Exists(depotRoot) && Directory.Exists(logsDirName))
            {
                foreach (string dirPath in ContentManagerCore.DepotFileLister.GetRootDirectoriesInDepot(depotRoot))
                {
                    logText += "Added " + dirPath + Environment.NewLine;
                    addDirectoryAndSubdirectories(dirPath, depotRoot);
                }
            }

            databaseHelper.CloseConnection();

            string depotName = System.IO.Path.GetFileName(depotRoot);
            string logfileName = System.IO.Path.Combine(logsDirName, depotName + "finished.txt");
            logText += "FINISHED!" + Environment.NewLine;
            logText += "Dirs added to database: " + databaseHelper.NumOfNewDirs + Environment.NewLine;
            logText += "dir subdir mappings added to database: " + databaseHelper.NumOfNewDirSubDirMappings + Environment.NewLine;
            logText += "dirs not added as already in database: " + databaseHelper.NumOfDuplicateDirs + Environment.NewLine;
            logText += "subdir mappings not added as already in database: " + databaseHelper.NumOfDuplicateDirSubDirMappings + Environment.NewLine;
            logText += "fir dir mappings added to database: " + databaseHelper.NumOfNewDirectoryMappings + Environment.NewLine;
            logText += "file dir mappings not added as already in database: " + databaseHelper.NumOfDuplicateDirectoryMappings + Environment.NewLine;

            File.WriteAllText(logfileName, logText);
            statusTextBlock.Text = "FINISHED!";
        }

        private void addDirectoryAndSubdirectories(string dirPath, string depotRoot)
        {
            string hashValue = SH1HashUtilities.HashString(dirPath);
            // add directory to directory list

            if (!databaseHelper.DirectoryAlreadyInDatabase(hashValue))
            {
                databaseHelper.addDirectory(hashValue, dirPath);
            }

            DirListing listing = ContentManagerCore.DepotFileLister.GetDirListing(dirPath, depotRoot);

            // find subdirectories, add each one to list + add  mapping for each one
            foreach (string directory in listing.Directories)
            {
                // computing hash twice for each subdir, maybe fix that if problem
                string subdirPath = System.IO.Path.Combine(dirPath, directory);
                string subdirPathHash = SH1HashUtilities.HashString(subdirPath);

                if (!databaseHelper.DirSubdirMappingExists(hashValue, subdirPathHash))
                    databaseHelper.AddDirSubdirMapping(hashValue, subdirPathHash);

                addDirectoryAndSubdirectories(subdirPath, depotRoot);
            }

            // assume file/dir mapping already in database!
            //foreach (string filename in listing.Files)
            //{
            //    Console.WriteLine(filename);
            //    // WHAT DO WE DO HERE? 
            //}           
        }
    }
}
