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

using MoreLinq;
using Newtonsoft.Json;
using SAM.API.Types;
using SAM.Game.Stats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using APITypes = SAM.API.Types;

namespace SAM.Game
{
    internal partial class Manager : Form
    {
        private readonly long _GameId;
        private readonly API.Client _SteamClient;

        private readonly WebClient _IconDownloader = new WebClient();

        private readonly List<Stats.AchievementInfo> _IconQueue = new List<Stats.AchievementInfo>();
        private readonly List<Stats.StatDefinition> _StatDefinitions = new List<Stats.StatDefinition>();

        private readonly List<Stats.AchievementDefinition> _AchievementDefinitions =
            new List<Stats.AchievementDefinition>();

        private readonly BindingList<Stats.StatInfo> _Statistics = new BindingList<Stats.StatInfo>();

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        private readonly API.Callbacks.UserStatsReceived _UserStatsReceivedCallback;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        private TimeSpan TimeForAchievements { get; set; }
        private TimeSpan TimeForAchievement { get; set; }
        private int Counter { get; set; }

        //private API.Callback<APITypes.UserStatsStored> UserStatsStoredCallback;

        public Manager(long gameId, API.Client client)
        {
            this.InitializeComponent();

            this._MainTabControl.SelectedTab = this._AchievementsTabPage;
            //this.statisticsList.Enabled = this.checkBox1.Checked;

            this._AchievementImageList.Images.Add("Blank", new Bitmap(64, 64));

            this._StatisticsDataGridView.AutoGenerateColumns = false;

            this._StatisticsDataGridView.Columns.Add("name", "Name");
            this._StatisticsDataGridView.Columns[0].ReadOnly = true;
            this._StatisticsDataGridView.Columns[0].Width = 200;
            this._StatisticsDataGridView.Columns[0].DataPropertyName = "DisplayName";

            this._StatisticsDataGridView.Columns.Add("value", "Value");
            this._StatisticsDataGridView.Columns[1].ReadOnly = this._EnableStatsEditingCheckBox.Checked == false;
            this._StatisticsDataGridView.Columns[1].Width = 90;
            this._StatisticsDataGridView.Columns[1].DataPropertyName = "Value";

            this._StatisticsDataGridView.Columns.Add("extra", "Extra");
            this._StatisticsDataGridView.Columns[2].ReadOnly = true;
            this._StatisticsDataGridView.Columns[2].Width = 200;
            this._StatisticsDataGridView.Columns[2].DataPropertyName = "Extra";

            this._StatisticsDataGridView.DataSource = new BindingSource
            {
                DataSource = this._Statistics,
            };

            this._GameId = gameId;
            this._SteamClient = client;

            this._IconDownloader.DownloadDataCompleted += this.OnIconDownload;

            string name = this._SteamClient.SteamApps001.GetAppData((uint)this._GameId, "name");
            if (name != null)
            {
                base.Text += " | " + name;
            }
            else
            {
                base.Text += " | " + this._GameId.ToString(CultureInfo.InvariantCulture);
            }

            this._UserStatsReceivedCallback = client.CreateAndRegisterCallback<API.Callbacks.UserStatsReceived>();
            this._UserStatsReceivedCallback.OnRun += this.OnUserStatsReceived;

            //this.UserStatsStoredCallback = new API.Callback(1102, new API.Callback.CallbackFunction(this.OnUserStatsStored));
            this.RefreshStats();
        }

        private void AddAchievementIcon(Stats.AchievementInfo info, System.Drawing.Image icon)
        {
            if (icon == null)
            {
                info.ImageIndex = 0;
            }
            else
            {
                info.ImageIndex = this._AchievementImageList.Images.Count;
                this._AchievementImageList.Images.Add(info.IsAchieved == true ? info.IconNormal : info.IconLocked, icon);
            }
        }

        private void OnIconDownload(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error == null && e.Cancelled == false)
            {
                var info = e.UserState as Stats.AchievementInfo;

                Bitmap bitmap;

                try
                {
                    using (var stream = new MemoryStream())
                    {
                        stream.Write(e.Result, 0, e.Result.Length);
                        bitmap = new Bitmap(stream);
                    }
                }
                catch (Exception)
                {
                    bitmap = null;
                }

                this.AddAchievementIcon(info, bitmap);
                this._AchievementListView.Update();
            }

