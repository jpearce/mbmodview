using System;
using System.Collections.Generic;

namespace MBModViewer
{
    internal class DataItemSettings
    {
        internal String Name;
        internal List<String> LineItemLabels;
        internal List<LineItemTypes> LineItemTypes;

        internal DataItemSettings(String name)
        {
            this.Name = name;
            this.LineItemLabels = new List<String>();
            this.LineItemTypes = new List<LineItemTypes>();
        }
    }

}
