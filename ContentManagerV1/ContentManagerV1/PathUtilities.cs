using System.IO;
using System.Text.RegularExpressions;


namespace ContentManagerV1
{
    class PathUtilities
    {
        static public string PathOnly(string filePath)
        {
            try
            {
                return Path.GetDirectoryName(filePath);
            }
            catch (System.IO.PathTooLongException)
            {
                // UGh, ugly and inefficient, figure out cleaner way
                // get path, not including filename
                string filenameOnly = Regex.Match(filePath, @"[^\\]*$").Value;
                string pathOnly = filePath.Remove((filePath.Length - filenameOnly.Length), filenameOnly.Length);
                pathOnly = pathOnly.TrimEnd(Path.DirectorySeparatorChar);
                if (pathOnly[pathOnly.Length - 1] == ':')
                    pathOnly = pathOnly + @"\";
                return pathOnly;
            }
        }

        // probably better to use a regex and get everything after \\, fix later
        static public string DirNameOnly(string dirPath)
        {
            string directoryName = dirPath;
            string parent = ParentDir(dirPath);
            directoryName = directoryName.Remove(0, parent.Length);
            directoryName = directoryName.Trim(Path.DirectorySeparatorChar);
            return directoryName;
        }

        static public string ParentDir(string dirPath)
        {
            try
            {
                return Directory.GetParent(dirPath).FullName;
            }
            catch (PathTooLongException)
            {
                // UGh, ugly and inefficient, figure out cleaner way
                dirPath = dirPath.TrimEnd(Path.DirectorySeparatorChar);
                string dirNameOnly = Regex.Match(dirPath, @"[^\\]*$").Value;
                string parent = dirPath.Remove((dirPath.Length - dirNameOnly.Length), dirNameOnly.Length);
                parent = parent.TrimEnd(Path.DirectorySeparatorChar);
                if (parent[parent.Length - 1] == ':')
                    parent = parent + @"\";
                return parent;
            }
        }
    }
}

