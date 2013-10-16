using ContentManagerCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectFileListWithSearch
{
    class FileViewModel
    {
        public ObjectFileInfo ObjectFileInfo { get; set; }

        public string FileName
        {
            get { return ObjectFileInfo.HashValue; }
        }


        //public event PropertyChangedEventHandler PropertyChanged;

        //private void RaisePropertyChanged(string propertyName)
        //{
        //    // take a copy to prevent thread issues
        //    PropertyChangedEventHandler handler = PropertyChanged;
        //    if (handler != null)
        //    {
        //        handler(this, new PropertyChangedEventArgs(propertyName));
        //    }
        //}
    }
}
