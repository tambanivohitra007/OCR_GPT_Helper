// Application: OCR GPT Helper
// Version: 1.0.0
// Developer: Rindra Razafinjatovo
// Occupation: IT Administration/Instructor

namespace OCR_Capture
{
    partial class Answer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Answer));
            pictureBox1 = new PictureBox();
            messageLbl = new RichTextBox();
            panel1 = new Panel();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Top;
            pictureBox1.Image = Properties.Resources.answer_ai;
            pictureBox1.Location = new Point(0, 12);
            pictureBox1.Margin = new Padding(2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(266, 30);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // messageLbl
            // 
            messageLbl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            messageLbl.BorderStyle = BorderStyle.None;
            messageLbl.Font = new Font("Segoe UI", 12F);
            messageLbl.Location = new Point(20, 22);
            messageLbl.Margin = new Padding(20);
            messageLbl.Name = "messageLbl";
            messageLbl.Size = new Size(226, 277);
            messageLbl.TabIndex = 2;
            messageLbl.Text = "";
            // 
            // panel1
            // 
            panel1.Controls.Add(messageLbl);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 42);
            panel1.Name = "panel1";
            panel1.Size = new Size(266, 319);
            panel1.TabIndex = 3;
            // 
            // Answer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            ClientSize = new Size(266, 361);
            Controls.Add(panel1);
            Controls.Add(pictureBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Answer";
            Opacity = 0.95D;
            Padding = new Padding(0, 12, 0, 0);
            StartPosition = FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.PictureBox pictureBox1;
        private RichTextBox messageLbl;
        private Panel panel1;
    }
}