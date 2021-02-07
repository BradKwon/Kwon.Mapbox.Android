
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Annotations;
using AndroidX.Fragment.App;
using AndroidX.Preference;
using Java.IO;

namespace MapboxNavigation.UI.Droid.TestApp
{
    [Activity(Label = "NavigationSettingsActivity")]
    public class NavigationSettingsActivity : FragmentActivity, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        public static readonly string UNIT_TYPE_CHANGED = "unit_type_changed";
        public static readonly string LANGUAGE_CHANGED = "language_changed";
        public static readonly string OFFLINE_CHANGED = "offline_changed";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            PreferenceManager.GetDefaultSharedPreferences(this).RegisterOnSharedPreferenceChangeListener(this);
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.content, new NavigationViewPreferenceFragment()).Commit();
        }

        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            Intent resultIntent = new Intent();
            resultIntent.PutExtra(UNIT_TYPE_CHANGED, key.Equals(GetString(Resource.String.unit_type_key)));
            resultIntent.PutExtra(LANGUAGE_CHANGED, key.Equals(GetString(Resource.String.language_key)));
            resultIntent.PutExtra(OFFLINE_CHANGED, key.Equals(GetString(Resource.String.offline_version_key)));
            SetResult(Result.Ok, resultIntent);
        }
    }

    public class NavigationViewPreferenceFragment : PreferenceFragmentCompat
    {
        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            AddPreferencesFromResource(Resource.Xml.fragment_navigation_preferences);

            string gitHashTitle = string.Format("Last Commit Hash: %s", GetHashCode());
            FindPreference(GetString(Resource.String.git_hash_key)).Title = gitHashTitle;
        }

        public override void OnResume()
        {
            base.OnResume();

            GetOfflineVersions();
            PreferenceManager.SetDefaultValues(Activity, Resource.Xml.fragment_navigation_preferences, false);
        }

        private void GetOfflineVersions()
        {
            File file = new File(Context.GetExternalFilesDir("Offline"), "tiles");
            if (!file.Exists())
            {
                file.Mkdirs();
            }

            ListPreference offlineVersions = (ListPreference)FindPreference(GetString(Resource.String.offline_version_key));
            List<string> list = BuildFileList(file);
            if (list.Any())
            {
                string[] entries = list.ToArray();
                offlineVersions.SetEntries(entries);
                offlineVersions.SetEntryValues(entries);
                offlineVersions.Enabled = true;
            }
            else
            {
                offlineVersions.Enabled = false;
            }
        }

        [NonNull]
        private List<string> BuildFileList(File file)
        {
            List<string> list;
            if (file.List() != null && file.List().Length != 0)
            {
                list = file.List().ToList();
            }
            else
            {
                list = new List<string>();
            }
            return list;
        }
    }
}
