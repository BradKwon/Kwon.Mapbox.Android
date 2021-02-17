
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Widget;
using AndroidX.Annotations;
using Mapbox.Navigation.Navigator.Internal;
using TimberLog;

namespace MapboxNavigation.UI.Droid.TestApp
{
#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.
    [Activity(Label = "NavigationSettingsActivity")]
    public class NavigationSettingsActivity : PreferenceActivity,
        ISharedPreferencesOnSharedPreferenceChangeListener
    {
        public static readonly string UNIT_TYPE_CHANGED = "unit_type_changed";
        public static readonly string LANGUAGE_CHANGED = "language_changed";
        public static readonly string OFFLINE_CHANGED = "offline_changed";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            PreferenceManager
                .GetDefaultSharedPreferences(this)
                .RegisterOnSharedPreferenceChangeListener(this);

            FragmentManager.BeginTransaction()
                .Replace(Android.Resource.Id.Content,
                    new NavigationViewPreferenceFragment())
                .Commit();
        }

        public void OnSharedPreferenceChanged(
            ISharedPreferences sharedPreferences,
            string key)
        {
            Intent resultIntent = new Intent();
            resultIntent.PutExtra(UNIT_TYPE_CHANGED,
                key.Equals(GetString(Resource.String.unit_type_key)));
            resultIntent.PutExtra(LANGUAGE_CHANGED,
                key.Equals(GetString(Resource.String.language_key)));
            resultIntent.PutExtra(OFFLINE_CHANGED,
                key.Equals(GetString(Resource.String.offline_version_key)));
            SetResult(Result.Ok, resultIntent);
        }
    }
#pragma warning restore CS0618 // 형식 또는 멤버는 사용되지 않습니다.

#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.
    public class NavigationViewPreferenceFragment : PreferenceFragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AddPreferencesFromResource(Resource.Xml.fragment_navigation_preferences);

            string gitHashTitle = $"Last Commit Hash: {GetHashCode()}";
            FindPreference(GetString(Resource.String.git_hash_key))
                .Title = gitHashTitle;

            FindPreference(GetString(Resource.String.nav_native_history_retrieve_key))
                .PreferenceChange += (s, e) =>
                {
                    string history = MapboxNativeNavigatorImpl.Instance.History;
                    var path = Environment.GetExternalStoragePublicDirectory("navigation_debug");
                    if (!path.Exists())
                    {
                        path.Mkdirs();
                    }

                    var file = Path.Combine(path.Path,
                        $"history_{System.DateTime.Now.Millisecond}.json");

                    try
                    {
                        using var streamWriter = new StreamWriter(file, true);
                        streamWriter.WriteLine(history);
                        Toast.MakeText(Activity, $"Saved to {file}", ToastLength.Long)
                            .Show();
                        Timber.I($"History file saved to {file}");
                    }
                    catch (System.Exception ex)
                    {
                        Timber.E($"History file write failed: {ex.Message}");
                    }

                    e.Handled = true;
                };
        }

        public override void OnResume()
        {
            base.OnResume();

            GetOfflineVersions();
            PreferenceManager.SetDefaultValues(Activity,
                Resource.Xml.fragment_navigation_preferences, false);
        }

        private void GetOfflineVersions()
        {
            var file = new Java.IO.File(Context.GetExternalFilesDir("Offline"), "tiles");
            if (!file.Exists())
            {
                file.Mkdirs();
            }

            ListPreference offlineVersions = (ListPreference)FindPreference(
                GetString(Resource.String.offline_version_key));
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
        private List<string> BuildFileList(Java.IO.File file)
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
#pragma warning restore CS0618 // 형식 또는 멤버는 사용되지 않습니다.
}
