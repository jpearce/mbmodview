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
            rtbScript.Hide(); 
            rtbScript.Lines = scriptcontents;
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

        private static ConstantForm constants = null;
        private void constantsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (constants == null || constants.IsDisposed)
            {
                constants = new ConstantForm();
                constants.Show();
            }
            constants.BringToFront();
        }

        
    }
}
