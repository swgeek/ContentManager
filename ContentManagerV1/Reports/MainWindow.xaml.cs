using DbInterface;
using MpvUtilities;
using System;
using System.Data;
using System.IO;
using System.Windows;

namespace Reports
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            databaseHelper.CloseConnection();
        }

        private void reportTotalsButton_Click(object sender, RoutedEventArgs e)
        {
            int countFiles = databaseHelper.GetNumberOfFiles();
            string output = String.Format("Total number of files: {0}" + Environment.NewLine, countFiles);

            int countOriginalFiles = databaseHelper.GetNumberOfOriginalVersionsOfFiles();
            output = output + String.Format("Original locations of files: {0}" + Environment.NewLine, countOriginalFiles);

            int countDirs = databaseHelper.GetNumberOfDirectories();
            output = output + String.Format("Total number of directories: {0}" + Environment.NewLine, countDirs);

            outputTextBox.Text = output;
        }

        private void extensionListButton_Click(object sender, RoutedEventArgs e)
        {

            DataSet extensionData = databaseHelper.GetListOfExtensions(false) ;


            int resultCount = extensionData.Tables[0].Rows.Count;

            // TODO: use stringbuilder instead
            string outputText = String.Format("Number of extensions: {0}", resultCount) + Environment.NewLine;
            if (resultCount > 100)
                outputText = outputText + "Showing first 100 results " + Environment.NewLine;
            outputText = outputText + Environment.NewLine;

            for (int i = 0; i < Math.Min(100, resultCount); i++)
            {
                string outputline = String.Format("Extension: {0} NumOfFiles: {1}",
                    extensionData.Tables[0].Rows[i][0].ToString(), extensionData.Tables[0].Rows[i][1].ToString());

                outputText = outputText + outputline + Environment.NewLine;
            }

            outputTextBox.Text = outputText;
        }


        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
            {
                string now = DateTime.Now.ToLocalTime().ToString("yyMMHHmm");
                string filename = String.Format("DbReport_{0}.txt", now);
                string filepath = System.IO.Path.Combine(dirname, filename);

                File.WriteAllText(filepath, outputTextBox.Text);
            }

        }


    }
}
