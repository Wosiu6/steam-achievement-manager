using SAM.Game.Stats;
using System.Collections;
using System.Windows.Forms;

namespace SAM.Game
{
    class ListViewItemComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            return ((AchievementInfo)((ListViewItem)y).Tag).Percent.CompareTo(((AchievementInfo)((ListViewItem)x).Tag).Percent);
        }
    }
}
