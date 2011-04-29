using System;
using System.Drawing;
using System.Collections.Generic;

namespace MBModViewer
{
    /// <summary>Too bad I had to actually do some GUI work!
    /// Pulled a class off the internet but it was predictably bloated so custom all the way baby!</summary>
    public class CodeTextBox : System.Windows.Forms.RichTextBox
    {
        #region const helpers
        private const char _period = '.', _comma = ',', _quote = '"', _newline = '\n', _space = ' ', _leftparen = '(';
        private static int newlinelen = System.Environment.NewLine.Length;
        #endregion

        #region fields
        //no need for accessors
        public bool HighlightStrings;
        private Color ColorString, ColorCommand;
        #endregion


        #region ctor
        public CodeTextBox()
            : base()
        {
            working = false;
            this.ColorString = Color.Maroon;//Color.FromArgb(64, 64, 64);
            this.ColorCommand = Color.Blue;
            this.HighlightStrings = true;
            this.lastlinesdone = new int[0];
        }


        #endregion
        #region overrides
        private static bool working;
        /// <summary>Stops it from painting while it's being checked.  I tried like hell to find 
        /// another way, hope it works in Mono.</summary>
        /// <param name="m"></param>
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == 0x00f && working) { m.Result = IntPtr.Zero; }
            else { base.WndProc(ref m); }
        }

        private int[] lastlinesdone;//hacky but i really don't want to spend hours for all the possibilities


        protected override void OnTextChanged(EventArgs e)
        {
            if (!working)
            {
                if (this.Lines != null && this.Lines.Length != this.lastlinesdone.Length)
                {
                    working = true;
                    this.SuspendLayout();
                    this.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
                    //this.HideSelection = true;
                    DoHighlight(this);
                    this.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
                    //this.HideSelection = false;
                    this.ResumeLayout();
                    this.lastlinesdone = new int[this.Lines.Length];
                    for (int i = 0; i < this.Lines.Length; ++i) { this.lastlinesdone[i] = this.Lines[i].Length; }
                    working = false;
                    base.OnTextChanged(e);
                }
                else { base.OnTextChanged(e); }
            }
        }

        private void Highlight(int start, int len, Color color)
        {
            this.Select(start, len);
            this.SelectionColor = color;
        }

        private static void DoHighlight(CodeTextBox ctb)
        {
            int caretpos = ctb.SelectionStart;//put it back to this

 
            int startchar = 0;//limit rehighlighting everything
            for (int i = 1; i < ctb.lastlinesdone.Length && (ctb.lastlinesdone[i] == ctb.Lines[i].Length); ++i)
            {//firstline should be left at the first line that's different                
                startchar += (ctb.Lines[i - 1].Length + newlinelen); //newline                
            }


            //command is a temp(?) hack to highlight commands
            bool quotesel = false, command = false, startline = true;
            int newselpos = 0;
            for (int i = startchar; i < ctb.Text.Length; ++i) 
            {
                switch (ctb.Text[i])
                {
                    case _newline:
                        quotesel = command = false;
                        startline = true;                        
                        break;
                    case _quote:
                        if (command)
                        {
                            ctb.Highlight(newselpos, (i - newselpos), ctb.ColorCommand);
                            command = false;
                        }
                        else if (quotesel) { ctb.Highlight(newselpos, (i - newselpos) + 1, ctb.ColorString);}
                        else if (!quotesel) { newselpos = i; }
                        quotesel = !quotesel;
                        startline = false;
                        break;
                    case _space:
                        if (!startline)//starting spaces
                        {                            
                            if (command)
                            {
                                ctb.Highlight(newselpos, (i - newselpos), ctb.ColorCommand); 
                                command = false;
                            }
                        }
                        break;
                    case _comma:                       
                        if (!startline)//starting spaces
                        {
                            if (command)
                            {
                                ctb.Highlight(newselpos, (i - newselpos), ctb.ColorCommand);
                                command = false;
                            }
                        }
                        break;
                    case _leftparen://temphack to skip ( at start of line
                        if (!startline)//same as space essentially
                        {
                            if (command)
                            {
                                ctb.Highlight(newselpos, (i - newselpos), ctb.ColorCommand);
                                command = false;
                            }
                        }
                        break;
                    default:
                        if (startline && !command) 
                        { 
                            newselpos = i;  
                            command = true; 
                            startline = false; 
                        }
                        break;
                }
            }
            ctb.DeselectAll();
            ctb.SelectionStart = caretpos;
        }
        #endregion
    }


}





