using DbInterface;
using MpvUtilities;
using System;
using System.Collections.Generic;
using System.Data;
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

namespace ManageStoreLocations
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string databasePath = @"C:\depot\db.sqlite";
        DbHelper databaseHelper = null;
        int currentId = -1;

        public MainWindow()
        {
            InitializeComponent();
            databaseHelper = new DbHelper(databasePath);
            databaseHelper.OpenConnection();

            DataSet objectStoreData = databaseHelper.GetObjectStores();
            objectStores.DataContext = objectStoreData.Tables[0].DefaultView;
        }

        private void objectStore_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            var selectedItem = e.AddedItems[0];
            DataRowView trythis = selectedItem as DataRowView;
            long id = (long) trythis.Row.ItemArray[0];
            currentId = (int)id;
            string dirPath = (string)trythis.Row.ItemArray[1];

            if (!Directory.Exists(dirPath))
                dirPath = dirPath + " (dir does not exist)";
            pathTextBlock.Text = dirPath;

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            databaseHelper.CloseConnection();
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                pathTextBlock.Text = dirname;

            if (Directory.Exists(pathTextBlock.Text))
                 databaseHelper.UpdateObjectStore(currentId, pathTextBlock.Text);

        }

    }
}
