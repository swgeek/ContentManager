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
using System.Xml.Linq;


namespace ObjectStoreViewer
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

        private void OnChooseRootDirButtonClick(object sender, RoutedEventArgs e)
        {
            string workingDir;
            string dirname = FilePickerUtility.PickDirectory();
            if ((dirname != null) && (dirname != String.Empty))
            {
                // for now disable the button. In real version allow user to change the root directory
                ChooseRootDirButton.Visibility = System.Windows.Visibility.Collapsed;
                directoryTreeView.Visibility = System.Windows.Visibility.Visible;

                workingDir = System.IO.Path.Combine(dirname, "working");
                if (Directory.Exists(workingDir))
                {
                    ProcessWorkingDir(workingDir);
                }
            }

        }

        private void ProcessWorkingDir(string workingDir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(workingDir);
            foreach (FileInfo file in dirInfo.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
            {
                XDocument dirXml = XDocument.Load(file.FullName);
                string dirPath = dirXml.Root.Attribute("path").Value.ToString();
                TreeViewItem newItem = new TreeViewItem();
                newItem.Header = dirPath;
                directoryTreeView.Items.Add(newItem);
                newItem.Selected += newItem_Selected;


            }
        }

        void newItem_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;

            RecursivelyAddDirectoriesAndFiles(dirPath, item);

        }

        private void RecursivelyAddDirectoriesAndFiles(string dirPath, TreeViewItem newItem)
        {
            throw new NotImplementedException();
        }
    }
}
