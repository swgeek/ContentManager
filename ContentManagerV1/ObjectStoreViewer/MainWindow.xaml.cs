using ContentManagerCore;
using MpvUtilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;


namespace ObjectStoreViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string depotRoot;
        string depotName;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void PickDepotRootDirButton_Click(object sender, RoutedEventArgs e)
        {
            depotRoot = FilePickerUtility.PickDirectory();
            if ((depotRoot != null) && (depotRoot != String.Empty))
            {
                // depot name is name of directory, have to make those unique.
                depotName = System.IO.Path.GetFileName(depotRoot);

                // for now disable the button. In real version allow user to change the root directory
                ChooseDepot.Visibility = System.Windows.Visibility.Collapsed;
                directoryTreeView.Visibility = System.Windows.Visibility.Visible;

                foreach (string dirName in ContentManagerCore.DepotFileLister.GetRootDirectoriesInDepot(depotRoot))
                {
                    directoryTreeView.Items.Add(CreateTreeViewItem(dirName, dirName));
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

            DirListing listing = ContentManagerCore.DepotFileLister.GetDirListing(dirPath, depotRoot);

            // for now, reload entries every time. Inefficient, temporary. 
            senderItem.Items.Clear();

            foreach (string directory in listing.Directories)
            {
                string subdirPath = System.IO.Path.Combine(dirPath, directory);
                senderItem.Items.Add(CreateTreeViewItem(subdirPath, directory));
            }

            foreach (string filename in listing.Files)
            {
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

            ExtractFromDepot.RecursivelyRestoreFiles(depotRoot, dirPath, destinationDirectory);

            currentDirectoryTextBlock.Text = "Finished";

            Process.Start(destinationDirectory);
        }
    }
}
