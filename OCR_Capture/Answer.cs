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

        public enum ScreenSide
        {
            Left,
            Right
        }

        public ScreenSide Side { get; set; } = ScreenSide.Right; // Default to right side

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            messageLbl.Text = AnswerText; // Set the label text when the form loads

            // Position the form on the chosen side of the primary screen
            var workingArea = Screen.PrimaryScreen.WorkingArea;
            int x = Side == ScreenSide.Right
                ? workingArea.Right - this.Width
                : workingArea.Left;
            int y = workingArea.Top + (workingArea.Height - this.Height) / 2;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(x, y);
        }

        // Add this method to allow updating the answer text and resizing dynamically
        public void UpdateAnswerText(string newText)
        {
            AnswerText = newText;
            messageLbl.Text = newText;
        }
    }
}
