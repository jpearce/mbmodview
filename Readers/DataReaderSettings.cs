using System;
using System.IO;

namespace MBModViewer
{
    internal class DataReaderSettings
    {
        internal String Name;
        internal DataItemSettings DataType;
        internal FileInfo FileLocation;
        internal Int32 StartLine;

        internal DataReaderSettings(String name)
        {
            this.Name = name;
        }
    }
}
