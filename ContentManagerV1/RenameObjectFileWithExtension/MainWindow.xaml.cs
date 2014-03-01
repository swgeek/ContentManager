using ContentManagerCore;
using MpvUtilities;
using System;
using System.Collections.Generic;
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
using System.Xml.Linq;

namespace RenameObjectFileWithExtension
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

        private void OnFileInfoXmlDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                fileInfoXmlDirectoryTextBlock.Text = dirname;
        }

        private void OnDepotRootButtonClick(object sender, RoutedEventArgs e)
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
            string fileInfoDirName = fileInfoXmlDirectoryTextBlock.Text;
            string depotRootDir = depotRootDirectoryTextBlock.Text;
            string depotName = System.IO.Path.GetFileName(depotRootDir);
            string logsDirName = logsDirectoryTextBlock.Text;

            if (fileInfoDirName == String.Empty || depotRootDir == String.Empty || logsDirName == String.Empty)
                return;

            if (Directory.Exists(fileInfoDirName) && Directory.Exists(depotRootDir) && Directory.Exists(logsDirName))
            {
                {
                    string filesDirectory = System.IO.Path.Combine(depotRootDir, "files");
                    if (!Directory.Exists(filesDirectory))
                    {
                        throw new Exception(filesDirectory + " does not exist!");
                    }

                    string[] directoryList = Directory.GetDirectories(filesDirectory);

                    foreach (string directory in directoryList)
                    {
                        string dirNameOnly = System.IO.Path.GetFileName(directory);
                        string matchingFileInfoDir = System.IO.Path.Combine(fileInfoDirName, dirNameOnly);
                        ProcessFilesFromDir(directory, logsDirName);
                        statusTextBlock.Text = directory + " done";
                    }

                    string logfileName = System.IO.Path.Combine(logsDirName, "finished.txt");
                    File.WriteAllText(logfileName, directoryList.Length.ToString() + " directories done, FINISHED!");
                    statusTextBlock.Text = "FINISHED!";
                }
            }
        }

        // temporary, just to undo the file rename, i.e. remove extension
        private void ProcessFilesFromDir(string objectStoreDirName, string logsDirName)
        {
            string[] fileList = Directory.GetFiles(objectStoreDirName);

            foreach (string filename in fileList)
            {
                string filenameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filename);

                string newFilePath = System.IO.Path.Combine(objectStoreDirName, filenameWithoutExtension);
                if (File.Exists(filename) && (!File.Exists(newFilePath)))
                    File.Move(filename, newFilePath);
            }

            // log results
            string sourceDirNameOnly = System.IO.Path.GetFileName(objectStoreDirName);
            string logfileName = System.IO.Path.Combine(logsDirName, sourceDirNameOnly + ".txt");
            File.WriteAllText(logfileName, fileList.Length.ToString() + " files processed from " + sourceDirNameOnly);
        }

        private void ProcessFilesFromDirOriginal(string objectStoreDirName, string fileInfoDirName, string logsDirName)
        {
            string[] fileList = Directory.GetFiles(fileInfoDirName);

            foreach (string filename in fileList)
            {
                // should be just xml files in here, skip any extra files. Production quality code should log this somewhere
                if (System.IO.Path.GetExtension(filename).ToLower() != ".xml")
                    continue;

                XDocument xdoc = XDocument.Load(filename);

                XElement element = xdoc.Root.Elements("NodeInfo").First();

                string objectFileOriginalPath = element.Attribute("Fullpath").Value.ToString();
                string extension = System.IO.Path.GetExtension(objectFileOriginalPath).ToLower();
                if (extension == "")
                    extension = ".NoExtension";

                string objectFileName = System.IO.Path.GetFileNameWithoutExtension(filename);
                objectFileName = System.IO.Path.Combine(objectStoreDirName, objectFileName);
                string newFileName = objectFileName + extension;
                if (File.Exists(objectFileName) && (! File.Exists(newFileName)))
                    File.Move(objectFileName, newFileName);
            }
            
            // log results
            string sourceDirNameOnly = System.IO.Path.GetFileName(fileInfoDirName);
            string logfileName = System.IO.Path.Combine(logsDirName, sourceDirNameOnly + ".txt");
            File.WriteAllText(logfileName, fileList.Length.ToString() + " files processed from " + sourceDirNameOnly);
        }
    }
}
