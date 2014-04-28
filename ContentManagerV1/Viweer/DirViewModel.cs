using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viweer
{
class DirViewModel
{
    public string FullPath { get; set; }
    public string DirName { get; set; }
    public string DirHash { get; set; }
    public string Status { get; set; }

    public ObservableCollection<DirViewModel> SubDirs { get; set; }

    public DirViewModel(string dirName, string dirPath, string dirHash, string status)
    {
        DirName = dirName;
        FullPath = dirPath;
        DirHash = dirHash;
        Status = status;



            //SubDirs = new ObservableCollection<DirViewModel>();
            //if (trythis != null)
            //{
            //    trythis.ForEach(x => SubDirs.Add(new DirViewModel(x, null)));
            //}
        }

        //public void AddChildren(List<string> newSubDirs)
        //{
        //    if (newSubDirs != null)
        //    {
        //        newSubDirs.ForEach(x => SubDirs.Add(new DirViewModel(x, null)));    
        //    } 
        //}
    }
}
