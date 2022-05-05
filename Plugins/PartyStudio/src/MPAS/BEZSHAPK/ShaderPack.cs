using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using CafeLibrary.Rendering;
using BfshaLibrary;

namespace PartyStudio
{
    public class ShaderPack
    {
        public Dictionary<string, BfshaFile> Shaders = new Dictionary<string, BfshaFile>();

        public ShaderPack(Stream stream)
        {
            using (var reader = new FileReader(stream)) {
                Read(reader);
            }
        }

        private void Read(FileReader reader)
        {
            reader.Position = 0;

            reader.ReadSignature(8, "BEZSHAPK");
            reader.SeekBegin(40);
            uint numShaders = reader.ReadUInt32();
            reader.ReadUInt32();
            for (int i = 0; i < numShaders; i++)
            {
                var offset = reader.ReadInt64();
                using (reader.TemporarySeek(offset, SeekOrigin.Begin)) {
                    reader.SeekBegin(offset + 16); //file name
                    uint nameOffset = reader.ReadUInt32();

                    reader.SeekBegin(offset + 28); //file size
                    uint size = reader.ReadUInt32();

                    reader.SeekBegin(offset);
                    byte[] data = reader.ReadBytes((int)size);

                    reader.SeekBegin(offset + nameOffset);
                    string name = reader.ReadZeroTerminatedString();

                    Shaders.Add(name, new BfshaFile(new MemoryStream(data)));
                    Console.WriteLine($"Loading {name}");
                }
            }
        }
    }
}
