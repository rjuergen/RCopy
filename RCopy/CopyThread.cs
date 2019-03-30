using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace RCopy
{
    class CopyThread
    {
        public delegate void Finished();
        public delegate void UpdateProgressGUI(long progress);
        public delegate void UpdateCurrentFolderGUI(string currentPath);

        private Thread                      t;
        private Task                        copyTask;
        private CancellationTokenSource     cts;        
        private Finished                    f;    
        private UpdateProgressGUI           u_pGUI;
        private UpdateCurrentFolderGUI      u_cfGUI;
        private string                      from;
        private string                      to;

        public CopyThread(string from, string to, Finished f, UpdateProgressGUI pg, UpdateCurrentFolderGUI cfg)
        {
            if (!Directory.Exists(from))
                throw new FileNotFoundException("Folder '"+from+"' doesn't exist!");
            if (!Directory.Exists(to))
                IOUtil.CreateDirectory(to);
            this.from = from;
            this.to = to;
            this.f = f;
            this.u_pGUI = pg;
            this.u_cfGUI = cfg;
            t = new Thread(StartCopy);
            cts = new CancellationTokenSource();
        }
               

        public void Start()
        {
            t.Start();
        }

        public void Stop()
        {            
            t.Abort();
            cts.Cancel();
        }
                
        /// <summary>
        /// start copying files and folders from source to destination
        /// </summary>
        private void StartCopy()
        {
            DirectoryInfo from = new DirectoryInfo(this.from);
            copyTask = Task.Factory.StartNew(() => Copy(from), cts.Token);
            while (!copyTask.IsCompleted)
            {
                Thread.Sleep(1000);
            }
            if(!cts.IsCancellationRequested)
                f();
        }
        
        private void Copy(DirectoryInfo from)
        {
            if (cts.IsCancellationRequested)
                return;
            u_cfGUI(from.FullName);
            CopyFiles(from);
            IEnumerable<DirectoryInfo> dirFrom = IOUtil.EnumerateDirectoriesSafe(from);
            foreach (DirectoryInfo dir in dirFrom)
            {
                if (cts.IsCancellationRequested)
                    return;
                string copyTo = CreateCopyToName(dir.FullName);
                if (!Directory.Exists(copyTo))
                    IOUtil.CreateDirectory(copyTo);
                Copy(dir);
            }
        }

        private void CopyFiles(DirectoryInfo from)
        {
            IEnumerable<FileInfo> filesFrom = IOUtil.EnumerateFilesSafe(from);
            foreach (FileInfo file in filesFrom)
            {
                if (cts.IsCancellationRequested)
                    return;
                string copyTo = CreateCopyToName(file.FullName);
                if (!IOUtil.IsUpToDate(file, copyTo))
                {
                    IOUtil.CopyFile(file, copyTo);
                    u_pGUI(file.Length);
                }                
            }
        }

        /// <summary>
        /// get real size to copy. checks all files if they are already coppied.
        /// </summary>
        /// <param name="from"></param>
        public long GetRealSizeSafe(DirectoryInfo from)
        {
            long result = 0;
            IEnumerable<FileInfo> filesFrom = IOUtil.EnumerateFilesSafe(from);
            foreach (FileInfo file in filesFrom)
            {
                string copyTo = CreateCopyToName(file.FullName);
                // update GUI if file is already coppied
                if (!IOUtil.IsUpToDate(file, copyTo))
                    result += file.Length;
            }
            IEnumerable<DirectoryInfo> dirFrom = IOUtil.EnumerateDirectoriesSafe(from);
            foreach (DirectoryInfo dir in dirFrom)
            {
                string copyTo = CreateCopyToName(dir.FullName);
                if (Directory.Exists(copyTo))
                {
                    result += GetRealSizeSafe(dir);
                } else
                {
                    result += IOUtil.GetSizeSafe(dir);
                }
            }
            return result;
        }

        private string CreateCopyToName(string from)
        {
            return from.Replace(this.from, this.to);
        }        
    }
}
