﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace PartyStudio
{
    /// <summary>
    /// Represents a board for space data and path rendering.
    /// </summary>
    public class BoardLoader
    {
        public List<string> SpaceTypeList = new List<string>();

        public static string ActiveSpaceType = "";

        /// <summary>
        /// List of active spaces for loading.
        /// </summary>
        public virtual List<Space> Spaces { get; set; } = new List<Space>();

        /// <summary>
        /// The type of space instance to create.
        /// </summary>
        public virtual Type SpaceType => typeof(Space);

        /// <summary>
        /// The path renderer to display and edit paths on.
        /// </summary>
        public BoardPathRenderer PathRender;

        public virtual void LoadFile(MapEditorPlugin mapEditor, Stream data, string fileName)
        {

        }

        public virtual void SaveFile(MapEditorPlugin mapEditor, Stream data)
        {
        }
    }
}
