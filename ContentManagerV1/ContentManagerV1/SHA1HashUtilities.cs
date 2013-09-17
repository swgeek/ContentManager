using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace ContentManagerV1
{
    class SH1HashUtilities
    {
        static public string HashToString(byte[] hashValue)
        {
            StringBuilder sBuilder = new StringBuilder();

            foreach (byte b in hashValue)
            {
                sBuilder.Append(b.ToString("X2"));
             }

            return sBuilder.ToString();
        }

        static public string HashString(string stringToHash)
        {
            byte[] bytesToHash = Encoding.ASCII.GetBytes(stringToHash);
            SHA1Managed hasher = new SHA1Managed();
            byte[] hashValue = hasher.ComputeHash(bytesToHash);

            return HashToString(hashValue);
        }

        static public string HashFile(string fileName)
        {
            FileInfo file = new FileInfo(fileName);

            if (!file.Exists)
                throw new Exception(fileName + " does not exist");

            long filesize = file.Length;


            SHA1Managed hasher = new SHA1Managed();
            Stream fileStream;
            try
            {
                fileStream = file.OpenRead();
            }
            catch
            {
                // probably permissions problems, return null for now, through propogating exception may be better. think it through later.
                return null;
            }

            byte[] hashBytes = hasher.ComputeHash(fileStream);

            fileStream.Close();

            return HashToString(hashBytes);
        }

        static public string CombineHashValues(string hash1, string hash2)
        {
            //if ((hash1 == Constants.InvalidHash) || (hash2 == Constants.InvalidHash))
            //    return Constants.InvalidHash;

            if (hash1.Length != hash2.Length)
                throw new Exception();

            byte[] combinedBytes = new byte[hash1.Length / 2];

            // just add byte by byte with no overflow for now, may change
            for (int i = 0; i < hash1.Length; i += 2)
            {
                byte byte1 = Byte.Parse(hash1.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                byte byte2 = Byte.Parse(hash2.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                combinedBytes[i / 2] = (byte)(byte1 + byte2);
            }

            return HashToString(combinedBytes);
        }
    }
}
