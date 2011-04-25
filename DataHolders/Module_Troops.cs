using System;
using System.Collections.Generic;
using System.IO;

namespace MBScriptEditor
{
    internal static class Module_Troops
    {//this is very simple comparatively
        //don't care about anything other than the name at the moment
        internal static FileInfo filelocation;
        internal static String[] TroopNames;        

        /// <summary>Static initializer</summary>
        static Module_Troops()
        {
            filelocation = new FileInfo(Config.GetSetting("filelocation_troops_txt"));
            if(filelocation.Exists) {
                Config.SetSetting("filelocation_troops_txt", filelocation.FullName);
                List<String> templist = new List<string>(2048);
                using (TextReader tr = new StreamReader(filelocation.FullName))
                {
                    tr.ReadLine();//Troopfiles version 1
                    tr.ReadLine();//count, could use it for array but just letting c/p ride
                    String linestr;                    
                    while ((linestr = tr.ReadLine()) != null)
                    {
                        if (linestr.IndexOf(' ') > -1)
                        {                            
                            linestr = linestr.Remove(linestr.IndexOf(' '));
                            templist.Add(linestr.Trim());
                        }
                        tr.ReadLine();
                        tr.ReadLine();
                        tr.ReadLine();
                        tr.ReadLine();
                        tr.ReadLine();
                        tr.ReadLine();
                    }
                    tr.Close();
                }
                TroopNames = templist.ToArray();
            }       
     
        }
    }
}
