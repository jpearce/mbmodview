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
            LoadScripts();//after vars/constants/ops
            LoadTriggers();//after scripts (for call_script)
            lb_ti_once.Text = String.Format("(only once = {0})", StaticDataHolder.Header_Triggers.KeyValue("ti_once").ToString());
        }

        private void LoadTriggers()
        {
            lb_Triggers.Items.Clear();
            try
            {
                Module_OpCode.LoadTriggers();                
                for (int i = 0; i < Module_OpCode.TriggerCount; ++i)
                {
                    lb_Triggers.Items.Add("Trigger" + i.ToString());
                }
                Module_OpCode.LoadSimpleTriggers();                
                for (int i = 0; i < Module_OpCode.SimpleTriggerCount; ++i)
                {
                    lb_Triggers.Items.Add("SimpleTrigger" + i.ToString());
                }
                groupBox1.Text = String.Format("Triggers({0})", (Module_OpCode.TriggerCount + Module_OpCode.SimpleTriggerCount).ToString());
            }
            catch (Exception loadex)
            {
                MessageBox.Show(loadex.Message, "Trigger loading error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }            
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
                Module_OpCode.LoadScripts();
                groupBox1.Text = String.Format("Scripts({0})", Module_OpCode.ScriptNames.Length);
                SortedList<String, byte> tempsort = new SortedList<string, byte>(Module_OpCode.ScriptNames.Length);
                for (int i = 0; i < Module_OpCode.ScriptNames.Length; ++i)
                {
                    tempsort.Add(Module_OpCode.ScriptNames[i], 0);
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
                foreach (PythonDataItem pdi in StaticDataHolder.Header_Triggers.PythonItems)
                {
                    lvConst.Items.Add(pdi.Name).SubItems.AddRange(
                        new String[] { pdi.Value.ToString(), pdi.Source });
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
            for(int i = 0; i < Module_OpCode.ScriptNames.Length; ++i)
            {
                if(Module_OpCode.ScriptNames[i] == scriptname)
                {
                    groupBox2.Text = String.Format("Script#{1}: {0}", scriptname, i);
                    break;
                }
            }            
            String[] scriptcontents;
            Module_OpCode.SetScriptContents(scriptname, out scriptcontents);
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

        private void codeboxTextChange(object sender, EventArgs e)
        {
            RichTextBox codebox = (RichTextBox)sender;
            if (codebox.Text != null && codebox.Lines.Length > 0)
            {
                codebox.SuspendLayout();
                int curlen = 0, linelen = 0;
                for (int i = 0; i < codebox.Lines.Length; ++i)
                {
                    string[] split = codebox.Lines[i].Split(' ');
                    linelen = 0;
                    bool commanddone = false;
                    for (int j = 0; j < split.Length; ++j)
                    {
                        ++linelen;
                        if (!commanddone && split[j].Length > 1)
                        {
                            rtbhighlight(codebox, curlen + linelen, (split[j].Length - 1), Color.Blue, (split[j].Contains("try_") || split[j].Contains("_try")));
                            commanddone = true;
                        }
                        else if (split[j].StartsWith("\"$"))
                        {
                            rtbhighlight(codebox, (curlen - 1) + linelen, (split[j].Length + 1), Color.DarkGreen, true);
                        }
                        else if (split[j].StartsWith("\":"))
                        {
                            rtbhighlight(codebox, (curlen - 1) + linelen, (split[j].Length + 1), Color.DarkGray, true);
                        }
                        linelen += split[j].Length;
                    }
                    curlen += (codebox.Lines[i].Length + 1);
                }
                //revert to top
                codebox.SelectionStart = 0;
                codebox.SelectionLength = codebox.Text.Length;
                codebox.DeselectAll();
                codebox.ScrollToCaret();
                codebox.Refresh();
                codebox.Show();
                codebox.ResumeLayout();
            }
        }

        private void rtbhighlight(RichTextBox targetbox, Int32 start, Int32 len, Color newcolor, bool Bold)
        {
            targetbox.Select(start, len);
            targetbox.SelectionColor = newcolor;
            if (Bold)
            {
                targetbox.SelectionFont = new Font(
                   targetbox.SelectionFont.FontFamily,
                   targetbox.SelectionFont.Size,
                   FontStyle.Bold
                );
            }
            targetbox.DeselectAll();
        }

        private void lb_Triggers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lb_Triggers.SelectedItem.ToString().StartsWith("Simple"))
            {
                txt_TriggerCheck.Text = Module_OpCode.TriggerCheck(lb_Triggers.SelectedItem.ToString());
                txt_TriggerDelay.Hide();
                txt_TriggerDelay.Text = null;
                txt_TriggerRearm.Hide();
                txt_TriggerRearm.Text = null;
                rtb_TriggerCondition.Hide();
                rtb_TriggerCondition.Text = null;
            }
            else
            {
                txt_TriggerCheck.Text = Module_OpCode.TriggerCheck(lb_Triggers.SelectedItem.ToString());
                txt_TriggerDelay.Show();
                txt_TriggerDelay.Text = Module_OpCode.TriggerDelay(lb_Triggers.SelectedItem.ToString());
                txt_TriggerDelay.Show();
                txt_TriggerRearm.Text = Module_OpCode.TriggerRearm(lb_Triggers.SelectedItem.ToString());
                String[] conditions;
                Module_OpCode.SetTriggerConditions(lb_Triggers.SelectedItem.ToString(), out conditions);
                rtb_TriggerCondition.Hide();
                rtb_TriggerCondition.Lines = conditions;
                
            }
            String[] execute;
            Module_OpCode.SetTriggerContents(lb_Triggers.SelectedItem.ToString(), out execute);
            rtb_TriggerExecute.Hide();
            rtb_TriggerExecute.Lines = execute;
        }
    }
}
