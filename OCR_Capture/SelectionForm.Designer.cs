// Application: OCR GPT Helper
// Version: 1.0.0
// Developer: Rindra Razafinjatovo
// Occupation: IT Administration/Instructor

namespace OCR_Capture
{
    partial class SelectionForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // SelectionForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(800, 450);
            Cursor = Cursors.Default;
            ForeColor = Color.DarkGray;
            FormBorderStyle = FormBorderStyle.None;
            Name = "SelectionForm";
            Opacity = 0.5D;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Select ";
            TopMost = true;
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true; // Enable double buffering for smoother rendering
            KeyPreview = true; // Allow key events to be captured
            Paint += SelectionForm_Paint;
            KeyDown += SelectionForm_KeyDown;
            MouseDown += SelectionForm_MouseDown;
            MouseMove += SelectionForm_MouseMove;
            MouseUp += SelectionForm_MouseUp;
            ResumeLayout(false);
        }

        #endregion
    }
}