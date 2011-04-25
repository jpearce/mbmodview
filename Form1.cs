using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using System.Text;
using System.Windows.Forms;

namespace MBScriptEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();        
            LoadConstants();
            LoadOps();
            LoadScripts();//last
        }

        private void LoadScripts()
        {
            lbScripts.Items.Clear();
            try
            {
                Module_Scripts.LoadFromFile();
                groupBox1.Text = String.Format("Scripts({0})", Module_Scripts.ScriptNames.Length);
                for (int i = 0; i < Module_Scripts.ScriptNames.Length; ++i)
                {
                    lbScripts.Items.Add(Module_Scripts.ScriptNames[i]);
                }
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
                Header_Common.LoadFromFile();
                foreach (KeyValuePair<String, String> kvp in Header_Common.StringValues)
                {
                    lvConst.Items.Add(kvp.Key).SubItems.AddRange(
                        new String[]{Header_Common.UintValues[kvp.Key].ToString(), kvp.Value});
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
                Header_Operations.LoadFromFile();
                foreach (KeyValuePair<String, Int64> kvp in Header_Operations.StringValues)
                {
                    lv_Ops.Items.Add(kvp.Key).SubItems.Add(kvp.Value.ToString());
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
            groupBox2.Text = scriptname;
            String[] scriptcontents;
            Module_Scripts.SetScriptContents(scriptname, out scriptcontents);
            rtbScript.Lines = scriptcontents;
        }
    }
}
