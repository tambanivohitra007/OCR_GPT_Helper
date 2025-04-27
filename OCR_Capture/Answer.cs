using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OCR_Capture
{
    public partial class Answer : Form
    {
        public string AnswerText { get; set; } // Property to hold the answer text
        public Answer()
        {
            InitializeComponent();
            // Move the text assignment to the OnLoad event
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            messageLbl.Text = AnswerText; // Set the label text when the form loads
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW: Adds a shadow to the form
                return cp;
            }
        }
    }
}
