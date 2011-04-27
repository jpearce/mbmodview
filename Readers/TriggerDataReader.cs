using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MBModViewer
{
    /// <summary>Reads triggers from triggers.txt.</summary>
    internal sealed class TriggerDataReader : DataReader
    {
        #region properties
        internal override FileInfo SourceFile { get { return _fileloc; } }
        internal override DataItem[] Items { get { return _triggeritems; } }
        internal TriggerDataItem[] TriggerItems { get { return _triggeritems; } }
        //String Name { get { return this._settings.Name; } }
        internal override String ItemType { get { return "trigger"; } }
        #endregion

        #region fields
        private FileInfo _fileloc; //no settings
        //DataItem[] _items = null; //not used
        private TriggerDataItem[] _triggeritems;
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
                    tr.ReadLine();//triggersfile version 1\n
                    String linestr = tr.ReadLine();                    
                    Int64 parsedval;
                    Int32 linenumber = 2;//linenumber is 1-based
                    if (!Int64TryParse(linestr, out parsedval)) { throw new FileLoadException("Invalid trigger count on line 2."); }
                    this._triggeritems = new TriggerDataItem[parsedval];
                    Module_OpCode.TriggerNames = new String[parsedval];
                    for (int i = 0; i < this._triggeritems.Length; ++i)
                    {
                        //no graceful error handling yet too much crap added when i tried
                        linestr = tr.ReadLine().Trim();//3 doubles #conditions[double space][block][double space]#execute [block]
                        ++linenumber;  
                        //double spaces
                        string[] doubles = (linestr.Remove(linestr.IndexOf("  ")).Trim()).Split(' '); //double space split by single                        
                        string temp = linestr.Substring(linestr.IndexOf("  ") + 2);
                        string[] executes = (temp.Substring(temp.IndexOf("  ") + 2).Trim()).Split(' ');
                        string[] conditions = (temp.Remove(temp.IndexOf("  ")).Trim()).Split(' ');;
                        //check delay rearm
                        this._triggeritems[i] = new TriggerDataItem(i);
                        this._triggeritems[i].Name = "Trigger" + i.ToString(); //don't think they have names but might be wrong
                        Module_OpCode.TriggerNames[i] = this._triggeritems[i].Name;
                        this._triggeritems[i].Source = linestr;
                        this._triggeritems[i].Check = Double.Parse(doubles[0]);
                        this._triggeritems[i].Delay = Double.Parse(doubles[1]);
                        this._triggeritems[i].Rearm = Double.Parse(doubles[2]);
                        //conditions block(separate Int64[])
                        this._triggeritems[i].Condition = new Int64[conditions.Length];
                        for (int j = 0; j < conditions.Length; ++j)
                        {
                            if (!Int64TryParse(conditions[j], out this._triggeritems[i].Condition[j])) 
                                { err_notint(linenumber, conditions[j]); }                            
                        }
                        //execute block(uses content Int64[])
                        this._triggeritems[i].Content = new Int64[executes.Length];
                        for (int j = 0; j < executes.Length; ++j)
                        {
                            if (!Int64TryParse(executes[j], out this._triggeritems[i].Content[j]))
                            { err_notint(linenumber, executes[j]); }
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
        internal TriggerDataReader(String filelocation)
            : base(null)
        {
            this._fileloc = new FileInfo(filelocation);
            this._items = null; //uses trigger items
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
