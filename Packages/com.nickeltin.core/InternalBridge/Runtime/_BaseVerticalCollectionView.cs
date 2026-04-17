using UnityEngine.UIElements;

namespace nickeltin.InternalBridge.Runtime
{
    public static class _BaseVerticalCollectionView
    {
        public static ScrollView GetScrollView(this BaseVerticalCollectionView instance)
        {
            return instance.scrollView;
        }
    }
}