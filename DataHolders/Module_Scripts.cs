using System;
using System.Collections.Generic;

using System.Text;
using System.IO;

namespace MBScriptEditor
{
    internal static class Module_Scripts
    {
        internal static FileInfo filelocation;
        private static Script[] scripts;
        internal static ScriptLookup ByName;
        internal static String[] ScriptNames;
        private static Int64 minvaluebitshift;
        private static Int32 bitshifts;
        private static Dictionary<Int64, String> varmasks;

        /// <summary>Static initializer</summary>
        static Module_Scripts()
        {
            filelocation = new FileInfo(Config.GetSetting("filelocation_scripts_txt"));
            if(filelocation.Exists) {
                Config.SetSetting("filelocation_scripts_txt", filelocation.FullName);                
            }
            ByName = new ScriptLookup();
            setvarmasks();
        }

        private static void setvarmasks()
        {//put them in a lookup so we can determine on the fly what they are
            varmasks = new Dictionary<long, string>();
            if(Header_Common.UintValues.ContainsKey("op_num_value_bits"))
            {
                bitshifts = (Int32)Header_Common.UintValues["op_num_value_bits"];
                minvaluebitshift = 1 << bitshifts;
                foreach(KeyValuePair<String, Int64> kvp in Header_Common.UintValues)
                {
                    if (kvp.Value >= minvaluebitshift)
                    {
                        switch (kvp.Key)
                        {
                            case "opmask_local_variable":
                                varmasks.Add(kvp.Value, "[localvar]");
                                break;
                            case "opmask_variable":
                                varmasks.Add(kvp.Value, "$globalvar");
                                break;
                            case "opmask_register":
                                varmasks.Add(kvp.Value, "#register");
                                break;
                            case "opmask_quest_index":
                                varmasks.Add(kvp.Value, "@questvar");
                                break;
                            case "opmask_quick_string":
                                varmasks.Add(kvp.Value, "!qstring");
                                break;
                            default:
                                if (!kvp.Key.StartsWith("reg"))
                                {
                                    varmasks.Add(kvp.Value, String.Format("[{0}]", kvp.Key));//already masked                                
                                }
                                break;
                        }
                    }
                    else if (kvp.Key.StartsWith("tag_") && kvp.Key.Length > 4)
                    {
                        try
                        {
                            if (!varmasks.ContainsKey((kvp.Value << bitshifts)))
                            {
                                varmasks.Add(kvp.Value << bitshifts, String.Format("[{0}]", kvp.Key.Substring(4)));
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
        //solely so i can say ByName[ScriptName]
        internal sealed class ScriptLookup
        {
            internal Script this[String name]
            {
                get
                {
                    for (int i = 0; i < scripts.Length; i++)
                    {
                        if (scripts[i].ScriptName.Equals(name, StringComparison.InvariantCultureIgnoreCase)) { return scripts[i]; }
                    }
                    throw new NullReferenceException(String.Format("Given script name {0} was not found.", name));
                }
            }
        }

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
            Script s = ByName[scriptname];
            List<string> translatedStatements = new List<string>(s.Contents.Length);
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
            for (int i = 1; i < s.Contents.Length; ++i)
            {
                translateLine(ref s.Contents, ref i, ref translatedStatements, ref indents);
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
            String workstr = translateOperation(contents[index]), iter;
            switch (workstr)
            {
                //#region conditionals
                //case "try_begin":
                //    workstr = "if";
                //    index += 2;
                //    workstr += translateConditional(ref contents, ref index) + " {";
                //    statements.Add(doIndents(indents,workstr));                   
                //    ++indents;
                //    break;
                //case "try_end":
                //    --indents;
                //    statements.Add(doIndents(indents,"}"));
                //    ++index;                    
                //    break;
                //case "else_try":
                //    --indents;
                //    statements.Add(doIndents(indents,"}"));
                //    statements.Add(doIndents(indents,"else {"));
                //    ++index;
                //    ++indents;
                //    break;
                //case "try_for_range":
                //    //try_for_range,<destination>,<lower_bound>,<upper_bound>  
                //    iter = prettyVar(contents[index + 2]);                        
                //    workstr = String.Format("for({0} = {1}; {0} < {2}; {0}++)", iter, prettyVar(contents[index + 3]), contents[index + 4]) + " {";
                //    index += 4;    
                //    statements.Add(doIndents(indents,workstr));
                //    statements.Add(doIndents(indents,"{"));
                //    ++indents;
                //    break;
                //case "try_for_range_backwards":
                //    //try_for_range_backwards,<destination>,<lower_bound>,<upper_bound>
                //    iter = prettyVar(contents[index + 2]);
                //    workstr = String.Format("for({0} = {2}; {0} > {1}; {0}--)", iter, prettyVar(contents[index + 3]), contents[index + 4]) + " {";
                //    index += 4;
                //    statements.Add(doIndents(indents,workstr));
                //    ++indents;
                //    break;
                //case "try_for_parties":
                //    //try_for_parties,<destination>
                //    statements.Add(doIndents(indents,"foreach (party in parties)"));
                //    statements.Add(doIndents(indents,"if (party == " + prettyVar(contents[index + 2]) + ") {"));
                //    index += 2;
                //    ++indents;
                //    break;
                //case "try_for_agents":
                //    //try_for_agents,<destination>)
                //    statements.Add(doIndents(indents,"foreach (agent in agents)"));
                //    statements.Add(doIndents(indents,"if (party == " + prettyVar(contents[index + 2]) + ") {"));
                //    index += 2;
                //    ++indents;
                //    break;
                //#endregion
                //#region self-assignment
                //case "val_add":
                //    statements.Add(doIndents(indents, String.Format("{0} += {1};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;                
                //case "val_sub":
                //    statements.Add(doIndents(indents, String.Format("{0} -= {1};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "val_mul":
                //    statements.Add(doIndents(indents, String.Format("{0} = ({0} * {1});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "val_div":
                //    statements.Add(doIndents(indents, String.Format("{0} = ({0} / {1});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "val_mod":
                //    statements.Add(doIndents(indents, String.Format("{0} = ({0} % {1});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "val_min":
                //    statements.Add(doIndents(indents, String.Format("{0} = Min({0},{1});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "val_max":
                //    statements.Add(doIndents(indents, String.Format("{0} = Max({0},{1});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "val_clamp":
                //    statements.Add(doIndents(indents, String.Format("{0} = Clamp({1},{2}", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]))));
                //    index += 4;
                //    break;
                //case "val_abs":
                //    statements.Add(doIndents(indents, String.Format("{0} = Abs({0});", prettyVar(contents[index + 2]))));
                //    index += 1;
                //    break;
                //case "val_or":
                //    statements.Add(doIndents(indents, String.Format("{0} |= {1};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "val_and":
                //    statements.Add(doIndents(indents, String.Format("{0} &= {1};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //#endregion
                //#region assigment
                //case "assign":
                //    statements.Add(doIndents(indents, String.Format("{0} = {1};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;      
                //case "store_or":
                //    statements.Add(doIndents(indents, String.Format("{0} = ({1} | {2});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]))));
                //    index += 4;
                //    break;
                //case "store_and":
                //    statements.Add(doIndents(indents, String.Format("{0} = ({1} & {2});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]))));
                //    index += 4;
                //    break;
                //case "store_mod":
                //    statements.Add(doIndents(indents, String.Format("{0} = {1} % {2};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]))));
                //    index += 4;
                //    break;
                //case "store_add":
                //    statements.Add(doIndents(indents, String.Format("{0} = {1} + {2};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]))));
                //    index += 4;
                //    break;
                //case "store_sub":
                //    statements.Add(doIndents(indents, String.Format("{0} = {1} - {2};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]))));
                //    index += 4;
                //    break;
                //case "store_mul":
                //    statements.Add(doIndents(indents, String.Format("{0} = {1} * {2};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]))));
                //    index += 4;
                //    break;
                //case "store_div":
                //    statements.Add(doIndents(indents, String.Format("{0} = {1} / {2};", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]))));
                //    index += 4;
                //    break;
                //case "store_sqrt":
                //    statements.Add(doIndents(indents, String.Format("{0} = Sqrt({1});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "store_pow":
                //    statements.Add(doIndents(indents, String.Format("{0} = Pow({1},{2});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]))));
                //    index += 4;
                //    break;
                //case "store_sin":
                //    statements.Add(doIndents(indents, String.Format("{0} = Sin({1});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "store_cos":
                //    statements.Add(doIndents(indents, String.Format("{0} = Cos({1});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "store_tan":
                //    statements.Add(doIndents(indents, String.Format("{0} = Tan({1});", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "store_script_param":
                //    statements.Add(doIndents(indents, String.Format("{0} = script_params[{1}];", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]))));
                //    index += 3;
                //    break;
                //case "store_script_param_1":
                //    statements.Add(doIndents(indents, String.Format("{0} = script_params[1];", prettyVar(contents[index + 2]))));
                //    index += 2;
                //    break;
                //case "store_script_param_2":
                //    statements.Add(doIndents(indents, String.Format("{0} = script_params[2];", prettyVar(contents[index + 2]))));
                //    index += 2;
                //    break;
                //#endregion

                default:                    
                    if (contents.Length > ++index)
                    {
                        switch (workstr)
                        {
                            case "try_begin":
                                ++indents;
                                break;
                            case "try_end":
                                --indents;
                                break;
                            case "try_for_range":
                                ++indents;
                                break;
                            case "try_for_range_backwards":
                                ++indents;
                                break;
                            case "try_for_parties":
                                ++indents;
                                break;
                            case "try_for_agents":
                                ++indents;
                                break;
                        }
                        workstr = "(" + workstr;
                        if (contents[index] > 0)
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
                    statements.Add(doIndents(indents,workstr));
                    break;
            }           
        }

        private static String translateOperation(Int64 refnum)
        {
            String retVal = String.Empty;
            if (Header_Operations.IDValues.ContainsKey(refnum))
            {
                retVal += Header_Operations.IDValues[refnum];
            }
            return retVal;
        }

        private static String translateConditional(ref Int64[] contents, ref Int32 index)
        {
            String workstr = translateOperation(contents[index]);
            switch (workstr)
            {
                case "is_between":
                    //(is_between,<value>,<lower_bound>,<upper_bound>), Is Lower <= Value < Upper?
                    workstr = String.Format("({0} >= {1} && {0} < {2} )", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]), prettyVar(contents[index + 4]));
                    index += 4;
                    break;
                case "eq"://(eq,<value>,<value>)
                    workstr = String.Format("({0} == {1})", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]));
                    index += 3;
                    break;
                case "neq"://(neq,<value>,<value>)
                    workstr = String.Format("({0} != {1})", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]));
                    index += 3;
                    break;
                case "gt"://(gt,<value>,<value>)
                    workstr = String.Format("({0} > {1})", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]));
                    index += 3;
                    break;
                case "ge"://(ge,<value>,<value>)
                    workstr = String.Format("({0} >= {1})", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]));
                    index += 3;
                    break;
                case "lt"://(lt,<value>,<value>),
                    workstr = String.Format("({0} < {1})", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]));
                    index += 3;
                    break;
                case "le"://(le,<value>,<value>),
                    workstr = String.Format("({0} <= {1})", prettyVar(contents[index + 2]), prettyVar(contents[index + 3]));
                    index += 3;
                    break;
                default://same as generic translateline
                    if (contents.Length > ++index)
                    {
                        workstr = "(" + workstr + "(";
                        if (contents[index] > 0)
                        {
                            int statementend = (Int32)contents[index] + (index + 1);
                            ++index;//skip past the # args
                            while (index < statementend)
                            {
                                workstr += prettyVar(contents[index]);
                                if (++index < statementend) { workstr += ", "; }
                            }
                            --index;
                            workstr += "))";
                        }
                    }
                    break;
            }
            return workstr;
        }


        private static String prettyVar(Int64 refnum)
        {
            string retVal = String.Empty;
            
            foreach (KeyValuePair<Int64, String> kvp in varmasks)
            {
                if ((refnum ^ kvp.Key) < minvaluebitshift)
                {
                    retVal += tryGetVarName(kvp.Value, (refnum ^ kvp.Key));
                    if (retVal == String.Empty)
                    {
                        retVal += kvp.Value + (refnum ^ kvp.Key).ToString();
                    }
                }
            }
            if (retVal == string.Empty) { retVal += refnum.ToString(); }
            return retVal;
        }

        private static String tryGetVarName(String type, Int64 val)
        {
            string retVal = String.Empty;
            switch (type)
            {
                case "[script]":
                    if (val >= 0 && val < scripts.Length) { retVal = scripts[val].ScriptName; }
                    break;
                case "[faction]":
                    if (val >= 0 && val < Module_Factions.FactionNames.Length)
                    {
                        //dunno if they want the # or not
                        retVal = String.Format("\"{1}\"", val, Module_Factions.FactionNames[val]);
                    }                    
                    break;
                case "[troop]":
                    if (val >= 0 && val < Module_Troops.TroopNames.Length)
                    {
                        //dunno if they want the # or not
                        retVal = String.Format("\"{1}\"", val, Module_Troops.TroopNames[val]);
                    }
                    break;
                case "[scene]":
                    if (val >= 0 && val < Module_Scenes.SceneNames.Length)
                    {
                        //dunno if they want the # or not
                        retVal = String.Format("\"{1}\"", val, Module_Scenes.SceneNames[val]);
                    }
                    break;
                case "[localvar]":
                        retVal = String.Format("\":localvar{0}\"", val);
                    break;
                case "[item]":
                    if (val >= 0 && val < Module_Items.ItemNames.Length)
                    {
                        //dunno if they want the # or not
                        retVal = String.Format("\"{1}\"", val, Module_Items.ItemNames[val]);
                    }
                    break;
                case "$globalvar":
                    if (val >= 0 && val < Module_Variables.GlobalVarNames.Length) { retVal = Module_Variables.GlobalVarNames[val]; }
                    break;
            }
            return retVal;
        }

        #endregion
    }
}
