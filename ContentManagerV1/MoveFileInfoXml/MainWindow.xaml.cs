using ContentManagerCore;
using MpvUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Documents;


// Not a good name. Copies/Merges the fileInfoXML to another directory. This gives us one directory
// listing of all files in all depots. May add previews later...

// Note, think this is the actual fileInfo XML, i.e. originally in the object store, NOT the dir Info
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
                    //CMXmlUtilities.CopyOrMergeXmlFiles(filename, newFilePath);
                    CMXmlUtilities.MoveOrMergeXmlFiles(filename, newFilePath);
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
