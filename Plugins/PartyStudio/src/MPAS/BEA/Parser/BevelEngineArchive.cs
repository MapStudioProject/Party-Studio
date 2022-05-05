using System;
using System.Collections.Generic;
using Syroot.BinaryData;
using System.IO;

namespace BezelEngineArchive_Lib
{
    public class BezelEngineArchive : IFileData
    {
        private const string _signature = "SCNE";

        public BezelEngineArchive(Stream stream, bool leaveOpen = false)
        {
            using (FileLoader loader = new FileLoader(this, stream, leaveOpen))
            {
                loader.Execute();
            }
        }

        public BezelEngineArchive(string fileName)
        {
            using (FileLoader loader = new FileLoader(this, fileName))
            {
                loader.Execute();
            }
        }

        public ushort ByteOrder { get; private set; }
        public uint VersionMajor { get; set; }
        public uint VersionMajor2 { get; set; }
        public uint VersionMinor { get; set; }
        public uint VersionMinor2 { get; set; }
        public uint Alignment { get; set; }
        public uint TargetAddressSize { get; set; }
        public string Name { get; set; }

        public ResDict FileDictionary { get; set; } //Todo, Need to setup ResDict to grab indexes quicker
        public Dictionary<string, ASST> FileList { get; set; } //Use a dictionary for now so i can look up files quickly

        /// <summary>s
        /// Gets or sets the alignment to use for raw data blocks in the file.
        /// </summary>
        public int RawAlignment { get; set; }

        public void Save(Stream stream, bool leaveOpen = false)
        {
            using (FileSaver saver = new FileSaver(this, stream, leaveOpen))
            {
                saver.Execute();
            }
        }

        public void Save(string FileName)
        {
            using (FileSaver saver = new FileSaver(this, FileName))
            {
                saver.Execute();
            }
        }

        internal uint SaveVersion()
        {
            return VersionMajor << 24 | VersionMajor2 << 16 | VersionMinor << 8 | VersionMinor2;
        }

        private void SetVersionInfo(uint Version)
        {
            VersionMajor = Version >> 24;
            VersionMajor2 = Version >> 16 & 0xFF;
            VersionMinor = Version >> 8 & 0xFF;
            VersionMinor2 = Version & 0xFF;
        }

        internal Stream _stream;

        void IFileData.Load(FileLoader loader)
        {
            _stream = loader.BaseStream;

            loader.CheckSignature(_signature);
            uint padding = loader.ReadUInt32();
            uint Version = loader.ReadUInt32();
            SetVersionInfo(Version);
            ByteOrder = loader.ReadUInt16();
            Alignment = (uint)loader.ReadByte();
            TargetAddressSize = (uint)loader.ReadByte();
            uint Padding = loader.ReadUInt32(); //Usually name offset for file with other switch formats
            ushort Padding2 = loader.ReadUInt16();
            ushort BlockOffset = loader.ReadUInt16(); //Goes to ASST section which seems to have block headers
            uint RelocationTableOffset = loader.ReadUInt32();
            uint EndOfFile = loader.ReadUInt32();
            ulong FileCount = loader.ReadUInt64();
            ulong FileInfoOffset = loader.ReadUInt64();
            FileDictionary = loader.LoadDict();
            ulong unk = loader.ReadUInt64();
            Name = loader.LoadString();
            ulong BlockOffset2 = loader.ReadUInt64(); //Same offset?

            FileList = loader.LoadCustom(() =>
            {
                Dictionary<string, ASST> asstList = new Dictionary<string, ASST>();
                for (int i = 0; i < (int)FileCount; i++)
                {
                    asstList.Add(FileDictionary.GetKey(i), loader.Load<ASST>());
                }
                return asstList;
            }, (long)FileInfoOffset);
        }
        void IFileData.Save(FileSaver saver)
        {
            RawAlignment = (1 << (int)Alignment);

            saver.WriteSignature(_signature);
            saver.Write(0);
            saver.Write(SaveVersion());
            saver.Write(ByteOrder);
            saver.Write((byte)Alignment);
            saver.Write((byte)TargetAddressSize);
            saver.Write(0);
            saver.Write((ushort)0);
            saver.SaveFirstBlock();
            saver.SaveRelocationTablePointer();
            saver.SaveFileSize();
            saver.Write((ulong)FileList.Count);
            saver.SaveRelocateEntryToSection(saver.Position, 1, 1, 0, 1, "Asst Offset Array"); //      <------------ Entry Set
            saver.SaveFileAsstPointer();
            saver.SaveRelocateEntryToSection(saver.Position, 1, 1, 0, 1, "DIC"); //      <------------ Entry Set
            saver.SaveFileDictionaryPointer();
            saver.Write(0L);
            saver.SaveRelocateEntryToSection(saver.Position, 1, 1, 0, 1, "FileName"); //      <------------ Entry Set
            saver.SaveString(Name);
        }
    }
}
