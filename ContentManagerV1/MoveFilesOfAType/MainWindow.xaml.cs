using ContentManagerCore;
using DbInterface;
using MpvUtilities;
using System;
using System.Data;
using System.IO;
using System.Windows;

namespace MoveFilesOfAType
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string databasePath = @"C:\depot\db.sqlite";
        DbHelper databaseHelper = null;

        public MainWindow()
        {
            InitializeComponent();

            databaseHelper = new DbHelper(databasePath);

            databaseHelper.OpenConnection();
        }

        private void sourceDirButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                sourceDirTextBlock.Text = dirname;
        }

        private void destDirButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                destDirTextBlock.Text = dirname;

        }

        private void OnProcess(object sender, RoutedEventArgs e)
        {
            string sourceDir = sourceDirTextBlock.Text;
            string destDir = destDirTextBlock.Text;

            if (sourceDir == String.Empty || destDir == String.Empty)
                return;


            if (Directory.Exists(sourceDir) && Directory.Exists(destDir))
            {
                DataTable tableOfFiles = databaseHelper.GetListOfFilesWithExtensionInOneObjectStore(".jpg", sourceDir);

                if (tableOfFiles.Rows.Count == 0)
                    return;

                for (int i = 0; i < tableOfFiles.Rows.Count; i++ )
                {
                    string filehash = tableOfFiles.Rows[i][0].ToString();

                    string originalFilePath = DepotPathUtilities.GetExistingFilePath(sourceDir, filehash);

                    if (originalFilePath == null)
                        throw new Exception(filehash + "is not in " + sourceDir + "query said it was");

                    string newPath = DepotPathUtilities.GetHashFilePathV2(destDir, filehash);
                    if (! File.Exists(newPath))
                    {
                        File.Move(originalFilePath, newPath);
                        databaseHelper.MoveFileLocation(filehash, sourceDir, destDir);
                    }
                }
            }
        }
    }
}
