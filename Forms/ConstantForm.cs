using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Forms;
using System.Drawing;

namespace MBModViewer
{
    public partial class ConstantForm : Form
    {
        public ConstantForm()
        {
            InitializeComponent();
            LoadModVars();
        }

        private void LoadModVars()
        {
            lb_mvtypes.Items.Clear();
            SortedList<String, byte> tempsort = new SortedList<string, byte>();
            tempsort.Add("common", 0);
            tempsort.Add("operations", 0);
            tempsort.Add("triggers", 0);
            foreach (KeyValuePair<String, byte> kvp in tempsort) { lb_mvtypes.Items.Add(kvp.Key); }
        }


        private void lb_mvtypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            String t = lb_mvtypes.SelectedItem.ToString();
            PythonReader pr = null;
            if (t == "common") { pr = StaticDataHolder.Header_Common; }
            else if (t == "operations") { pr = StaticDataHolder.Header_Operations; }
            else if (t == "triggers") { pr = StaticDataHolder.Header_Triggers; }            
            lb_mvnames.Items.Clear();
            SortedList<String, byte> tempsort = new SortedList<string, byte>();
            for (int j = 0; j < pr.Items.Length; ++j)
            {
                if (tempsort.ContainsKey(pr.Items[j].Name))
                {
                    for (int k = 0; k < 100; ++k)
                    {
                        if (!tempsort.ContainsKey(pr.Items[j].Name + "(" + k.ToString() + ")"))
                        {
                            tempsort.Add(pr.Items[j].Name + "(" + k.ToString() + ")", 0);
                            k = 100;
                        }
                    }
                }
                else { tempsort.Add(pr.Items[j].Name, 0); }
            }
            foreach (KeyValuePair<String, byte> kvp in tempsort) { lb_mvnames.Items.Add(kvp.Key); }            

        }

        private void lb_mvnames_SelectedIndexChanged(object sender, EventArgs e)
        {
            String t = lb_mvtypes.SelectedItem.ToString();
            PythonReader pr = null;
            if (t == "common") { pr = StaticDataHolder.Header_Common; }
            else if (t == "operations") { pr = StaticDataHolder.Header_Operations; }
            else if (t == "triggers") { pr = StaticDataHolder.Header_Triggers; }            
            String v = lb_mvnames.SelectedItem.ToString();
            for (int j = 0; j < pr.PythonItems.Length; ++j)
            {
                if (pr.Items[j].Name == v)
                {
                    lv_mvvals.Items.Clear();
                    lv_mvvals.Items.Add("Name").SubItems.Add(pr.PythonItems[j].Name);
                    lv_mvvals.Items.Add("Value").SubItems.Add(pr.PythonItems[j].Value.ToString());
                    lv_mvvals.Items.Add("Source").SubItems.Add(String.Format("\"{0}\"", pr.PythonItems[j].Source));
                }
            }            
            splitContainer7.SplitterDistance = 50;
        }

        private void lb_mvtypes_MouseEnter(object sender, EventArgs e)
        {            
            splitContainer7.SplitterDistance = splitContainer7.Height - 50;            
        }

        private void lb_mvnames_MouseEnter(object sender, EventArgs e)
        {
            splitContainer7.SplitterDistance = 50;
        }


    }
}
