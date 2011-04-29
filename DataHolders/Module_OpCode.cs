using System;
using System.Collections.Generic;

using System.Text;
using System.IO;

namespace MBModViewer
{
    internal static class Module_OpCode
    {
        internal static Boolean compat808 = false;
        internal static String[] ScriptNames;//gui access      
        internal static Int32 TriggerCount { get { return triggerreader.Items.Length; } }
        internal static Int32 SimpleTriggerCount { get { return simpletriggerreader.Items.Length; } }
        private static ScriptDataReader scriptreader;
        private static TriggerDataReader triggerreader;
        private static TriggerDataReader simpletriggerreader;
        private static Int64 minvaluebitshift;
        private static Int32 bitshifts;
        private static Dictionary<Int64, String> varmasks;

        /// <summary>list of possible or operations as in op1|op2 since there are several combinations that overlap
        /// if it's allowed to look at all ops</summary>
        private static HashSet<String> oroplist =
            new HashSet<string>(new String[] { "this_or_next", "neq", "le", "ge", "neg", "eq", "lt", "gt" });
        //assigned at runtime since we don't know the opcodes yet
        /// <summary>Indentlist will indent + 1 on next statement, unindent will indent - 1 current statement, 
        /// can be in both and will do both(unindent current statement then indent next ie else_try)</summary>
        private static HashSet<Int64> indentlist, unindentlist;

        /// <summary>Static initializer, minimize chance of failure</summary>
        static Module_OpCode()
        {            
            if(Config.GetSetting("compat808") != null && Config.GetSetting("compat808") == "true") { compat808 = true; }            
        }        

