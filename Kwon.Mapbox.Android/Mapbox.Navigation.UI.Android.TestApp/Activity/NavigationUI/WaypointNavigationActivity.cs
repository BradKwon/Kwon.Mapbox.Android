
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
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Geojson;
using Mapbox.Services.Android.Navigation.UI.V5;
using Mapbox.Services.Android.Navigation.UI.V5.Listeners;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Mapbox.Services.Android.Navigation.V5.Routeprogress;

namespace MapboxNavigation.UI.Droid.TestApp.Activity.NavigationUI
{
    [Activity(Label = "WaypointNavigationActivity")]
    public class WaypointNavigationActivity : AppCompatActivity, IOnNavigationReadyCallback, INavigationListener,
        IRouteListener, IProgressChangeListener
    {
        private NavigationView navigationView;
        private bool dropoffDialogShown;
        private Location lastKnownLocation;

        private List<Point> points = new List<Point>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetTheme(Resource.Style.Theme_AppCompat_NoActionBar);
            base.OnCreate(savedInstanceState);

            points.Add(Point.FromLngLat(-77.04012393951416, 38.9111117447887));
            points.Add(Point.FromLngLat(-77.03847169876099, 38.91113678979344));
            points.Add(Point.FromLngLat(-77.03848242759705, 38.91040213277608));
            points.Add(Point.FromLngLat(-77.03850388526917, 38.909650771013034));
            points.Add(Point.FromLngLat(-77.03651905059814, 38.90894949285854));
            SetContentView(Resource.Layout.activity_navigation);
            navigationView = FindViewById<NavigationView>(Resource.Id.navigationView);
            navigationView.OnCreate(savedInstanceState);
            navigationView.Initialize(this);
        }

        protected override void OnStart()
        {
            base.OnStart();
            navigationView.OnStart();
        }

        protected override void OnResume()
        {
            base.OnResume();
            navigationView.OnResume();
        }

        public override void OnLowMemory()
        {
            base.OnLowMemory();
            navigationView.OnLowMemory();
        }

        public override void OnBackPressed()
        {
            // If the navigation view didn't need to do anything, call super
            if (!navigationView.OnBackPressed())
            {
                base.OnBackPressed();
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            navigationView.OnSaveInstanceState(outState);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
            navigationView.OnRestoreInstanceState(savedInstanceState);
        }

        protected override void OnPause()
        {
            base.OnPause();
            navigationView.OnPause();
        }

        protected override void OnStop()
        {
            base.OnStop();
            navigationView.OnStop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            navigationView.OnDestroy();
        }

        public bool AllowRerouteFrom(Point p0)
        {
            return true;
        }

        public void OnArrival()
        {
            if (!dropoffDialogShown && points.Any())
            {
                ShowDropoffDialog();
                dropoffDialogShown = true; // Accounts for multiple arrival events
                Toast.MakeText(this, "You have arrived!", ToastLength.Short).Show();
            }
        }

        public void OnCancelNavigation()
        {
            // Navigation canceled, finish the activity
            Finish();
        }

        public void OnFailedReroute(string p0)
        {
        }

        public void OnNavigationFinished()
        {
        }

        public void OnNavigationReady(bool p0)
        {
            var origin = points[0];
            points.RemoveAt(0);
            var dest = points[0];
            points.RemoveAt(0);

            FetchRoute(origin, dest);
        }

        public void OnNavigationRunning()
        {
        }

        public void OnOffRoute(Point p0)
        {
        }

        public void OnProgressChange(Location p0, RouteProgress p1)
        {
            lastKnownLocation = p0;
        }

        public void OnRerouteAlong(DirectionsRoute p0)
        {
        }

        private void StartNavigation(DirectionsRoute directionsRoute)
        {
            NavigationViewOptions navigationViewOptions = SetupOptions(directionsRoute);
            navigationView.StartNavigation(navigationViewOptions);
        }

        private void ShowDropoffDialog()
        {
            Android.Support.V7.App.AlertDialog alertDialog = new Android.Support.V7.App.AlertDialog.Builder(this).Create();
            alertDialog.SetMessage(GetString(Resource.String.dropoff_dialog_text));
            alertDialog.SetButton((int)DialogButtonType.Positive, GetString(Resource.String.dropoff_dialog_positive_text), (s, e) =>
            {
                FetchRoute(GetLastKnownLocation(), points[0]);
                points.RemoveAt(0);
            });
            alertDialog.SetButton((int)DialogButtonType.Negative, GetString(Resource.String.dropoff_dialog_negative_text), (s, e) =>
            {
                // Do nothing
            });

            alertDialog.Show();
        }

        private void FetchRoute(Point origin, Point destination)
        {
            NavigationRoute.InvokeBuilder(this)
              .AccessToken(Mapbox.Mapboxsdk.Mapbox.AccessToken)
              .Origin(origin)
              .Destination(destination)
              .Alternatives((Java.Lang.Boolean)true)
              .Build()
              .GetRoute(new SimplifiedCallback((res) =>
              {
                  var body = res.Body() as DirectionsResponse;
                  if (body != null && body.Routes().Any())
                  {
                    StartNavigation(body.Routes()[0]);
                  }
              }));
        }

        private NavigationViewOptions SetupOptions(DirectionsRoute directionsRoute)
        {
            dropoffDialogShown = false;

            NavigationViewOptions.Builder options = NavigationViewOptions.InvokeBuilder();
            options.DirectionsRoute(directionsRoute)
              .NavigationListener(this)
              .ProgressChangeListener(this)
              .RouteListener(this)
              .ShouldSimulateRoute(true);
            return options.Build();
        }

        private Point GetLastKnownLocation()
        {
            return Point.FromLngLat(lastKnownLocation.Longitude, lastKnownLocation.Latitude);
        }
    }
}
