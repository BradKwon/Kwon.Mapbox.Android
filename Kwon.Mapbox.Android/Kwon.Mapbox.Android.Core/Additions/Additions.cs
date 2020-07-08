
namespace Mapbox.Android.Core
{
    public sealed partial class FileUtils
    {
        public sealed partial class LastModifiedComparator
        {
            public unsafe int Compare(Java.Lang.Object o1, Java.Lang.Object o2)
            {
                return Compare(o1 as Java.IO.File, o2 as Java.IO.File);
            }
        }
    }
}