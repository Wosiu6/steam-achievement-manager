/* Copyright (c) 2019 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Windows.Forms;

namespace SAM.Game
{
    internal partial class NumericPrompt : Form
    {
        public int? NumberOfMinutes { get; set; }

        public NumericPrompt()
        {
            this.InitializeComponent();
        }

            private void InitializeComponent()
        {
            this.run_btn = new System.Windows.Forms.Button();
            this.description_lbl = new System.Windows.Forms.Label();
            this.minutes_lbl = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // run_btn
            // 
            this.run_btn.Location = new System.Drawing.Point(98, 70);
            this.run_btn.Name = "run_btn";
            this.run_btn.Size = new System.Drawing.Size(75, 23);
            this.run_btn.TabIndex = 0;
            this.run_btn.Text = "Run";
            this.run_btn.UseVisualStyleBackColor = true;
            this.run_btn.Click += new System.EventHandler(this.run_btn_Click);
            // 
            // description_lbl
            // 
            this.description_lbl.Enabled = false;
            this.description_lbl.Location = new System.Drawing.Point(12, 9);
            this.description_lbl.Margin = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.description_lbl.Name = "description_lbl";
            this.description_lbl.Size = new System.Drawing.Size(150, 32);
            this.description_lbl.TabIndex = 1;
            this.description_lbl.Text = "Please input time over which the achievements are going to start to unlock.";
            // 
            // minutes_lbl
            // 
            this.minutes_lbl.AutoSize = true;
            this.minutes_lbl.Location = new System.Drawing.Point(12, 46);
            this.minutes_lbl.Name = "minutes_lbl";
            this.minutes_lbl.Size = new System.Drawing.Size(35, 13);
            this.minutes_lbl.TabIndex = 2;
            this.minutes_lbl.Text = "Mins: ";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(53, 44);
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown1.TabIndex = 3;
            this.numericUpDown1.Maximum = Int32.MaxValue;
            // 
            // NumericPrompt
            // 
            this.ClientSize = new System.Drawing.Size(180, 103);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.minutes_lbl);
            this.Controls.Add(this.description_lbl);
            this.Controls.Add(this.run_btn);
            this.Name = "NumericPrompt";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void run_btn_Click(object sender, System.EventArgs e)
        {
            NumberOfMinutes = Convert.ToInt32(numericUpDown1.Value);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
