using System;
using System.Collections.Generic;

using System.Text;
using System.IO;

namespace MBModViewer
{
    internal static class Module_Script
    {
        internal static Boolean compat808 = false;
        internal static FileInfo filelocation;
        private static Script[] scripts;
        internal static String[] ScriptNames;
        private static Int64 minvaluebitshift;
        private static Int32 bitshifts;
        private static Dictionary<Int64, String> varmasks;
        /// <summary>list of possible or operations as in op1|op2 since there are several combinations that overlap
        /// if it's allowed to look at all ops</summary>
        private static HashSet<String> oroplist = 
            new HashSet<string>(new String[] { "this_or_next", "neg", "eq",  "neq", "lt", "le", "gt", "ge" });

        /// <summary>Static initializer</summary>
        static Module_Script()
        {
            filelocation = new FileInfo(Config.GetSetting("filelocation_txtdir") + "scripts.txt");            
            if(Config.GetSetting("compat808") != null && Config.GetSetting("compat808") == "true") { compat808 = true; }
            setvarmasks();
        }

        private static void setvarmasks()
        {//put them in a lookup so we can determine on the fly what they are
            varmasks = new Dictionary<long, string>();
            if(StaticDataHolder.Header_Common.HaveKeyWithValue("op_num_value_bits"))
            {
                bitshifts = (Int32)StaticDataHolder.Header_Common.KeyValue("op_num_value_bits");
                minvaluebitshift = 1 << bitshifts;
                foreach(PythonDataItem pdi in StaticDataHolder.Header_Common.PythonItems)
                {
                    if (pdi.Value >= minvaluebitshift)
                    {
                        try
                        {
                            switch (pdi.Name)
                            {
                                //these will overlap, opmask_variable is the same value as tag_variable
                                //thus the try/catch
                                case "opmask_local_variable":
                                    varmasks.Add(pdi.Value, "[local_variable]");
                                    break;
                                case "opmask_variable":
                                    varmasks.Add(pdi.Value, "[variable]");
                                    break;
                                case "opmask_register":
                                    varmasks.Add(pdi.Value, "#register");
                                    break;
                                case "opmask_quest_index":
                                    varmasks.Add(pdi.Value, "[quest]");
                                    break;
                                case "opmask_quick_string":
                                    varmasks.Add(pdi.Value, "[quick_string]");
                                    break;
                                default:
                                    if (!pdi.Name.StartsWith("reg"))
                                    {

                                        varmasks.Add(pdi.Value, String.Format("[{0}]", pdi.Name));//already masked                                

                                    }

                                    break;
                            }
                        }
                        catch (System.ArgumentException ex)
                        {//already in the dictionary

                        }
                    }
                    else if (pdi.Name.StartsWith("tag_") && pdi.Name.Length > 4)
                    {
                        try
                        {
                            if (!varmasks.ContainsKey((pdi.Value << bitshifts)))
                            {
                                varmasks.Add(pdi.Value << bitshifts, String.Format("[{0}]", pdi.Name.Substring(4)));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
        }

        #region fileread
        internal static void LoadFromFile()
        {//todo: want to check if we have any loaded & changed etc 
            if(filelocation.Exists)
            {                
                using (TextReader tr = new StreamReader(filelocation.FullName))
                {
                    tr.ReadLine();//scriptsfile version 1\n
                    String linestr = tr.ReadLine();
                    String[] tokens;
                    Int32 scriptcount, indexspace, linenumber = 2;//1-based
                    if (!Int32.TryParse(linestr, out scriptcount)) { throw new FileLoadException("Invalid script count on line 2."); }
                    scripts = new Script[scriptcount];
                    ScriptNames = new String[scriptcount];
                    for(int i = 0; i < scripts.Length; ++i)
                    {
                        linestr = tr.ReadLine();
                        ++linenumber;
                        //script_name -1 (have not seen otherwise)
                        linestr = linestr.Trim();
                        if (Object.ReferenceEquals(linestr, null) || String.Empty.Equals(linestr)) { err_blankoreof(linenumber); }                            
                        indexspace = linestr.IndexOf(' ');
                        if (indexspace == -1) { err_badheader(linenumber, linestr); }                            
                        scripts[i] = new Script(linestr.Remove(indexspace));
                        ScriptNames[i] = scripts[i].ScriptName;
                        linestr = tr.ReadLine();
                        ++linenumber;
                        if(!Object.ReferenceEquals(linestr,null)) {linestr = linestr.Trim();}//not sure if it throws on null                        
                        if (Object.ReferenceEquals(linestr, null) || String.Empty.Equals(linestr)) { err_blankoreof(linenumber); }
                        indexspace = linestr.IndexOf(' ');
                        if (indexspace > -1)
                        {
                            tokens = linestr.Split(' ');
                            scripts[i].Contents = new Int64[tokens.Length];
                            for (int j = 0; j < scripts[i].Contents.Length; ++j)
                            {
                                scripts[i].Contents[j] = tryparselong(linenumber, tokens[j]); 
                            }
                        }
                        else
                        {
                            scripts[i].Contents = new Int64[] { tryparselong(linenumber, linestr) };
                        }                        
                    }
                }
            }
        }

        
        #endregion

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

        #region subclasses
        //subclassed so it can read the private in Module_Scripts
        internal struct Script
        {
            internal String ScriptName;
            internal Int64[] Contents;

            internal Script(String name)
            {
                this.ScriptName = name;
                this.Contents = new Int64[0];
            }
        }
        #endregion

        #region utility
        private static Int64 tryparselong(Int32 linenumber, String value)
        {
            Int64 retval;
            if (!Int64.TryParse(value, out retval)) 
            {
                err_notint(linenumber, value); 
            }
            return retval;
        }
        #endregion

        #region formatting
        internal static void SetScriptContents(String scriptname, out String[] outlines)
        {
            List<string> translatedStatements = new List<string>(64);
            for (int i = 0; i < ScriptNames.Length; ++i)
            {
                if (ScriptNames[i] == scriptname)
                {
                    Script s = scripts[i];
                    int indents = 0;
                    //like so:
                    //start--
                    //# of total statements
                    //operation
                    //# of args
                    //arg1...
                    //# of args
                    //arg1...
                    //end--
                    //ignore first line
                    for (int j = 1; j < s.Contents.Length; ++j)
                    {
                        translateLine(ref s.Contents, ref j, ref translatedStatements, ref indents);
                    }                    
                }
            }
            outlines = translatedStatements.ToArray();
        }

        private const int numspaceindent = 4;
        private static String doIndents(int numindents, String toindent)
        {
            char[] newstring = new char[toindent.Length + (numindents * numspaceindent)];
            Array.Copy(toindent.ToCharArray(), 0, newstring, (numindents * numspaceindent), toindent.Length);
            for (int i = 0; i < (numindents * numspaceindent); ++i) { newstring[i] = ' '; }
            return new String(newstring);
        }

        private static void translateLine(ref Int64[] contents, ref Int32 index, ref List<String> statements, ref int indents)
        {
            String workstr = translateOperation(contents[index]);
            bool postindent = false; //we want to indent AFTER the try so it reads
            //try_begin
            //[indent]statement
            //try_end
            if (contents.Length > ++index)
            {
                switch (workstr)
                {
                    case "try_begin":
                        postindent = true;
                        break;                    
                    case "try_for_range":
                        postindent = true;
                        break;
                    case "try_for_range_backwards":
                        postindent = true;
                        break;
                    case "try_for_parties":
                        postindent = true;
                        break;
                    case "try_for_agents":
                        postindent = true;
                        break;
                    case "else_try":
                        if (indents > 0) { --indents; }
                        postindent = true; //drop it down 1 then put it back
                        break;
                    case "try_end":
                        if (indents > 0) { --indents; }
                        break;
                }
                workstr = "(" + workstr;
                if (compat808)
                {//either a 0 or nonzero and 2 following args
                    workstr += ", ";
                    if (contents[index] > 0)
                    {//if == 0 then we're on the last arg already which is where we want to end up                    
                        for (int i = 0; i < 3; ++i)
                        {
                            workstr += prettyVar(contents[index + i]);
                            if (i < 2) { workstr += ", "; }
                        }
                        --index;
                    }       
                    
                }
                else if (contents[index] > 0)
                {
                    workstr += ", ";
                    int statementend = (Int32)contents[index] + (index + 1);
                    ++index;//skip past the # args
                    while (index < statementend)
                    {
                        workstr += prettyVar(contents[index]);
                        if (++index < statementend) { workstr += ", "; }
                    }
                    --index;
                }
                workstr += "),";
                
            }
            statements.Add(doIndents(indents, workstr));
            if (postindent) { ++indents; }
        }

        
        private static String translateOperation(Int64 refnum)
        {
            String retVal = String.Empty;
            if (StaticDataHolder.Header_Operations.HaveValue(refnum)) 
            {
                retVal += StaticDataHolder.Header_Operations.ValueKey(refnum);
            }
            else
            {//check for OR operations, one of them must be in oroplist                             
                for (int i = 0; i < StaticDataHolder.Header_Operations.PythonItems.Length && ReferenceEquals(retVal, String.Empty); ++i)
                {
                    if (oroplist.Contains(StaticDataHolder.Header_Operations.PythonItems[i].Name))
                    {
                        for (int j = 0; j < i; ++j)
                        {//n^2 :(
                            if ((StaticDataHolder.Header_Operations.PythonItems[i].Value |
                                StaticDataHolder.Header_Operations.PythonItems[j].Value) == refnum)
                            {
                                retVal = StaticDataHolder.Header_Operations.PythonItems[i].Name + "|" +
                                        StaticDataHolder.Header_Operations.PythonItems[j].Name;
                                //add this so next time we don't have to do this again                            
                                StaticDataHolder.Header_Operations.AddItem(retVal,
                                    (StaticDataHolder.Header_Operations.PythonItems[i].Value |
                                        StaticDataHolder.Header_Operations.PythonItems[j].Value));
                                break;
                            }
                        }
                        if (ReferenceEquals(retVal, String.Empty))
                        {
                            for (int j = i + 1; j < StaticDataHolder.Header_Operations.PythonItems.Length; ++j)
                            {//n^2 :(
                                if ((StaticDataHolder.Header_Operations.PythonItems[i].Value |
                                    StaticDataHolder.Header_Operations.PythonItems[j].Value) == refnum)
                                {
                                    retVal = StaticDataHolder.Header_Operations.PythonItems[i].Name + "|" +
                                            StaticDataHolder.Header_Operations.PythonItems[j].Name;
                                    //add this so next time we don't have to do this again                            
                                    StaticDataHolder.Header_Operations.AddItem(retVal,
                                        (StaticDataHolder.Header_Operations.PythonItems[i].Value |
                                            StaticDataHolder.Header_Operations.PythonItems[j].Value));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (String.ReferenceEquals(retVal, String.Empty)) { retVal = "UNKNOWN_OPERATION#" + refnum.ToString(); }
            return retVal;
        }

        private static String prettyVar(Int64 refnum)
        {
            string retVal = String.Empty;
            
            foreach (KeyValuePair<Int64, String> kvp in varmasks)
            {
                if ((refnum ^ kvp.Key) < minvaluebitshift && (refnum ^ kvp.Key) >= 0)
                {
                    retVal += tryGetVarName(kvp.Value, (refnum ^ kvp.Key));
                    if (retVal == String.Empty)
                    {
                        retVal += kvp.Value + (refnum ^ kvp.Key).ToString();
                    }
                }
            }
            if (retVal == string.Empty) 
            { 
                retVal += refnum.ToString(); 
            }
            return retVal;
        }

        private static String tryGetVarName(String type, Int64 val)
        {
            string retVal = String.Empty;
            try
            {
                switch (type)
                {
                    case "[script]":
                        retVal = "\"script_" + scripts[val].ScriptName + "\"";
                        break;                    
                    case "[local_variable]":
                        retVal = "\":local" + val.ToString() + "\""; 
                        break;
                    case "#register":
                        retVal = "reg" + val.ToString();
                        break;                    
                    default:                        
                        retVal = StaticDataHolder.FindVarName(type, (Int32)val);
                        break;
                }
            }
            catch (IndexOutOfRangeException ex)
            {//will just return the String.Empty
                
            }
            return retVal;
        }

        #endregion
    }
}
