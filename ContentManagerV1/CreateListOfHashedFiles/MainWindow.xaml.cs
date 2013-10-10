using MpvUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using ContentManagerCore;

namespace CreateListOfHashedFiles
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

        private void OnGetFilesButtonClick(object sender, RoutedEventArgs e)
        {
            string sourceDirName = sourceDirectoryTextBlock.Text;
            string rootDestDirName = destinationDirectoryTextBlock.Text;
            string depotName = System.IO.Path.GetFileName(sourceDirName);

            if (sourceDirName == String.Empty || rootDestDirName == String.Empty)
                return;

            if (Directory.Exists(sourceDirName) && Directory.Exists(rootDestDirName))
            {
                int count = DepotFileLister.ListFiles(sourceDirName, rootDestDirName);
                statusTextBlock.Text = count.ToString() + " files found, listed at " + rootDestDirName + ", FINISHED";
            }
        }

    }
}