            this.DownloadNextIcon();
        }

        private void DownloadNextIcon()
        {
            if (this._IconQueue.Count == 0)
            {
                this._DownloadStatusLabel.Visible = false;
                return;
            }

            if (this._IconDownloader.IsBusy == true)
            {
                return;
            }

            this._DownloadStatusLabel.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Downloading {0} icons...",
                this._IconQueue.Count);
            this._DownloadStatusLabel.Visible = true;

            var info = this._IconQueue[0];
            this._IconQueue.RemoveAt(0);


            this._IconDownloader.DownloadDataAsync(
                new Uri(string.Format(
                    CultureInfo.InvariantCulture,
                    "http://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{0}/{1}",
                    this._GameId,
                    info.IsAchieved == true ? info.IconNormal : info.IconLocked)),
                info);
        }

        private static string TranslateError(int id)
        {
            switch (id)
            {
                case 2:
                    {
                        return "generic error -- this usually means you don't own the game";
                    }
            }

            return id.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetLocalizedString(KeyValue kv, string language, string defaultValue)
        {
            var name = kv[language].AsString("");
            if (string.IsNullOrEmpty(name) == false)
            {
                return name;
            }

            if (language != "english")
            {
                name = kv["english"].AsString("");
                if (string.IsNullOrEmpty(name) == false)
                {
                    return name;
                }
            }

            name = kv.AsString("");
            if (string.IsNullOrEmpty(name) == false)
            {
                return name;
            }

            return defaultValue;
        }

        private bool LoadUserGameStatsSchema()
        {
            List<Achievement> achievementPercentStats = Task.Run(() => GetGlobalStats()).Result;

            string path;

            try
            {
                path = API.Steam.GetInstallPath();
                path = Path.Combine(path, "appcache");
                path = Path.Combine(path, "stats");
                path = Path.Combine(path, string.Format(
                    CultureInfo.InvariantCulture,
                    "UserGameStatsSchema_{0}.bin",
                    this._GameId));

                if (File.Exists(path) == false)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            var kv = KeyValue.LoadAsBinary(path);

            if (kv == null)
            {
                return false;
            }

            var currentLanguage = this._SteamClient.SteamApps008.GetCurrentGameLanguage();

            this._AchievementDefinitions.Clear();
            this._StatDefinitions.Clear();

            var stats = kv[this._GameId.ToString(CultureInfo.InvariantCulture)]["stats"];
            if (stats.Valid == false ||
                stats.Children == null)
            {
                return false;
            }

            foreach (var stat in stats.Children)
            {
                if (stat.Valid == false)
                {
                    continue;
                }

                var rawType = stat["type_int"].Valid
                                  ? stat["type_int"].AsInteger(0)
                                  : stat["type"].AsInteger(0);
                var type = (APITypes.UserStatType)rawType;
                switch (type)
                {
                    case APITypes.UserStatType.Invalid:
                        {
                            break;
                        }

                    case APITypes.UserStatType.Integer:
                        {
                            var id = stat["name"].AsString("");
                            string name = GetLocalizedString(stat["display"]["name"], currentLanguage, id);

                            this._StatDefinitions.Add(new Stats.IntegerStatDefinition()
                            {
                                Id = stat["name"].AsString(""),
                                DisplayName = name,
                                MinValue = stat["min"].AsInteger(int.MinValue),
                                MaxValue = stat["max"].AsInteger(int.MaxValue),
                                MaxChange = stat["maxchange"].AsInteger(0),
                                IncrementOnly = stat["incrementonly"].AsBoolean(false),
                                DefaultValue = stat["default"].AsInteger(0),
                                Permission = stat["permission"].AsInteger(0),
                            });
                            break;
                        }

                    case APITypes.UserStatType.Float:
                    case APITypes.UserStatType.AverageRate:
                        {
                            var id = stat["name"].AsString("");
                            string name = GetLocalizedString(stat["display"]["name"], currentLanguage, id);

                            this._StatDefinitions.Add(new Stats.FloatStatDefinition()
                            {
                                Id = stat["name"].AsString(""),
                                DisplayName = name,
                                MinValue = stat["min"].AsFloat(float.MinValue),
                                MaxValue = stat["max"].AsFloat(float.MaxValue),
                                MaxChange = stat["maxchange"].AsFloat(0.0f),
                                IncrementOnly = stat["incrementonly"].AsBoolean(false),
                                DefaultValue = stat["default"].AsFloat(0.0f),
                                Permission = stat["permission"].AsInteger(0),
                            });
                            break;
                        }

                    case APITypes.UserStatType.Achievements:
                    case APITypes.UserStatType.GroupAchievements:
                        {
                            if (stat.Children != null)
                            {
                                foreach (var bits in stat.Children.Where(
                                    b => string.Compare(b.Name, "bits", StringComparison.InvariantCultureIgnoreCase) == 0))
                                {
                                    if (bits.Valid == false ||
                                        bits.Children == null)
                                    {
                                        continue;
                                    }

                                    foreach (var bit in bits.Children)
                                    {
                                        string id = bit["name"].AsString("");
                                        string name = GetLocalizedString(bit["display"]["name"], currentLanguage, id);
                                        string desc = GetLocalizedString(bit["display"]["desc"], currentLanguage, "");
                                        //Add priority

                                        this._AchievementDefinitions.Add(new Stats.AchievementDefinition()
                                        {
                                            Id = id,
                                            Name = name,
                                            Description = desc,
                                            IconNormal = bit["display"]["icon"].AsString(""),
                                            IconLocked = bit["display"]["icon_gray"].AsString(""),
                                            IsHidden = bit["display"]["hidden"].AsBoolean(false),
                                            Permission = bit["permission"].AsInteger(0),
                                            Percent = achievementPercentStats.First(achiev => string.Equals(achiev.Name, id, StringComparison.InvariantCultureIgnoreCase)).Percent
                                        });
                                    }
                                }
                            }

                            break;
                        }

                    default:
                        {
                            throw new InvalidOperationException("invalid stat type");
                        }
                }
            }

            return true;
        }

        async private Task<List<Achievement>> GetGlobalStats()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(string.Format("https://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/?gameid={0}&format=json", _GameId.ToString()));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            Root root = JsonConvert.DeserializeObject<Root>(responseBody);

            return root.Achievementpercentages.Achievements
                .OrderByDescending(x => x.Percent)
                .ToList();
        }

        private void OnUserStatsReceived(APITypes.UserStatsReceived param)
        {
            if (param.Result != 1)
            {
                this._GameStatusLabel.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    "Error while retrieving stats: {0}",
                    TranslateError(param.Result));
                this.EnableInput();
                return;
            }

            if (this.LoadUserGameStatsSchema() == false)
            {
                this._GameStatusLabel.Text = "Failed to load schema.";
                this.EnableInput();
                return;
            }

            try
            {
                this.GetAchievements();
                this.GetStatistics();
            }
            catch (Exception e)
            {
                this._GameStatusLabel.Text = "Error when handling stats retrieval.";
                this.EnableInput();
                MessageBox.Show(
                    "Error when handling stats retrieval:\n" + e,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            this._GameStatusLabel.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Retrieved {0} achievements and {1} statistics.",
                this._AchievementListView.Items.Count,
                this._StatisticsDataGridView.Rows.Count);
            this.EnableInput();
        }

        private void RefreshStats()
        {
            this._AchievementListView.Items.Clear();
            this._StatisticsDataGridView.Rows.Clear();

            if (this._SteamClient.SteamUserStats.RequestCurrentStats() == false)
            {
                MessageBox.Show(this, "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this._GameStatusLabel.Text = "Retrieving stat information...";
            this.DisableInput();
        }

        private bool _IsUpdatingAchievementList;

        private void GetAchievements()
        {
            this._IsUpdatingAchievementList = true;

            this._AchievementListView.Items.Clear();
            this._AchievementListView.BeginUpdate();
            //this.Achievements.Clear();

            foreach (var def in this._AchievementDefinitions.OrderByDescending(x => x.Percent))
            {
                if (string.IsNullOrEmpty(def.Id) == true)
                {
                    continue;
                }

                if (this._SteamClient.SteamUserStats.GetAchievementState(def.Id, out bool isAchieved) == false)
                {
                    continue;
                }

                var info = new Stats.AchievementInfo()
                {
                    Id = def.Id,
                    IsAchieved = isAchieved,
                    IconNormal = string.IsNullOrEmpty(def.IconNormal) ? null : def.IconNormal,
                    IconLocked = string.IsNullOrEmpty(def.IconLocked) ? def.IconNormal : def.IconLocked,
                    Permission = def.Permission,
                    Name = def.Name,
                    Description = def.Description + " (" + Math.Round(def.Percent, 2) + "%)",
                    Percent = def.Percent
                };

                var item = new OrderableListViewItem()
                {
                    Checked = isAchieved,
                    Tag = info,
                    Text = info.Name,
                    BackColor = (def.Permission & 3) == 0 ? Color.Black : Color.FromArgb(64, 0, 0),
                };

                info.Item = item;

                if (item.Text.StartsWith("#", StringComparison.InvariantCulture) == true)
                {
                    item.Text = info.Id;
                }
                else
                {
                    item.SubItems.Add(info.Description);
                }

                info.ImageIndex = 0;

                this.AddAchievementToIconQueue(info, false);
                this._AchievementListView.Items.Add(item);
                //this.Achievements.Add(info.Id, info);
            }


            _AchievementListView.ListViewItemSorter = new ListViewItemPercentageComparer();
            this._AchievementListView.EndUpdate();
            this._IsUpdatingAchievementList = false;


            this.achievementsNumber_lbl.Text = $"{_AchievementListView.Items.Cast<ListViewItem>().Where(x => x.Checked).Count()}/{_AchievementListView.Items.Count} Done";

            this.DownloadNextIcon();
        }

        private void GetStatistics()
        {
            this._Statistics.Clear();
            foreach (var rdef in this._StatDefinitions)
            {
                if (string.IsNullOrEmpty(rdef.Id) == true)
                {
                    continue;
                }

                if (rdef is Stats.IntegerStatDefinition idef)
                {
                    if (this._SteamClient.SteamUserStats.GetStatValue(idef.Id, out int value))
                    {
                        this._Statistics.Add(new Stats.IntStatInfo()
                        {
                            Id = idef.Id,
                            DisplayName = idef.DisplayName,
                            IntValue = value,
                            OriginalValue = value,
                            IsIncrementOnly = idef.IncrementOnly,
                            Permission = idef.Permission,
                        });
                    }
                }
                else if (rdef is Stats.FloatStatDefinition fdef)
                {
                    if (this._SteamClient.SteamUserStats.GetStatValue(fdef.Id, out float value))
                    {
                        this._Statistics.Add(new Stats.FloatStatInfo()
                        {
                            Id = fdef.Id,
                            DisplayName = fdef.DisplayName,
                            FloatValue = value,
                            OriginalValue = value,
                            IsIncrementOnly = fdef.IncrementOnly,
                            Permission = fdef.Permission,
                        });
                    }
                }
            }
        }

        private void AddAchievementToIconQueue(Stats.AchievementInfo info, bool startDownload)
        {
            int imageIndex = this._AchievementImageList.Images.IndexOfKey(
                info.IsAchieved == true ? info.IconNormal : info.IconLocked);

            if (imageIndex >= 0)
            {
                info.ImageIndex = imageIndex;
            }
            else
            {
                this._IconQueue.Add(info);

                if (startDownload == true)
                {
                    this.DownloadNextIcon();
                }
            }
        }

        private int StoreAchievements()
        {
            if (this._AchievementListView.Items.Count == 0)
            {
                return 0;
            }

            var achievements = new List<Stats.AchievementInfo>();
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                if (item.Tag is Stats.AchievementInfo achievementInfo &&
                    achievementInfo.IsAchieved != item.Checked)
                {
                    achievementInfo.IsAchieved = item.Checked;
                    achievements.Add(item.Tag as Stats.AchievementInfo);
                }
            }

            if (achievements.Count == 0)
            {
                return 0;
            }

            foreach (Stats.AchievementInfo info in achievements)
            {
                if (this._SteamClient.SteamUserStats.SetAchievement(info.Id, info.IsAchieved) == false)
                {
                    MessageBox.Show(
                        this,
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "An error occurred while setting the state for {0}, aborting store.",
                            info.Id),
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return -1;
                }
            }

            return achievements.Count;
        }

        private int StoreStatistics()
        {
            if (this._Statistics.Count == 0)
            {
                return 0;
            }

            var statistics = this._Statistics.Where(stat => stat.IsModified == true).ToList();
            if (statistics.Count == 0)
            {
                return 0;
            }

            foreach (Stats.StatInfo stat in statistics)
            {
                if (stat is Stats.IntStatInfo intStat)
                {
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        intStat.Id,
                        intStat.IntValue) == false)
                    {
                        MessageBox.Show(
                            this,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "An error occurred while setting the value for {0}, aborting store.",
                                stat.Id),
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return -1;
                    }
                }
                else if (stat is Stats.FloatStatInfo floatStat)
                {
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        floatStat.Id,
                        floatStat.FloatValue) == false)
                    {
                        MessageBox.Show(
                            this,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "An error occurred while setting the value for {0}, aborting store.",
                                stat.Id),
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return -1;
                    }
                }
                else
                {
                    throw new InvalidOperationException("unsupported stat type");
                }
            }

            return statistics.Count;
        }

        private void DisableInput()
        {
            this._ReloadButton.Enabled = false;
            this._StoreButton.Enabled = false;
        }

        private void EnableInput()
        {
            this._ReloadButton.Enabled = true;
            this._StoreButton.Enabled = true;
        }

        private void OnTimer(object sender, EventArgs e)
        {
            this._CallbackTimer.Enabled = false;
            this._SteamClient.RunCallbacks(false);
            this._CallbackTimer.Enabled = true;
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            this.RefreshStats();
        }

        private void OnLockAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = false;
            }
        }

        private void OnInvertAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = !item.Checked;
            }
        }

        private void OnUnlockAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = true;
            }
        }

        private bool Store()
        {
            if (this._SteamClient.SteamUserStats.StoreStats() == false)
            {
                MessageBox.Show(
                    this,
                    "An error occurred while storing, aborting.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void StoreStatsAndAchievs()
        {
            OnStore(null, null, false);
        }

        private void OnStore(object sender, EventArgs e)
        {
            OnStore(null, null, true);
        }
        private void OnStore(object sender, EventArgs e, bool showNotification)
        {
            int achievements = this.StoreAchievements();
            if (achievements < 0)
            {
                this.RefreshStats();
                return;
            }

            int stats = this.StoreStatistics();
            if (stats < 0)
            {
                this.RefreshStats();
                return;
            }

            if (this.Store() == false)
            {
                this.RefreshStats();
                return;
            }

            if (showNotification)
            {
                MessageBox.Show(
                this,
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Stored {0} achievements and {1} statistics.",
                    achievements,
                    stats),
                "Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            }

            this.RefreshStats();
        }

        private void OnStatDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Context == DataGridViewDataErrorContexts.Commit)
            {
                var view = (DataGridView)sender;
                if (e.Exception is Stats.StatIsProtectedException)
                {
                    e.ThrowException = false;
                    e.Cancel = true;
                    view.Rows[e.RowIndex].ErrorText = "Stat is protected! -- you can't modify it";
                }
                else
                {
                    e.ThrowException = false;
                    e.Cancel = true;
                    view.Rows[e.RowIndex].ErrorText = "Invalid value";
                }
            }
        }

        private void OnStatAgreementChecked(object sender, EventArgs e)
        {
            this._StatisticsDataGridView.Columns[1].ReadOnly = this._EnableStatsEditingCheckBox.Checked == false;
        }

        private void OnStatCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var view = (DataGridView)sender;
            view.Rows[e.RowIndex].ErrorText = "";
        }

        private void OnResetAllStats(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Are you absolutely sure you want to reset stats?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.No)
            {
                return;
            }

            bool achievementsToo = DialogResult.Yes == MessageBox.Show(
                "Do you want to reset achievements too?",
                "Question",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (MessageBox.Show(
                "Really really sure?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error) == DialogResult.No)
            {
                return;
            }

            if (this._SteamClient.SteamUserStats.ResetAllStats(achievementsToo) == false)
            {
                MessageBox.Show(this, "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.RefreshStats();
        }

        private void OnCheckAchievement(object sender, ItemCheckEventArgs e)
        {
            if (sender != this._AchievementListView)
            {
                return;
            }

            if (this._IsUpdatingAchievementList == true)
            {
                return;
            }

            if (!(this._AchievementListView.Items[e.Index].Tag is Stats.AchievementInfo info))
            {
                return;
            }

            if ((info.Permission & 3) != 0)
            {
                MessageBox.Show(
                    this,
                    "Sorry, but this is a protected achievement and cannot be managed with Steam Achievement Manager.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                e.NewValue = e.CurrentValue;
            }
        }

        private void UnlockLegitButton_Click(object sender, EventArgs e)
        {
            IEnumerable<ListViewItem> lv = _AchievementListView.Items.Cast<ListViewItem>();
            int numberOfAchievementsToGet = lv.Where(x => !x.Checked).Count();
            int numberOfAchievementsObtained = lv.Where(x => x.Checked).Count();
            int totalNumberOfAchievements = lv.Count();

            using (NumericPrompt form = new NumericPrompt(_SteamClient.SteamApps001.GetAppData((uint)this._GameId, "name"), numberOfAchievementsObtained, totalNumberOfAchievements))
            {
                var result = form.ShowDialog();

                if (result == DialogResult.OK && numberOfAchievementsToGet > 0)
                {
                    TimeForAchievements = TimeSpan.FromMinutes(form.NumberOfMinutes ?? 60);
                    TimeForAchievement = new TimeSpan(TimeForAchievements.Ticks / numberOfAchievementsToGet);

                    backgroundWorker.Interval = (int)TimeForAchievement.TotalMilliseconds;

                    SetProgressLegit(numberOfAchievementsObtained, totalNumberOfAchievements);

                    backgroundWorker.Start();
                    secondsCounter.Start();
                }
            }
        }

        private void SetProgressLegit(int numberOfAchievementsObtained, int totalNumberOfAchievements)
        {
            _UnlockLegitButton.Enabled = false;
            _StopLegitButton.Enabled = true;

            Counter = (int)TimeForAchievement.TotalSeconds;
            countdown_lbl.Text = Counter.ToString() + "s (" + TimeForAchievements.Hours + "h " + TimeForAchievements.Minutes + "m)";

            progressBar.Visible = true;
            progressBar.Maximum = totalNumberOfAchievements;
            progressBar.Value = numberOfAchievementsObtained;
            progressBar.Step = 1;

            _GameStatusLabel.Visible = false;
            progressBar.Show();
            countdown_lbl.Show();
        }

        private void BackgroundWorker_tick(object sender, EventArgs e)
        {
            IEnumerable<ListViewItem> lv = _AchievementListView.Items.Cast<ListViewItem>();

            ListViewItem achievement = lv
                .Where(x => !x.Checked)
                .FirstOrDefault();

            if (achievement != null)
            {
                achievement.Checked = true;
                TimeForAchievements = TimeForAchievements.Subtract(TimeSpan.FromMilliseconds(backgroundWorker.Interval));
                IncreaseInterval();
                StoreStatsAndAchievs();
                Counter = (int)TimeForAchievement.TotalSeconds;

                progressBar.PerformStep();
                progressBar.Update();
            }
            else
            {
                backgroundWorker.Stop();
                secondsCounter.Stop();

                HideProgressLegit();
                MessageBox.Show($"All achievements completed legitemately for {this.Text}");
                this.Activate();
            }

        }

        private void IncreaseInterval()
        {
            long oldIntervalTicks = TimeSpan.FromMilliseconds(backgroundWorker.Interval).Ticks;
            long newIntervalTicks = GetRandomLong(oldIntervalTicks, (long)Math.Round(oldIntervalTicks * 1.2));

            TimeForAchievement = TimeSpan.FromTicks(newIntervalTicks);
            TimeForAchievements = TimeForAchievements.Add(TimeForAchievement.Subtract(TimeSpan.FromTicks(oldIntervalTicks))); //account for 'achievement difficulty' change

            backgroundWorker.Interval = (int)TimeForAchievement.TotalMilliseconds;

            long GetRandomLong(long min, long max)
            {
                byte[] buf = new byte[8];
                new Random().NextBytes(buf);
                long longRand = BitConverter.ToInt64(buf, 0);

                return (Math.Abs(longRand % (max - min)) + min);
            }
        }

        private void SecondsCounter_Tick(object sender, EventArgs e)
        {
            if (Counter > 0) Counter--;
            countdown_lbl.Text = Counter.ToString() + "s (" + TimeForAchievements.Hours + "h " + TimeForAchievements.Minutes + "m)";
        }

        private void StopLegitButton_Click(object sender, EventArgs e)
        {
            backgroundWorker.Stop();
            secondsCounter.Stop();

            HideProgressLegit();
        }

        private void HideProgressLegit()
        {
            _StopLegitButton.Enabled = false;
            _UnlockLegitButton.Enabled = true;

            progressBar.Hide();
            countdown_lbl.Hide();

            _GameStatusLabel.Visible = true;
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            _AchievementListView.ListViewItemSorter = new ListViewSortComparer();
            _AchievementListView.BeginUpdate();

            try
            {
                foreach (OrderableListViewItem item in _AchievementListView.Items)
                {
                    item.Found = 0;
                    item.Selected = false;
                }

                string searchText = searchTextBox.Text.Trim();

                if (searchText.Length == 0)
                {
                    ResetListFilters();
                    return;
                }

                _AchievementListView.Items
                    .Cast<OrderableListViewItem>()
                    .Where(item =>
                        ((AchievementInfo)item.Tag)?.Name?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         ((AchievementInfo)item.Tag)?.Description?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ForEach(fi => fi.Found = 1);

                ListViewItem foundItem = _AchievementListView.Items.Cast<OrderableListViewItem>().Where(x=>x.Found ==1).FirstOrDefault();

                if (foundItem != null)
                {
                    _AchievementListView.TopItem = foundItem;
                    _SelectedFilteredItem = foundItem.Index;
                    foundItem.Selected = true;
                }
                else
                {
                    ResetListFilters();
                }
            }
            finally
            {
                _AchievementListView.EndUpdate();
            }

        }

        private void ResetListFilters()
        {
            _AchievementListView.TopItem = _AchievementListView.Items[0];
            _AchievementListView.SelectedItems.Clear();
            _AchievementListView.ListViewItemSorter = new ListViewItemPercentageComparer();
        }

        private void searchTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (!searchTextBox.AcceptsReturn)
                {
                    nextSearchResult_btn.PerformClick();
                }
            }
        }


        private int _SelectedFilteredItem;

        private void nextSearchResult_btn_Click(object sender, EventArgs e)
        {
            _AchievementListView.BeginUpdate();

            IEnumerable<OrderableListViewItem> filteredItems = _AchievementListView.Items.Cast<OrderableListViewItem>().Where(x => x.Found == 1);

            if (filteredItems.Any())
            {
                _AchievementListView.SelectedItems.Clear();

                if (filteredItems.Last().Index == _SelectedFilteredItem)
                {
                    int indexOfFirstFoundItem = filteredItems.First().Index;
                    _AchievementListView.Items[indexOfFirstFoundItem].Selected = true;
                    _SelectedFilteredItem = indexOfFirstFoundItem;
                }
                else
                {
                    _AchievementListView.Items[_SelectedFilteredItem + 1].Selected = true;
                    _SelectedFilteredItem += 1;
                }

                _AchievementListView.TopItem = _AchievementListView.SelectedItems[0];
            }

            _AchievementListView.EndUpdate();
        }
    }
}
