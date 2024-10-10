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

namespace PartyStudio.GCN
{
    public class MPBIN : FileEditor, IFileFormat, IArchiveFile, IDisposable
    {
        public bool CanSave { get; set; } = true;
        public string[] Description { get; set; } = new string[] { "Mario Party GCN .bin" };
        public string[] Extension { get; set; } = new string[] { "*.bin" };

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
            if (stream.Length < 16)
                return false;

            if (Utils.GetExtension(fileInfo.FileName) == ".bin" || 
                Utils.GetExtension(fileInfo.FileName) == ".dat")
            {
                using (var reader = new FileReader(stream, true))
                {
                    reader.SetByteOrder(true);
                    uint count = reader.ReadUInt32();
                    uint offset = reader.ReadUInt32();
                    if (offset == 4 + (4 * count) || offset == 8 + (4 * count))
                        return true;
                }
            }
            return false;
        }

        public enum CompressionType
        {
            None = 0,
            LZSS = 1,
            SLIDE = 2,
            FSLIDE_Alt = 3,
            FLIDE = 4,
            RLE = 5,
            INFLATE = 7,
        }

        public MPBIN() { }

        public MPBIN(string FileName)
        {
            using (var stream = new System.IO.FileStream(FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read)) {
                Read(FileName, stream);
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

        public void Load(System.IO.Stream stream) {
            Read(FileInfo.FileName, stream);
        }

        public void Dispose()
        {
            foreach (var file in this.Files)
            {
                if (file.FileFormat is IDisposable)
                    ((IDisposable)file.FileFormat).Dispose();
            }
        }

        void Read(string FileName, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                reader.ByteOrder = Syroot.BinaryData.ByteOrder.BigEndian;
                uint numFiles = reader.ReadUInt32();
                uint[] offsets = reader.ReadUInt32s((int)numFiles);

                for (int i = 0; i < numFiles; i++)
                {
                    reader.SeekBegin(offsets[i]);
                    uint compSize = 0;
                    if (i < numFiles - 1)
                        compSize = offsets[i + 1] - offsets[i] - 8;
                    else
                        compSize = (uint)reader.BaseStream.Length - offsets[i] - 8;

                    var file = new FileEntry();
                    uint decompressedSize = reader.ReadUInt32();
                    file.CompressionType = (CompressionType)reader.ReadUInt32();
                    byte[] data = reader.ReadBytes((int)compSize);
                    files.Add(file);

                    switch (file.CompressionType)
                    {
                        case CompressionType.LZSS:
                            data = DecompressLZSS(data, (int)decompressedSize);
                            break;
                        case CompressionType.SLIDE:
                        case CompressionType.FLIDE:
                        case CompressionType.FSLIDE_Alt:
                            data = DecompressSlide(data, (int)decompressedSize);
                            break;
                        case CompressionType.RLE:
                            data = DecompressRLE(data, (int)decompressedSize);
                            break;
                        case CompressionType.INFLATE:
                            data = STLibraryCompression.ZLIB.Decompress(ByteUtils.SubArray(data, 8));
                            break;
                        default:
                            break;
                    }

                    file.SetData(data);
                }
                UpdateFileNames(FileName);
            }
        }

        private void UpdateFileNames(string FileName)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(FileName);
            for (int i = 0; i < files.Count; i++)
            {
                string comp = "";
                switch (files[i].CompressionType)
                {
                    case CompressionType.LZSS: comp = ".lz"; break;
                    case CompressionType.SLIDE: comp = ".s"; break;
                    case CompressionType.FLIDE: comp = ".fs"; break;
                    case CompressionType.FSLIDE_Alt: comp = ".fsa"; break;
                    case CompressionType.INFLATE: comp = ".z"; break;
                }
                files[i].FileName = $"{name}{string.Format("{0:00}", i)}{comp}";

                using (var fileReader = new FileReader(files[i].FileData))
                {
                    fileReader.SetByteOrder(true);
                    string magic = fileReader.ReadString(4, Encoding.ASCII);
                    if (magic == "HSFV" && fileReader.BaseStream.Length > 200)
                    {
                        fileReader.SeekBegin(12);
                        bool hasFog = fileReader.ReadUInt32() > 0;

                        fileReader.SeekBegin(44);
                        bool hasMesh = fileReader.ReadUInt32() > 0;

                        fileReader.SeekBegin(76);
                        bool hasObjects = fileReader.ReadUInt32() > 0;

                        fileReader.SeekBegin(84);
                        bool hasTexture = fileReader.ReadUInt32() > 0;

                        fileReader.SeekBegin(140);
                        bool hasShapeMorph = fileReader.ReadUInt32() > 0;

                        fileReader.SeekBegin(148);
                        bool hasMapAttr = fileReader.ReadUInt32() > 0;

                        fileReader.SeekBegin(180);
                        bool hasMotion = fileReader.ReadUInt32() > 0;

                        if (hasMapAttr)
                            files[i].FileName = $"MESH_COLLISION_{files[i].FileName}.hsf";
                        else if (hasShapeMorph)
                            files[i].FileName = $"MESH_MORPH_{files[i].FileName}.hsf";
                        else if (hasMesh)
                            files[i].FileName = $"MESH_{files[i].FileName}.hsf";
                        else if (hasMotion)
                            files[i].FileName = $"MOTION_{files[i].FileName}.hsf";
                        else if (hasObjects)
                            files[i].FileName = $"OBJ_{files[i].FileName}.hsf";
                        else if (hasTexture)
                            files[i].FileName = $"TEX_{files[i].FileName}.hsf";
                        else if (hasFog)
                            files[i].FileName = $"FOG_{files[i].FileName}.hsf";
                        else
                            files[i].FileName = $"{files[i].FileName}.hsf";

                        files[i].ImageKey = "model";

                        //files[i].FileFormat = files[i].OpenFile();
                    }
                    else if (fileReader.BaseStream.Length > 16)
                    {
                        fileReader.SeekBegin(12);
                        uint offset = fileReader.ReadUInt32();
                        if (offset == 20)
                            files[i].FileName = $"{files[i].FileName}.anm";
                        else
                            files[i].FileName = $"{files[i].FileName}.dat";
                    }
                    else
                        files[i].FileName = $"{files[i].FileName}.dat";
                }
            }
        }

