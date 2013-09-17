﻿using System;
using System.Collections.Generic;
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
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Linq;
using MpvUtilities;

namespace ContentManagerV1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string sourceDirName = String.Empty;
        private string rootDestDirName = String.Empty;
        private string objectDestDirName = String.Empty;
        private string workingDestDirName = String.Empty;

        bool stopProcessing = false;
        FileList filelist = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnGetFilesButtonClick(object sender, RoutedEventArgs e)
        {
            sourceDirName = sourceDirectoryTextBlock.Text;
            rootDestDirName = destinationDirectoryTextBlock.Text;

            if (sourceDirName == String.Empty || rootDestDirName == String.Empty)
                return;

            DirectoryInfo sourceDir = new DirectoryInfo(sourceDirName);

            if (sourceDir.Exists && Directory.Exists(rootDestDirName))
            {
                filelist = TraverseDir.GetAllFilesInDir(sourceDir);
                countRemainingTextBlock.Text = filelist.Count.ToString();
                filelistTextBox.Text = filelist.FileNames();
                getFilesButton.Visibility = System.Windows.Visibility.Collapsed;
                hashFilesbutton.Visibility = System.Windows.Visibility.Visible;

                string filesSubdirName = "files";
                objectDestDirName = System.IO.Path.Combine(rootDestDirName, filesSubdirName);
                if (!Directory.Exists(objectDestDirName))
                    Directory.CreateDirectory(objectDestDirName);

                string workingSubDirName = "working";
                workingDestDirName = System.IO.Path.Combine(rootDestDirName, workingSubDirName);
                if (!Directory.Exists(workingDestDirName))
                    Directory.CreateDirectory(workingDestDirName);

                XDocument rootDirXmlDoc = FileXmlUtilities.GenerateRootDirInfoDocument(sourceDirName);
                string rootDirXmlPath = System.IO.Path.Combine(workingDestDirName, "RootDir.xml");
                rootDirXmlDoc.Save(rootDirXmlPath);
            }
            else
            {
                // error handling
            }
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

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            stopProcessing = true;
        }

        async private void hashFilesbutton_Click(object sender, RoutedEventArgs e)
        {
            stopProcessing = false;

            if (filelist == null)
                return;

            // not the best way to do this, should probably have fileList do the hash and remove
            while (filelist.Count > 0)
            {
                if (stopProcessing)
                    break;

                string currentFile = filelist.CurrentFile();
                currentFileTextBlock.Text = currentFile;
                await HashCurrentFile(currentFile);
                filelist.RemoveCurrentFile();
                countRemainingTextBlock.Text = filelist.Count.ToString();
            }

        }

        private string GetXmlDirectoryInfoFileName(string dirPath)
        {
            string hashValue = SH1HashUtilities.HashString(dirPath);

            string subDirName = hashValue.Substring(0, 2);
            string dirName = System.IO.Path.Combine(workingDestDirName, subDirName);

            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            string workingDirName = System.IO.Path.Combine(dirName, hashValue + ".xml");

            return workingDirName;
        }

        private XDocument DoDirectoryInfoFileStuff(string dirPath, string workingDirName)
        {

            XDocument dirXml;

            if (File.Exists(workingDirName))
            {
                // Should be the same, could either sanity check or not worry about it. Don't worry for now.
                dirXml = XDocument.Load(workingDirName);
            }
            else
            {
                dirXml = FileXmlUtilities.GenerateDirInfoDocument(dirPath);
                dirXml.Save(workingDirName);
            }

            return dirXml;
        }

        private Task HashCurrentFile(string filePath)
        {

            return Task.Run(() =>
                {
                    // should do this part when building filelist, not now.
                    if (System.IO.Directory.Exists(filePath))
                    {
                        // it is a directory
                        string workingDirName = GetXmlDirectoryInfoFileName(filePath);
                        DoDirectoryInfoFileStuff(filePath, workingDirName);
                    }
                    else if (System.IO.File.Exists(filePath))
                    {
                        // it is a file

                        string hashValue = SH1HashUtilities.HashFile(filePath);

                        string subDirName = hashValue.Substring(0, 2);
                        string dirName = System.IO.Path.Combine(objectDestDirName, subDirName);

                        if (!Directory.Exists(dirName))
                            Directory.CreateDirectory(dirName);

                        string objectStoreFileName = System.IO.Path.Combine(dirName, hashValue);
                        if (File.Exists(objectStoreFileName))
                        {
                            // should be the exact same content. Should we binary check to make sure?
                            // for now, just check filesize.
                            FileInfo existingFile = new FileInfo(objectStoreFileName);
                            FileInfo newFile = new FileInfo(filePath);
                            if (newFile.Length != existingFile.Length)
                                throw new Exception("Collision with different length files - should be impossible?");
                        }
                        else
                        {
                            File.Copy(filePath, objectStoreFileName);
                        }

                        string xmlFilename = objectStoreFileName + ".xml";
                        XDocument fileXml;
                        if (File.Exists(xmlFilename))
                        {
                            // get existing
                            fileXml = XDocument.Load(xmlFilename);
                        }
                        else
                        {
                            fileXml = FileXmlUtilities.GenerateEmptyFileInfoDocument();
                        }

                        FileXmlUtilities.AddFileInfoElement(fileXml, filePath, hashValue);

                        fileXml.Save(xmlFilename);

                        // save hash value to directoryInfo file
                        string dirPath = System.IO.Path.GetDirectoryName(filePath);
                        string filename = System.IO.Path.GetFileName(filePath);

                        string workingDirName = GetXmlDirectoryInfoFileName(dirPath);
 
                        XDocument directoryInfoXmlDoc = DoDirectoryInfoFileStuff(dirPath, workingDirName);

                        var trythis = directoryInfoXmlDoc.Root.Elements("File");

                        XElement fileElement = (from element in directoryInfoXmlDoc.Root.Elements("File")
                                               where element.Attribute("filename").Value.ToString() == filename
                                               select element).Single();

                        fileElement.SetAttributeValue("Hash", hashValue);

                        directoryInfoXmlDoc.Save(workingDirName);
                    }
                    else
                    {
                        throw new Exception(filePath + " does not exist!");
                    }
                });
        }

        private void OnSaveStateAndExitButtonPress(object sender, RoutedEventArgs e)
        {
            CurrentState state = new CurrentState();
            state.sourceDirectory = sourceDirName;

            state.destinationDirectory = rootDestDirName;
            state.list = filelist.fileList.ToArray();

            string outputFileName = System.IO.Path.Combine(rootDestDirName,  "inprocess.xml");


            using (var writer = new System.IO.StreamWriter(outputFileName))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(state.GetType());
                serializer.Serialize(writer, state);
                writer.Flush();
            }
        }
    }
}
