using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.CardView.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using CheeseBind;
using Google.Android.Material.FloatingActionButton;
using Mapbox.Android.Core.Permissions;

namespace MapboxNavigation.UI.Droid.TestApp
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IPermissionsListener
    {
        private PermissionsManager permissionsManager;
        const int CHANGE_SETTING_REQUEST_CODE = 1001;

        [BindView(Resource.Id.settingsFab)]
        FloatingActionButton settingsFab;

        [BindView(Resource.Id.cardCore)]
        CardView cardCore;

        [BindView(Resource.Id.cardUI)]
        CardView cardUI;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // Check for location permission
            permissionsManager = new PermissionsManager(this);
            if (!PermissionsManager.AreLocationPermissionsGranted(this))
            {
                permissionsManager.RequestLocationPermissions(this);
            }
            else
            {
                RequestPermissionsIfNotGranted(Android.Manifest.Permission.WriteExternalStorage);
            }

            settingsFab.Click += (s, e) => StartActivityForResult(new Intent(this, typeof(NavigationSettingsActivity)), CHANGE_SETTING_REQUEST_CODE);
            //cardCore.Click += (s, e) => StartActivity(new Intent(this, typeof(CoreActivity)));
            //cardUI.Click += (s, e) => StartActivity(new Intent(this, typeof(UIActivity)));
        }

        public override void OnRequestPermissionsResult(int requestCode,
            string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == 0)
            {
                List<int> grants = new List<int>();
                foreach (Permission grant in grantResults)
                {
                    grants.Add((int)grant);
                }

                permissionsManager.OnRequestPermissionsResult(requestCode,
                                                              permissions,
                                                              grants.ToArray());
            }
            else
            {
                bool granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
                if (!granted)
                {
                    cardCore.Clickable = false;
                    Toast.MakeText(this, "You didn't grant storage permissions.", ToastLength.Long).Show();
                }
                else
                {
                    cardCore.Clickable = true;
                }
            }
        }

        public void OnExplanationNeeded(IList<string> p0)
        {
            Toast.MakeText(this,
                "This app needs location and storage permissions in order to show its functionality.",
                ToastLength.Long).Show();
        }

        public void OnPermissionResult(bool p0)
        {
            if (p0)
            {
                RequestPermissionsIfNotGranted(Android.Manifest.Permission.WriteExternalStorage);
            }
            else
            {
                Toast.MakeText(this, "You didn't grant location permissions.", ToastLength.Long).Show();
            }
        }

        private void RequestPermissionsIfNotGranted(string permission)
        {
            var permissionsNeeded = new List<string>();
            if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
            {
                permissionsNeeded.Add(permission);
                ActivityCompat.RequestPermissions(this, permissionsNeeded.ToArray(), 10);
            }
        }
    }
}
