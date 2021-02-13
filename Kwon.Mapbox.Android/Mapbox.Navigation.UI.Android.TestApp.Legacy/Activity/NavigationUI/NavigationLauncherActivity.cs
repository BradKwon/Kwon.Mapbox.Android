
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
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using CheeseBind;
using Google.Android.Material.Snackbar;
using Java.Lang;
using Java.Util;
using Mapbox.Android.Core.Location;
using Mapbox.Api.Directions.V5;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Core.Utils;
using Mapbox.Geojson;
using Mapbox.Mapboxsdk.Camera;
using Mapbox.Mapboxsdk.Exceptions;
using Mapbox.Mapboxsdk.Geometry;
using Mapbox.Mapboxsdk.Location.Modes;
using Mapbox.Mapboxsdk.Maps;
using Mapbox.Services.Android.Navigation.UI.V5;
using Mapbox.Services.Android.Navigation.UI.V5.Camera;
using Mapbox.Services.Android.Navigation.UI.V5.Map;
using Mapbox.Services.Android.Navigation.UI.V5.Route;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Mapbox.Services.Android.Navigation.V5.Utils;
using Square.Retrofit2;
using TimberLog;

namespace MapboxNavigation.UI.Droid.TestApp.Legacy.Activity.NavigationUI
{
    [Activity(Label = "NavigationLauncherActivity", ParentActivity = typeof(MainActivity))]
    public class NavigationLauncherActivity : AppCompatActivity, IOnMapReadyCallback, MapboxMap.IOnMapLongClickListener, IOnRouteSelectionChangeListener
    {
        private static int CAMERA_ANIMATION_DURATION = 1000;
        private static int DEFAULT_CAMERA_ZOOM = 16;
        private static int CHANGE_SETTING_REQUEST_CODE = 1;
        private static int INITIAL_ZOOM = 16;
        private static long UPDATE_INTERVAL_IN_MILLISECONDS = 1000;
        private static long FASTEST_UPDATE_INTERVAL_IN_MILLISECONDS = 500;

        private readonly NavigationLauncherLocationCallback callback;
        private readonly LocaleUtils localeUtils = new LocaleUtils();
        private readonly List<Point> wayPoints = new List<Point>();
        private ILocationEngine locationEngine;
        private NavigationMapboxMap map;
        private DirectionsRoute route;
        private Point currentLocation;
        private bool locationFound;

        [BindView(Resource.Id.mapView)]
        MapView mapView;
        [BindView(Resource.Id.launch_route_btn)]
        Button launchRouteBtn;
        [BindView(Resource.Id.loading)]
        ProgressBar loading;
        [BindView(Resource.Id.launch_btn_frame)]
        FrameLayout launchBtnFrame;

        public NavigationLauncherActivity()
        {
            callback = new NavigationLauncherLocationCallback(this);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_navigation_launcher);
            Cheeseknife.Bind(this);
            mapView.OnCreate(savedInstanceState);
            mapView.GetMapAsync(this);
        }

        //public override bool OnCreateOptionsMenu(IMenu menu)
        //{
        //    MenuInflater.Inflate(Resource.Menu.navigation_view_activity_menu, menu);
        //    return true;
        //}

