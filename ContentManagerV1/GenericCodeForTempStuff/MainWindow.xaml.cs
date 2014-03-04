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

namespace GenericCodeForTempStuff
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

        private void OnDir1ButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                dir1TextBlock.Text = dirname;
        }

        private void OnDir2ButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                dir2TextBlock.Text = dirname;
        }

        private void OnDir3ButtonClick(object sender, RoutedEventArgs e)
        {
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
                dir3TextBlock.Text = dirname;
        }

        private void OnProcess(object sender, RoutedEventArgs e)
        {
            string dir1 = dir1TextBlock.Text;
            string dir2 = dir2TextBlock.Text;
            string dir3 = dir3TextBlock.Text;

            if (dir1 == String.Empty || dir2 == String.Empty || dir3 == String.Empty)
                return;


            if (Directory.Exists(dir1) && Directory.Exists(dir2) && Directory.Exists(dir3))
            {
                string[] filelist = Directory.GetFiles(dir1);

                foreach (string filePath in filelist)
                {
                    string hashValue = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    string originalFile = ContentManagerCore.DepotPathUtilities.GetObjectFileXmlPath(dir2, hashValue);
                    string newPath = System.IO.Path.Combine(dir3, System.IO.Path.GetFileName(originalFile));
                    File.Copy(originalFile, newPath);
                }
            }
        }
    }
}
