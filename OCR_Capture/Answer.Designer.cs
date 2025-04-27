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
            messageLbl = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // messageLbl
            // 
            messageLbl.Dock = DockStyle.Fill;
            messageLbl.Font = new Font("Segoe UI", 16F);
            messageLbl.Location = new Point(0, 70);
            messageLbl.Name = "messageLbl";
            messageLbl.Size = new Size(906, 268);
            messageLbl.TabIndex = 0;
            messageLbl.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Top;
            pictureBox1.Image = Properties.Resources.answer_ai;
            pictureBox1.Location = new Point(0, 20);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(906, 50);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // Answer
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            ClientSize = new Size(906, 338);
            Controls.Add(messageLbl);
            Controls.Add(pictureBox1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "Answer";
            Opacity = 0.9D;
            Padding = new Padding(0, 20, 0, 0);
            StartPosition = FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Label messageLbl;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}