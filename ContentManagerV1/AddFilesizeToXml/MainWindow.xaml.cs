using MpvUtilities;
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


// TEMPORARY Project.
// Original code did not add filesize to the xml file, this will go to the old depots and fill in that field.
// future versions should not need this, filesize should be added when initially hash file
namespace AddFilesizeToXml
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

        private void AddFilesizesButton_Click(object sender, RoutedEventArgs e)
        {
            string depotRootDir = depotRootTextBlock.Text;
            if ((depotRootDir != null) && (depotRootDir != String.Empty))
            {
                AddFilesizesButton.IsEnabled = false;
                statusTextBlock.Text = "Processing";
            }
            else
            {
                statusTextBlock.Text = depotRootDir + "does not exist, pick new";
            }

            ContentManagerCore.TempMiscUtilities.AddFilesizeToXml(depotRootDir);

            statusTextBlock.Text = "Finished";
        }

        private void ChooseDepotRootButton_Click(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != string.Empty))
                depotRootTextBlock.Text = dirname;
        }
    }
}
