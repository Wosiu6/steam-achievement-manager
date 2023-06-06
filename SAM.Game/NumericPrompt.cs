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

using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Game
{
    internal partial class NumericPrompt : Form
    {
        public int? NumberOfMinutes { get; set; }

        public NumericPrompt(string gameName, int numberOfAchievementsToGet, int totalNumberOfAchievements)
        {
            this.InitializeComponent();

            completion_lbl.Text = $"{numberOfAchievementsToGet}/{totalNumberOfAchievements}";

            link_lbl.Text = $@"https://howlongtobeat.com/stats?q={gameName.Replace(" ", "%2520")}";
            link_lbl.Text = Regex.Replace(link_lbl.Text, "[^\x00-\x80]+", ""); ;

            estimatedTime_try_lbl.Text = Task.Run(() => TryGetCompletionistTimeForGame(gameName)).Result;
        }

        private async Task<string> TryGetCompletionistTimeForGame(string gameName)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://howlongtobeat.com/api/search");

            request.Headers.Add("origin", "https://howlongtobeat.com");
            request.Headers.Add("referer", "https://howlongtobeat.com");
            request.Headers.Add("user-agent", "Chrome");

            StringBuilder sb = new StringBuilder();
            foreach (string gameNamePart in gameName.Split(' '))
            {
                sb.Append($"\"{gameNamePart}\", ");
            }
            sb.Length--;
            sb.Length--;

            string bodyJson = Regex.Replace(sb.ToString(), "[^\x00-\x80]+", "");

            var content = new StringContent("{\"searchTerms\":[" + bodyJson + "]}", null, "application/json");
            request.Content = content;


            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            HLTB.Root root = JsonConvert.DeserializeObject<HLTB.Root>(response.Content.ReadAsStringAsync().Result);
            var time = TimeSpan.FromSeconds(root.data[0].comp_100);

            return time.Hours + "h " + time.Minutes + "m (" + Math.Round(time.TotalMinutes, 0) + "m)";
        }

        private void InitializeComponent()
        {
            this.run_btn = new System.Windows.Forms.Button();
            this.description_lbl = new System.Windows.Forms.Label();
            this.minutes_lbl = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.currentCompletion_lbl = new System.Windows.Forms.Label();
            this.linkDesc_lbl = new System.Windows.Forms.Label();
            this.link_lbl = new System.Windows.Forms.LinkLabel();
            this.estimatedTimeLeftDesc_lbl = new System.Windows.Forms.Label();
            this.estimatedTime_try_lbl = new System.Windows.Forms.Label();
            this.completion_lbl = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // run_btn
            // 
            this.run_btn.Location = new System.Drawing.Point(294, 192);
            this.run_btn.Name = "run_btn";
            this.run_btn.Size = new System.Drawing.Size(75, 23);
            this.run_btn.TabIndex = 0;
            this.run_btn.Text = "Run";
            this.run_btn.UseVisualStyleBackColor = true;
            this.run_btn.Click += new System.EventHandler(this.run_btn_Click);
            // 
            // description_lbl
            // 
            this.description_lbl.Location = new System.Drawing.Point(12, 10);
            this.description_lbl.Margin = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.description_lbl.Name = "description_lbl";
            this.description_lbl.Size = new System.Drawing.Size(370, 72);
            this.description_lbl.TabIndex = 1;
            this.description_lbl.Text = "Please input time over which the achievements are going to get unlocked.\r\n\r\n\r\nHer" +
    "e is some information on how long it should take:";
            // 
            // minutes_lbl
            // 
            this.minutes_lbl.Location = new System.Drawing.Point(12, 166);
            this.minutes_lbl.Name = "minutes_lbl";
            this.minutes_lbl.Size = new System.Drawing.Size(231, 31);
            this.minutes_lbl.TabIndex = 2;
            this.minutes_lbl.Text = "Input value of minutes close to what you want your playtime for the achievements " +
    "left to be (round down)";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(249, 166);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown1.TabIndex = 3;
            // 
            // currentCompletion_lbl
            // 
            this.currentCompletion_lbl.Location = new System.Drawing.Point(12, 83);
            this.currentCompletion_lbl.Margin = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.currentCompletion_lbl.Name = "currentCompletion_lbl";
            this.currentCompletion_lbl.Size = new System.Drawing.Size(114, 16);
            this.currentCompletion_lbl.TabIndex = 6;
            this.currentCompletion_lbl.Text = "- Current Completion: ";
            // 
            // linkDesc_lbl
            // 
            this.linkDesc_lbl.Location = new System.Drawing.Point(12, 120);
            this.linkDesc_lbl.Margin = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.linkDesc_lbl.Name = "linkDesc_lbl";
            this.linkDesc_lbl.Size = new System.Drawing.Size(64, 16);
            this.linkDesc_lbl.TabIndex = 7;
            this.linkDesc_lbl.Text = "- HLtB link:";
            // 
            // link_lbl
            // 
            this.link_lbl.Location = new System.Drawing.Point(69, 120);
            this.link_lbl.Name = "link_lbl";
            this.link_lbl.Size = new System.Drawing.Size(296, 31);
            this.link_lbl.TabIndex = 8;
            this.link_lbl.TabStop = true;
            this.link_lbl.Text = "https://howlongtobeat.com/game/";
            this.link_lbl.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.link_lbl_LinkClicked);
            // 
            // estimatedTimeLeftDesc_lbl
            // 
            this.estimatedTimeLeftDesc_lbl.Location = new System.Drawing.Point(12, 100);
            this.estimatedTimeLeftDesc_lbl.Margin = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.estimatedTimeLeftDesc_lbl.Name = "estimatedTimeLeftDesc_lbl";
            this.estimatedTimeLeftDesc_lbl.Size = new System.Drawing.Size(114, 16);
            this.estimatedTimeLeftDesc_lbl.TabIndex = 9;
            this.estimatedTimeLeftDesc_lbl.Text = "- HLTB Completionist: ";
            // 
            // estimatedTime_try_lbl
            // 
            this.estimatedTime_try_lbl.Location = new System.Drawing.Point(119, 100);
            this.estimatedTime_try_lbl.Margin = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.estimatedTime_try_lbl.Name = "estimatedTime_try_lbl";
            this.estimatedTime_try_lbl.Size = new System.Drawing.Size(250, 16);
            this.estimatedTime_try_lbl.TabIndex = 10;
            this.estimatedTime_try_lbl.Text = "loading...";
            // 
            // completion_lbl
            // 
            this.completion_lbl.Location = new System.Drawing.Point(119, 83);
            this.completion_lbl.Margin = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.completion_lbl.Name = "completion_lbl";
            this.completion_lbl.Size = new System.Drawing.Size(246, 16);
            this.completion_lbl.TabIndex = 11;
            this.completion_lbl.Text = "loading...";
            // 
            // NumericPrompt
            // 
            this.ClientSize = new System.Drawing.Size(377, 228);
            this.Controls.Add(this.completion_lbl);
            this.Controls.Add(this.estimatedTime_try_lbl);
            this.Controls.Add(this.estimatedTimeLeftDesc_lbl);
            this.Controls.Add(this.link_lbl);
            this.Controls.Add(this.linkDesc_lbl);
            this.Controls.Add(this.currentCompletion_lbl);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.minutes_lbl);
            this.Controls.Add(this.description_lbl);
            this.Controls.Add(this.run_btn);
            this.Name = "NumericPrompt";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);

        }

        private void run_btn_Click(object sender, System.EventArgs e)
        {
            NumberOfMinutes = Convert.ToInt32(numericUpDown1.Value);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void link_lbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(((LinkLabel)sender).Text);
        }
    }
}
