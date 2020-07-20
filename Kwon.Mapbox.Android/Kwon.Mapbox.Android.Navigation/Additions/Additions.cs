namespace Mapbox.Services.Android.Navigation.V5.Utils
{
    public partial class DownloadTask
    {
        protected override unsafe global::Java.Lang.Object DoInBackground(params global::Java.Lang.Object[] responseBodies)
        {
            return DoInBackground(responseBodies as global::Square.OkHttp3.ResponseBody[]);
        }
    }
}
