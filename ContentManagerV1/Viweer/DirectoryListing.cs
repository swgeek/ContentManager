using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viweer
{
    class DirectoryListing
    {
        public List<string> fileNames { get; set; }
        public List<string> subdirNames { get; set; }

        DirectoryListing()
        {
            fileNames = null;
            subdirNames = null;
        }

    }
}
