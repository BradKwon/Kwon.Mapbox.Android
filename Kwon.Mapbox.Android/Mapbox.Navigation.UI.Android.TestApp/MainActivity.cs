using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using Mapbox.Android.Core.Permissions;
using MapboxNavigation.UI.Droid.TestApp.Activity.NavigationUI;

namespace MapboxNavigation.UI.Droid.TestApp
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IPermissionsListener
    {
        private RecyclerView recyclerView;
        private PermissionsManager permissionsManager;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            List<SampleItem> sampleItems = new List<SampleItem>
            {
                new SampleItem(
                    GetString(Resource.String.title_navigation_launcher),
                    GetString(Resource.String.description_navigation_launcher),
                    typeof(NavigationLauncherActivity)
                ),
                //new SampleItem(
                //    GetString(Resource.String.title_end_navigation),
                //    GetString(Resource.String.description_end_navigation),
                //    typeof(EndNavigationActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_dual_navigation_map),
                //    GetString(Resource.String.description_dual_navigation_map),
                //    typeof(DualNavigationMapActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_mock_navigation),
                //    GetString(Resource.String.description_mock_navigation),
                //    typeof(MockNavigationActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_reroute),
                //    GetString(Resource.String.description_reroute),
                //    typeof(RerouteActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_navigation_route_ui),
                //    GetString(Resource.String.description_navigation_route_ui),
                //    typeof(NavigationMapRouteActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_waypoint_navigation),
                //    GetString(Resource.String.description_waypoint_navigation),
                //    typeof(WaypointNavigationActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_embedded_navigation),
                //    GetString(Resource.String.description_embedded_navigation),
                //    typeof(EmbeddedNavigationActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_fragment_navigation),
                //    GetString(Resource.String.description_fragment_navigation),
                //    typeof(FragmentNavigationActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_component_navigation),
                //    GetString(Resource.String.description_component_navigation),
                //    typeof(ComponentNavigationActivity)
                //),
            };

            // RecyclerView
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recycler_view);
            recyclerView.HasFixedSize = true;

            // Use a linear layout manager
            RecyclerView.LayoutManager layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            // Specify an adapter
            RecyclerView.Adapter adapter = new MainAdapter(recyclerView, sampleItems);
            recyclerView.SetAdapter(adapter);

            // Check for location permission
            permissionsManager = new PermissionsManager(this);
            if (!PermissionsManager.AreLocationPermissionsGranted(this))
            {
                recyclerView.Visibility = ViewStates.Invisible;
                permissionsManager.RequestLocationPermissions(this);
            }
            else
            {
                RequestPermissionsIfNotGranted(Android.Manifest.Permission.WriteExternalStorage);
            }
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
                    recyclerView.Visibility = ViewStates.Invisible;
                    Toast.MakeText(this, "You didn't grant storage permissions.", ToastLength.Long).Show();
                }
                else
                {
                    recyclerView.Visibility = ViewStates.Visible;
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

        /*
         * Recycler view
         */
        private class MainAdapter : RecyclerView.Adapter
        {
            private RecyclerView recyclerView;
            private List<SampleItem> samples;

            public override int ItemCount => samples.Count;

            private class ViewHolder : RecyclerView.ViewHolder
            {
                public TextView nameView;
                public TextView descriptionView;

                public ViewHolder(View view) : base(view)
                {
                    nameView = view.FindViewById<TextView>(Resource.Id.nameView);
                    descriptionView = view.FindViewById<TextView>(Resource.Id.descriptionView);
                }
            }

            public MainAdapter(RecyclerView recyclerView, List<SampleItem> samples)
            {
                this.recyclerView = recyclerView;
                this.samples = samples;
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var vh = holder as MainAdapter.ViewHolder;
                vh.nameView.Text = samples[position].Name;
                vh.descriptionView.Text = samples[position].Description;
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                View view = LayoutInflater
                    .From(parent.Context)
                    .Inflate(Resource.Layout.item_main_feature, parent, false);

                view.SetOnClickListener(new MyOnClickListener(recyclerView, samples));

                return new ViewHolder(view);
            }

            private class MyOnClickListener : Java.Lang.Object, View.IOnClickListener
            {
                private RecyclerView recyclerView;
                private List<SampleItem> samples;

                public MyOnClickListener(RecyclerView recyclerView, List<SampleItem> samples)
                {
                    this.recyclerView = recyclerView;
                    this.samples = samples;
                }

                public void OnClick(View v)
                {
                    int position = recyclerView.GetChildLayoutPosition(v);
                    var intent = new Intent(v.Context, samples[position].Activity);
                    recyclerView.Context.StartActivity(intent);
                }
            }
        }
    }
}
