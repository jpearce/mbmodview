using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MBModViewer
{
    /// <summary>Reads scripts from scripts.txt.</summary>
    internal class ScriptDataReader : DataReader
    {
        #region properties
        internal override FileInfo SourceFile { get { return _fileloc; } }
        //DataItem[] Items { get { return _items; } }
        //String Name { get { return this._settings.Name; } }
        internal override String ItemType { get { return "script"; } }
        #endregion

        #region fields
        private FileInfo _fileloc; //no settings
        //protected DataItem[] _items;
        //internal DataReaderSettings _settings;
        #endregion

        /// <summary>Generic read function.  Contains a while(ReadLine()) loop and close.</summary>
        internal override void Read()
        {
            if (!this.SourceFile.Exists)
                throw new FileNotFoundException("File not found: " + this.SourceFile.FullName);            
            if (_fileloc.Exists)
            {
                using (TextReader tr = new StreamReader(_fileloc.FullName))
                {
                    tr.ReadLine();//scriptsfile version 1\n
                    String linestr = tr.ReadLine();
                    String[] tokens;
                    Int64 parsedval;
                    Int32 indexspace, linenumber = 2;//1-based
                    if (!Int64TryParse(linestr, out parsedval)) { throw new FileLoadException("Invalid script count on line 2."); }
                    this._items = new DataItem[parsedval];
                    Module_OpCode.ScriptNames = new String[parsedval];
                    for (int i = 0; i < this._items.Length; ++i)
                    {
                        linestr = tr.ReadLine();//script_name -1 (have not seen otherwise)
                        ++linenumber;                        
                        linestr = linestr.Trim();
                        if (Object.ReferenceEquals(linestr, null) || String.Empty.Equals(linestr)) { err_blankoreof(linenumber); }
                        indexspace = linestr.IndexOf(' ');
                        if (indexspace == -1) { err_badheader(linenumber, linestr); }
                        this._items[i] = new DataItem(i);
                        this._items[i].Name = linestr.Remove(indexspace);
                        Module_OpCode.ScriptNames[i] = this._items[i].Name;
                        linestr = tr.ReadLine();
                        ++linenumber;
                        if (!Object.ReferenceEquals(linestr, null)) { linestr = linestr.Trim(); }//not sure if it throws on null                        
                        if (Object.ReferenceEquals(linestr, null) || String.Empty.Equals(linestr)) { err_blankoreof(linenumber); }
                        indexspace = linestr.IndexOf(' ');
                        if (indexspace > -1)
                        {
                            tokens = linestr.Split(' ');
                            this._items[i].Content = new Int64[tokens.Length];
                            for (int j = 0; j < this._items[i].Content.Length; ++j)
                            {
                                if (!Int64TryParse(tokens[j], out this._items[i].Content[j])) { err_notint(linenumber, tokens[j]); }
                            }
                        }
                        else
                        {//1 number only (unlikely!)
                            this._items[i].Content = new Int64[1];
                            if (!Int64TryParse(linestr, out this._items[i].Content[0])) { err_notint(linenumber, linestr); }
                        }
                    }
                }
            }
        }


        #region errors
        private static void err_blankoreof(Int32 linenumber)
        {
            throw new FormatException(String.Format("Unexpected blank line or end of file: \nLine #{0}", linenumber));
        }

        private static void err_badheader(Int32 linenumber, String linestr)
        {
            throw new FormatException(String.Format("Unrecognized script declaration: \nLine#{0} ({1})", linenumber, linestr));
        }

        private static void err_notint(Int32 linenumber, String value)
        {
            throw new FormatException(String.Format("Unable to parse integer value: \nLine#{0} ({1})", linenumber, value));
        }
        #endregion

        protected override DataItem ReadItem(TextReader reader, Int32 CurrentIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>Generic constructor.</summary>        
        internal ScriptDataReader(String filelocation) : base(null) 
        {
            this._fileloc = new FileInfo(filelocation);
        }

        /// <summary>This should read and verify the header, and also set the size of _items since
        /// they all contain the count at the beginning.</summary>
        /// <param name="reader">Open TextReader</param>
        /// <returns>Whether it recognized the initial lines and successfully set the Items array size.</returns>
        protected override bool ReadFileHeader(TextReader reader)
        {
            throw new NotImplementedException();
        }



    }
}
