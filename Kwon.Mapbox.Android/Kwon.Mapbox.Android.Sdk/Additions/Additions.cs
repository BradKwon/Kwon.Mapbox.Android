namespace Mapbox.Mapboxsdk.Utils
{
    public partial class FileUtils
    {
        public partial class CheckFileReadPermissionTask
        {
            protected override unsafe global::Java.Lang.Object DoInBackground(params Java.Lang.Object[] files)
            {
                return DoInBackground(files as global::Java.IO.File[]);
            }
        }

        public partial class CheckFileWritePermissionTask
        {
            protected override unsafe global::Java.Lang.Object DoInBackground(params global::Java.Lang.Object[] files)
            {
                return DoInBackground(files as global::Java.IO.File[]);
            }
        }
    }
}
