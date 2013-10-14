using System;
using System.Xml.Linq;

namespace ContentManagerCore
{
    // not sure where this is best abstracted out, so putting here for now.
    public class TempMiscUtilities
    {
        public static void AddFilesizeToXml(string depotRoot)
        {
            string[] filelist = DepotFileLister.GetListOfAllHashedFilesInDepot(depotRoot);

            foreach (string file in filelist)
            {
                long filesize = DepotFileLister.GetFileSize(depotRoot, file);
                XDocument fileXml = DepotFileLister.GetXml(depotRoot, file);

                if (fileXml.Root.Attribute("Filesize") == null)
                    fileXml.Root.Add(new XAttribute("Filesize", filesize.ToString()));
                else if (!fileXml.Root.Attribute("Filesize").ToString().Equals(filesize.ToString()))
                    throw new Exception(depotRoot + " filesize mismatch");

                if (fileXml.Root.Attribute("Status") == null)
                    fileXml.Root.Add(new XAttribute("Status", "todo"));

                DepotFileLister.UpdateXml(depotRoot, file, fileXml);
            }
        }

    }
}
