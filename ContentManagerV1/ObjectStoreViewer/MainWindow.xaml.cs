using MpvUtilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;


namespace ObjectStoreViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string workingDir;
        string depotRoot;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void PickDepotRootDirButton_Click(object sender, RoutedEventArgs e)
        {
            depotRoot = FilePickerUtility.PickDirectory();
            if ((depotRoot != null) && (depotRoot != String.Empty))
            {
                // for now disable the button. In real version allow user to change the root directory
                ChooseDepot.Visibility = System.Windows.Visibility.Collapsed;
                directoryTreeView.Visibility = System.Windows.Visibility.Visible;

                workingDir = System.IO.Path.Combine(depotRoot, "working");
                if (!Directory.Exists(workingDir))
                    throw new Exception(workingDir + "does not exist");

                foreach (string xmlFileName in Directory.GetFiles(workingDir, "*.xml"))
                {
                    // for now just put xml filename in list
                    string rootDirectory = MpvUtilities.MoreXmlUtilities.GetRootDirectoryFromXmlRootFile(xmlFileName);
                    directoryTreeView.Items.Add(CreateTreeViewItem(rootDirectory, rootDirectory));
                }
            }
        }

        private TreeViewItem CreateTreeViewItem(string rootDirectory, string displayName)
        {
            TreeViewItem newItem = new TreeViewItem();
            newItem.Header = displayName + "\\";
            newItem.Tag = rootDirectory;
            newItem.Expanded += treeItem_Expanded;
            newItem.Selected += treeItem_Selected;
            return newItem;
        }

        void treeItem_Selected(object sender, RoutedEventArgs e)
        {
            currentDirectoryTextBlock.Text = (sender as TreeViewItem).Tag as string;
            e.Handled = true;
        }

        void treeItem_Expanded(object sender, RoutedEventArgs e)
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
                string subdirPath = System.IO.Path.Combine(dirPath, subdirName);
                senderItem.Items.Add(CreateTreeViewItem(subdirPath, subdirName));
            }

            foreach (XElement fileXml in dirXml.Root.Elements("File"))
            {
                string filename = fileXml.Attribute("filename").Value.ToString();
                senderItem.Items.Add(filename);
            }           
        }

        private void ChooseDestinationDirectory_Click(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                destinationDirectoryTextBlock.Text = dirname;

        }

        private void ExtractCurrentDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            string destinationDirectory = destinationDirectoryTextBlock.Text;
            if (destinationDirectory == String.Empty)
                return;

            if (!Directory.Exists(destinationDirectory))
                throw new Exception(destinationDirectory + " does not exist!");

            string dirPath = currentDirectoryTextBlock.Text;
            if (dirPath == string.Empty)
                return;

            string directoryName = System.IO.Path.GetFileName(dirPath);
            destinationDirectory = System.IO.Path.Combine(destinationDirectory, directoryName);

            MpvUtilities.ExtractFromDepot.RecursivelyRestoreFiles(depotRoot, dirPath, destinationDirectory);

            currentDirectoryTextBlock.Text = "Finished";

            Process.Start(destinationDirectory);
        }
    }
}