        public void Save(System.IO.Stream stream)
        {
            using (var writer = new FileWriter(stream))
            {
                writer.SetByteOrder(true);
                writer.Write(files.Count);
                writer.Write(new uint[files.Count]); //reserve space for offsets
                for (int i = 0; i < files.Count; i++)
                {
                    files[i].SaveFileFormat();

                    uint uncomp_size = (uint)files[i].FileData.Length;

                    writer.WriteUint32Offset(4 + (i * 4));
                    writer.Write(uncomp_size);
                    writer.Write((uint)files[i].CompressionType);

                    byte[] savedBytes = files[i].AsBytes();
                    switch (files[i].CompressionType)
                    {
                        case CompressionType.LZSS:
                            savedBytes = LZSS.Encode(savedBytes);
                            break;
                        case CompressionType.INFLATE:
                            {
                                var compressed = STLibraryCompression.ZLIB.Compress(savedBytes);

                                List<byte> output = new List<byte>();
                                output.AddRange(WriteBigEndian(uncomp_size));
                                output.AddRange(WriteBigEndian((uint)compressed.Length));
                                output.AddRange(compressed);
                                savedBytes = output.ToArray();
                                output.Clear();
                            }
                            break;
                        case CompressionType.SLIDE:
                        case CompressionType.FSLIDE_Alt:
                        case CompressionType.FLIDE:
                            {
                                List<byte> output = new List<byte>();
                                output.AddRange(WriteBigEndian(uncomp_size));
                                output.AddRange(CompressSlide(savedBytes, uncomp_size));
                                savedBytes = output.ToArray();
                                output.Clear();
                            }
                            break;
                        default:
                            throw new Exception($"Compression not supported! {files[i].CompressionType}");
                    }
                    writer.Write(savedBytes);
                }
            }
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            CompressionType type = CompressionType.None;
            if (archiveFileInfo.FileName.Contains(".lz"))
                type = CompressionType.LZSS;
            if (archiveFileInfo.FileName.Contains(".s"))
                type = CompressionType.SLIDE;
            if (archiveFileInfo.FileName.Contains(".fs"))
                type = CompressionType.FLIDE;
            if (archiveFileInfo.FileName.Contains(".fsa"))
                type = CompressionType.FSLIDE_Alt;
            if (archiveFileInfo.FileName.Contains(".z"))
                type = CompressionType.INFLATE;

            files.Add(new FileEntry()
            {
                FileData = archiveFileInfo.FileData,
                CompressionType = type,
            });
            UpdateFileNames(FileInfo.FileName);

            return false;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Remove((FileEntry)archiveFileInfo);
            return true;
        }

