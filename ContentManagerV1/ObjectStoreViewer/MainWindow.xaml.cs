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


namespace ObjectStoreViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string workingDir;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ProcessWorkingDir(string workingDir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(workingDir);
            foreach (FileInfo file in dirInfo.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
            {
                XDocument dirXml = XDocument.Load(file.FullName);
                string dirPath = dirXml.Root.Attribute("path").Value.ToString();
                TreeViewItem newItem = new TreeViewItem();
                newItem.Header = dirPath;
                directoryTreeView.Items.Add(newItem);
                newItem.Selected += rootItem_Expanded;
            }
        }

        private void ChooseWorkingDirButton_Click(object sender, RoutedEventArgs e)
        {
            workingDir = FilePickerUtility.PickDirectory();
            if ((workingDir != null) && (workingDir != String.Empty))
            {
                // for now disable the button. In real version allow user to change the root directory
                ChooseWorkingDirButton.Visibility = System.Windows.Visibility.Collapsed;
                directoryTreeView.Visibility = System.Windows.Visibility.Visible;

                foreach (string xmlFileName in Directory.GetFiles(workingDir, "*.xml"))
                {
                    // for now just put xml filename in list
                    string rootDirectory = MpvUtilities.MoreXmlUtilities.GetRootDirectoryFromXmlRootFile(xmlFileName);
                    TreeViewItem newItem = new TreeViewItem();
                    newItem.Header = rootDirectory;
                    newItem.Tag = rootDirectory;
                    newItem.Expanded += rootItem_Expanded;
                    directoryTreeView.Items.Add(newItem);
                }
            }
        }

        //private void RecursivelyLoadDirectoryIntoTree(string rootPath, TreeViewItem treeRoot)
        //{
        //    string dirhash = MpvUtilities.SH1HashUtilities.HashString(rootPath);
        //    string dirInfoPath = MpvUtilities.MiscUtilities.GetExistingHashFileName(workingDir, dirhash, ".xml");
        //    XDocument dirXml = XDocument.Load(dirInfoPath);

        //    foreach (XElement subdirXml in dirXml.Root.Elements("Subdirectory"))
        //    {
        //        string subdirName = subdirXml.Attribute("directoryName").Value.ToString();
        //        TreeViewItem newItem = new TreeViewItem();
        //        newItem.Header = subdirName + "\\";

        //        string subdirPath = System.IO.Path.Combine(rootPath, subdirName);
        //        RecursivelyLoadDirectoryIntoTree(subdirPath, newItem);
        //        treeRoot.Items.Add(newItem);

        //    }


        //    foreach (XElement fileXml in dirXml.Root.Elements("File"))
        //    {
        //        string filename = fileXml.Attribute("filename").Value.ToString();
        //        treeRoot.Items.Add(filename);
        //    }
        //}

        void rootItem_Expanded(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            TreeViewItem senderItem = sender as TreeViewItem;
            string dirPath = senderItem.Tag as string;

            string dirhash = MpvUtilities.SH1HashUtilities.HashString(dirPath);
            string dirInfoPath = MpvUtilities.MiscUtilities.GetExistingHashFileName(workingDir, dirhash, ".xml");
            XDocument dirXml = XDocument.Load(dirInfoPath);

            // for now, reload entries every time. Inefficient, temporary. 
            senderItem.Items.Clear();

            foreach (XElement subdirXml in dirXml.Root.Elements("Subdirectory"))
            {
                string subdirName = subdirXml.Attribute("directoryName").Value.ToString();
                TreeViewItem newItem = new TreeViewItem();
                newItem.Header = subdirName + "\\";

                string subdirPath = System.IO.Path.Combine(dirPath, subdirName);

                newItem.Tag = subdirPath;

                newItem.Expanded += rootItem_Expanded;

               // newItem.Items.Add("test");

                senderItem.Items.Add(newItem);

            }


            foreach (XElement fileXml in dirXml.Root.Elements("File"))
            {
                string filename = fileXml.Attribute("filename").Value.ToString();
                senderItem.Items.Add(filename);
            }       
        
        
        
        }
    }
}
