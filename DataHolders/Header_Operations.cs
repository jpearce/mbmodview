using System;
using System.Collections.Generic;

using System.Text;
using System.IO;

namespace MBScriptEditor
{
    internal static class Header_Operations
    {
        internal static FileInfo filelocation;
        internal static SortedDictionary<String, Int64> StringValues;
        internal static SortedDictionary<Int64, String> IDValues;//reversed
        internal static List<String> lhs_operations, global_lhs_operations, can_fail_operations;
        /// <summary>Static initializer</summary>
        static Header_Operations()
        {
            filelocation = new FileInfo(Config.GetSetting("filelocation_header_operations_py"));
            if (filelocation.Exists)
            {
                Config.SetSetting("filelocation_header_operations_py", filelocation.FullName);
            }
            StringValues = new SortedDictionary<String, Int64>();
            lhs_operations = new List<string>(128);
            global_lhs_operations = new List<string>(128);
            can_fail_operations = new List<string>(128);
        }

        #region fileread
        internal static void LoadFromFile()
        {//todo: want to check if we have any loaded & changed etc 

            if (filelocation.Exists)
            {
                StringValues.Clear();                
                lhs_operations.Clear();
                global_lhs_operations.Clear();
                can_fail_operations.Clear();
                using (TextReader tr = new StreamReader(filelocation.FullName))
                {
                    String linestr, key, val;
                    Int32 indexequals, indexhash;                    
                    while ((linestr = tr.ReadLine()) != null)
                    {
                        linestr = linestr.Trim().Replace(",", String.Empty);
                        indexequals = linestr.IndexOf('=');
                        indexhash = linestr.IndexOf('#');
                        //if there is a hash and it's before any equals sigh, if there's no equals sign or if there's a deprecated
                        if ((indexhash > -1 && indexhash <= indexequals)
                            || indexequals == -1 || linestr.IndexOf("deprecated") > -1)
                        {//skip line
                        }
                        else
                        {
                            if (indexhash > -1) { linestr = linestr.Remove(indexhash); }//chop off end #comments for now
                            key = linestr.Remove(indexequals).Trim();
                            val = (indexequals + 1 < linestr.Length ? linestr.Substring(indexequals + 1).Trim() : String.Empty);
                            if (key == "lhs_operations")
                            {
                                lhs_operations.Add(val.Substring(val.IndexOf('[') + 1));//already chopped comma
                                do 
                                {
                                    linestr = tr.ReadLine().Replace(",", String.Empty).Trim();
                                    lhs_operations.Add(linestr); 
                                }
                                while (linestr != null && linestr.IndexOf(']') == -1);
                                if (linestr == null) { break; }
                                lhs_operations[lhs_operations.Count - 1] = 
                                    lhs_operations[lhs_operations.Count - 1].Replace("]", String.Empty).Trim();
                            }
                            else if (key == "global_lhs_operations")
                            {
                                global_lhs_operations.Add(val.Substring(val.IndexOf('[') + 1));//already chopped comma                                
                                do
                                {
                                    linestr = tr.ReadLine().Replace(",", String.Empty).Trim();
                                    global_lhs_operations.Add(linestr);
                                }
                                while (linestr != null && linestr.IndexOf(']') == -1);
                                if (linestr == null) { break; }
                                global_lhs_operations[global_lhs_operations.Count - 1] = 
                                    global_lhs_operations[global_lhs_operations.Count - 1].Replace("]", String.Empty).Trim();
                            }
                            else if (key == "can_fail_operations")
                            {
                                can_fail_operations.Add(val.Substring(val.IndexOf('[') + 1));//already chopped comma
                                do
                                {
                                    linestr = tr.ReadLine().Replace(",", String.Empty).Trim();
                                    can_fail_operations.Add(linestr);
                                }
                                while (linestr != null && linestr.IndexOf(']') == -1);
                                if (linestr == null) { break; }
                                can_fail_operations[can_fail_operations.Count - 1] = 
                                    can_fail_operations[can_fail_operations.Count - 1].Replace("]", String.Empty).Trim();
                            }
                            else
                            {                                
                                if (!StringValues.ContainsKey(key)) 
                                {
                                    Int64 parsedval = 0;
                                    if (!Int64.TryParse(val, out parsedval))                                        
                                    {
                                        if(val.StartsWith("0x"))
                                        {
                                            Int64.TryParse(val.Substring(2), 
                                                System.Globalization.NumberStyles.AllowHexSpecifier, null, out parsedval);
                                        }                                        
                                    }                                    
                                    StringValues.Add(key, parsedval); 
                                }
                            }
                        }
                    }
                }
                //fill reversed dict
                IDValues = new SortedDictionary<Int64, String>();
                foreach (KeyValuePair<String, Int64> kvp in StringValues)
                {
                    if (!IDValues.ContainsKey(kvp.Value))
                    {
                        IDValues.Add(kvp.Value, kvp.Key);
                    }
                    else
                    {
                        Console.WriteLine("");
                    }
                }
            }            
        }

        

       
        #endregion






    }
}
