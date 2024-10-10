using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PartyStudio.GCN
{
    /// <summary>
    /// Represents user input meta data for the contents of an archive file.
    /// </summary>
    internal class FileDefinition
    {
        /// <summary>
        /// The file list with information of the file. The key being the index of the file.
        /// </summary>
        public Dictionary<int, File> FileList = new Dictionary<int, File>();

        public FileDefinition()
        {

        }

        public FileDefinition(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream) {
                    string[] values = reader.ReadLine().Trim().Split(",");
                    if (values.Length < 4 || values[0] == "File ID")
                        continue;

                    int fileID = int.Parse(values[0].Trim());
                    string name = values[1].Trim();
                    bool display = values[3].Trim() == "True";

                    FileList.Add(fileID, new File()
                    {
                        Name = name,
                        Display = display,
                    });
                }
            }
        }

        /// <summary>
        /// Represents a file with meta data info.
        /// </summary>
        public class File
        {
            /// <summary>
            /// The user defined name of the file.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Determines to display the contents of the file or not in editor.
            /// </summary>
            public bool Display { get; set; }
        }
    }
}
