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
using DbInterface;
using System.Data;
using System.IO;
using ContentManagerCore;
using MpvUtilities;
using System.Diagnostics;

namespace Viweer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DbHelper databaseHelper = null;
        string currentFileHash = null;

        public MainWindow()
        {
            InitializeComponent();


            string dbFileName = Viweer.Properties.Settings.Default.DatabaseFilePath;
            if ((dbFileName != null) && (dbFileName != String.Empty))
                databaseHelper = new DbHelper(dbFileName);

            databaseHelper.OpenConnection();

            DataSet fileData = databaseHelper.TryThis();
            fileList.DataContext = fileData.Tables[0].DefaultView;

        }

        private void fileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems[0];
            Console.WriteLine(selectedItem.ToString());
            DataRowView trythis = selectedItem as DataRowView;
            string value = trythis.Row.ItemArray[0] as string;
            Console.WriteLine("selected file: " + value);
            currentFileHash = value;

            DataSet dirListData = databaseHelper.GetOriginalDirectoriesForFile(value);

            //int resultCount = dirListData.Tables[0].Rows.Count;
            //for (int i = 0; i < resultCount; i++)
            //{
            //    Console.WriteLine(dirListData.Tables[0].Rows[i][0].ToString());
            //}

            dirList.DataContext = dirListData.Tables[0].DefaultView;

            // filename could be different in different dirs, 
            // get first filename in dirlist and use that for now
            string filename = dirListData.Tables[0].Rows[0]["filename"].ToString();
            filenameTextBlock.Text = filename;
        }

        private void dirList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems[0];
            Console.WriteLine(selectedItem.ToString());
            DataRowView trythis = selectedItem as DataRowView;
            string value = trythis.Row.ItemArray[1] as string;
            Console.WriteLine("selected dir hash: " + value);

            DataSet dirListData = databaseHelper.GetListOfFilesInOriginalDirectory(value);

            //int resultCount = dirListData.Tables[0].Rows.Count;
            //for (int i = 0; i < resultCount; i++)
            //{
            //    Console.WriteLine(dirListData.Tables[0].Rows[i][0].ToString());
            //}

            otherFilesInDir.DataContext = dirListData.Tables[0].DefaultView;

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            databaseHelper.CloseConnection();
        }

        private void OnExtractFile(object sender, RoutedEventArgs e)
        {
            if (extractDirectoryTextBlock.Text == String.Empty)
                return;

            string filename = filenameTextBlock.Text;

            List<string> locations = databaseHelper.GetFileLocationPaths(currentFileHash);

            string filePath = null;
            foreach(string location in locations)
            {
                filePath = DepotPathUtilities.GetExistingFilePath(location, currentFileHash);
                if (filePath != null)
                    break;
            }

            if (filePath == null)
            {
                // cannot extract, use msgbox or status field
            }
            else
            {
                string newPath = System.IO.Path.Combine(extractDirectoryTextBlock.Text, filename);
               // File.Copy(filePath, newPath);
                Process.Start(extractDirectoryTextBlock.Text);

            }
        }

        private void OnExtractDir(object sender, RoutedEventArgs e)
        {

        }

        private void OnExtractDirButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                extractDirectoryTextBlock.Text = dirname;
        }

        private void OnDeleteFile(object sender, RoutedEventArgs e)
        {
            if (currentFileHash == null)
                return;

            databaseHelper.SetToDelete(currentFileHash);
            currentFileHash = null;
            filenameTextBlock.Text = String.Empty;

            otherFilesInDir.DataContext = null;
            dirList.DataContext = null;
            fileList.Items.Refresh();
            // clear directory list
            // reset file list
        }

    }
}
