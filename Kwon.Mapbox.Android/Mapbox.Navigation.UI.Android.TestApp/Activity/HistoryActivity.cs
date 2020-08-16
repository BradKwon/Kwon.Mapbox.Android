
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Lang;
using Java.Text;
using Java.Util;
using Mapbox.Services.Android.Navigation.V5.Routeprogress;

namespace MapboxNavigation.UI.Droid.TestApp.Activity
{
    [Activity(Label = "HistoryActivity")]
    public class HistoryActivity : AppCompatActivity
    {
        private static SimpleDateFormat DATE_FORMAT = new SimpleDateFormat("yyyy_MM_dd_HH_mm_ss");
        private static string JSON_EXTENSION = ".json";

        private Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation navigation;
        private string filename;

        public void AddNavigationForHistory(Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation navigation)
        {
            if (navigation == null)
            {
                throw new IllegalArgumentException("MapboxNavigation cannot be null");
            }
            this.navigation = navigation;
            navigation.AddProgressChangeListener(new MyProgressChangeListener((location, routeProgress) => ExecuteStoreHistoryTask()));
            navigation.ToggleHistory(true);
            filename = BuildFileName();
        }

        protected void ExecuteStoreHistoryTask()
        {
            new StoreHistoryTask(navigation, filename).Execute();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            navigation.ToggleHistory(false);
        }

        private string BuildFileName()
        {
            Java.Lang.StringBuilder sb = new Java.Lang.StringBuilder();
            sb.Append(ObtainCurrentTimeStamp());
            sb.Append(JSON_EXTENSION);
            return sb.ToString();
        }

        private string ObtainCurrentTimeStamp()
        {
            Date now = new Date();
            string strDate = DATE_FORMAT.Format(now);
            return strDate;
        }

        private class MyProgressChangeListener : Java.Lang.Object, IProgressChangeListener
        {
            Action<Location, RouteProgress> progressChangeAction;

            public MyProgressChangeListener(Action<Location, RouteProgress> action)
            {
                progressChangeAction = action;
            }

            public void OnProgressChange(Location p0, RouteProgress p1)
            {
                progressChangeAction.Invoke(p0, p1);
            }
        }
    }
}
