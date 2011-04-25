using System;
using System.Collections.Generic;
using System.IO;

namespace MBScriptEditor
{
    internal static class Module_Items
    {//this is very simple comparatively
        //don't care about anything other than the name at the moment
        internal static FileInfo filelocation;
        internal static String[] ItemNames;        

        /// <summary>Static initializer</summary>
        static Module_Items()
        {
            filelocation = new FileInfo(Config.GetSetting("filelocation_itemkinds_txt"));
            if(filelocation.Exists) {
                Config.SetSetting("filelocation_itemkinds_txt", filelocation.FullName);
                List<String> templist = new List<string>(2048);
                using (TextReader tr = new StreamReader(filelocation.FullName))
                {
                    tr.ReadLine();//Itemfiles version 1
                    tr.ReadLine();//count, could use it for array but just letting c/p ride
                    String linestr;                    
                    while ((linestr = tr.ReadLine()) != null)
                    {
                        //apparently it's not well-formed to 3 lines, steppe_horse is 4
                        //so we search for " itm_" instead of jumping x lines
                        while (!linestr.StartsWith(" itm_"))
                        {
                            linestr = tr.ReadLine();
                            if (linestr == null) { break; }
                        }
                        if (linestr == null) { break; }
                        
                            linestr = linestr.Substring(1);
                            if (linestr.IndexOf(' ') > -1)
                            {
                                linestr = linestr.Remove(linestr.IndexOf(' '));
                                templist.Add(linestr.Trim());
                            }
                                              
                    }
                    tr.Close();
                }
                ItemNames = templist.ToArray();
            }       
     
        }
    }
}
