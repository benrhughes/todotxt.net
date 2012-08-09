namespace BuildController
{
	partial class Form1
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.btnRun = new System.Windows.Forms.Button();
			this.clbOptions = new System.Windows.Forms.CheckedListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.tbVersion = new System.Windows.Forms.TextBox();
			this.tbChangelog = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnRun
			// 
			this.btnRun.Location = new System.Drawing.Point(95, 287);
			this.btnRun.Name = "btnRun";
			this.btnRun.Size = new System.Drawing.Size(154, 23);
			this.btnRun.TabIndex = 0;
			this.btnRun.Text = "Run Selected";
			this.btnRun.UseVisualStyleBackColor = true;
			this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
			// 
			// clbOptions
			// 
			this.clbOptions.CheckOnClick = true;
			this.clbOptions.FormattingEnabled = true;
			this.clbOptions.Location = new System.Drawing.Point(13, 148);
			this.clbOptions.Name = "clbOptions";
			this.clbOptions.Size = new System.Drawing.Size(305, 124);
			this.clbOptions.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(10, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(45, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Version:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(10, 39);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(61, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Changelog:";
			// 
			// tbVersion
			// 
			this.tbVersion.Location = new System.Drawing.Point(95, 6);
			this.tbVersion.Name = "tbVersion";
			this.tbVersion.Size = new System.Drawing.Size(223, 20);
			this.tbVersion.TabIndex = 4;
			// 
			// tbChangelog
			// 
			this.tbChangelog.AcceptsReturn = true;
			this.tbChangelog.Location = new System.Drawing.Point(95, 39);
			this.tbChangelog.Multiline = true;
			this.tbChangelog.Name = "tbChangelog";
			this.tbChangelog.Size = new System.Drawing.Size(223, 75);
			this.tbChangelog.TabIndex = 5;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(10, 123);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(45, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Actions:";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(330, 323);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.tbChangelog);
			this.Controls.Add(this.tbVersion);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.clbOptions);
			this.Controls.Add(this.btnRun);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Form1";
			this.Text = "todotxt.net build controller";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnRun;
		private System.Windows.Forms.CheckedListBox clbOptions;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox tbVersion;
		private System.Windows.Forms.TextBox tbChangelog;
		private System.Windows.Forms.Label label3;
	}
}

