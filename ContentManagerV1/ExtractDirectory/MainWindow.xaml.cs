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
using MpvUtilities;

namespace ExtractDirectory
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string sourceBaseDir = String.Empty;
        string destBaseDir = String.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnPickArchiveRootDirButtonClick(object sender, RoutedEventArgs e)
        {
            sourceBaseDir = MpvUtilities.FilePickerUtility.PickDirectory();
            
        }


        private void OnPickDestinationDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            destBaseDir = MpvUtilities.FilePickerUtility.PickDirectory();
        }


        private void OnStartExtractButtonClick(object sender, RoutedEventArgs e)
        {
            if (sourceBaseDir == String.Empty || destBaseDir == String.Empty)
                return;

            ExtractFromDepot.ExtractFilesAndDirs(sourceBaseDir, destBaseDir);
        }
    }
}
