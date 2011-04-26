using System;

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
        #endregion

        #region private fields
        protected Int32 _id;
        protected String _name, _source;
        protected Int64[] _content;

        #endregion

        #region ctor
        /// <summary>Generic constructor.</summary>        
        /// <param name="ItemID">Reference ID.  This will be accessible as .ID</param>
        internal DataItem(Int32 ItemID)
        {
            this._id = ItemID;            
        }
        #endregion        
    }
}
