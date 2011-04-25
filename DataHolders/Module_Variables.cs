using System;
using System.Collections.Generic;
using System.IO;

namespace MBScriptEditor
{
    internal static class Module_Variables
    {//this is very simple comparatively
        internal static FileInfo filelocation;
        internal static String[] GlobalVarNames;        

        /// <summary>Static initializer</summary>
        static Module_Variables()
        {
            filelocation = new FileInfo(Config.GetSetting("filelocation_variables_txt"));
            if(filelocation.Exists) {
                Config.SetSetting("filelocation_variables_txt", filelocation.FullName);
                List<String> templist = new List<string>(2048);
                using (TextReader tr = new StreamReader(filelocation.FullName))
                {
                    String linestr;                    
                    while ((linestr = tr.ReadLine()) != null)
                    {
                        templist.Add(linestr.Trim());
                    }
                }
                GlobalVarNames = templist.ToArray();
            }       
     
        }
    }
}
