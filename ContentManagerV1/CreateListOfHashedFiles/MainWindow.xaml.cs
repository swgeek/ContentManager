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
                int count = 0;
                string filesSubdirName = "files";
                string objectDirName = System.IO.Path.Combine(sourceDirName, filesSubdirName);
                if (!Directory.Exists(objectDirName))
                    throw new Exception(objectDirName + " does not exist - did you give correct root of archive directory?");

                string currentDirectoryName = string.Empty;
                for (int i = 0x00; i < 0x100; i++)
                {
                    List<string> filenameList = new List<string>();

                    currentDirectoryName = System.IO.Path.Combine(objectDirName, i.ToString("X2"));
                    if (Directory.Exists(currentDirectoryName))
                    {
                        DirectoryInfo currentDirectory = new DirectoryInfo(currentDirectoryName);

                        foreach (FileInfo file in currentDirectory.GetFiles())
                        {
                            if (file.Extension != ".xml")
                            {
                                count++;
                                //string fileNameAndSize = file.Name; // just file name for old version files
                                string fileNameAndSize = file.Name + ";;" + file.Length.ToString() + ";;" + depotName;
                                filenameList.Add(fileNameAndSize);
                            }
                        }

                        if (filenameList.Count > 0)
                        {
                            string outputFileName = System.IO.Path.Combine(rootDestDirName, i.ToString("X2")) + ".txt";

                            if (File.Exists(outputFileName))
                            {
                                string[] existingFilenames = File.ReadAllLines(outputFileName);
                                filenameList.AddRange(existingFilenames);
                            }
 
                            File.WriteAllLines(outputFileName, filenameList.Distinct().ToArray());                           
                        }
                    }
                }

                statusTextBlock.Text = count.ToString() + " files found, listed at " + rootDestDirName + ", FINISHED";
            }
        }
    }
}