        #region load
        private static void setvarmasks()
        {//put them in a lookup so we can determine on the fly what they are
            varmasks = new Dictionary<long, string>();
            if (StaticDataHolder.Header_Common.HaveKeyWithValue("op_num_value_bits"))
            {
                bitshifts = (Int32)StaticDataHolder.Header_Common.KeyValue("op_num_value_bits");
                minvaluebitshift = 1 << bitshifts;
                foreach (PythonDataItem pdi in StaticDataHolder.Header_Common.PythonItems)
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
                                        varmasks.Add(pdi.Value, String.Format("[{0}]", pdi.Name));//already masked                                4
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

        internal static void LoadScripts()
        {//todo: want to check if we have any loaded & changed etc 
            setvarmasks();
            scriptreader = new ScriptDataReader(Config.GetSetting("filelocation_txtdir") + "scripts.txt");
            scriptreader.Read();
            setIndentLists();
        }

        internal static void LoadTriggers()
        {//todo: want to check if we have any loaded & changed etc 
            
            triggerreader = new TriggerDataReader(Config.GetSetting("filelocation_txtdir") + "triggers.txt");
            triggerreader.Read();            
        }

        internal static void LoadSimpleTriggers()
        {//todo: want to check if we have any loaded & changed etc 

            simpletriggerreader = new TriggerDataReader(Config.GetSetting("filelocation_txtdir") + "simple_triggers.txt");
            simpletriggerreader.SimpleTriggers = true;
            simpletriggerreader.Read();
        }

        /// <summary>Set indentlist, unindentlist, reindentlist </summary>
        private static void setIndentLists()
        {
            indentlist = new HashSet<long>();
            unindentlist = new HashSet<long>();
            for (int i = 0; i < StaticDataHolder.Header_Operations.Items.Length; ++i)
            {
                switch (StaticDataHolder.Header_Operations.Items[i].Name)
                {
                    case "try_begin":
                        indentlist.Add(StaticDataHolder.Header_Operations.PythonItems[i].Value);
                        break;
                    case "try_for_range":
                        indentlist.Add(StaticDataHolder.Header_Operations.PythonItems[i].Value);
                        break;
                    case "try_for_range_backwards":
                        indentlist.Add(StaticDataHolder.Header_Operations.PythonItems[i].Value);
                        break;
                    case "try_for_parties":
                        indentlist.Add(StaticDataHolder.Header_Operations.PythonItems[i].Value);
                        break;
                    case "try_for_agents":
                        indentlist.Add(StaticDataHolder.Header_Operations.PythonItems[i].Value);
                        break;
                    case "else_try":
                        unindentlist.Add(StaticDataHolder.Header_Operations.PythonItems[i].Value);
                        indentlist.Add(StaticDataHolder.Header_Operations.PythonItems[i].Value);
                        break;
                    case "try_end":
                        unindentlist.Add(StaticDataHolder.Header_Operations.PythonItems[i].Value);
                        break;
                }
            }
        }
        #endregion

        #region translation to text
        /// <summary>Baseline per-line translation, given the entire working set.</summary>
        /// <param name="contents">Contents of a DataItem.</param>
        /// <param name="index">Current index in said DataItem.Content.  Should be pointing at the operation Int64 when received.
        /// Incremented as consumed, leaves pointing at next operation Int64.</param>        
        /// <param name="indents">Current indent count, passed by ref per line so it can be modified.</param>
        private static String translateLine(Int64[] contents, ref Int32 index, ref int indents)
        {
            String retVal = String.Empty;
            Int64 operation = contents[index];
            if (retVal == String.Empty)
            {
                Console.Write("");
            }
            if (!compat808)
            {
                ++index;//index now at first arg
                if (index < contents.Length)
                {//check we're not outside the array
                    Int64 argcount = contents[index];
                    ++index;
                    switch (argcount)
                    {//hardcode the most likely with a catch-all
                        case 0:
                            retVal = TranslateOpCode(operation);//done!                                
                            break;
                        case 1:                                
                            retVal = TranslateOpCode(operation, contents[index]);
                            ++index;                                
                            break;
                        case 2:
                            if ((index + 1) < contents.Length)
                            {
                                retVal = TranslateOpCode(operation, contents[index], contents[index + 1]);
                                index += 2;
                            }
                            else { throw new FormatException("Script not in recognized format."); } //was outside array
                            break;
                        case 3:
                            if ((index + 2) < contents.Length)
                            {
                                retVal = TranslateOpCode(operation, contents[index], contents[index + 1], contents[index + 2]);
                                index += 3;
                            }
                            else { throw new FormatException("Script not in recognized format."); } //was outside array
                            break;
                        default:
                            if ((index + (argcount - 1)) < contents.Length)
                            {
                                Int64[] args = new Int64[argcount];
                                for (int i = 0; i < argcount; ++i)
                                {
                                    args[i] = contents[index + i];
                                }
                                retVal = TranslateOpCode(operation, args);
                                index += (Int32)argcount;
                            }
                            else { throw new FormatException("Script not in recognized format."); } //was outside array
                            break;
                    }
                    
                }
                else { throw new FormatException("Script not in recognized format."); } //was outside array
            }
            else
            {//hardcoded 1 or 3 args
                ++index;//index now at first arg
                if (index < contents.Length)
                {//check we're not outside the array
                    if (contents[index] > 0)
                    {
                        if ((index + 2) < contents.Length)
                        {
                            retVal = TranslateOpCode(operation, contents[index], contents[index + 1], contents[index + 2]);
                            index += 3;
                        }
                        else { throw new FormatException("Script not in recognized format."); } //was outside array
                    }
                    else
                    {
                        retVal = TranslateOpCode(operation);
                        ++index;
                    }
                }
                else { throw new FormatException("Script not in recognized format."); } //was outside array
            }
            retVal = "(" + retVal + ")";
            if (index < contents.Length) { retVal += ","; }
            if (unindentlist.Contains(operation) && indents > 0) { --indents; }
            retVal = doIndents(indents, retVal);
            if (indentlist.Contains(operation)) { ++indents; }//always next statement
            return retVal;
        }
        #endregion

        #region formatting for gui
        internal static void SetScriptContents(String scriptname, out String[] outlines)
        {
            List<string> translatedStatements = new List<string>(64);
            for (int i = 0; i < ScriptNames.Length; ++i)
            {
                if (ScriptNames[i] == scriptname)
                {
                    DataItem s = scriptreader.Items[i];
                    int indents = 0;
                    int index = 1;
                    while(index < s.Content.Length)
                    {
                        translatedStatements.Add(translateLine(s.Content, ref index, ref indents));
                    }                    
                }
            }
            outlines = translatedStatements.ToArray();
        }

        private const int numspaceindent = 4;
        /// <summary>Very simply, adds indent to string</summary>
        /// <param name="numindents">Number of indents (not spaces).  
        /// Will be indented by (numspaceindent*numindents) spaces</param>
        /// <param name="toindent">String to modify.  Not a reference because it has to create a new one anyway.</param>
        /// <returns>Modified string with indents.</returns>
        private static String doIndents(int numindents, String toindent)
        {
            if (numindents == 0) { return toindent; }
            else
            {
                char[] temp = new char[numindents * numspaceindent];                
                for (int i = 0; i < temp.Length; ++i)
                {
                    temp[i] = ' ';
                }
                return new String(temp) + toindent;
            }
        }

        /// <summary>Translate given operation and arguments[].  If supporting another type like 808, modify
        /// arguments in the function that calls it.
        /// </summary>
        /// <param name="operation">Operation code</param>
        /// <param name="arguments">Exact list of arguments.</param>
        /// <returns>Plaintext code.</returns>
        internal static String TranslateOpCode(Int64 operation, params Int64[] arguments)
        {
            String retVal = translateOperation(operation);
            if (arguments != null && arguments.Length > 0)
            {                
                for (int i = 0; i < arguments.Length; ++i)
                {
                    retVal += ", "; 
                    retVal += prettyVar(arguments[i]);
                }
            }
            return retVal;
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
                        retVal = "\"script_" + scriptreader.Items[val].Name + "\"";
                        break;                    
                    case "[local_variable]":
                        retVal = "\":local" + val.ToString() + "\""; 
                        break;
                    case "[quick_string]":
                        retVal = "\"" + StaticDataHolder.FindQuickString((Int32)val) + "\"";
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

        #region trigger-specific
        private static TriggerDataItem targetByName(String triggerName)
        {
            String target = triggerName.Replace("Trigger", String.Empty);
            if (target.Contains("Simple"))
            {
                target = target.Replace("Simple", String.Empty);
                return simpletriggerreader.TriggerItems[Int32.Parse(target)];
            }
            return triggerreader.TriggerItems[Int32.Parse(target)];
        }

        internal static String TriggerCheck(String triggerName)
        {   
            return targetByName(triggerName).Check.ToString();
        }

        internal static String TriggerDelay(String triggerName)
        {
            if (triggerName.StartsWith("Simple")) { return String.Empty; }
            return targetByName(triggerName).Delay.ToString(); 
        }

        internal static String TriggerRearm(String triggerName)
        {
            if (triggerName.StartsWith("Simple")) { return String.Empty; }
            return targetByName(triggerName).Rearm.ToString(); 
        }

        internal static void SetTriggerConditions(String triggerName, out String[] outlines)
        {
            if (triggerName.StartsWith("Simple")) 
            {
                outlines = new String[0];
            }
            else
            {
                List<string> translatedStatements = new List<string>(64);
                TriggerDataItem s = targetByName(triggerName);
                int indents = 0;
                int index = 1;
                while (index < s.Condition.Length)
                {
                    translatedStatements.Add(translateLine(s.Condition, ref index, ref indents));
                }
                outlines = translatedStatements.ToArray();
            }
        }

        internal static void SetTriggerContents(String triggerName, out String[] outlines)
        {
            TriggerDataItem s = targetByName(triggerName);
            List<string> translatedStatements = new List<string>(64);            
            int indents = 0;
            int index = 1;
            while (index < s.Content.Length)
            {
                translatedStatements.Add(translateLine(s.Content, ref index, ref indents));
            }            
            outlines = translatedStatements.ToArray();
        }
        #endregion

        #endregion
    }
}
