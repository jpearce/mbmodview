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
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();            
            StaticDataHolder.LoadAll();            
            //MessageBox.Show("Error loading xml configurations:\n" + ex.Message);                  
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
            rtb_Script.Lines = scriptcontents; 
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
                
                rtb_TriggerCondition.Lines = conditions;
                
            }
            String[] execute;
            Module_OpCode.SetTriggerContents(lb_Triggers.SelectedItem.ToString(), out execute);            
            rtb_TriggerExecute.Lines = execute;
        }

        private static OptionForm options = null;
        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (options == null || options.IsDisposed) 
            { 
                options = new OptionForm();
                options.Show();
            }
            options.BringToFront();
        }

        private static VariableForm variables = null;
        private void variablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (variables == null || variables.IsDisposed)
            {
                variables = new VariableForm();
                variables.Show();
            }
            variables.BringToFront();
        }

        private ConstantForm constants = null;
        private void constantsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (constants == null || constants.IsDisposed)
            {
                constants = new ConstantForm();
                constants.Show();
            }
            constants.BringToFront();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult retry = DialogResult.Retry;
            while (retry == DialogResult.Retry)
            {
                retry = MessageBox.Show("A new what, exactly?", "Guidance required!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Question);
            }
        }

        private void ctscript_menuCut_Click(object sender, EventArgs e)
        {
            if (ct_Script0.SourceControl == rtb_Script) { rtb_Script.Cut(); }
            else if (ct_Script0.SourceControl == rtb_TriggerCondition) { rtb_TriggerCondition.Cut(); }
            else if (ct_Script0.SourceControl == rtb_TriggerExecute) { rtb_TriggerExecute.Cut(); }
        }

        private void ctscript_menuCopy_Click(object sender, EventArgs e)
        {
            if (ct_Script0.SourceControl == rtb_Script) { rtb_Script.Copy(); }
            else if (ct_Script0.SourceControl == rtb_TriggerCondition) { rtb_TriggerCondition.Copy(); }
            else if (ct_Script0.SourceControl == rtb_TriggerExecute) { rtb_TriggerExecute.Copy(); }
        }

        private void ctscript_menuPaste_Click(object sender, EventArgs e)
        {
            if (ct_Script0.SourceControl == rtb_Script) { rtb_Script.Paste(); }
            else if (ct_Script0.SourceControl == rtb_TriggerCondition) { rtb_TriggerCondition.Paste(); }
            else if (ct_Script0.SourceControl == rtb_TriggerExecute) { rtb_TriggerExecute.Paste(); }
        }

        private void ctscript_menuUndo_Click(object sender, EventArgs e)
        {
            if (ct_Script0.SourceControl == rtb_Script) { rtb_Script.Undo(); }
            else if (ct_Script0.SourceControl == rtb_TriggerCondition) { rtb_TriggerCondition.Undo(); }
            else if (ct_Script0.SourceControl == rtb_TriggerExecute) { rtb_TriggerExecute.Undo(); }
        }

        private void ctscript_menuRedo_Click(object sender, EventArgs e)
        {
            if (ct_Script0.SourceControl == rtb_Script) { rtb_Script.Redo(); }
            else if (ct_Script0.SourceControl == rtb_TriggerCondition) { rtb_TriggerCondition.Redo(); }
            else if (ct_Script0.SourceControl == rtb_TriggerExecute) { rtb_TriggerExecute.Redo(); }
        }

        private void ctscript_menuSelectAll_Click(object sender, EventArgs e)
        {
            if (ct_Script0.SourceControl == rtb_Script) { rtb_Script.SelectAll(); }
            else if (ct_Script0.SourceControl == rtb_TriggerCondition) { rtb_TriggerCondition.SelectAll(); }
            else if (ct_Script0.SourceControl == rtb_TriggerExecute) { rtb_TriggerExecute.SelectAll(); }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (rtb_Script.Focused) { rtb_Script.Cut(); }
            else if (rtb_TriggerCondition.Focused) { rtb_TriggerCondition.Cut(); }
            else if (rtb_TriggerExecute.Focused) { rtb_TriggerExecute.Cut(); }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (rtb_Script.Focused) { rtb_Script.Copy(); }
            else if (rtb_TriggerCondition.Focused) { rtb_TriggerCondition.Copy(); }
            else if (rtb_TriggerExecute.Focused) { rtb_TriggerExecute.Copy(); }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (rtb_Script.Focused) { rtb_Script.Paste(); }
            else if (rtb_TriggerCondition.Focused) { rtb_TriggerCondition.Paste(); }
            else if (rtb_TriggerExecute.Focused) { rtb_TriggerExecute.Paste(); }
        }

        private void selectallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (rtb_Script.Focused) { rtb_Script.SelectAll(); }
            else if (rtb_TriggerCondition.Focused) { rtb_TriggerCondition.SelectAll(); }
            else if (rtb_TriggerExecute.Focused) { rtb_TriggerExecute.SelectAll(); }
        }

        private void ctscript_menuLookup_Click(object sender, EventArgs e)
        {//move lookup functionality to staticdataholder at some point
            if (rtb_Script.Focused || rtb_TriggerCondition.Focused || rtb_TriggerExecute.Focused)
            {
                String temp = null;
                if (rtb_Script.Focused) { temp = rtb_Script.GetWordAtCaret(); }
                else if (rtb_TriggerCondition.Focused) { temp = rtb_TriggerCondition.GetWordAtCaret(); }
                else if (rtb_TriggerExecute.Focused) { temp = rtb_TriggerExecute.GetWordAtCaret(); }
                
                if (temp == null || temp.Length < 2 || temp[0] != '"' || temp[1] == ':')
                {
                    MessageBox.Show("No valid lookup text found.");
                }
                else
                {
                    String lookitup = temp.Replace("\"", String.Empty);
                    bool foundit = false;
                    //see if it's a script
                    if (lookitup.StartsWith("script_"))
                    {
                        lookitup = lookitup.Substring(7);
                        for (int i = 0; i < lbScripts.Items.Count && !foundit; ++i)
                        {
                            if (lbScripts.Items[i].ToString() == lookitup)
                            {
                                lbScripts.SelectedIndex = i;
                                foundit = true;
                            }
                        }
                        if(!foundit) { lookitup = "script_" + lookitup; }//put it back
                    }
                    if (!foundit)
                    {//look through variables 
                        String vartype = null, varname = null;
                        if (lookitup[0] == '$')
                        {
                            lookitup = lookitup.Substring(1);
                            vartype = "globalvar";
                            for (int j = 0; j < StaticDataHolder.DataReaders.Length && !foundit; ++j)
                            {
                                if (StaticDataHolder.DataReaders[j].ItemType == vartype)
                                {
                                    for (int k = 0; k < StaticDataHolder.DataReaders[j].Items.Length && !foundit; ++k)
                                    {
                                        if (StaticDataHolder.DataReaders[j].Items[k].Name == lookitup)
                                        {
                                            //MessageBox.Show(vartype + "->" + lookitup);
                                            varname = StaticDataHolder.DataReaders[j].Name;
                                            foundit = true;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {                            
                            //variables with known prefixes
                            for (int i = 0; i < StaticDataHolder.ItemSettings.Length && !foundit; ++i)
                            {
                                if (!String.IsNullOrEmpty(StaticDataHolder.ItemSettings[i].Prefix) &&
                                    lookitup.StartsWith(StaticDataHolder.ItemSettings[i].Prefix))
                                {
                                    vartype = StaticDataHolder.ItemSettings[i].Name;
                                    for (int j = 0; j < StaticDataHolder.DataReaders.Length && !foundit; ++j)
                                    {
                                        if (StaticDataHolder.DataReaders[j].ItemType == vartype)
                                        {
                                            for (int k = 0; k < StaticDataHolder.DataReaders[j].Items.Length && !foundit; ++k)
                                            {
                                                if (StaticDataHolder.DataReaders[j].Items[k].Name == lookitup)
                                                {
                                                    //MessageBox.Show(vartype + "->" + lookitup);
                                                    varname = StaticDataHolder.DataReaders[j].Name;
                                                    foundit = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //variables without known prefixes
                            for (int i = 0; i < StaticDataHolder.ItemSettings.Length && !foundit; ++i)
                            {
                                if (String.IsNullOrEmpty(StaticDataHolder.ItemSettings[i].Prefix))
                                {
                                    vartype = StaticDataHolder.ItemSettings[i].Name;
                                    for (int j = 0; j < StaticDataHolder.DataReaders.Length && !foundit; ++j)
                                    {
                                        if (StaticDataHolder.DataReaders[j].ItemType == vartype)
                                        {
                                            for (int k = 0; k < StaticDataHolder.DataReaders[j].Items.Length && !foundit; ++k)
                                            {
                                                if (StaticDataHolder.DataReaders[j].Items[k].Name == lookitup)
                                                {
                                                    //MessageBox.Show(vartype + "->" + lookitup);
                                                    foundit = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!String.IsNullOrEmpty(varname))
                        {
                            variablesToolStripMenuItem_Click(null, null);//just use the menuitem clicker
                            variables.GoToVar(varname, lookitup);
                        }
                    }
                    if (!foundit) { MessageBox.Show("Unable to find " + lookitup); }
                }
            }
        }

        
    }
}
