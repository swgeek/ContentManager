using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectStoreViewer
{
    public class MNode
    {
        public string name { get; set; }
        public string fullPath { get; set; }
        public string hash { get; set; }
        public List<MNode> subdirectories = new List<MNode>();
    }
}
