using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MBModViewer
{
    /// <summary>
    /// Generic class to hold DataItems.  Handles file locations, reading, etc.
    /// </summary>
    class DataContainer
    {
        /// <summary>Individual numbers assigned, ie for scripts these are the commands.</summary>
        internal virtual FileInfo FileLocation { get { return _fileloc; } }
        protected FileInfo _fileloc;

        internal String DataType;

        internal DataItem[] Items { get { return _dr.Items; } }
        protected DataReader _dr;

    }
}
