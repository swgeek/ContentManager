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

namespace MoveXmlFilesToXmlDir
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

        private void OnCopyXmlFilesButtonClick(object sender, RoutedEventArgs e)
        {
            string sourceDirName = sourceDirectoryTextBlock.Text;
            string rootDestDirName = destinationDirectoryTextBlock.Text;

            if (sourceDirName == String.Empty || rootDestDirName == String.Empty)
                return;


            if (Directory.Exists(sourceDirName) && Directory.Exists(rootDestDirName))
            {
                List<string> xmlFileList = GetXmlFileList(sourceDirName);
                CopyXmlFilesToDestDir(rootDestDirName, xmlFileList);

                statusTextBlock.Text = xmlFileList.Count.ToString() + " xml files found, Now adding filesizes to xml";

                Dictionary<string, long> filesizeList = GetFilesizeList(sourceDirName);
                AddFileSizesToXml(rootDestDirName, filesizeList);
                statusTextBlock.Text = statusTextBlock.Text + "\r\n" +  filesizeList.Count.ToString() + " regular files found \r\n FINISHED!";
            }

        }

        private void AddFileSizesToXml(string rootDestDirName, Dictionary<string, long> filesizeList)
        {
            foreach (string filename in filesizeList.Keys)
            {
                string xmlFilePath = System.IO.Path.Combine(MpvUtilities.MiscUtilities.GetOrCreateDirectoryForHashName(filename, rootDestDirName), 
                                                             filename + ".xml");

                XDocument xmlDoc = XDocument.Load(xmlFilePath);

                XElement newElement = new XElement("FileInfo");
                newElement.Add(new XAttribute("length", filesizeList[filename]));

                MpvUtilities.MoreXmlUtilities.AddElementToXml(xmlDoc.Root, newElement);

                xmlDoc.Save(xmlFilePath);
            }
        }

        private void CopyXmlFilesToDestDir(string rootDestDirName, List<string> xmlFileList)
        {
            foreach (string fullPathName in xmlFileList)
            {
                string filename = System.IO.Path.GetFileName(fullPathName);      
                string destinationDir = MpvUtilities.MiscUtilities.GetOrCreateDirectoryForHashName(filename, rootDestDirName);
                string destinationPath = System.IO.Path.Combine(destinationDir, filename);
                if (File.Exists(destinationPath))
                {
                    MpvUtilities.MoreXmlUtilities.MergeXmlFiles(fullPathName, destinationPath);
                }
                else
                {
                    File.Copy(fullPathName, destinationPath);
                }
            }
        }

        private List<string> GetXmlFileList(string sourceDirName)
        {
            List<string> xmlFileList = new List<string>();

            string filesSubdirName = "files";
            string objectDirName = System.IO.Path.Combine(sourceDirName, filesSubdirName);

            if (!Directory.Exists(objectDirName))
                throw new Exception(objectDirName + " does not exist - did you give correct root of archive directory?");

            string currentDirectoryName = string.Empty;

            for (int i = 0x00; i < 0x100; i++)
            {

                currentDirectoryName = System.IO.Path.Combine(objectDirName, i.ToString("X2"));
                if (Directory.Exists(currentDirectoryName))
                {
                    DirectoryInfo currentDirectory = new DirectoryInfo(currentDirectoryName);

                    foreach (FileInfo file in currentDirectory.GetFiles())
                    {
                        if (file.Extension == ".xml")
                        {
                            xmlFileList.Add(file.FullName);
                        }
                    }
                }
            }
            return xmlFileList;
        }

        private Dictionary<string, long> GetFilesizeList(string sourceDirName)
        {
            Dictionary<string, long> filesizeList = new Dictionary<string, long>();

            string filesSubdirName = "files";
            string objectDirName = System.IO.Path.Combine(sourceDirName, filesSubdirName);

            if (!Directory.Exists(objectDirName))
                throw new Exception(objectDirName + " does not exist - did you give correct root of archive directory?");

            string currentDirectoryName = string.Empty;

            for (int i = 0x00; i < 0x100; i++)
            {

                currentDirectoryName = System.IO.Path.Combine(objectDirName, i.ToString("X2"));
                if (Directory.Exists(currentDirectoryName))
                {
                    DirectoryInfo currentDirectory = new DirectoryInfo(currentDirectoryName);

                    foreach (FileInfo file in currentDirectory.GetFiles())
                    {
                        if (file.Extension == "")
                        {
                            filesizeList.Add(file.Name, file.Length);
                        }
                    }
                }
            }
            return filesizeList;
        }
    }
}
