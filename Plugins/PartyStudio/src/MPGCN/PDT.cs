using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox;
using Toolbox.Core;
using Toolbox.Core.IO;
using System.IO;
using MapStudio.UI;
using UIFramework;
using MPLibrary.GCWii.Audio;

namespace PartyStudio.GCN
{
    public class PDT : FileEditor, IFileFormat, IArchiveFile
    {
        public bool CanSave { get; set; } = true;
        public string[] Description { get; set; } = new string[] { "Mario Party GCN .pdt" };
        public string[] Extension { get; set; } = new string[] { "*.pdt" };

        public File_Info FileInfo { get; set; }

        public bool CanAddFiles { get; set; } = true;
        public bool CanRenameFiles { get; set; }
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public void ClearFiles() { files.Clear(); }

        public IEnumerable<ArchiveFileInfo> Files => files;
        public List<FileEntry> files = new List<FileEntry>();

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            return fileInfo.Extension == ".pdt";
        }

        public PDT() { }

        public PDT(string FileName)
        {
            using (var stream = new System.IO.FileStream(FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read)) {
                Load(stream);
            }
        }

        /// <summary>
        /// Prepares the dock layouts to be used for the file format.
        /// </summary>
        public override List<DockWindow> PrepareDocks()
        {
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            return windows;
        }

        PtdFile PtdFile;

        public void Load(System.IO.Stream stream) {
            PtdFile = new PtdFile(stream);

            foreach (var f in PtdFile.Files)
            {
                this.files.Add(new FileEntry()
                {
                    FileName = $"File{this.files.Count}.dsp",
                    FileData = new MemoryStream(f.CreateDSP()),
                });
            }
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Remove((FileEntry)archiveFileInfo);
            return true;
        }

        public void Save(Stream stream)
        {
            PtdFile.Save(stream);
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Add((FileEntry)archiveFileInfo);
            return true;
        }

        public class FileEntry : ArchiveFileInfo
        {
        }
    }
}
