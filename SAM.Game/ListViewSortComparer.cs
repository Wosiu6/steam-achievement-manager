using System.Collections;

namespace SAM.Game
{
    class ListViewSortComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            return ((OrderableListViewItem)y).Found.CompareTo(((OrderableListViewItem)x).Found);
        }
    }
}
