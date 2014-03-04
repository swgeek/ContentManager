using MpvUtilities;
using System;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using DbInterface;
using System.Diagnostics;

namespace PortFileInfoIntoDatabase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DbHelper databaseHelper = null;
        int filecount = 0;
        int errors = 0;
        string errorDir = null;
        Stopwatch watch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();

            string dbFileName = PortFileInfoIntoDatabase.Properties.Settings.Default.DatabaseFilePath;
            if ((dbFileName != null) && (dbFileName != String.Empty))
                databaseHelper = new DbHelper(dbFileName);

        }

       private void OnFileInfoXmlDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                fileInfoXmlDirectoryTextBlock.Text = dirname;
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
            string fileInfoDirName = fileInfoXmlDirectoryTextBlock.Text;
            string logsDirName = logsDirectoryTextBlock.Text;

            if (fileInfoDirName == String.Empty || logsDirName == String.Empty)
                return;

            if (Directory.Exists(fileInfoDirName) && Directory.Exists(logsDirName))
            {
                errorDir = System.IO.Path.Combine(fileInfoDirName, "errorFiles");
                string[] directoryList = Directory.GetDirectories(fileInfoDirName);

                foreach (string directory in directoryList)
                {
                    ProcessFilesFromDir(directory, logsDirName);
                    statusTextBlock.Text = directory + " done";
                }

                databaseHelper.CloseConnection();

                string logfileName = System.IO.Path.Combine(logsDirName, "finished.txt");
                string logText = directoryList.Length.ToString() + " directories with " + filecount + " processed, FINISHED!" + Environment.NewLine;
                logText += "From: " + fileInfoDirName + Environment.NewLine;
                logText += "Files added to database: " + databaseHelper.NumOfNewFiles + Environment.NewLine;
                logText += "directory mappings added to database: " + databaseHelper.NumOfNewDirectoryMappings + Environment.NewLine;
                logText += "Files not added as already in database: " + databaseHelper.NumOfDuplicateFiles + Environment.NewLine;
                logText += "dirs not added as already in database: " + databaseHelper.NumOfDuplicateDirectoryMappings + Environment.NewLine;
                logText += "errors: " + errors + Environment.NewLine;
                File.WriteAllText(logfileName, logText);
                statusTextBlock.Text = "FINISHED!";
            }
        }

        void StartTimer(string msg)
        {
            //Console.WriteLine(msg);
            //watch.Reset();
            //watch.Start();
        }

        void StopTimer()
        {
            //watch.Stop();
            //Console.WriteLine("Elapsed: {0}", watch.Elapsed);
            //Console.WriteLine("In milliseconds: {0}", watch.ElapsedMilliseconds);
            //Console.WriteLine("In timer ticks: {0}", watch.ElapsedTicks);
        }

        private void ProcessFilesFromDir(string fileInfoDirName, string logsDirName)
        {
            string[] fileList = Directory.GetFiles(fileInfoDirName);

            foreach (string filename in fileList)
            {
                // should be just xml files in here, skip any extra files. Production quality code should log this somewhere
                if (System.IO.Path.GetExtension(filename).ToLower() != ".xml")
                    continue;

                XDocument xdoc = null ;
                try
                {
                    xdoc = XDocument.Load(filename);
                }
                catch
                {
                    if (!Directory.Exists(errorDir))
                        Directory.CreateDirectory(errorDir);
                    
                    string filenameOnly = System.IO.Path.GetFileName(filename);
                    string newFilename = System.IO.Path.Combine(errorDir, filenameOnly);
                    File.Move(filename, newFilename);
                    errors++;
                    continue;
                }

                XElement fileElement = xdoc.Root;

                string hashValue =  System.IO.Path.GetFileNameWithoutExtension(filename);

                long filesize = 0;
                XAttribute filesizeAttribute = fileElement.Attribute("Filesize");

                if (filesizeAttribute != null)
                {
                    filesize = Int64.Parse(filesizeAttribute.Value.ToString());
                }
                else
                {
                    string objectFilePath = System.IO.Path.Combine(fileInfoDirName, hashValue);
                    if (File.Exists(objectFilePath))
                    {
                        FileInfo fileInfo = new FileInfo(objectFilePath);
                        filesize = fileInfo.Length;
                    }
                    else
                    {
                        filesize = -1;
                    }
                }

                //string status = "todo"; // fileElement.Attribute("Status").Value.ToString();

                StartTimer("Check if file in database");
                bool exists = databaseHelper.FileAlreadyInDatabase(hashValue, filesize);
                StopTimer();

                if (!exists)
                {
                    StartTimer("insert file");
                    databaseHelper.AddFile(hashValue, filesize);
                    StopTimer();
                }


                var elements = xdoc.Root.Elements("NodeInfo");
                foreach (XElement element in elements)
                {
                    string objectFileOriginalPath = element.Attribute("Fullpath").Value.ToString();
                    StartTimer("Check if fileDirectory mapping already in db");
                    bool alreadyExists = databaseHelper.FileDirectoryLocationExists(hashValue, objectFileOriginalPath);
                    StopTimer();

                    if (!alreadyExists)
                    {
                        StartTimer("insert file directory mapping");
                        databaseHelper.AddFileDirectoryLocationOld(hashValue, objectFileOriginalPath);
                        StopTimer();
                    }
                }
            }
            
            // log results
            string sourceDirNameOnly = System.IO.Path.GetFileName(fileInfoDirName);
            string logfileName = System.IO.Path.Combine(logsDirName, sourceDirNameOnly + ".txt");
            File.WriteAllText(logfileName, fileList.Length.ToString() + " files processed from " + sourceDirNameOnly);
            filecount += fileList.Length;
        }

    }
}
