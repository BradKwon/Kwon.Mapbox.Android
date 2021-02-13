
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Geojson;
using Mapbox.Services.Android.Navigation.UI.V5;
using Mapbox.Services.Android.Navigation.UI.V5.Listeners;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Mapbox.Services.Android.Navigation.V5.Routeprogress;

namespace MapboxNavigation.UI.Droid.TestApp.Legacy.Activity.NavigationUI.Fragment
{
    public class NavigationFragment : AndroidX.Fragment.App.Fragment, IOnNavigationReadyCallback,
        INavigationListener, IProgressChangeListener
    {
        private static double ORIGIN_LONGITUDE = -3.714873;
        private static double ORIGIN_LATITUDE = 40.397389;
        private static double DESTINATION_LONGITUDE = -3.712331;
        private static double DESTINATION_LATITUDE = 40.401686;

        private NavigationView navigationView;
        private DirectionsRoute directionsRoute;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.navigation_view_fragment_layout, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            UpdateNightMode();
            navigationView = view.FindViewById<NavigationView>(Resource.Id.navigation_view_fragment);
            navigationView.OnCreate(savedInstanceState);
            navigationView.Initialize(this);
        }

        public override void OnStart()
        {
            base.OnStart();
            navigationView.OnStart();
        }

        public override void OnResume()
        {
            base.OnResume();
            navigationView.OnResume();
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            navigationView.OnSaveInstanceState(outState);
            base.OnSaveInstanceState(outState);
        }

        public override void OnViewStateRestored(Bundle savedInstanceState)
        {
            base.OnViewStateRestored(savedInstanceState);
            if (savedInstanceState != null)
            {
                navigationView.OnRestoreInstanceState(savedInstanceState);
            }
        }

        public override void OnPause()
        {
            base.OnPause();
            navigationView.OnPause();
        }

        public override void OnStop()
        {
            base.OnStop();
            navigationView.OnStop();
        }

        public override void OnLowMemory()
        {
            base.OnLowMemory();
            navigationView.OnLowMemory();
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
            navigationView.OnDestroy();
        }

        public void OnCancelNavigation()
        {
            navigationView.StopNavigation();
            StopNavigation();
        }

        public void OnNavigationFinished()
        {
            // no-op
        }

        public void OnNavigationReady(bool p0)
        {
            Point origin = Point.FromLngLat(ORIGIN_LONGITUDE, ORIGIN_LATITUDE);
            Point destination = Point.FromLngLat(DESTINATION_LONGITUDE, DESTINATION_LATITUDE);
            FetchRoute(origin, destination);
        }

        public void OnNavigationRunning()
        {
            // no-op
        }

        public void OnProgressChange(Location p0, RouteProgress p1)
        {
            var location = p0;
            var routeProgress = p1;

            bool isInTunnel = routeProgress.InTunnel();
            bool wasInTunnel = WasInTunnel();
            if (isInTunnel)
            {
                if (!wasInTunnel)
                {
                    UpdateWasInTunnel(true);
                    UpdateCurrentNightMode(AppCompatDelegate.ModeNightYes);
                }
            }
            else
            {
                if (wasInTunnel)
                {
                    UpdateWasInTunnel(false);
                    UpdateCurrentNightMode(AppCompatDelegate.ModeNightAuto);
                }
            }
        }

        private void UpdateNightMode()
        {
            if (WasNavigationStopped())
            {
                UpdateWasNavigationStopped(false);
                AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightAuto;
                Activity.Recreate();
            }
        }

        private void FetchRoute(Point origin, Point destination)
        {
            NavigationRoute.InvokeBuilder(Context)
              .AccessToken(Mapbox.Mapboxsdk.Mapbox.AccessToken)
              .Origin(origin)
              .Destination(destination)
              .Build()
              .GetRoute(new SimplifiedCallback((response) =>
              {
                  var body = response.Body() as DirectionsResponse;
                  if (body != null && body.Routes().Any())
                  {
                      directionsRoute = body.Routes()[0];
                      StartNavigation();
                  }
              }));
        }

        private void StartNavigation()
        {
            if (directionsRoute == null)
            {
                return;
            }
            NavigationViewOptions options = NavigationViewOptions.InvokeBuilder()
              .DirectionsRoute(directionsRoute)
              .ShouldSimulateRoute(true)
              .NavigationListener(this)
              .ProgressChangeListener(this)
              .Build();
            navigationView.StartNavigation(options);
        }

        private void StopNavigation()
        {
            FragmentActivity activity = Activity;
            if (activity != null && activity is FragmentNavigationActivity) {
                FragmentNavigationActivity fragmentNavigationActivity = (FragmentNavigationActivity)activity;
                fragmentNavigationActivity.ShowPlaceholderFragment();
                fragmentNavigationActivity.ShowNavigationFab();
                UpdateWasNavigationStopped(true);
                UpdateWasInTunnel(false);
            }
        }

        private bool WasInTunnel()
        {
            Context context = Activity;
            var preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            return preferences.GetBoolean(context.GetString(Resource.String.was_in_tunnel), false);
        }

        private void UpdateWasInTunnel(bool wasInTunnel)
        {
            Context context = Activity;
            var preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            var editor = preferences.Edit();
            editor.PutBoolean(context.GetString(Resource.String.was_in_tunnel), wasInTunnel);
            editor.Apply();
        }

        private void UpdateCurrentNightMode(int nightMode)
        {
            AppCompatDelegate.DefaultNightMode = nightMode;
            Activity.Recreate();
        }

        private bool WasNavigationStopped()
        {
            Context context = Activity;
            var preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            return preferences.GetBoolean(GetString(Resource.String.was_navigation_stopped), false);
        }

        public void UpdateWasNavigationStopped(bool wasNavigationStopped)
        {
            Context context = Activity;
            var preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            var editor = preferences.Edit();
            editor.PutBoolean(GetString(Resource.String.was_navigation_stopped), wasNavigationStopped);
            editor.Apply();
        }
    }
}
