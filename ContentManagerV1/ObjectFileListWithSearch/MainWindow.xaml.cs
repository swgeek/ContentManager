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
            //resultsListBox.Items.Clear();
            string depotRootDir = depotRootTextBlock.Text;
            if (!Directory.Exists(depotRootDir))
                return;

            List<string> outputText = new List<string>();

            string searchString = searchTextBox.Text;
            if (searchString == "*")
                searchString = string.Empty;

            bool sortBySize = false;
           if (SortBySizeCheckBox.IsChecked == true)
            {
                sortBySize = true;
            }

            ObjectFileInfo[] objectFileList = DepotFileLister.SearchForFilenamesContaining(depotRootDir, searchString, sortBySize);
                
            resultsListBox.ItemsSource = objectFileList;

            foreach (ObjectFileInfo fileInfo in objectFileList)
            {
                outputText.Add(depotRootDir);
                outputText.Add(fileInfo.HashValue);
                outputText.Add(fileInfo.FileSize.ToString());
                outputText.AddRange(fileInfo.OriginalPaths);
            }
            
            File.WriteAllLines(@"C:\output\results.txt", outputText);            
        }
    }
}
