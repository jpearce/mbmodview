using System;
using System.IO;
using System.Collections.Generic;

namespace MBModViewer
{
    /// <summary>
    /// DataReader implementation that reads variable decs from Python files (.py)
    /// Overrides items to be PythonDataItems specifically, probably should have made
    /// DataReader an interface instead but too late now!
    /// </summary>
    internal sealed class PythonReader : DataReader
    {
        #region properties
        //base unchanged:                
        //String Name { get { return this._settings.Name; } }
        internal override DataItem[] Items { get { return (DataItem[])this._pythonitems; } }
        internal override FileInfo SourceFile { get { return this._fileloc; } }
        internal override String ItemType { get { return "python"; } }
        internal PythonDataItem[] PythonItems { get { return this._pythonitems; } }//the preferred method
        #endregion

        #region fields
        //baes unchanged:
        //protected DataItem[] _items;
        //DataReaderSettings _settings;
        private FileInfo _fileloc;
        private PythonDataItem[] _pythonitems;
        #endregion

        #region ctor
        /// <summary>Base constructor plus filelocation</summary>        
        internal PythonReader(DataReaderSettings ReaderSettings, String FileLocation)
            : base(ReaderSettings)
        {
            this._fileloc = new FileInfo(FileLocation);
            this._items = null;
        }
        #endregion

        #region reads
        /// <summary>Reads lines, for Python it's just looking for a = b, recording the source but not
        /// attempting to resolve the values at this stage if they're not literals.</summary>
        internal override void Read()
        {
            if (!this.SourceFile.Exists)
                throw new FileNotFoundException("File not found: " + this.SourceFile.FullName);
            List<PythonDataItem> tempitems = new List<PythonDataItem>();
            using (TextReader tr = new StreamReader(this.SourceFile.FullName))
            {
                PythonDataItem newitem = ReadPythonItem(tr, tempitems.Count);
                if (newitem != null)
                {
                    do
                    {
                        tempitems.Add(newitem);
                        newitem = ReadPythonItem(tr, tempitems.Count - 1);
                    }
                    while (newitem != null);
                }
                tr.Close();
            }
            this._pythonitems = tempitems.ToArray();
            PostResolve();
        }

        /// <summary> Specific PythonDataItem return method to avoid boxing.</summary>        
        private PythonDataItem ReadPythonItem(TextReader reader, int CurrentIndex)
        {
            PythonDataItem pdi = new PythonDataItem(CurrentIndex);
            String linestr = reader.ReadLine();
            Int32 indexequals, indexhash;
            while (linestr != null)
            {
                //Source in PythonDataItem isn't going to be the whole thing just after the = and before any #
                linestr = linestr.Trim().Replace(",", String.Empty);//remove commas
                indexequals = linestr.IndexOf('=');
                indexhash = linestr.IndexOf('#');
                //if there is a hash and it's before any equals sigh, if there's no equals sign or if there's a deprecated                
                if (linestr.IndexOf('[') > -1 && (indexhash == -1 || indexhash > linestr.IndexOf('[')))
                {
                    //start of an array like lhs_operations etc
                    //keep going until we hit a ] - if it's a one liner then it will just go to the next readline
                    while (linestr != null && linestr.IndexOf(']') == -1)
                    {
                        linestr = reader.ReadLine();
                    }
                    if (linestr == null) { break; } //EOF
                    else { linestr = reader.ReadLine(); } //have to do it this way in case we're EOF
                    if (linestr == null) { break; }
                    linestr = linestr.Trim(); //prep for next line
                }
                else if (linestr.StartsWith("def"))
                {
                    //start of a function - they don't all have to return but in the 2 we look at they do at this time
                    //keep looping till we find a return (or hit EOF)
                    while (linestr != null && linestr.IndexOf("return") == -1)
                    {
                        linestr = reader.ReadLine().TrimStart();//has to be trimmed so we can use StartsWith
                    }
                    if (linestr == null) { break; } //EOF
                    else { linestr = reader.ReadLine(); } //have to do it this way in case we're EOF
                    if (linestr == null) { break; }
                    linestr = linestr.Trim(); //prep for next line
                }
                else if ((indexhash > -1 && indexhash <= indexequals)
                || indexequals == -1 || linestr.IndexOf("deprecated") > -1)
                {//skip line
                    linestr = reader.ReadLine();
                }
                else
                {
                    if (indexhash > -1)
                    {//chop off end #comments for now but store them in InlineComment for possible future use
                        pdi.InlineComment = linestr.Substring(indexhash + 1).Trim();
                        linestr = linestr.Remove(indexhash);
                    }
                    pdi.Name = linestr.Remove(indexequals).Trim();
                    pdi.Source = (indexequals + 1 < linestr.Length ? linestr.Substring(indexequals + 1).Trim() : String.Empty);                    
                    //short attempt to resolve, if it's not a literal or hex literal then it will not happen here
                    Int64 parsedval = 0; //because we can't pass pdi.Value to Int64.TryParse
                    if (Int64TryParse(pdi.Source, out parsedval))
                    {
                        pdi.Value = parsedval;
                    }
                    
                    break;
                }
            }
            if (linestr == null) { return null; } //means we hit EOF
            return pdi;
        }

