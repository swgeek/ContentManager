using System;
using System.Windows.Forms;

namespace MpvUtilities
{
    public class FilePickerUtility
    {
         static public string PickFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            //dlg.DefaultExt = ".txt";
            //dlg.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
                return dlg.FileName;
            else
                return null;
        }

        static public string PickDirectory()
        {
            FolderBrowserDialog folderPicker = new FolderBrowserDialog();
            folderPicker.ShowDialog();
            return folderPicker.SelectedPath;

        }


    }
}