        //public override bool OnOptionsItemSelected(IMenuItem item)
        //{
        //    switch (item.ItemId)
        //    {
        //        case Resource.Id.settings:
        //            ShowSettings();
        //            return true;
        //        default:
        //            return base.OnOptionsItemSelected(item);
        //    }
        //}

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            //if (requestCode == CHANGE_SETTING_REQUEST_CODE && resultCode == Result.Ok)
            //{
            //    bool shouldRefresh = data.GetBooleanExtra(NavigationSettingsActivity.UNIT_TYPE_CHANGED, false) ||
            //        data.GetBooleanExtra(NavigationSettingsActivity.LANGUAGE_CHANGED, false);
            //    if (wayPoints.Any() && shouldRefresh)
            //    {
            //        FetchRoute();
            //    }
            //}
        }

        protected override void OnStart()
        {
            base.OnStart();
            mapView.OnStart();
        }

        protected override void OnResume()
        {
            base.OnResume();
            mapView.OnResume();
            if (locationEngine != null)
            {
                locationEngine.RequestLocationUpdates(BuildEngineRequest(), callback, null);
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            mapView.OnPause();
            if (locationEngine != null)
            {
                locationEngine.RemoveLocationUpdates(callback);
            }
        }

        public override void OnLowMemory()
        {
            base.OnLowMemory();
            mapView.OnLowMemory();
        }

        protected override void OnStop()
        {
            base.OnStop();
            mapView.OnStop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            mapView.OnDestroy();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            mapView.OnSaveInstanceState(outState);
        }

        [OnClick(Resource.Id.launch_route_btn)]
        public void OnRouteLaunchClick(object sender, EventArgs args)
        {
            LaunchNavigationWithRoute();
        }

        public void OnMapReady(MapboxMap p0)
        {
            p0.SetStyle(Style.MapboxStreets, new MyOnStyleLoaded(() =>
            {
                p0.AddOnMapLongClickListener(this);
                map = new NavigationMapboxMap(mapView, p0);
                map.SetOnRouteSelectionChangeListener(this);
                map.UpdateLocationLayerRenderMode(RenderMode.Compass);
                InitializeLocationEngine();
            }));
        }

        public bool OnMapLongClick(LatLng p0)
        {
            if (wayPoints.Count == 2)
            {
                Snackbar.Make(mapView, "Max way points exceeded. Clearing route...", Snackbar.LengthShort).Show();
                wayPoints.Clear();
                map.ClearMarkers();
                map.RemoveRoute();
                return false;
            }

            wayPoints.Add(Point.FromLngLat(p0.Longitude, p0.Latitude));
            launchRouteBtn.Enabled = false;
            loading.Visibility = ViewStates.Visible;
            SetCurrentMarkerPosition(p0);

            if (locationFound)
            {
                FetchRoute();
            }

            return false;
        }

        public void OnNewPrimaryRouteSelected(DirectionsRoute p0)
        {
            route = p0;
        }

        void UpdateCurrentLocation(Point currentLocation)
        {
            this.currentLocation = currentLocation;
        }

        void OnLocationFound(Location location)
        {
            map.UpdateLocation(location);
            if (!locationFound)
            {
                AnimateCamera(new LatLng(location.Latitude, location.Longitude));
                Snackbar.Make(mapView, Resource.String.explanation_long_press_waypoint, Snackbar.LengthLong).Show();
                locationFound = true;
                HideLoading();
            }
        }

        //private void ShowSettings()
        //{
        //    StartActivityForResult(new Intent(this, typeof(NavigationSettingsActivity)), CHANGE_SETTING_REQUEST_CODE);
        //}

        private void InitializeLocationEngine()
        {
            locationEngine = LocationEngineProvider.GetBestLocationEngine(ApplicationContext);
            LocationEngineRequest request = BuildEngineRequest();
            locationEngine.RequestLocationUpdates(request, callback, null);
            locationEngine.GetLastLocation(callback);
        }

        private void FetchRoute()
        {
            NavigationRoute.Builder builder = NavigationRoute.InvokeBuilder(this)
                .AccessToken(Mapbox.Mapboxsdk.Mapbox.AccessToken)
                .Origin(currentLocation)
                .Profile(GetRouteProfileFromSharedPreferences())
                .Alternatives((Java.Lang.Boolean)true);

            foreach (Point wayPoint in wayPoints)
            {
                builder.AddWaypoint(wayPoint);
            }

            SetFieldsFromSharedPreferences(builder);
            builder.Build().GetRoute(new MyGetRouteCallback((routes) =>
            {
                HideLoading();
                route = routes[0];
                if (Convert.ToInt32(route.Distance()) > 25)
                {
                    launchRouteBtn.Enabled = true;
                    map.DrawRoutes(routes);
                    BoundCameraToRoute();
                }
                else
                {
                    Snackbar.Make(mapView, Resource.String.error_select_longer_route, Snackbar.LengthShort).Show();
                }
            }));
            loading.Visibility = ViewStates.Visible;
        }

        private void SetFieldsFromSharedPreferences(NavigationRoute.Builder builder)
        {
            builder.Language(GetLanguageFromSharedPreferences())
                .VoiceUnits(GetUnitTypeFromSharedPreferences());
        }

        private string GetUnitTypeFromSharedPreferences()
        {
            var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this);
            string defaultUnitType = GetString(Resource.String.default_unit_type);
            string unitType = sharedPreferences.GetString(GetString(Resource.String.unit_type_key), defaultUnitType);
            if (unitType.Equals(defaultUnitType))
            {
                unitType = localeUtils.GetUnitTypeForDeviceLocale(this);
            }

            return unitType;
        }

        private Locale GetLanguageFromSharedPreferences()
        {
            var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this);
            string defaultLanguage = GetString(Resource.String.default_locale);
            string language = sharedPreferences.GetString(GetString(Resource.String.language_key), defaultLanguage);
            if (language.Equals(defaultLanguage))
            {
                return localeUtils.InferDeviceLocale(this);
            }
            else
            {
                return new Locale(language);
            }
        }

        private bool GetShouldSimulateRouteFromSharedPreferences()
        {
            var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this);
            return sharedPreferences.GetBoolean(GetString(Resource.String.simulate_route_key), false);
        }

        private string GetRouteProfileFromSharedPreferences()
        {
            var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this);
            return sharedPreferences.GetString(GetString(Resource.String.route_profile_key), DirectionsCriteria.ProfileDrivingTraffic);
        }

        private string ObtainOfflinePath()
        {
            var offline = Android.OS.Environment.GetExternalStoragePublicDirectory("Offline");
            return offline.AbsolutePath;
        }

        private string RetrieveOfflineVersionFromPreferences()
        {
            var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this);
            return sharedPreferences.GetString(GetString(Resource.String.offline_version_key), "");
        }

        private void LaunchNavigationWithRoute()
        {
            if (route == null)
            {
                Snackbar.Make(mapView, Resource.String.error_route_not_available, Snackbar.LengthShort).Show();
                return;
            }

            NavigationLauncherOptions.Builder optionsBuilder = NavigationLauncherOptions.InvokeBuilder()
                .ShouldSimulateRoute(GetShouldSimulateRouteFromSharedPreferences());
            CameraPosition initialPosition = new CameraPosition.Builder()
                .Target(new LatLng(currentLocation.Latitude(), currentLocation.Longitude()))
                .Zoom(INITIAL_ZOOM)
                .Build();
            optionsBuilder.InitialMapCameraPosition(initialPosition);
            optionsBuilder.DirectionsRoute(route);
            string offlinePath = ObtainOfflinePath();
            if (!TextUtils.IsEmpty(offlinePath))
            {
                optionsBuilder.OfflineRoutingTilesPath(offlinePath);
            }

            string offlineVersion = RetrieveOfflineVersionFromPreferences();
            if (!string.IsNullOrWhiteSpace(offlineVersion))
            {
                optionsBuilder.OfflineRoutingTilesVersion(offlineVersion);
            }
            NavigationLauncher.StartNavigation(this, optionsBuilder.Build());
        }

        private void HideLoading()
        {
            if (loading.Visibility == ViewStates.Visible)
            {
                loading.Visibility = ViewStates.Invisible;
            }
        }

        public void BoundCameraToRoute()
        {
            if (route != null)
            {
                var routeCoords = LineString.FromPolyline(route.Geometry(), Mapbox.Core.Constants.Constants.Precision6).Coordinates();
                List<LatLng> bboxPoints = new List<LatLng>();

                foreach (var point in routeCoords)
                {
                    bboxPoints.Add(new LatLng(point.Latitude(), point.Longitude()));
                }

                if (bboxPoints.Count > 1)
                {
                    try
                    {
                        var bounds = new LatLngBounds.Builder().Includes(bboxPoints).Build();
                        // left, top, right, bottom
                        int topPadding = launchBtnFrame.Height * 2;
                        AnimateCameraBbox(bounds, CAMERA_ANIMATION_DURATION, new int[] { 50, topPadding, 50, 100 });
                    }
                    catch (InvalidLatLngBoundsException ex)
                    {
                        Toast.MakeText(this, Resource.String.error_valid_route_not_found, ToastLength.Short).Show();
                    }
                }
            }
        }

        private void AnimateCameraBbox(LatLngBounds bounds, int animationTime, int[] padding)
        {
            var position = map.RetrieveMap().GetCameraForLatLngBounds(bounds, padding);
            var cameraUpdate = CameraUpdateFactory.NewCameraPosition(position);
            var navigationCameraUpdate = new NavigationCameraUpdate(cameraUpdate);
            navigationCameraUpdate.SetMode(CameraUpdateMode.Override);
            map.RetrieveCamera().Update(navigationCameraUpdate, animationTime);
        }

        private void AnimateCamera(LatLng point)
        {
            var cameraUpdate = CameraUpdateFactory.NewLatLngZoom(point, DEFAULT_CAMERA_ZOOM);
            var navigationCameraUpdate = new NavigationCameraUpdate(cameraUpdate);
            navigationCameraUpdate.SetMode(CameraUpdateMode.Override);
            map.RetrieveCamera().Update(navigationCameraUpdate, CAMERA_ANIMATION_DURATION);
        }

        private void SetCurrentMarkerPosition(LatLng position)
        {
            if (position != null)
            {
                map.AddDestinationMarker(Point.FromLngLat(position.Longitude, position.Latitude));
            }
        }

        private LocationEngineRequest BuildEngineRequest()
        {
            return new LocationEngineRequest.Builder(UPDATE_INTERVAL_IN_MILLISECONDS)
                .SetPriority(LocationEngineRequest.PriorityHighAccuracy)
                .SetFastestInterval(FASTEST_UPDATE_INTERVAL_IN_MILLISECONDS)
                .Build();
        }

        private class MyGetRouteCallback : Java.Lang.Object, ICallback
        {
            Action<IList<DirectionsRoute>> action;

            public MyGetRouteCallback(Action<IList<DirectionsRoute>> action)
            {
                this.action = action;
            }

            public void OnFailure(ICall p0, Throwable p1)
            {
            }

            public void OnResponse(ICall p0, Response p1)
            {
                if (ValidRouteResponse(p1))
                {
                    var response = p1.Body() as DirectionsResponse;
                    action(response.Routes());
                }
            }

            private bool ValidRouteResponse(Response p1)
            {
                var response = p1.Body() as DirectionsResponse;
                return response != null && response.Routes().Any();
            }
        }

        private class MyOnStyleLoaded : Java.Lang.Object, Style.IOnStyleLoaded
        {
            Action action;

            public MyOnStyleLoaded(Action action)
            {
                this.action = action;
            }

            public void OnStyleLoaded(Style p0)
            {
                action();
            }
        }

        private class NavigationLauncherLocationCallback : Java.Lang.Object, ILocationEngineCallback
        {
            private readonly WeakReference<NavigationLauncherActivity> activityWeakReference;

            public NavigationLauncherLocationCallback(NavigationLauncherActivity activity)
            {
                activityWeakReference = new WeakReference<NavigationLauncherActivity>(activity);
            }

            public void OnFailure(Java.Lang.Exception p0)
            {
                Timber.E(p0);
            }

            public void OnSuccess(Java.Lang.Object p0)
            {
                if (activityWeakReference.TryGetTarget(out var activity))
                {
                    Location location = (p0 as LocationEngineResult).LastLocation;
                    if (location == null) return;
                    activity.UpdateCurrentLocation(Point.FromLngLat(location.Longitude, location.Latitude));
                    activity.OnLocationFound(location);
                }
            }
        }
    }
}
