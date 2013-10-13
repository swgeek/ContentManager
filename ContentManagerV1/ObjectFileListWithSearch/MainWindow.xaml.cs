using ContentManagerCore;
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
