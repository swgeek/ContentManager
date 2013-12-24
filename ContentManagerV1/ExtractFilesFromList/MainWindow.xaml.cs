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

namespace ExtractFilesFromList
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

        private void OnObjectStoreDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                objectStoreDirectoryTextBlock.Text = dirname;
        }

        private void OnDestinationDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                destinationDirectoryTextBlock.Text = dirname;
        }

        private void OnFileListButtonClick(object sender, RoutedEventArgs e)
        {
            string fileName = FilePickerUtility.PickFile();
            if ((fileName != null) && (fileName != String.Empty))
                fileListFileTextBlock.Text = fileName;
        }

        private void OnProcessFilesButtonClick(object sender, RoutedEventArgs e)
        {
            string objectStoreDirectory = objectStoreDirectoryTextBlock.Text;
            string destinationDirName = destinationDirectoryTextBlock.Text;
            string fileListFileName = fileListFileTextBlock.Text;

            if (objectStoreDirectory == String.Empty || destinationDirName == String.Empty || fileListFileName == String.Empty)
                return;


            if (Directory.Exists(objectStoreDirectory) && Directory.Exists(destinationDirName) && File.Exists(fileListFileName))
            {
                string[] filesToExtract = File.ReadAllLines(fileListFileName);
                string extension = System.IO.Path.GetFileNameWithoutExtension(fileListFileName);

                string filesDirectory = System.IO.Path.Combine(objectStoreDirectory, "files");
                if (! Directory.Exists(filesDirectory))
                    throw new Exception(filesDirectory + " does not exist!");

                destinationDirName = System.IO.Path.Combine( destinationDirName, extension);
                if (! Directory.Exists(destinationDirName))
                    Directory.CreateDirectory(destinationDirName);

                foreach (string filename in filesToExtract)
                {
                    string filePath = System.IO.Path.Combine(filesDirectory, filename.Substring(0, 2), filename);
                    string newFilePath = System.IO.Path.Combine(destinationDirName, filename);
                    newFilePath += "." + extension;
                    File.Copy(filePath, newFilePath);
                }

                statusTextBlock.Text = "Finished";
            }
        }
    }
}