        protected override DataItem ReadItem(TextReader reader, int CurrentIndex)
        {
            throw new NotImplementedException("Use ReadPythonItem");
        }

        protected override bool ReadFileHeader(TextReader reader)
        {
            throw new NotImplementedException("Not applicable to PythonReader.");
        }
        #endregion

        #region lookups

        /// <summary>Should only ever get a false during the processing stage (hopefully!) for a valid
        /// key</summary>        
        internal bool HaveValue(Int64 longvalue)
        {
            for (int i = 0; i < this._pythonitems.Length; ++i)
            {
                if (this._pythonitems[i].Value == longvalue)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Should only ever get a false during the processing stage (hopefully!) for a valid
        /// key</summary>        
        internal bool HaveKeyWithValue(String varname)
        {
            for (int i = 0; i < this._pythonitems.Length; ++i)
            {
                if (this._pythonitems[i].Name == varname)
                {
                    return this._pythonitems[i].HasValue;
                }
            }
            return false;
        }

        /// <summary>Returns value for key given varname</summary>        
        internal Int64 KeyValue(string varname)
        {
            for (int i = 0; i < this._pythonitems.Length; ++i)
            {
                if (this._pythonitems[i].Name == varname)
                {
                    return this._pythonitems[i].Value;
                }
            }
            throw new Exception("Key not found for variable " + varname);
        }

        /// <summary>Reverse of KeyValue, returns varname for Int64 value</summary> 
        /// <returns>Variable name or null if not found.</returns>
        internal String ValueKey(Int64 value)
        {
            for (int i = 0; i < this._pythonitems.Length; ++i)
            {
                if (this._pythonitems[i].Value == value)
                {
                    return this._pythonitems[i].Name;
                }
            }
            return null;
        }
        #endregion

        #region postload value resolution
        /// <summary>Resolve any values that weren't literals</summary>
        private void PostResolve()
        {
            //initialcheck
            bool allresolved = true;
            for (int i = 0; i < this._pythonitems.Length && allresolved; ++i)
            {
                allresolved = this._pythonitems[i].HasValue;
            }
            //if initial passed this will skip otherwise will go up to 25 deep
            for (int i = 0; i < 25 && !allresolved; ++i)
            {
                allresolved = true;
                tryParse();
                for (int j = 0; j < this._pythonitems.Length && allresolved; ++j)
                {
                    allresolved = this._pythonitems[j].HasValue;
                }
            }
        }

        private void tryParse()
        {
            Int64 parsedval;
            String workingval;
            int indexsplit;
            Int64 tempval;
            String[] splitter;
            bool foundallvals;//was checking for 0 this is safer
            for (int i = 0; i < this._pythonitems.Length; ++i)
            {
                if (!this._pythonitems[i].HasValue)
                {
                    foundallvals = false;
                    workingval = this._pythonitems[i].Source;//should be everything between = (and optionally #)
                    if (this.HaveKeyWithValue(workingval))
                    {
                        this._pythonitems[i].Value = this.KeyValue(workingval);
                        foundallvals = true;
                    }
                    if (!this._pythonitems[i].HasValue)
                    {//didn't find it, now we check splits
                        parsedval = 0;
                        splitter = null;
                        //+ and << and OR seem to be the only operations going on
                        indexsplit = workingval.IndexOf('+');
                        if (indexsplit > -1)
                        {//hit on add
                            splitter = workingval.Split('+');
                            foundallvals = true;
                            for (int j = 0; j < splitter.Length && foundallvals; ++j)
                            {
                                foundallvals = false; //have to find every value or we might get incorrect values
                                workingval = splitter[j].Trim();
                                //if it's a literal just add it if it's not look it up
                                if (Int64TryParse(workingval, out tempval))
                                {
                                    parsedval += tempval;
                                    foundallvals = true;
                                }//literal value
                                else
                                {
                                    if (this.HaveKeyWithValue(workingval))
                                    {
                                        parsedval += this.KeyValue(workingval);
                                        foundallvals = true;
                                    }
                                }
                            }
                            if (foundallvals) { this._pythonitems[i].Value = parsedval; }
                        }
                    }
                    if (!this._pythonitems[i].HasValue)
                    {//bitshift, not allowing more than 2 vars since i don't see it and have no idea of python ooo
                        indexsplit = workingval.IndexOf("<<");
                        if (indexsplit > -1)
                        {//hit on symbol
                            foundallvals = false;
                            //allow 2 only
                            splitter = new String[] { workingval.Remove(indexsplit).Trim(), workingval.Substring(indexsplit + 2).Trim() };
                            parsedval = 0; //otherwise the final op complains it's not initialized
                            if (Int64TryParse(splitter[0], out tempval))
                            {
                                parsedval = tempval;
                                foundallvals = true;
                            }
                            if (this.HaveKeyWithValue(splitter[0]))
                            {
                                parsedval = this.KeyValue(splitter[0]);
                                foundallvals = true;
                            }
                            if (!foundallvals) { break; } //didn't get 1st value
                            else { foundallvals = false; }
                            tempval = 0; //just in case
                            //now check second, will end up doing parsedval << tempval
                            if (Int64TryParse(splitter[1], out tempval))
                            {
                                foundallvals = true;
                            }
                            if (this.HaveKeyWithValue(splitter[1]))
                            {
                                tempval = this.KeyValue(splitter[1]);
                                foundallvals = true;
                            }
                            if (!foundallvals) { break; } //didn't get 2nd value                            
                            this._pythonitems[i].Value = (parsedval << (Int32)tempval);
                        }
                    }
                    if (!this._pythonitems[i].HasValue)
                    {//bitwise OR, not allowing more than 2 vars since i don't see it and have no idea of python ooo
                        indexsplit = workingval.IndexOf("|");
                        parsedval = 0; //otherwise the final op complains it's not initialized
                        if (indexsplit > -1)
                        {//hit on symbol
                            foundallvals = false;
                            //allow 2 only
                            splitter = new String[] { workingval.Remove(indexsplit).Trim(), workingval.Substring(indexsplit + 1).Trim() };
                            if (Int64TryParse(splitter[0], out tempval))
                            {
                                parsedval = tempval;
                                foundallvals = true;
                            }
                            if (this.HaveKeyWithValue(splitter[0]))
                            {
                                parsedval = this.KeyValue(splitter[0]);
                                foundallvals = true;
                            }
                            if (!foundallvals) { break; } //didn't get 1st value
                            else { foundallvals = false; }
                            tempval = 0; //just in case
                            //now check second, will end up doing parsedval << tempval
                            if (Int64TryParse(splitter[1], out tempval))
                            {
                                foundallvals = true;
                            }
                            if (this.HaveKeyWithValue(splitter[1]))
                            {
                                tempval = this.KeyValue(splitter[1]);
                                foundallvals = true;
                            }
                            if (!foundallvals) { break; } //didn't get 2nd value                            
                            this._pythonitems[i].Value = (parsedval | tempval);
                        }
                    }
                }
            }
        }
        #endregion

        /// <summary>On the fly additions like operation1|operation2</summary>        
        internal void AddItem(String pdiname, Int64 pdivalue)
        {
            PythonDataItem[] newitems = new PythonDataItem[this._pythonitems.Length + 1];
            Array.Copy(this._pythonitems, newitems, this._pythonitems.Length);
            newitems[this._pythonitems.Length] = new PythonDataItem(this._pythonitems.Length);
            newitems[this._pythonitems.Length].Name = pdiname;
            newitems[this._pythonitems.Length].Value = pdivalue;
            this._pythonitems = newitems;
        }
    }


    
}

