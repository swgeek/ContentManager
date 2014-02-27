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

namespace Viweer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DbHelper databaseHelper = null;

        public MainWindow()
        {
            InitializeComponent();
            string dbFileName = Viweer.Properties.Settings.Default.DatabaseFilePath;
            if ((dbFileName != null) && (dbFileName != String.Empty))
                databaseHelper = new DbHelper(dbFileName);

            DataSet fileData = databaseHelper.TryThis();
            fileList.DataContext = fileData.Tables[0].DefaultView;
        }

        private void fileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems[0];
            Console.WriteLine(selectedItem.ToString());

        }

    }
}
