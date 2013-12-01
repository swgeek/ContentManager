using System;
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
using ContentManagerCore;
using MpvUtilities;
using System.IO;


// Not a good name. Copies/Merges the fileInfoXML to another directory. This gives us one directory
// listing of all files in all depots. May add previews later...
namespace MoveFileInfoXml
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

        private void OnCopyXmlInfoButtonClick(object sender, RoutedEventArgs e)
        {
            copyXmlInfoButton.IsEnabled = false;

            statusTextBlock.Text = "working";

            string sourceDirName = sourceDirectoryTextBlock.Text;
            string rootDestDirName = destinationDirectoryTextBlock.Text;
            string depotName = System.IO.Path.GetFileName(sourceDirName);

            if (sourceDirName == String.Empty || rootDestDirName == String.Empty)
                return;

            if (Directory.Exists(sourceDirName) && Directory.Exists(rootDestDirName))
            {
                // get list of xml files and copy to dest dirs
                List<string> xmlInfoFileList = DepotFileLister.GetListOfXmlInfoFilesInDepot(sourceDirName);
                foreach (string filename in xmlInfoFileList)
                {
                    // copy/merge into destination dir
                    string newFilePath = DepotPathUtilities.GetXmFileInfoPath(rootDestDirName, filename);
                    CMXmlUtilities.CopyOrMergeXmlFiles(filename, newFilePath);
                }
            }

            statusTextBlock.Text = "finished";

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
    }
}
