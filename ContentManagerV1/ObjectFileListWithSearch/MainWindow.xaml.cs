using ContentManagerCore;
using MpvUtilities;
using System;
using System.IO;
using System.Windows;

namespace ObjectFileListWithSearch
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

        private void ChooseDepotDirButton_Click(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                depotRootTextBlock.Text = dirname;
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            resultsListBox.Items.Clear();

            string depotRootDir = depotRootTextBlock.Text;
            if (Directory.Exists(depotRootDir))
            {
                string searchString = searchTextBox.Text;

                // probably should put the following into core
                string[] objectFileList = ContentManagerCore.DepotFileLister.GetListOfAllHashedFilesInDepot(depotRootDir);
                foreach (string filename in objectFileList)
                {
                     ObjectFileInfo fileInfo = new ObjectFileInfo(depotRootDir, filename);
                    

                     if (searchString == "*" || fileInfo.Contains(searchString))
                     {
                         resultsListBox.Items.Add(filename);
                         resultsListBox.Items.Add(fileInfo.FileSize);

                         foreach (string path in fileInfo.OriginalPaths)
                             resultsListBox.Items.Add(path);
                     }
                }
            }
        }
    }
}
