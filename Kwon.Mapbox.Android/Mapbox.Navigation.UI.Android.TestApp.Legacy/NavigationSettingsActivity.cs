
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MapboxNavigation.UI.Droid.TestApp.Legacy
{
    [Activity(Label = "NavigationSettingsActivity")]
    [Obsolete]
    public class NavigationSettingsActivity : PreferenceActivity
    {
        public static readonly string UNIT_TYPE_CHANGED = "unit_type_changed";
        public static readonly string LANGUAGE_CHANGED = "language_changed";
        public static readonly string OFFLINE_CHANGED = "offline_changed";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


        }
    }
}
