using System;
using System.Collections.Generic;

namespace MBModViewer
{
    /// <summary>Base class for data items</summary>
    internal class DataItem
    {
        #region properties
        /// <summary>The number that will actually be displayed in ___.txt</summary>
        internal virtual Int64 ID { get { return _id; } }
        /// <summary>Common name (if applicable).</summary>
        internal virtual String Name { get { return _name; } set { _name = value; } }
        /// <summary>Literal string read from the file for this item.</summary>
        internal virtual String Source { get { return _source; } set { _source = value; } }
        /// <summary>Individual numbers assigned, ie for scripts these are the commands.</summary>
        internal virtual Int64[] Content { get { return _content; } set { _content = value; } }
        /// <summary>Random string values, in order that DataItem lists them.</summary>
        internal virtual String[] Strings { get { return _strings; } set { _strings = value; } }        
        #endregion

        #region private fields
        protected Int32 _id;
        protected String _name, _source;
        protected Int64[] _content;
        protected String[] _strings;
        #endregion

        #region ctor
        /// <summary>Generic constructor.</summary>        
        /// <param name="ItemID">Reference ID.  This will be accessible as .ID</param>
        internal DataItem(Int32 ItemID)
        {
            this._id = ItemID;            
        }
        #endregion        

        internal String[] ListContents(DataItemSettings dsi)
        {//counterpart to listlabels
            List<String> retval = new List<String>(dsi.LineItemLabels.Count);
            Int32 countstr = 0, countint = 0;
            for (int i = 0; i < dsi.LineItemLabels.Count; ++i)
            {
                if(dsi.LineItemLabels[i] != null)
                {
                    if(dsi.LineItemTypes[i] == LineItemTypes.Int64)
                    {
                        retval.Add(this._content[countint].ToString());
                        ++countint;
                    }
                    else if(dsi.LineItemTypes[i] == LineItemTypes.String && dsi.LineItemLabels[i] != "name")
                    {
                        retval.Add(this._strings[countstr]);
                        ++countstr;
                    }
                }
            }
            return retval.ToArray();
        }


    }
}
