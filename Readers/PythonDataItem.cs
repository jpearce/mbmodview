using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBModViewer
{
    /// <summary>
    /// DataItem implementation to hold python key/value pairs along with string source for PythonReader.
    /// Content is set to a static [1] and is accessed via Value.  Index doesn't really matter but is supported
    /// anyway.
    /// </summary>
    internal sealed class PythonDataItem : DataItem
    {
        #region properties
        //base unchanged:
        //Int64 ID { get { return _id; } }
        //String Name { get { return _name; } set { _name = value; } }
        //String Source { get { return _source; } set { _source = value; } }
        internal override Int64[] Content {
            get { throw new NotImplementedException("PythonDataItem does not allow access to Content.  Use Value instead."); }
            set { throw new NotImplementedException("PythonDataItem does not allow access to Content.  Use Value instead."); } 
        }
        internal Int64 Value { get { return this._content[0]; } set { this.HasValue = true;  this._content[0] = value; } }
        #endregion

        #region private fields
        //base:
        //Int32 _id;
        //String _name, _source;
        //Int64[] _content;
        internal String InlineComment; //might use this later
        internal Boolean HasValue; //because i don't want to rely on 0 meaning it doesn't have a value
        #endregion

        #region ctor
        /// <summary>Generic constructor.  References base constructor also sets Content to Int64[1]</summary>        
        /// <param name="ItemID">Reference ID.  This will be accessible as .ID</param>
        internal PythonDataItem(Int32 ItemID): base(ItemID)
        {
            this._content = new Int64[] { 0 };
            this.HasValue = false;
            this.InlineComment = String.Empty; //avoid null checking for GUI elements
            this.Name = String.Empty;
        }
        #endregion        
    }
}
