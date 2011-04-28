using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Forms;
using System.Drawing;

namespace MBModViewer
{
    public partial class VariableForm : Form
    {
        public VariableForm()
        {
            InitializeComponent();
            LoadModVars();
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
                            lv_mvvals.Items.Add("Source").SubItems.Add(String.Format("\"{0}\"", StaticDataHolder.DataReaders[i].Items[j].Source));
                            String[] labels = StaticDataHolder.DataReaders[i].ListLabels();
                            String[] values = StaticDataHolder.DataReaders[i].ListContents(j);
                            for (int k = 0; k < labels.Length; ++k)
                            {
                                lv_mvvals.Items.Add(labels[k]).SubItems.Add(values[k]);
                            }
                        }
                    }
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
