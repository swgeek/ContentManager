using MpvUtilities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace ContentManagerV1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private string sourceDirName = String.Empty;
        //private string depotRootPath = String.Empty;
        //private string otherArchiveDbDirName = String.Empty;
        HashToDepot hasher;

        bool stopProcessing = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnGetFilesButtonClick(object sender, RoutedEventArgs e)
        {
            string sourceDirName = sourceDirectoryTextBlock.Text;
            string depotRootPath = destinationDirectoryTextBlock.Text;
            string otherArchiveDbDirName = alreadyArchivedDbDirectoryTextBlock.Text;

            if (sourceDirName == String.Empty || depotRootPath == String.Empty)
                return;

            if (otherArchiveDbDirName != String.Empty)
            {
                if (!Directory.Exists(otherArchiveDbDirName))
                    throw new Exception(otherArchiveDbDirName + " does not exist!");
            }

            if (Directory.Exists(sourceDirName) && Directory.Exists(depotRootPath))
            {
                getFilesButton.Visibility = System.Windows.Visibility.Collapsed;
                hashFilesbutton.Visibility = System.Windows.Visibility.Visible;

                hasher = new HashToDepot(sourceDirName, depotRootPath, otherArchiveDbDirName);
                int count = hasher.GetFileList();
                countRemainingTextBlock.Text = count.ToString();
            }
            else
            {
                // error handling
            }
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

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            stopProcessing = true;
        }

        async private void hashFilesbutton_Click(object sender, RoutedEventArgs e)
        {
            stopProcessing = false;

            if (hasher == null)
                return;

            // not the best way to do this, should probably have fileList do the hash and remove
            while ((hasher.PathNameOfNextFileToHash() != null) && (! stopProcessing))
            {
                currentFileTextBlock.Text = hasher.PathNameOfNextFileToHash();

                await hasher.HashNext();

                countRemainingTextBlock.Text = hasher.NumberOfFilesLeftToHash().ToString();
            }

            // finished
            hasher.LogFinished();
        }







 
        private void OnSaveStateAndExitButtonPress(object sender, RoutedEventArgs e)
        {
            hasher.SaveState();
        }

        private void OnOtherArchivesDbDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                alreadyArchivedDbDirectoryTextBlock.Text = dirname;

        }
    }
}
