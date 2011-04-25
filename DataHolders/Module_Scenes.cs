using System;
using System.Collections.Generic;
using System.IO;

namespace MBScriptEditor
{
    internal static class Module_Scenes
    {//this is very simple comparatively
        //don't care about anything other than the name at the moment
        internal static FileInfo filelocation;
        internal static String[] SceneNames;        

        /// <summary>Static initializer</summary>
        static Module_Scenes()
        {
            filelocation = new FileInfo(Config.GetSetting("filelocation_scenes_txt"));
            if(filelocation.Exists) {
                Config.SetSetting("filelocation_scenes_txt", filelocation.FullName);
                List<String> templist = new List<string>(2048);
                using (TextReader tr = new StreamReader(filelocation.FullName))
                {
                    tr.ReadLine();//Scenefiles version 1
                    tr.ReadLine();//count, could use it for array but just letting c/p ride
                    String linestr;                    
                    while ((linestr = tr.ReadLine()) != null)
                    {
                        //apparently it's not well-formed to 3 lines, steppe_horse is 4
                        //so we search for " itm_" instead of jumping x lines
                        while (!linestr.StartsWith("scn_"))
                        {
                            linestr = tr.ReadLine();
                            if (linestr == null) { break; }
                        }
                        if (linestr == null) { break; }
                        if (linestr.IndexOf(' ') > -1)
                        {
                            linestr = linestr.Remove(linestr.IndexOf(' '));
                            templist.Add(linestr.Trim());
                        }                                          
                    }
                    tr.Close();
                }
                SceneNames = templist.ToArray();
            }       
     
        }
    }
}
