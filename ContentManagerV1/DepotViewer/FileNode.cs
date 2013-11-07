using System.Collections.Generic;

namespace DepotViewer
{
    // maybe get rid of this, use objectfileinfo instead
    public class FileNode
    {
        public string DepotName { get; private set; }
        public string FileName { get; private set; }
        //public List<string> OriginalPaths { get; set; }
        // FirstOriginalName
        // Filesize

        public FileNode(string depotName, string hashValue)
        {
            DepotName = depotName;
            FileName = hashValue;

            
        }
    }
}
