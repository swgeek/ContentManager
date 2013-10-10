using System.Collections.Generic;

namespace ContentManagerCore
{
    public class DirListing
    {
        public List<string> Directories { get; set; }
        public List<string> Files { get; set; }
        public string OriginalPath { get; private set; }

        public DirListing( string originalPath )
        {
            OriginalPath = originalPath;
            Directories = new List<string>();
            Files = new List<string>();
        }
    }
}
