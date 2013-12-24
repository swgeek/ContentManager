using MpvUtilities;
using System;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using ContentManagerCore;
using System.Xml.Linq;
using System.Text;

// Given a depot's fileinfo xml files, find all the file extensions and list the files with each extension.
// As with all projects in this solution: this is a prototype, not final quality code. 

namespace ListExtensionsAndFiles
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

        private void OnProcessFilesButtonClick(object sender, RoutedEventArgs e)
        {
            string sourceDirName = sourceDirectoryTextBlock.Text;
            string destinationDirName = destinationDirectoryTextBlock.Text;
            string logsDirName = logsDirectoryTextBlock.Text;

            if (sourceDirName == String.Empty || destinationDirName == String.Empty || logsDirectoryTextBlock.Text == String.Empty)
                return;


            if (Directory.Exists(sourceDirName) && Directory.Exists(destinationDirName) && Directory.Exists(logsDirName))
            {
                //string filesDirectory = System.IO.Path.Combine(sourceDirName, "files");
                //if (!Directory.Exists(filesDirectory))
                //{
                //    throw new Exception(filesDirectory + " does not exist!");
                //}

                string[] directoryList = Directory.GetDirectories(sourceDirName);

                foreach (string directory in directoryList)
                {
                    ProcessFilesFromDir(directory, destinationDirName, logsDirName);
                    statusTextBlock.Text = directory + " done";
                }

                string logfileName = System.IO.Path.Combine(logsDirName, "finished.txt");
                File.WriteAllText(logfileName, directoryList.Length.ToString() + " directories copied, FINISHED!");
                statusTextBlock.Text = "FINISHED!";
            }
        }

        private void ProcessFilesFromDir(string sourceDir, string destinationDirRoot, string logsDirName)
        {
            Dictionary<string, List<string>> listOfLists = new Dictionary<string, List<string>>();

            string[] fileList = Directory.GetFiles(sourceDir);

            foreach (string filename in fileList)
            {

                // should be just xml files in here, skip any extra files. Production quality code should log this somewhere
                if (Path.GetExtension(filename).ToLower() != ".xml")
                    continue;

                XDocument xdoc = XDocument.Load(filename);

                HashSet<string> allExtensions = new HashSet<string>();

                // replace this with linq query!
                foreach (XElement element in xdoc.Root.Elements("NodeInfo"))
                {
                    string objectFilePath = element.Attribute("Fullpath").Value.ToString();
                    string extension = Path.GetExtension(objectFilePath).ToLower();
                    if (extension == "")
                        extension = ".NoExtension";
                    extension = extension.Remove(0, 1);
                    allExtensions.Add(extension);
                }

                foreach (string extension in allExtensions)
                {
                    if (!listOfLists.ContainsKey(extension))
                    {
                        List<string> newList = new List<string>();
                        listOfLists.Add(extension, newList);
                    }

                    string objectFileName = System.IO.Path.GetFileNameWithoutExtension(filename);
                    listOfLists[extension].Add(objectFileName);
                }
            }

            foreach (string extension in listOfLists.Keys)
            {
                StringBuilder output = new StringBuilder();
                foreach (string objectFileName in listOfLists[extension])
                {
                    output.AppendLine(objectFileName);
                }

                string outputFilePath = System.IO.Path.Combine(destinationDirRoot, extension + ".txt");
                File.AppendAllText(outputFilePath, output.ToString());
            }
            
            // log results
            string sourceDirNameOnly = System.IO.Path.GetFileName(sourceDir);
            string logfileName = System.IO.Path.Combine(logsDirName, sourceDirNameOnly + ".txt");
            File.WriteAllText(logfileName, fileList.Length.ToString() + " files processed from " + sourceDirNameOnly);
        }

    }
}
