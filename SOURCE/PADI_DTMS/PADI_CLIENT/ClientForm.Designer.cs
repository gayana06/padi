namespace PADI_CLIENT
{
    partial class ClientForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tbxServerList = new System.Windows.Forms.TextBox();
            this.Result = new System.Windows.Forms.GroupBox();
            this.tbxResult = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tbxOperations = new System.Windows.Forms.TextBox();
            this.btnExecute = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.Result.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tbxServerList);
            this.groupBox1.Location = new System.Drawing.Point(397, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 185);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Object Servers";
            // 
            // tbxServerList
            // 
            this.tbxServerList.Enabled = false;
            this.tbxServerList.Location = new System.Drawing.Point(7, 21);
            this.tbxServerList.Multiline = true;
            this.tbxServerList.Name = "tbxServerList";
            this.tbxServerList.Size = new System.Drawing.Size(187, 125);
            this.tbxServerList.TabIndex = 0;
            // 
            // Result
            // 
            this.Result.Controls.Add(this.tbxResult);
            this.Result.Location = new System.Drawing.Point(12, 203);
            this.Result.Name = "Result";
            this.Result.Size = new System.Drawing.Size(585, 147);
            this.Result.TabIndex = 1;
            this.Result.TabStop = false;
            this.Result.Text = "Result";
            // 
            // tbxResult
            // 
            this.tbxResult.Location = new System.Drawing.Point(9, 21);
            this.tbxResult.Multiline = true;
            this.tbxResult.Name = "tbxResult";
            this.tbxResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbxResult.Size = new System.Drawing.Size(560, 108);
            this.tbxResult.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnExecute);
            this.groupBox2.Controls.Add(this.tbxOperations);
            this.groupBox2.Location = new System.Drawing.Point(21, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(357, 185);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Operations";
            // 
            // tbxOperations
            // 
            this.tbxOperations.Location = new System.Drawing.Point(7, 21);
            this.tbxOperations.Multiline = true;
            this.tbxOperations.Name = "tbxOperations";
            this.tbxOperations.Size = new System.Drawing.Size(344, 125);
            this.tbxOperations.TabIndex = 0;
            // 
            // btnExecute
            // 
            this.btnExecute.Location = new System.Drawing.Point(7, 153);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(75, 23);
            this.btnExecute.TabIndex = 1;
            this.btnExecute.Text = "Execute";
            this.btnExecute.UseVisualStyleBackColor = true;
            this.btnExecute.Click += new System.EventHandler(this.btnExecute_Click);
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(609, 362);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.Result);
            this.Controls.Add(this.groupBox1);
            this.Name = "ClientForm";
            this.Text = "ClientForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.Result.ResumeLayout(false);
            this.Result.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tbxServerList;
        private System.Windows.Forms.GroupBox Result;
        private System.Windows.Forms.TextBox tbxResult;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox tbxOperations;
        private System.Windows.Forms.Button btnExecute;
    }
}