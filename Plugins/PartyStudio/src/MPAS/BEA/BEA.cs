using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core.IO;
using Toolbox.Core;
using BezelEngineArchive_Lib;

namespace PartyStudioPlugin
{
    public class BEA : IArchiveFile
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "BEA" };
        public string[] Extension { get; set; } = new string[] { "*.bea" };

        public File_Info FileInfo { get; set; }

        public bool CanAddFiles { get; set; } = true;
        public bool CanRenameFiles { get; set; } = true;
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "SCNE");
            }
        }

        public Dictionary<string, FileEntry> FileLookup = new Dictionary<string, FileEntry>();

        public IEnumerable<ArchiveFileInfo> Files => FileLookup.Values;
        public void ClearFiles() { FileLookup.Clear(); }

        public BezelEngineArchive Header;

        public void Load(Stream stream) {
            Header = new BezelEngineArchive(stream);
            foreach (var file in Header.FileList.Values)
            {
                var entry = new FileEntry();
                entry.UncompressedSize = file.UncompressedSize;
                entry.IsCompressed = file.IsCompressed;
                entry.SetData(new MemoryStream(file.FileData));
                entry.FileName = file.FileName;
                FileLookup.Add(entry.FileName, entry);
            }
        }

        public void Save(Stream stream) {
            foreach (var file in FileLookup.Values)
            {
                Header.FileList[file.FileName].UncompressedSize = file.UncompressedSize;
                Header.FileList[file.FileName].FileData = file.CompressedData.ToArray();
            }

            Header.Save(stream);
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            FileLookup.Add(archiveFileInfo.FileName, new FileEntry()
            {
                FileName = archiveFileInfo.FileName,
                FileData = archiveFileInfo.FileData,
            });
            return true;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            FileLookup.Remove(archiveFileInfo.FileName);
            return true;
        }

        public class FileEntry : ArchiveFileInfo
        {
            public bool IsCompressed;

            public override Stream FileData 
            {
                get
                {
                    if (IsCompressed)
                        return new MemoryStream(Zstb.SDecompress(base.FileData.ToArray()));
                    return base.FileData;
                }
                set => base.FileData = value;
            }

            public Stream CompressedData => base.FileData;

            public long UncompressedSize;

            public void SetFileData(byte[] data) {
                UncompressedSize = data.Length;

                if (IsCompressed)
                    base.FileData = new MemoryStream(Zstb.SCompress(data));
                else
                    base.FileData = new MemoryStream(data);
            }
        }
    }
}