        public class FileEntry : ArchiveFileInfo
        {
            public string ImageKey { get; set; }

            public CompressionType CompressionType { get; set; }
        }

        //From https://github.com/Sage-of-Mirrors/HoneyBee/blob/master/HoneyBee/src/archive/Compression.cs#L78
        private static byte[] DecompressLZSS(byte[] compressed_data, int uncompressed_size)
        {
            int WINDOW_SIZE = 1024;
            int WINDOW_START = 0x3BE;
            int MIN_MATCH_LEN = 3;

            int src_offset = 0;
            int dest_offset = 0;
            int window_offset = WINDOW_START;

            byte[] dest = new byte[uncompressed_size];
            byte[] window_buffer = new byte[WINDOW_SIZE];

            ushort cur_code_byte = 0;

            while (dest_offset < uncompressed_size)
            {
                if ((cur_code_byte & 0x100) == 0)
                {
                    cur_code_byte = compressed_data[src_offset++];
                    cur_code_byte |= 0xFF00;
                }

                if ((cur_code_byte & 0x001) == 1)
                {
                    dest[dest_offset] = compressed_data[src_offset];
                    window_buffer[window_offset] = compressed_data[src_offset];

                    src_offset++;
                    dest_offset++;

                    window_offset = (window_offset + 1) % WINDOW_SIZE;
                }

                else
                {
                    byte byte1 = compressed_data[src_offset++];
                    byte byte2 = compressed_data[src_offset++];

                    int offset = ((byte2 & 0xC0) << 2) | byte1;
                    int length = (byte2 & 0x3F) + MIN_MATCH_LEN;

                    byte val = 0;
                    for (int i = 0; i < length; i++)
                    {
                        val = window_buffer[offset % WINDOW_SIZE];
                        window_buffer[window_offset] = val;

                        window_offset = (window_offset + 1) % WINDOW_SIZE;
                        dest[dest_offset] = val;

                        dest_offset++;
                        offset++;
                    }
                }

                cur_code_byte >>= 1;
            }


            return dest;
        }

        private static byte[] DecompressRLE(byte[] compressed_data, int uncompressed_size)
        {
            int dest_offset = 0;
            int src_offset = 0;
            int code_byte = 0;
            byte repeat_length;
            int i;

            byte[] dest = new byte[uncompressed_size];
            while (dest_offset < uncompressed_size)
            {
                code_byte = compressed_data[src_offset];
                src_offset++;
                repeat_length = (byte)(code_byte & 0x7F);

                if ((code_byte & 0x80) != 0)
                {
                    i = 0;
                    while (i < repeat_length)
                    {
                        dest[dest_offset] = compressed_data[src_offset];
                        dest_offset++;
                        src_offset++;
                        i++;
                    }
                }
                else
                {
                    byte repeated_byte = compressed_data[src_offset];
                    src_offset++;

                    i = 0;
                    while (i < repeat_length)
                    {
                        dest[dest_offset] = repeated_byte;
                        dest_offset++;
                        i++;
                    }
                }
            }
            return dest;
        }

        //From https://github.com/gamemasterplc/mpbintools/blob/master/bindump.c#L240
        private static byte[] DecompressSlide(byte[] compressed_data, int uncompressed_size)
        {
            int src_offset = 4;
            int dest_offset = 0;
            int code_word = 0;
            int num_code_word_bits_left = 0;
            int i = 0;

            byte[] dest = new byte[uncompressed_size];

            while (dest_offset < uncompressed_size)
            {
                if (num_code_word_bits_left == 0)
                {
                    code_word = ReadBigEndian(compressed_data, src_offset);
                    src_offset += 4;
                    num_code_word_bits_left = 32;
                }

                if ((code_word & 0x80000000) != 0)
                {
                    dest[dest_offset] = compressed_data[src_offset];
                    src_offset++;
                    dest_offset++;
                }
                else
                {
                    //Interpret Next 2 Bytes as a Backwards Distance and Length
                    byte byte1 = compressed_data[src_offset++];
                    byte byte2 = compressed_data[src_offset++];

                    int dist_back = (((byte1 & 0x0F) << 8) | byte2) + 1;
                    int copy_length = ((byte1 & 0xF0) >> 4) + 2;

                    //Special Case Where the Upper 4 Bits of byte1 are 0
                    if (copy_length == 2)
                    {
                        copy_length = compressed_data[src_offset++] + 18;
                    }

                    byte value;
                    i = 0;

                    while (i < copy_length && dest_offset < uncompressed_size)
                    {
                        if (dist_back > dest_offset)
                        {
                            value = 0;
                        }
                        else
                        {
                            value = dest[dest_offset - dist_back];
                        }
                        dest[dest_offset] = value;
                        dest_offset++;
                        i++;
                    }
                }
                code_word = code_word << 1;
                num_code_word_bits_left--;
            }

            return dest;
        }

