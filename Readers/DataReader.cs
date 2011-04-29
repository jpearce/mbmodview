using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MBModViewer
{
    /// <summary>Generic class for python/ssv readers and formatting of DataItems.</summary>
    internal class DataReader
    {
        #region properties
        internal virtual FileInfo SourceFile { get { return _settings.FileLocation; } }
        internal virtual DataItem[] Items { get { return _items; } }
        internal virtual String Name { get { return this._settings.Name; } }
        internal virtual String ItemType { get { return this._settings.DataType.Name; } }
        #endregion

        #region fields
        protected DataItem[] _items;
        internal DataReaderSettings _settings;        
        #endregion

        /// <summary>Int64.TryParse wrapper with additional attempt at decoding 0x000 hex literals</summary>        
        internal static bool Int64TryParse(String val, out Int64 dest)
        {//so i'm not copy pasting this everywhere
            return (Int64.TryParse(val, out dest) ||
                (val.StartsWith("0x") && Int64.TryParse(val.Substring(2),
                    System.Globalization.NumberStyles.AllowHexSpecifier, null, out dest)) ||
                    (val.IndexOf('.') > 0 && Int64.TryParse(val.Remove(val.IndexOf('.')), out dest))
                );
        }

        /// <summary>Generic read function.  Contains a while(ReadLine()) loop and close.</summary>
        internal virtual void Read()
        {
            if (!this.SourceFile.Exists)
                throw new FileNotFoundException("File not found: " + this.SourceFile.FullName);
            List<DataItem> tempitems = new List<DataItem>();
            using (TextReader tr = new StreamReader(this.SourceFile.FullName))
            {
                if (!this.ReadFileHeader(tr))
                    throw new FormatException("File format not recognized: " + this.SourceFile.FullName);
                DataItem newitem = ReadItem(tr, tempitems.Count);
                if (newitem != null)
                {
                    do
                    {
                        tempitems.Add(newitem);
                        newitem = ReadItem(tr, tempitems.Count - 1);
                    }
                    while (newitem != null);
                }
                tr.Close();
            }
            this._items = tempitems.ToArray();
        }

        protected virtual DataItem ReadItem(TextReader reader, Int32 CurrentIndex)
        {            
            DataItem newitem = new DataItem(CurrentIndex);
            String linestr = reader.ReadLine();
            //temp hack for nonstandards like items with 3 OR 4 lines
            //until i think of a good xml solution for ~4 different weird options
            switch (this._settings.Name)
            {
                case "faction"://lines actually repeat into the next with a 0 for w/e reason
                    if (CurrentIndex == 0) { linestr = "0 " + linestr; }
                    else if (linestr == "0 ") { return null; }
                    break;
                case "item":
                    if (linestr != null && !linestr.StartsWith(" itm_")) { linestr = reader.ReadLine(); }                    
                    break;
                case "scene":
                    if (linestr != null && !linestr.StartsWith("scn_")) { linestr = reader.ReadLine(); }
                    break;
                case "scene_prop"://can be up to 3+ extra lines
                    while (linestr != null && !linestr.StartsWith("spr_")) { linestr = reader.ReadLine(); }                    
                    break;
                case "mission_tpl"://can be monstrous
                    while (linestr != null && !linestr.StartsWith("mst_")) { linestr = reader.ReadLine(); }
                    break;
                case "action"://weird one, basically if it's got 1 space it's good if it's 2 then keep going
                    while (linestr != null && linestr.StartsWith("  ")) { linestr = reader.ReadLine(); }
                    break;
                case "map_icon"://can have an extra blank line
                    while (linestr != null && linestr.Equals(String.Empty)) { linestr = reader.ReadLine(); }
                    break;
                case "presentation"://can be up to 3+ extra lines
                    while (linestr != null && !linestr.StartsWith("prsnt_")) { linestr = reader.ReadLine(); }
                    break;
            }
            //end hack
            if (linestr != null)
            {
                newitem.Source += linestr + "\n";
                List<Int64> tempcontents = new List<Int64>(64);
                List<String> tempstrings = new List<String>(4); //unlikely to use this
                for (int i = 0; i < this._settings.DataType.LineItemTypes.Count; ++i)
                {
                    switch (this._settings.DataType.LineItemTypes[i])
                    {
                        case LineItemTypes.Int64:
                            Int64 newint = 0;
                            if (this._settings.DataType.LineItemTypes.Count > (i + 1))
                            {
                                if (this._settings.DataType.LineItemTypes[i + 1] == LineItemTypes.LineEnd)
                                {
                                    Int64.TryParse(linestr, out newint);
                                }
                                else if (this._settings.DataType.LineItemTypes[i + 1] == LineItemTypes.Space)
                                {
                                    Int64.TryParse(linestr.Remove(linestr.IndexOf(' ')), out newint);
                                }
                            }
                            tempcontents.Add(newint);
                            break;
                        case LineItemTypes.String:
                            if (this._settings.DataType.LineItemLabels[i] == "name")
                            {
                                if (this._settings.DataType.LineItemTypes.Count > (i + 1))
                                {
                                    if (this._settings.DataType.LineItemTypes[i + 1] == LineItemTypes.LineEnd)
                                    {
                                        newitem.Name = linestr.Trim();
                                    }
                                    else if (this._settings.DataType.LineItemTypes[i + 1] == LineItemTypes.Space)
                                    {
                                        newitem.Name = linestr.Remove(linestr.IndexOf(' ')).Trim();
                                    }
                                }
                            }
                            else
                            {
                                if (this._settings.DataType.LineItemTypes.Count > (i + 1))
                                {
                                    if (this._settings.DataType.LineItemTypes[i + 1] == LineItemTypes.LineEnd)
                                    {
                                        tempstrings.Add(linestr.Trim());
                                    }
                                    else if (this._settings.DataType.LineItemTypes[i + 1] == LineItemTypes.Space)
                                    {
                                        tempstrings.Add(linestr.Remove(linestr.IndexOf(' ')).Trim());
                                    }
                                }
                            }
                            break;
                        case LineItemTypes.Space:
                            linestr = linestr.Substring(linestr.IndexOf(' ') + 1);
                            break;
                        case LineItemTypes.LineEnd:
                            linestr = reader.ReadLine();
                            newitem.Source += linestr + "\n";
                            break;
                    }
                }
                newitem.Content = tempcontents.ToArray();
                newitem.Strings = tempstrings.ToArray();
                if (this.ItemType == "globalvar") { newitem.Name = newitem.Source.Trim(); } //they don't get a name
                return newitem;
            }
            return null;
        }

        /// <summary>Generic constructor.</summary>
        /// <param name="File"></param>
        /// <param name="OutputItems"></param>
        internal DataReader(DataReaderSettings ReaderSettings)
        {
            this._settings = ReaderSettings;
        }

        /// <summary>This should read and verify the header, and also set the size of _items since
        /// they all contain the count at the beginning.</summary>
        /// <param name="reader">Open TextReader</param>
        /// <returns>Whether it recognized the initial lines and successfully set the Items array size.</returns>
        protected virtual bool ReadFileHeader(TextReader reader)
        {
            for (int i = 1; i < this._settings.StartLine; ++i) { reader.ReadLine(); }
            return true;
        }        

        internal String[] ListLabels()
        {
            List<String> templist = new List<string>(this._settings.DataType.LineItemLabels.Count);
            for (int i = 0; i < this._settings.DataType.LineItemLabels.Count; ++i)
            {
                if (this._settings.DataType.LineItemLabels[i] != null && this._settings.DataType.LineItemLabels[i] != "name")
                {
                    templist.Add(this._settings.DataType.LineItemLabels[i]);
                }
            }
            return templist.ToArray();
        }

        internal String[] ListContents(Int32 index)
        {
            return this._items[index].ListContents(this._settings.DataType);
        }

    }
}
