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

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult retry = DialogResult.Retry;
            while (retry == DialogResult.Retry)
            {
                retry = MessageBox.Show("A new what, exactly?", "Guidance required!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Question);
            }
        }

        private void wbCut(WebBrowser wb) { wb.Document.ExecCommand("Cut", false, null); }
        private void wbCopy(WebBrowser wb) { wb.Document.ExecCommand("Copy", false, null); }
        private void wbPaste(WebBrowser wb) { wb.Document.ExecCommand("Paste", false, null); }
        private void wbUndo(WebBrowser wb) { wb.Document.ExecCommand("Undo", false, null); }
        private void wbRedo(WebBrowser wb) { wb.Document.ExecCommand("Redo", false, null); }
        private void wbSelectAll(WebBrowser wb) { wb.Document.ExecCommand("SelectAll", false, null); }

        private void ctscript_menuCut_Click(object sender, EventArgs e)
        {
            WebBrowser wb = ct_Script0.SourceControl as WebBrowser;
            if (!ReferenceEquals(wb, null)) { wbCut(wb); }
        }

        private void ctscript_menuCopy_Click(object sender, EventArgs e)
        {
            WebBrowser wb = ct_Script0.SourceControl as WebBrowser;
            if (!ReferenceEquals(wb, null)) { wbCopy(wb); }
        }
        private void ctscript_menuPaste_Click(object sender, EventArgs e)
        {
            WebBrowser wb = ct_Script0.SourceControl as WebBrowser;
            if (!ReferenceEquals(wb, null)) { wbPaste(wb); }
        }
        private void ctscript_menuUndo_Click(object sender, EventArgs e)
        {
            WebBrowser wb = ct_Script0.SourceControl as WebBrowser;
            if (!ReferenceEquals(wb, null)) { wbUndo(wb); }
        }
        private void ctscript_menuRedo_Click(object sender, EventArgs e)
        {
            WebBrowser wb = ct_Script0.SourceControl as WebBrowser;
            if (!ReferenceEquals(wb, null)) { wbRedo(wb); }
        }

        private void ctscript_menuSelectAll_Click(object sender, EventArgs e)
        {
            WebBrowser wb = ct_Script0.SourceControl as WebBrowser;
            if (!ReferenceEquals(wb, null)) { wbSelectAll(wb); }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (wb_script.Focused) { wbUndo(wb_script); }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (wb_script.Focused) { wbRedo(wb_script); }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (wb_script.Focused) { wbRedo(wb_script); }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (wb_script.Focused) { wbCopy(wb_script); }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (wb_script.Focused) { wbPaste(wb_script); }
        }

        private void selectallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (wb_script.Focused) { wbSelectAll(wb_script); }
        }

        
    }
}