        private static int ReadBigEndian(byte[] data, int offset)
        {
            int value = data[offset + 0] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3];
            return value;
        }

        private static byte[] CompressSlide(byte[] input, uint uncomp_len)
        {
            Ret r = new Ret();

            int dstSize = 0;
            int percent = -1;
            byte[] dst = new byte[96];// 8 codes * 3 bytes maximum

            uint validBitCount = 0; //number of valid bits left in "code" byte
            uint currCodeByte = 0;
            int offset = 0;

            List<byte> output = new List<byte>();

            var mem = new System.IO.MemoryStream();

            while (r.srcPos < uncomp_len)
            {
                uint numBytes;
                uint matchPos = 0;
                uint srcPosBak;

                numBytes = nintendoEnc(input, (int)uncomp_len, r.srcPos, ref matchPos);
                if (numBytes < 3)
                {
                    //straight copy
                    dst[r.dstPos] = input[r.srcPos];
                    output.Add(dst[r.dstPos]);
                    r.dstPos++;
                    r.srcPos++;
                    //set flag for straight copy
                    currCodeByte |= (0x80000000 >> (int)validBitCount);
                }
                else
                {
                    //RLE part
                    uint dist = (uint)(r.srcPos - matchPos - 1);
                    byte byte1, byte2, byte3;

                    if (numBytes >= 0x12)  // 3 byte encoding
                    {
                        byte1 = (byte)(0 | (dist >> 8));
                        byte2 = (byte)(dist & 0xff);
                        dst[r.dstPos++] = byte1;
                        dst[r.dstPos++] = byte2;
                        // maximum runlength for 3 byte encoding
                        if (numBytes > 0xff + 0x12)
                            numBytes = 0xff + 0x12;
                        byte3 = (byte)(numBytes - 0x12);
                        dst[r.dstPos++] = byte3;
                    }
                    else  // 2 byte encoding
                    {
                        byte1 = (byte)(((numBytes - 2) << 4) | (dist >> 8));
                        byte2 = (byte)(dist & 0xff);
                        dst[r.dstPos++] = byte1;
                        dst[r.dstPos++] = byte2;
                    }
                    r.srcPos += (int)numBytes;
                }
                validBitCount++;
                //write eight codes
                if (validBitCount == 32)
                {
                    WriteBigEndian(mem, currCodeByte, offset);
                    WriteFileArray(mem, dst, offset + 4, r.dstPos);

                    dstSize += r.dstPos + 4;
                    offset += r.dstPos + 4;

                    srcPosBak = (uint)r.srcPos;
                    currCodeByte = 0;
                    validBitCount = 0;
                    r.dstPos = 0;
                }
            }
            if (validBitCount > 0)
            {
                WriteBigEndian(mem, currCodeByte, offset);
                WriteFileArray(mem, dst, offset + 4, r.dstPos);

                dstSize += r.dstPos + 4;
                offset += r.dstPos + 4;

                currCodeByte = 0;
                validBitCount = 0;
                r.dstPos = 0;
            }
            return mem.ToArray();
        }

        private static void WriteFileArray(System.IO.MemoryStream mem, byte[] data, int offset, int len)
        {
            using (var writer = new FileWriter(mem, true))
            {
                writer.SetByteOrder(true);
                writer.SeekBegin(offset);
                for (int i = 0; i < len; i++)
                    writer.Write(data[i]);
            }
        }

        private static void WriteBigEndian(System.IO.MemoryStream mem, uint data, int offset)
        {
            using (var writer = new FileWriter(mem, true))
            {
                writer.SetByteOrder(true);
                writer.SeekBegin(offset);
                writer.Write(data);
            }
        }

        private static byte[] WriteBigEndian(uint value)
        {
            uint bigEndian = (uint)System.Net.IPAddress.HostToNetworkOrder((int)value);
            return BitConverter.GetBytes(bigEndian);
        }

        class Ret
        {
            public int srcPos;
            public int dstPos;
        }

        static uint numBytes1 = 0;
        static uint matchPos = 0;
        static int prevFlag = 0;
        private static uint nintendoEnc(byte[] src, int size, int pos, ref uint pMatchPos)
        {
            int startPos = pos - 0x1000;
            uint numBytes = 1;

            // if prevFlag is set, it means that the previous position was determined by look-ahead try.
            // so just use it. this is not the best optimization, but nintendo's choice for speed.
            if (prevFlag == 1)
            {
                pMatchPos = matchPos;
                prevFlag = 0;
                return numBytes1;
            }
            prevFlag = 0;
            numBytes = simpleEnc(src, size, pos, ref matchPos);
            pMatchPos = matchPos;

            // if this position is RLE encoded, then compare to copying 1 byte and next position(pos+1) encoding
            if (numBytes >= 3)
            {
                numBytes1 = simpleEnc(src, size, pos + 1, ref matchPos);
                // if the next position encoding is +2 longer than current position, choose it.
                // this does not guarantee the best optimization, but fairly good optimization with speed.
                if (numBytes1 >= numBytes + 2)
                {
                    numBytes = 1;
                    prevFlag = 1;
                }
            }
            return numBytes;
        }

        private static uint simpleEnc(byte[] src, int size, int pos, ref uint pMatchPos)
        {
            int startPos = pos - 0x1000;
            uint numBytes = 1;
            uint matchPos = 0;
            int i = 0;
            int j = 0;

            if (startPos < 0)
                startPos = 0;
            for (i = startPos; i < pos; i++)
            {
                for (j = 0; j < size - pos; j++)
                {
                    if (src[i + j] != src[j + pos])
                        break;
                }
                if (j > numBytes)
                {
                    numBytes = (uint)j;
                    matchPos = (uint)i;
                }
            }

            pMatchPos = matchPos;
            if (numBytes == 2)
                numBytes = 1;
            return numBytes;
        }

        public class LZSS
        {
            //From
            //https://github.com/bobjrsenior/GxUtilsNoUI/blob/436a6b2e3e0c51c1a261c07672d2c6d58c03d01e/GxUtils/LibGxFormat/Lz/Lz.cs
            static class LzssParameters
            {
                /// <summary>Size of the ring buffer.</summary>
                public static int N = 1024;
                /// <summary>Maximum match length for position coding. (0x0F + THRESHOLD).</summary>
                public static int F = 66;
                /// <summary>Minimum match length for position coding.</summary>
                public static int THRESHOLD = 3;
                /// <summary>Index for root of binary search trees.</summary>
                public static int NIL = N;
                /// <summary>Character used to fill the ring buffer initially.</summary>
                //private const ubyte BUFF_INIT = ' ';
                public static byte BUFF_INIT = 0; // Changed for F-Zero GX
            }

            public static byte[] Decompress(byte[] input, uint decompressedLength)
            {
                byte[] dest = new byte[decompressedLength];
                int dest_offset = 0;

                List<byte> output = new List<byte>();
                byte[] ringBuf = new byte[LzssParameters.N];
                int inputPos = 0, ringBufPos = LzssParameters.N - LzssParameters.F;

                ushort flags = 0;

                // Clear ringBuf with a character that will appear often
                for (int i = 0; i < LzssParameters.N - LzssParameters.F; i++)
                    ringBuf[i] = LzssParameters.BUFF_INIT;

                while (dest_offset < decompressedLength)
                {
                    // Use 16 bits cleverly to count to 8.
                    // (After 8 shifts, the high bits will be cleared).
                    if ((flags & 0xFF00) == 0)
                        flags = (ushort)(input[inputPos++] | 0x8000);

                    if ((flags & 1) == 1)
                    {
                        // Copy data literally from input
                        byte c = input[inputPos++];
                        output.Add(c);
                        ringBuf[ringBufPos++ % LzssParameters.N] = c;

                        dest_offset++;
                    }
                    else
                    {
                        byte byte1 = input[inputPos++];
                        byte byte2 = input[inputPos++];

                        // Copy data from the ring buffer (previous data).
                        int index = ((byte2 & 0xC0) << 2) | byte1;
                        int count = (byte2 & 0x3F) + LzssParameters.THRESHOLD;

                        for (int i = 0; i < count; i++)
                        {
                            byte c = ringBuf[(index + i) % LzssParameters.N];
                            output.Add(c);
                            ringBuf[ringBufPos++ % LzssParameters.N] = c;

                            dest_offset++;
                        }
                    }

                    // Advance flags & count bits
                    flags >>= 1;
                }

                return output.ToArray();
            }

            // Ring buffer of size N, with extra F-1 bytes to facilitate comparison
            static byte[] ringBuf = new byte[LzssParameters.N + LzssParameters.F - 1];

            // Match position and length of the longest match. Set by InsertNode().
            static int matchPosition, matchLength;

            // Binary search trees.
            static int[] left = new int[LzssParameters.N + 1];
            static int[] right = new int[LzssParameters.N + 257];
            static int[] parent = new int[LzssParameters.N + 1];

            /// Initialize binary trees.
            static void InitTree()
            {
                /* For i = 0 to N - 1, right[i] and left[i] will be the right and
                 * left children of node i. These nodes need not be initialized.
                 *
                 * Also, parent[i] is the parent of node i.
                 * These are initialized to NIL (= N), which stands for 'not used'.
                 *
                 * For i = 0 to 255, right[N + i + 1] is the root of the tree
                 * for strings that begin with character i. These are initialized
                 * to NIL. Note there are 256 trees.
                 */

                for (int i = LzssParameters.N + 1; i <= LzssParameters.N + 256; i++)
                    right[i] = LzssParameters.NIL;

                for (int i = 0; i < LzssParameters.N; i++)
                    parent[i] = LzssParameters.NIL;
            }

            /**
             * Inserts string of length F, ringBuf[r..r+F-1], into one of the
             * trees (ringBuf[r]'th tree) and returns the longest-match position
             * and length via the global variables matchPosition and matchLength.
             * If matchLength >= F, then removes the old node in favor of the new
             * one, because the old one will be deleted sooner.
             * Note r plays double role, as tree node and position in buffer.
             */
            static void InsertNode(int r)
            {
                int i, p, cmp;
                int keyIdx;

                cmp = 1; keyIdx = r; p = LzssParameters.N + 1 + ringBuf[keyIdx + 0];
                right[r] = left[r] = LzssParameters.NIL; matchLength = 0;
                for (; ; )
                {
                    if (cmp >= 0)
                    {
                        if (right[p] != LzssParameters.NIL) p = right[p];
                        else { right[p] = r; parent[r] = p; return; }
                    }
                    else
                    {
                        if (left[p] != LzssParameters.NIL) p = left[p];
                        else { left[p] = r; parent[r] = p; return; }
                    }
                    for (i = 1; i < LzssParameters.F; i++)
                        if ((cmp = ringBuf[keyIdx + i] - ringBuf[p + i]) != 0) break;
                    if (i > matchLength)
                    {
                        matchPosition = p;
                        if ((matchLength = i) >= LzssParameters.F) break;
                    }
                }
                parent[r] = parent[p]; left[r] = left[p]; right[r] = right[p];
                parent[left[p]] = r; parent[right[p]] = r;
                if (right[parent[p]] == p) right[parent[p]] = r;
                else left[parent[p]] = r;
                parent[p] = LzssParameters.NIL;  /* remove p */
            }

            /**
             * Deletes node p from tree.
             */
            static void DeleteNode(int p)
            {
                int q;

                if (parent[p] == LzssParameters.NIL) return;  /* not in tree */
                if (right[p] == LzssParameters.NIL) q = left[p];
                else if (left[p] == LzssParameters.NIL) q = right[p];
                else
                {
                    q = left[p];
                    if (right[q] != LzssParameters.NIL)
                    {
                        do { q = right[q]; } while (right[q] != LzssParameters.NIL);
                        right[parent[q]] = left[q]; parent[left[q]] = parent[q];
                        left[q] = left[p]; parent[left[p]] = q;
                    }
                    right[q] = right[p]; parent[right[p]] = q;
                }
                parent[q] = parent[p];
                if (right[parent[p]] == p) right[parent[p]] = q; else left[parent[p]] = q;
                parent[p] = LzssParameters.NIL;
            }

            public static byte[] Encode(byte[] input)
            {
                List<byte> app = new List<byte>();
                int inputPos = 0;

                int len, r, s, last_matchLength, i;

                byte[] code_buf = new byte[17];
                int code_buf_ptr;
                byte mask;

                InitTree(); // Initialize trees

                /* code_buf[1..16] saves eight units of code,
                   and code_buf[0] works as eight flags,
                   "1" representing that the unit is an unencoded ubyte (1 byte),
                   "0" a position-and-length pair (2 bytes).
                   Thus, eight units require at most 16 bytes of code. */
                code_buf[0] = 0;
                code_buf_ptr = 1;
                mask = 1;

                s = 0; r = LzssParameters.N - LzssParameters.F;

                // Clear the buffer with any character that will appear often.
                for (i = s; i < r; i++)
                    ringBuf[i] = LzssParameters.BUFF_INIT;

                // Read F bytes into the last F bytes of the buffer
                for (len = 0; len < LzssParameters.F && inputPos < input.Length; len++)
                    ringBuf[r + len] = input[inputPos++];

                if (len == 0) // Text of size zero
                    return null;

                /* Insert the F strings,
                   each of which begins with one or more 'space' characters.
                   Note	the order in which these strings are inserted.
                   This way, degenerate trees will be less likely to occur. */
                for (i = 1; i <= LzssParameters.F; i++)
                    InsertNode(r - i);

                /* Finally, insert the whole string just read.
                   The variables matchLength and matchPosition are set. */
                InsertNode(r);

                do
                {
                    // matchLength may be spuriously long near the end of text.
                    if (matchLength > len)
                        matchLength = len;

                    if (matchLength < LzssParameters.THRESHOLD)
                    {
                        // Not long enough match. Send one byte.
                        matchLength = 1;
                        code_buf[0] |= mask; // 'send one byte' flag
                        code_buf[code_buf_ptr++] = ringBuf[r];  // Send uncoded.
                    }
                    else
                    {
                        int match_pos_lo = matchPosition & 0xFF;
                        int match_pos_hi = ((matchPosition >> 2) & 0xC0);
                        int match_len = ((matchLength - (LzssParameters.THRESHOLD)) & 0x3F);

                        // Send position and length pair. Note matchLength >= THRESHOLD.
                        code_buf[code_buf_ptr++] = (byte)match_pos_lo;
                        code_buf[code_buf_ptr++] = (byte)(match_pos_hi | match_len);
                    }

                    if ((mask <<= 1) == 0)
                    { // Dropped high bit -> Buffer is full
                        for (i = 0; i < code_buf_ptr; i++)
                        {
                            app.Add(code_buf[i]);
                        }

                        code_buf[0] = 0;
                        code_buf_ptr = 1;
                        mask = 1;
                    }

                    last_matchLength = matchLength;
                    for (i = 0; i < last_matchLength && inputPos < input.Length; i++)
                    {
                        // Delete old strings and read new bytes
                        DeleteNode(s);
                        ringBuf[s] = input[inputPos++];

                        /* If the position is near the end of buffer,
                         * extend the buffer to make string comparison easier. */
                        if (s < LzssParameters.F - 1)
                            ringBuf[s + LzssParameters.N] = input[inputPos - 1];

                        // Since this is a ring buffer, increment the position modulo N.
                        s = (s + 1) % LzssParameters.N; r = (r + 1) % LzssParameters.N;
                        InsertNode(r);  /* Register the string in ringBuf[r..r+F-1] */
                    }

                    // After the end of text, no need to read, but buffer may not be empty
                    while (i++ < last_matchLength)
                    {
                        DeleteNode(s);
                        s = (s + 1) % LzssParameters.N; r = (r + 1) % LzssParameters.N;
                        if (--len != 0)
                            InsertNode(r);
                    }
                } while (len > 0);  /* until length of string to be processed is zero */

                if (code_buf_ptr > 1) // Send remaining code.
                {
                    for (i = 0; i < code_buf_ptr; i++)
                    {
                        app.Add(code_buf[i]);
                    }
                }

                return app.ToArray();
            }
        }
    }
}
