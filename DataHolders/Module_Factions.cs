using System;
using System.Collections.Generic;
using System.IO;

namespace MBScriptEditor
{
    internal static class Module_Factions
    {//this is very simple comparatively
        //don't care about anything other than the name at the moment
        internal static FileInfo filelocation;
        internal static String[] FactionNames;        

        /// <summary>Static initializer</summary>
        static Module_Factions()
        {
            filelocation = new FileInfo(Config.GetSetting("filelocation_factions_txt"));
            if(filelocation.Exists) {
                Config.SetSetting("filelocation_factions_txt", filelocation.FullName);
                List<String> templist = new List<string>(2048);
                using (TextReader tr = new StreamReader(filelocation.FullName))
                {
                    tr.ReadLine();//factionfiles version 1
                    tr.ReadLine();//count, could use it for array but just letting c/p ride
                    String linestr;
                    linestr = tr.ReadLine();
                    if (!String.IsNullOrEmpty(linestr) && linestr.IndexOf(' ') > -1)
                    {//first line doesn't have the 0 in front of the name
                        templist.Add(linestr.Remove(linestr.IndexOf(' ')));
                        tr.ReadLine();
                    }
                    while ((linestr = tr.ReadLine()) != null)
                    {
                        if (linestr.StartsWith("0 ") && linestr.Substring(2).IndexOf(' ') > -1)
                        {
                            linestr = linestr.Substring(2);
                            linestr = linestr.Remove(linestr.IndexOf(' '));
                            templist.Add(linestr.Trim());
                        }
                        tr.ReadLine();
                    }
                    tr.Close();
                }
                FactionNames = templist.ToArray();
            }       
     
        }
    }
}
