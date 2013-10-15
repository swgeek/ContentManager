using ContentManagerCore;
using MpvUtilities;
using System;
using System.Collections.Generic;
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
            if (!Directory.Exists(depotRootDir))
                return;

            List<string> outputText = new List<string>();

            string searchString = searchTextBox.Text;
            if (searchString == "*")
                searchString = null;

            string[] objectFileList = DepotFileLister.GetListOfHashedFilesInDepotMatchingSearch(depotRootDir, searchString);

            foreach (string filename in objectFileList)
            {
                ObjectFileInfo fileInfo = new ObjectFileInfo(depotRootDir, filename);
                    
                resultsListBox.Items.Add(filename);
                resultsListBox.Items.Add(fileInfo.FileSize);

                foreach (string path in fileInfo.OriginalPaths)
                    resultsListBox.Items.Add(path);

                outputText.Add(depotRootDir);
                outputText.Add(filename);
                outputText.Add(fileInfo.FileSize.ToString());
                outputText.AddRange(fileInfo.OriginalPaths);
            }
            
            File.WriteAllLines(@"C:\output\results.txt", outputText);            
        }
    }
}
