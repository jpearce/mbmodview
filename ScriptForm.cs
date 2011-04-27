using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Forms;
using System.Drawing;

namespace MBModViewer
{
    public partial class ScriptForm : Form
    {
        public ScriptForm()
        {
            InitializeComponent();
            
            StaticDataHolder.LoadAll();
            
            //MessageBox.Show("Error loading xml configurations:\n" + ex.Message);

            LoadModVars();
            LoadConstants();
            LoadOps();            
            LoadScripts();//last
        }

        private void LoadModVars()
        {
            lb_mvtypes.Items.Clear();
            SortedList<String, byte> tempsort = new SortedList<string, byte>();
            for (int i = 0; i < StaticDataHolder.DataReaders.Length; i++)
            {
                tempsort.Add(StaticDataHolder.DataReaders[i].Name, 0);
            }
            foreach (KeyValuePair<String, byte> kvp in tempsort) { lb_mvtypes.Items.Add(kvp.Key); }
        }

        private void LoadScripts()
        {
            lbScripts.Items.Clear();
            try
            {                
                Module_Script.LoadFromFile();
                groupBox1.Text = String.Format("Scripts({0})", Module_Script.ScriptNames.Length);
                SortedList<String, byte> tempsort = new SortedList<string, byte>(Module_Script.ScriptNames.Length);
                for (int i = 0; i < Module_Script.ScriptNames.Length; ++i)
                {
                    tempsort.Add(Module_Script.ScriptNames[i], 0);
                }
                foreach (KeyValuePair<String, byte> kvp in tempsort) { lbScripts.Items.Add(kvp.Key); }
            }
            catch (Exception loadex)
            {
                MessageBox.Show(loadex.Message, "Script loading error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadConstants()
        {
            lvConst.Items.Clear();
            try
            {                
                foreach (PythonDataItem pdi in StaticDataHolder.Header_Common.PythonItems)
                {
                    lvConst.Items.Add(pdi.Name).SubItems.AddRange(
                        new String[]{pdi.Value.ToString(), pdi.Source});
                }
            }
            catch (Exception loadex)
            {
                MessageBox.Show(loadex.Message, "Constants loading error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadOps()
        {
            lv_Ops.Items.Clear();
            try
            {
                foreach (PythonDataItem pdi in StaticDataHolder.Header_Operations.PythonItems)
                {
                    lv_Ops.Items.Add(pdi.Name).SubItems.AddRange(
                        new String[] { pdi.Source, pdi.Value.ToString() });
                }                
            }
            catch (Exception loadex)
            {
                MessageBox.Show(loadex.Message, "Constants loading error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lbScripts_SelectedIndexChanged(object sender, EventArgs e)
        {
            String scriptname = lbScripts.SelectedItem.ToString();
            for(int i = 0; i < Module_Script.ScriptNames.Length; ++i)
            {
                if(Module_Script.ScriptNames[i] == scriptname)
                {
                    groupBox2.Text = String.Format("Script#{1}: {0}", scriptname, i);
                    break;
                }
            }            
            String[] scriptcontents;
            Module_Script.SetScriptContents(scriptname, out scriptcontents);
            rtbScript.Hide(); 
            rtbScript.Lines = scriptcontents;
        }

        private void lb_mvtypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            String t = lb_mvtypes.SelectedItem.ToString();
            for (int i = 0; i < StaticDataHolder.DataReaders.Length; ++i)
            {
                if (StaticDataHolder.DataReaders[i].Name == t)
                {
                    lb_mvnames.Items.Clear();
                    SortedList<String, byte> tempsort = new SortedList<string, byte>();
                    for (int j = 0; j < StaticDataHolder.DataReaders[i].Items.Length; ++j)
                    {
                        if (tempsort.ContainsKey(StaticDataHolder.DataReaders[i].Items[j].Name))
                        {
                            for (int k = 0; k < 100; ++k)
                            {
                                if (!tempsort.ContainsKey(StaticDataHolder.DataReaders[i].Items[j].Name + "(" + k.ToString() + ")"))
                                {
                                    tempsort.Add(StaticDataHolder.DataReaders[i].Items[j].Name + "(" + k.ToString() + ")", 0);
                                    k = 100;
                                }
                            }
                        }
                        else { tempsort.Add(StaticDataHolder.DataReaders[i].Items[j].Name, 0); }
                    }
                    foreach (KeyValuePair<String, byte> kvp in tempsort) { lb_mvnames.Items.Add(kvp.Key); }
                }
            }
        }

        private void lb_mvnames_SelectedIndexChanged(object sender, EventArgs e)
        {
            String t = lb_mvtypes.SelectedItem.ToString();
            for (int i = 0; i < StaticDataHolder.DataReaders.Length; ++i)
            {
                if (StaticDataHolder.DataReaders[i].Name == t)
                {
                    String v = lb_mvnames.SelectedItem.ToString();
                    
                    for (int j = 0; j < StaticDataHolder.DataReaders[i].Items.Length; ++j)
                    {
                        if (StaticDataHolder.DataReaders[i].Items[j].Name == v)
                        {
                            lv_mvvals.Items.Clear();
                            lv_mvvals.Items.Add("ID").SubItems.Add(StaticDataHolder.DataReaders[i].Items[j].ID.ToString());
                            lv_mvvals.Items.Add("Name").SubItems.Add(StaticDataHolder.DataReaders[i].Items[j].Name);
                        }                        
                    }                    
                }
            }
        }

        private void rtbScript_TextChanged(object sender, EventArgs e)
        {
            
            rtbScript.SuspendLayout();
            int curlen = 0, linelen = 0;
            for (int i = 0; i < rtbScript.Lines.Length; ++i)
            {               
                string[] split = rtbScript.Lines[i].Split(' ');
                linelen = 0;
                bool commanddone = false;
                for (int j = 0; j < split.Length; ++j)
                {
                    ++linelen;
                    if (!commanddone && split[j].Length > 1)
                    {
                        rtbhighlight(curlen + linelen, (split[j].Length - 1), Color.Blue, (split[j].Contains("try_")));
                        commanddone = true;
                    }                    
                    else if (split[j].StartsWith("\"$"))
                    {
                        rtbhighlight((curlen - 1) + linelen, (split[j].Length + 1), Color.DarkGreen, true);
                    }
                    else if (split[j].StartsWith("\":"))
                    {
                        rtbhighlight((curlen - 1) + linelen, (split[j].Length + 1), Color.DarkGray, true);
                    }
                    linelen += split[j].Length;
                }
                curlen += (rtbScript.Lines[i].Length + 1);
            }            
            
            rtbScript.SelectionStart = 0;
            rtbScript.SelectionLength = rtbScript.Text.Length;
            rtbScript.DeselectAll();
            rtbScript.ScrollToCaret();
            rtbScript.Refresh();
            rtbScript.Show();
            rtbScript.ResumeLayout();
        }

        private void rtbhighlight(Int32 start, Int32 len, Color newcolor, bool Bold)
        {
            rtbScript.Select(start, len);
            rtbScript.SelectionColor = newcolor;
            if (Bold)
            {
                rtbScript.SelectionFont = new Font(
                   rtbScript.SelectionFont.FontFamily,
                   rtbScript.SelectionFont.Size,
                   FontStyle.Bold
                );
            }

            rtbScript.DeselectAll();
        }
    }
}
