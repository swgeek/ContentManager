using System.Collections.Generic;

namespace ContentManagerCore
{
    // maybe get rid of this, use objectfileinfo instead
    public class FileNode
    {
        public string DepotName { get; private set; }
        public string HashValue { get; private set; }
        public List<string> OriginalPaths { get; set; }

        public FileNode(string depotName, string hashValue)
        {
            DepotName = depotName;
            HashValue = hashValue;
        }
    }
}
