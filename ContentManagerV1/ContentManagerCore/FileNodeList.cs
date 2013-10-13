using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentManagerCore
{
    // holds a list of all files in object store. 
    // Will probably switch to dictionary (internally) so can find files using hash name, will see.
    // eventually should just use a real database, probably sqllite
    public class FileNodeList
    {
        // do not make this public as will change implementation later
        List<FileNode> listOfFiles = new List<FileNode>();

        public void AddFileNode(FileNode newNode)
        {
            listOfFiles.Add(newNode);
        }
    }
}
