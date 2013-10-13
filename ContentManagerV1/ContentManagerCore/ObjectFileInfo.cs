using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ContentManagerCore
{
    public class ObjectFileInfo
    {
        public string DepotName { get; private set; }
        public string HashValue { get; private set; }
        public List<string> OriginalPaths { get; set; }
        public long FileSize { get; set; }

        public ObjectFileInfo(string depotPath, string hashValue)
        {
            DepotName = System.IO.Path.GetFileName(depotPath);
            HashValue = hashValue;
            OriginalPaths = new List<string>();

            string xmlFile = MpvUtilities.DepotPathUtilities.GetObjectFileXmlPath(depotPath, hashValue);

            XDocument xdoc = XDocument.Load(xmlFile);

            FileSize =  long.Parse( xdoc.Root.Attribute("Filesize").Value.ToString());

            // replace this with linq query!
            foreach (XElement element in xdoc.Root.Elements("NodeInfo"))
            {
                string path = element.Attribute("Fullpath").Value.ToString();
                OriginalPaths.Add(path);
            }
        }

        public bool Contains(string searchString)
        {
            bool foundMatch = false;

            foreach (string s in OriginalPaths)
            {
                if (s.Contains(searchString))
                {
                    foundMatch = true;
                    break;
                }
            }
    
            return foundMatch;
        }
    }
}
