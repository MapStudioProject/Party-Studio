using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;
using MapStudio.UI;

namespace PartyStudio.MP10
{
    public class PAC : FileEditor, IFileFormat, IArchiveFile
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "ND Cubed PAC" };
        public string[] Extension { get; set; } = new string[] { "*.pac" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                return reader.CheckSignature(3, "PAC");
            }
        }

        public bool CanAddFiles { get; set; } = true;
        public bool CanRenameFiles { get; set; }
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public void ClearFiles() { files.Clear(); }

        public IEnumerable<ArchiveFileInfo> Files => files;
        public List<FileEntry> files = new List<FileEntry>();

        public PAC() { }

        public PAC(string fileName) {
            Load(System.IO.File.OpenRead(fileName));
        }

        public PAC(System.IO.Stream stream) {
            Load(stream);
        }

        public void Load(System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                reader.SetByteOrder(true);

                uint signature = reader.ReadUInt32();
                uint headerSize = reader.ReadUInt32();
                reader.ReadUInt32(); //padding
                uint fileInfoSize = reader.ReadUInt32();
                uint totalFileSize = reader.ReadUInt32();
                uint languageSize = reader.ReadUInt32();
                uint unknown1 = reader.ReadUInt32();
                uint unknown2 = reader.ReadUInt32();
                uint numFiles = reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadUInt32();
                uint languageOffset = reader.ReadUInt32();
                uint fileInfoOffset = reader.ReadUInt32();
                uint stringTableOffset = reader.ReadUInt32();
                uint firstFileOffset = reader.ReadUInt32();

                reader.SeekBegin(fileInfoOffset);
                for (int i = 0; i < numFiles; i++)
                {
                    uint fileNameOffset = reader.ReadUInt32();
                    uint fileNameHash = reader.ReadUInt32();
                    uint fileExtOffset = reader.ReadUInt32();
                    uint fileExtHash = reader.ReadUInt32();
                    uint dataOffset = reader.ReadUInt32();
                    uint dataSize = reader.ReadUInt32();
                    uint compressedSize = reader.ReadUInt32();
                    uint compressedSize2 = reader.ReadUInt32();
                    uint padding1 = reader.ReadUInt32();
                    uint padding2 = reader.ReadUInt32();
                    uint compressionFlags = reader.ReadUInt32();
                    uint padding3 = reader.ReadUInt32();

                    string fileName = GetString(reader, fileNameOffset);
                    string ext = GetString(reader, fileExtOffset);

                    using (reader.TemporarySeek(dataOffset, System.IO.SeekOrigin.Begin))
                    {
                        byte[] data = reader.ReadBytes((int)compressedSize);
                        var fileEntry = new FileEntry();
                        fileEntry.FileName = $"{fileName}";
                        fileEntry.Compressed = dataSize != compressedSize;
                        if (fileEntry.Compressed)
                            data = STLibraryCompression.ZLIB.Decompress(data);

                        fileEntry.SetData(data);
                        files.Add(fileEntry);
                    }
                }
            }
        }
   
        private string GetString(FileReader reader, uint offset)
        {
            using (reader.TemporarySeek(offset, System.IO.SeekOrigin.Begin)) {
                return reader.ReadZeroTerminatedString();
            }
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            return false;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            return false;
        }

        public void Save(System.IO.Stream stream)
        {
        }

        public class FileEntry : ArchiveFileInfo
        {
            public bool Compressed { get; set; }
        }
    }
}
