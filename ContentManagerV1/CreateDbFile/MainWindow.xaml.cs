using System;
using System.Collections.Generic;
using System.Data.SQLite;
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

namespace CreateDbFile
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string dbPath = CreateDbFile.Properties.Settings.Default.DbFilePath;
            dbPathTextLabel.Content = dbPath;
        }

        private void CreateDbButtonClick(object sender, RoutedEventArgs e)
        {
            string dbFilePath = CreateDbFile.Properties.Settings.Default.DbFilePath;
            if (System.IO.File.Exists(dbFilePath))
            {
                System.Windows.MessageBox.Show("File already exists! Exiting");
                Application.Current.Shutdown();
            }
            
            SQLiteConnection.CreateFile(dbFilePath);
            //SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=C:\\Trythis\\MyDatabase3.sqlite;Version=3;");
            string connectionString = String.Format("Data Source={0};Version=3;", dbFilePath);
            SQLiteConnection dbConnection = new SQLiteConnection(connectionString);
            dbConnection.Open();

            string sql = "create table Files (hash char(40) PRIMARY KEY, filesize int, status varchar(60))";
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            command.ExecuteNonQuery();

            string filedirSqlString = "create table OriginalDirectoriesForFile (hash char(40), directoryPath varchar(500))";
            SQLiteCommand filedirCommand = new SQLiteCommand(filedirSqlString, dbConnection);
            filedirCommand.ExecuteNonQuery();

            string dirSqlString = "create table OriginalDirectorySubdirectories (directoryPath varchar(500), subdirectoryPath varchar(500))";
            SQLiteCommand dirCommand = new SQLiteCommand(dirSqlString, dbConnection);
            dirCommand.ExecuteNonQuery();

            dbConnection.Close();
        }
    }
}
