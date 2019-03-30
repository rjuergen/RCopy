using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RCopy
{
    class IOUtil
    {

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CopyFile(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateDirectory(string lpPathName, IntPtr lpSecurityAttributes);

        public static void CopyFile(FileInfo file, string copyTo)
        {
            try {
                try
                {
                    file.CopyTo(copyTo, true);
                }
                catch (PathTooLongException pe)
                {
                    CopyFile(file.FullName, copyTo, false);
                }
            } catch (Exception e)
            {

            }
        }

        public static void CreateDirectory(string dir)
        {
            try { 
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (PathTooLongException pe)
                {
                    CreateDirectory(dir, IntPtr.Zero);
                }
            } catch (Exception e)
            {

            }
        }

        /// <summary>
        /// check if file is up to date
        /// </summary>
        /// <param name="copyFromPath"></param>
        /// <param name="copyToPath"></param>
        /// <returns></returns>
        public static bool IsUpToDate(FileInfo from, string copyToPath)
        {
            if (!File.Exists(copyToPath))
                return false;
            if (File.GetLastWriteTime(copyToPath).CompareTo(from.LastWriteTime) != 0)
                return false;
            return true;
        }

        public static IEnumerable<FileInfo> EnumerateFilesSafe(DirectoryInfo dir, SearchOption opt = SearchOption.TopDirectoryOnly)
        {
            var retval = Enumerable.Empty<FileInfo>();
             
            try
            {
                retval = dir.EnumerateFiles();
            }
            catch { }
            
            if (opt == SearchOption.AllDirectories)
            {
                foreach (DirectoryInfo dirInfo in EnumerateDirectoriesSafe(dir, opt))
                {                    
                    try
                    {
                        retval = retval.Concat(dirInfo.EnumerateFiles());
                    }
                    catch { }
                }
            }            

            return retval.Where(d => !ContainsBadAttributes(d.Attributes));
        }

        public static IEnumerable<DirectoryInfo> EnumerateDirectoriesSafe(DirectoryInfo dir, SearchOption opt = SearchOption.TopDirectoryOnly)
        {
            var retval = Enumerable.Empty<DirectoryInfo>();
                        
            try
            {
                retval = dir.EnumerateDirectories();
            }
            catch { }

            if (opt == SearchOption.AllDirectories)
            {
                try
                {
                    foreach (DirectoryInfo dirInfo in dir.EnumerateDirectories())
                    {
                        retval = retval.Concat(EnumerateDirectoriesSafe(dirInfo, opt));
                    }
                }
                catch { }
            }
            
            return retval.Where(d => !ContainsBadAttributes(d.Attributes));
        }

        private static bool ContainsBadAttributes(FileAttributes d)
        {
            return (d & FileAttributes.System) == FileAttributes.System || (d & FileAttributes.Temporary) == FileAttributes.Temporary
                || (d & FileAttributes.Hidden) == FileAttributes.Hidden;
        }

        public static long GetSizeSafe(DirectoryInfo dir)
        {
            return EnumerateFilesSafe(dir, SearchOption.AllDirectories).Sum(f => f.Length);
        }
        
    }
}
