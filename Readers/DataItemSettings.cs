using System;
using System.Collections.Generic;

namespace MBModViewer
{
    internal class DataItemSettings
    {
        internal String Name;
        internal List<String> LineItemIDs;
        internal List<LineItemTypes> LineItemTypes;

        internal DataItemSettings(String name)
        {
            this.Name = name;
            this.LineItemIDs = new List<String>();
            this.LineItemTypes = new List<LineItemTypes>();
        }
    }

}
