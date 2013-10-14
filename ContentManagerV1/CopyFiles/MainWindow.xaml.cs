using MpvUtilities;
using System;
using System.IO;
using System.Windows;

// Just to copy files from one repo to another. 
// Good for overnight runs as have logs if PC restarts in the night
// also good as windows pretty much sucks with large copies.
// Note that this is a copy, not a merge, so will overwrite existing files, including xml files

namespace CopyFiles
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

        }

        private void OnLogsDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                logsDirectoryTextBlock.Text = dirname;

        }

        private void OnCopyFilesButtonClick(object sender, RoutedEventArgs e)
        {
            string sourceDirName = sourceDirectoryTextBlock.Text;
            string destinationDirName = destinationDirectoryTextBlock.Text;
            string logsDirName = logsDirectoryTextBlock.Text;

            if (sourceDirName == String.Empty || destinationDirName == String.Empty || logsDirectoryTextBlock.Text == String.Empty)
                return;


            if (Directory.Exists(sourceDirName) && Directory.Exists(destinationDirName) && Directory.Exists(logsDirName))
            {
                string filesDirectory = System.IO.Path.Combine(sourceDirName, "files");
                if (! Directory.Exists(filesDirectory))
                {
                    throw new Exception(filesDirectory + " does not exist!");
                }

                destinationDirName = System.IO.Path.Combine( destinationDirName, "files");
                if (! Directory.Exists(destinationDirName))
                    Directory.CreateDirectory(destinationDirName);

                string[] directoryList = Directory.GetDirectories(filesDirectory);

                foreach (string directory in directoryList)
                {
                    CopyFilesFromDir(directory, destinationDirName, logsDirName);
                    statusTextBlock.Text = directory + " copied";
                }


                string logfileName = System.IO.Path.Combine(logsDirName, "finished.txt");
                File.WriteAllText(logfileName, directoryList.Length.ToString() + " directories copied, FINISHED!");
                statusTextBlock.Text = "FINISHED!";
            }
        }

        private void CopyFilesFromDir(string sourceDir, string destinationDirRoot, string logsDirName)
        {
            string[] fileList = Directory.GetFiles(sourceDir);

            // hopefully this is ok even though directory, not file, pretty sure not passing in a trailing slash so should be ok
            string sourceDirNameOnly = System.IO.Path.GetFileName(sourceDir);
            string destinationDir = System.IO.Path.Combine(destinationDirRoot, sourceDirNameOnly);
            if (! Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            foreach (string file in fileList)
            {
                string destFileName = System.IO.Path.Combine(destinationDir, System.IO.Path.GetFileName(file));
                File.Copy(file, destFileName);
            }

            // log results
            string logfileName = System.IO.Path.Combine(logsDirName, sourceDirNameOnly + ".txt");
            File.WriteAllText(logfileName, fileList.Length.ToString() + " files copied from " + sourceDirNameOnly);
        }

    }
}
