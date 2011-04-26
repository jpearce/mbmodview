using System;
using System.Collections.Generic;

using System.Text;
using System.IO;

namespace MBModViewer
{
    internal static class Header_Common
    {
        internal static FileInfo filelocation;
        internal static SortedDictionary<String, String> StringValues;
        internal static SortedDictionary<String, Int64> IntValues;

        /// <summary>Static initializer</summary>
        static Header_Common()
        {
            filelocation = new FileInfo(Config.GetSetting("filelocation_pydir") + "header_common.py");
            StringValues = new SortedDictionary<String, String>();
            IntValues = new SortedDictionary<String, Int64>();
        }

        #region fileread
        internal static void LoadFromFile()
        {//todo: want to check if we have any loaded & changed etc 

            if (filelocation.Exists)
            {
                StringValues.Clear();
                using (TextReader tr = new StreamReader(filelocation.FullName))
                {
                    String linestr, key, val;
                    Int32 indexequals, indexhash;
                    Boolean insidesomecode = false;
                    while ((linestr = tr.ReadLine()) != null)
                    {
                        linestr = linestr.Trim().Replace(",", String.Empty);
                        //for some reason some of them ahve commas at the end i don't claim to understand python
                        if (linestr.StartsWith("def "))
                        {
                            insidesomecode = true;
                        }
                        else if (linestr.StartsWith("return"))
                        {//idk if python makes every def return but in header_common they do
                            linestr = tr.ReadLine();
                            if (linestr == null) break; //break loop if we are null                          
                            linestr = linestr.Trim();
                            insidesomecode = false;
                        }
                        indexequals = linestr.IndexOf('=');
                        indexhash = linestr.IndexOf('#');
                        //if there is a hash and it's before any equals sigh, if there's no equals sign or if there's a deprecated
                        if (insidesomecode || (indexhash > -1 && indexhash <= indexequals)
                            || indexequals == -1 || linestr.IndexOf("deprecated") > -1)
                        {//skip line
                        }
                        else
                        {
                            if (indexhash > -1) { linestr = linestr.Remove(indexhash); }//chop off end #comments for now
                            key = linestr.Remove(indexequals).Trim();
                            val = (indexequals + 1 < linestr.Length ? linestr.Substring(indexequals + 1).Trim() : String.Empty);
                            if (!StringValues.ContainsKey(key)) { StringValues.Add(key, val); }
                        }
                    }
                }
            }
            parsevalues();
        }

        private static void parsevalues()
        {
            IntValues.Clear();
            Int64 parsedval;
            //pass 1 just get plain long values
            foreach (KeyValuePair<String, String> kvp in StringValues)
            {
                parsedval = 0;
                if (Int64.TryParse(kvp.Value, out parsedval) || (kvp.Value.StartsWith("0x") &&
                    Int64.TryParse(kvp.Value.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out parsedval)))
                {
                    IntValues.Add(kvp.Key, parsedval);
                }
            }
            //now run through them again with the literals defined
            //and do operations
            tryParse();
            //checkup
            bool allaccounted = false;
            for (int i = 0; i < 25 && !allaccounted; ++i)
            {//allow it to try 25 deep before giving up
                //what this means is if i have a line that says a=0 and c=a+1 and b=c+1 
                //b will not be assigned until the 2nd pass because c hasn't gotten parsed yet
                //allowing 10 passes got them all, 25 is overkill
                allaccounted = true;
                foreach (KeyValuePair<String, String> kvp in StringValues)
                {
                    if (!IntValues.ContainsKey(kvp.Key))
                    {
                        allaccounted = false;
                        break;
                    }
                }
                if (!allaccounted) { tryParse(); }
            }
            foreach (KeyValuePair<String, String> kvp in StringValues)
            {
                if (!IntValues.ContainsKey(kvp.Key))
                {
                    IntValues.Add(kvp.Key, 0);
                }
            }
        }

        private static void tryParse()
        {
            Int64 parsedval;
            String workingval;
            int indexsplit;
            Int64 tempval;
            String[] splitter;
            foreach (KeyValuePair<String, String> kvp in StringValues)
            {
                if (!IntValues.ContainsKey(kvp.Key))
                {
                    if (IntValues.ContainsKey(kvp.Value))
                    {//just a straight a=b
                        IntValues.Add(kvp.Key, IntValues[kvp.Value]);
                    }
                    else
                    {
                        parsedval = 0;
                        workingval = kvp.Value;
                        splitter = null;
                        //+ and << and XOR seem to be the only operations going on
                        indexsplit = workingval.IndexOf('+');
                        if (indexsplit > -1)
                        {//hit on add
                            splitter = workingval.Split('+');
                            for (int i = 0; i < splitter.Length; ++i)
                            {
                                workingval = splitter[i].Trim();
                                //if it's a literal just add it if it's not look it up
                                if (Int64.TryParse(workingval, out tempval)) { parsedval += tempval; }
                                else
                                {
                                    if (IntValues.ContainsKey(workingval)) { parsedval += IntValues[workingval]; }
                                    else
                                    {//problem!
                                        Console.WriteLine("Unknown value {0}", workingval);
                                    }
                                }
                            }
                            IntValues.Add(kvp.Key, parsedval);
                        }
                        //bitshift, not allowing more than 2 vars
                        indexsplit = workingval.IndexOf("<<");
                        if (indexsplit > -1)
                        {//hit on add
                            splitter = new String[] { workingval.Remove(indexsplit).Trim(), workingval.Substring(indexsplit + 2).Trim() };
                            if (Int64.TryParse(splitter[0], out tempval)) { parsedval = tempval; }
                            else if (IntValues.ContainsKey(splitter[0])) { parsedval = IntValues[splitter[0]]; }
                            tempval = 0; //just in case
                            //now check second, will end up doing parsedval << tempval
                            if (!Int64.TryParse(splitter[1], out tempval) &&
                                IntValues.ContainsKey(splitter[1])) { tempval = IntValues[splitter[1]]; }
                            parsedval = parsedval << (Int32)tempval;
                            if (tempval > 0) { IntValues.Add(kvp.Key, parsedval); } //if it's 0 we just shifted 0 which i would hope is not a real value
                        }
                        //XOR, not allowing more than 2 vars
                        indexsplit = workingval.IndexOf('|');
                        if (indexsplit > -1)
                        {//hit on add
                            splitter = new String[] { workingval.Remove(indexsplit).Trim(), workingval.Substring(indexsplit + 1).Trim() };                         
                            if (Int64.TryParse(splitter[0], out tempval)) { parsedval = tempval; }
                            else if (IntValues.ContainsKey(splitter[0])) { parsedval = IntValues[splitter[0]]; }
                            tempval = 0; //just in case
                            //now check second, will end up doing parsedval << tempval
                            if (!Int64.TryParse(splitter[1], out tempval) &&
                            IntValues.ContainsKey(splitter[1])) { tempval = IntValues[splitter[1]]; }
                            parsedval |= (UInt32)tempval;
                            if (tempval > 0) { IntValues.Add(kvp.Key, parsedval); } //if it's 0 we just XOR'd 0 which i would hope is not a real value                            
                        }
                    }
                }
            }
        }
        #endregion






    }
}
