using ContentManagerCore;
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

// work in progress

namespace DepotViewer
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

        private void PickDepotRootDirButton_Click(object sender, RoutedEventArgs e)
        {
            string depotRootPath = FilePickerUtility.PickDirectory();
            if ((depotRootPath != null) && (depotRootPath != String.Empty))
            {
                DepotRootViewModel depotRootModel = new DepotRootViewModel(depotRootPath);

                string dir =  ContentManagerCore.DepotFileLister.GetRootDirectoriesInDepot(depotRootPath).First();

                // for now disable the button. In real version allow user to change the root directory
                ChooseDepot.Visibility = System.Windows.Visibility.Collapsed;
                dirTreeView.Visibility = System.Windows.Visibility.Visible;

                //DirTreeNode node = DirTreeNode.GetBaseDirs(depotRoot);
                dirTreeView.DataContext = depotRootModel;      
            }
        }

        private void ChooseDestinationDirectory_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExtractCurrentDirectoryButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
