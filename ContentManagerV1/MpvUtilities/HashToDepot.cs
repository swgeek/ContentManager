using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MpvUtilities
{
    public class HashToDepot
    {
        private string sourceDirPath = String.Empty;
        private string depotRootPath = String.Empty;
        private string alreadyArchivedDbPath = String.Empty;
        FileList filelist;

        public HashToDepot(string sourceDirPath, string depotRootPath, string alreadyArchivedDbPath)
        {
            this.sourceDirPath = sourceDirPath;
            this.depotRootPath = depotRootPath;
            this.alreadyArchivedDbPath = alreadyArchivedDbPath;
        }

        public int GetFileList()
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourceDirPath);
            filelist = TraverseDir.GetAllFilesInDir(sourceDir);
            MiscUtilities.SetupForHashDirectory(depotRootPath, sourceDirPath);
            MiscUtilities.Log(sourceDirPath + "\r\n" + filelist.Count.ToString() + " files", depotRootPath);
            return filelist.Count;
        }

        public string PathNameOfNextFileToHash()
        {
            if (filelist == null || filelist.Count == 0)
                return null;

            return filelist.CurrentFile();
        }


        public async Task HashNext()
        {

            string filePath = filelist.CurrentFile();

            await Task.Run(() =>
            {
                // should do this part when building filelist, not now.
                if (System.IO.Directory.Exists(filePath))
                {
                    // it is a directory
                    string workingDirName = DepotPathUtilities.GetXmlDirectoryInfoFileName(filePath, depotRootPath);
                    DoDirectoryInfoFileStuff(filePath, workingDirName);
                }
                else if (System.IO.File.Exists(filePath))
                {
                    DoHashFileStuff(filePath);
                }
                else
                {
                    throw new Exception(filePath + " does not exist!");
                }
            });

            filelist.RemoveCurrentFile();
        }

        public XDocument DoDirectoryInfoFileStuff(string dirPath, string workingDirName)
        {

            XDocument dirXml;

            if (File.Exists(workingDirName))
            {
                // Should be the same, could either sanity check or not worry about it. Don't worry for now.
                dirXml = XDocument.Load(workingDirName);
            }
            else
            {
                dirXml = FileXmlUtilities.GenerateDirInfoDocument(dirPath);
                dirXml.Save(workingDirName);
            }

            return dirXml;
        }


        public void DoHashFileStuff(string filePath)
        {
            // it is a file

            string hashValue = SH1HashUtilities.HashFile(filePath);

            string objectStoreFileName = DepotPathUtilities.GetHashFilePath(depotRootPath, hashValue);

            // copy file to object store
            CopyFileIfNeedTo(filePath, objectStoreFileName, hashValue);

            FileInfo fileInfo = new FileInfo(filePath);

            // add location to corresponding xml file
            addLocationToXmlFile(objectStoreFileName, filePath, fileInfo.Length);

            // save hash value to directoryInfo file
            string dirPath = System.IO.Path.GetDirectoryName(filePath);
            string filename = System.IO.Path.GetFileName(filePath);

            string workingDirName = DepotPathUtilities.GetXmlDirectoryInfoFileName(dirPath, depotRootPath);

            XDocument directoryInfoXmlDoc = DoDirectoryInfoFileStuff(dirPath, workingDirName);

            var trythis = directoryInfoXmlDoc.Root.Elements("File");

            XElement fileElement = (from element in directoryInfoXmlDoc.Root.Elements("File")
                                    where element.Attribute("filename").Value.ToString() == filename
                                    select element).Single();

            fileElement.SetAttributeValue("Hash", hashValue);

            directoryInfoXmlDoc.Save(workingDirName);

        }

        public void CopyFileIfNeedTo(string filePath, string objectStoreFileName, string hashValue)
        {
            if (MpvUtilities.MiscUtilities.CheckIfFileInOtherArchiveDb(hashValue, alreadyArchivedDbPath))
                return;

            if (File.Exists(objectStoreFileName))
            {
                // should be the exact same content. Should we binary check to make sure?
                // for now, just check filesize.
                FileInfo existingFile = new FileInfo(objectStoreFileName);
                FileInfo newFile = new FileInfo(filePath);
                if (newFile.Length != existingFile.Length)
                    throw new Exception("Collision with different length files - should be impossible?");
            }
            else
            {
                File.Copy(filePath, objectStoreFileName);
            }

        }

        private void addLocationToXmlFile(string objectStoreFileName, string filePath, long filesize)
        {
            string xmlFilename = objectStoreFileName + ".xml";
            XDocument fileXml;
            if (File.Exists(xmlFilename))
            {
                // get existing
                fileXml = XDocument.Load(xmlFilename);
            }
            else
            {
                fileXml = FileXmlUtilities.GenerateEmptyFileInfoDocument(filesize);
            }

            FileXmlUtilities.AddFileInfoElement(fileXml, filePath);

            fileXml.Save(xmlFilename);
        }

        public int NumberOfFilesLeftToHash()
        {
            return filelist.Count;
        }

        public void LogFinished()
        {
            MiscUtilities.AppendToLog(" finished", depotRootPath);

        }

        // don't have way to start from saved state yet, so either remove this or add the restore code
        public void SaveState()
        {
            CurrentState state = new CurrentState();
            state.sourceDirectory = sourceDirPath;

            state.destinationDirectory = depotRootPath;
            state.list = filelist.fileList.ToArray();

            string outputFileName = System.IO.Path.Combine(depotRootPath, "inprocess.xml");


            using (var writer = new System.IO.StreamWriter(outputFileName))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(state.GetType());
                serializer.Serialize(writer, state);
                writer.Flush();
            }
        }

    }
}
