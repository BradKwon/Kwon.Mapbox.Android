
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.Transitions;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.ConstraintLayout.Widget;
using Java.Lang;
using Mapbox.Android.Core.Location;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Geojson;
using Mapbox.Mapboxsdk.Annotations;
using Mapbox.Mapboxsdk.Camera;
using Mapbox.Mapboxsdk.Geometry;
using Mapbox.Mapboxsdk.Location;
using Mapbox.Mapboxsdk.Location.Modes;
using Mapbox.Mapboxsdk.Maps;
using Mapbox.Services.Android.Navigation.UI.V5;
using Mapbox.Services.Android.Navigation.UI.V5.Listeners;
using Mapbox.Services.Android.Navigation.UI.V5.Route;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Square.Retrofit2;
using TimberLog;
using NavigationView = Mapbox.Services.Android.Navigation.UI.V5.NavigationView;

namespace MapboxNavigation.UI.Droid.TestApp.Activity.NavigationUI
{
    [Activity(Label = "DualNavigationMapActivity")]
    public class DualNavigationMapActivity : AppCompatActivity,
        IOnNavigationReadyCallback, INavigationListener, ICallback, IOnMapReadyCallback,
        MapboxMap.IOnMapLongClickListener, IOnRouteSelectionChangeListener
    {
        private static int CAMERA_ANIMATION_DURATION = 1000;
        private static int DEFAULT_CAMERA_ZOOM = 16;
        private static long UPDATE_INTERVAL_IN_MILLISECONDS = 1000;
        private static long FASTEST_UPDATE_INTERVAL_IN_MILLISECONDS = 500;
        private DualNavigationLocationCallback callback;
        private ConstraintLayout dualNavigationMap;
        private NavigationView navigationView;
        private MapView mapView;
        private ProgressBar loading;
        private FloatingActionButton launchNavigationFab;
        private Point origin = Point.FromLngLat(-122.423579, 37.761689);
        private Point destination = Point.FromLngLat(-122.426183, 37.760872);
        private DirectionsRoute route;
        private ILocationEngine locationEngine;
        private NavigationMapRoute mapRoute;
        private MapboxMap mapboxMap;
        private Marker currentMarker;
        private bool isNavigationRunning;
        private bool locationFound;
        private bool[] constraintChanged;
        private ConstraintSet navigationMapConstraint;
        private ConstraintSet navigationMapExpandedConstraint;

        public void OnCancelNavigation()
        {
            navigationView.StopNavigation();
            ExpandCollapse();
        }

        public void OnFailure(ICall p0, Throwable p1)
        {
        }

        public bool OnMapLongClick(LatLng p0)
        {
            destination = Point.FromLngLat(p0.Longitude, p0.Latitude);
            UpdateLoadingTo(true);
            SetCurrentMarkerPosition(p0);
            if (origin != null)
            {
                FetchRoute();
            }
            return true;
        }

        public void OnMapReady(MapboxMap p0)
        {
            mapboxMap = p0;
            mapboxMap.AddOnMapLongClickListener(this);
            mapboxMap.SetStyle(Style.MapboxStreets, new MapboxMapSetStyleListener((style) =>
            {
                InitializeLocationEngine();
                InitializeLocationComponent(style);
                InitMapRoute();
                FetchRoute();
            }));
        }

        public void OnNavigationFinished()
        {
        }

        public void OnNavigationReady(bool p0)
        {
            isNavigationRunning = p0;
        }

        public void OnNavigationRunning()
        {
        }

        public void OnNewPrimaryRouteSelected(DirectionsRoute p0)
        {
            route = p0;
        }

        public void OnResponse(ICall p0, Response p1)
        {
            if (ValidRouteResponse(p1))
            {
                UpdateLoadingTo(false);
                launchNavigationFab.Show();
                var body = p1.Body() as DirectionsResponse;
                route = body.Routes()[0];
                mapRoute.AddRoutes(body.Routes());
                if (isNavigationRunning)
                {
                    LaunchNavigation();
                }
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetTheme(Resource.Style.Theme_AppCompat_NoActionBar);
            base.OnCreate(savedInstanceState);

            callback = new DualNavigationLocationCallback(this);

            InitializeViews(savedInstanceState);
            navigationView.Initialize(this);
            navigationMapConstraint = new ConstraintSet();
            navigationMapConstraint.Clone(dualNavigationMap);
            navigationMapExpandedConstraint = new ConstraintSet();
            navigationMapExpandedConstraint.Clone(this, Resource.Layout.activity_dual_navigation_map_expanded);

            constraintChanged = new bool[] { false };
            launchNavigationFab.SetOnClickListener(new LaunchNavigationFabClickListener((v) =>
            {
                ExpandCollapse();
                LaunchNavigation();
            }));
        }

        protected override void OnStart()
        {
            base.OnStart();
            navigationView.OnStart();
            mapView.OnStart();
        }

        protected override void OnResume()
        {
            base.OnResume();
            navigationView.OnResume();
            mapView.OnResume();
            if (locationEngine != null)
            {
                LocationEngineRequest request = BuildEngineRequest();
                locationEngine.RequestLocationUpdates(request, callback, null);
            }
        }

        public override void OnLowMemory()
        {
            base.OnLowMemory();
            navigationView.OnLowMemory();
            mapView.OnLowMemory();
        }

        public override void OnBackPressed()
        {
            if (!navigationView.OnBackPressed())
            {
                base.OnBackPressed();
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            navigationView.OnSaveInstanceState(outState);
            mapView.OnSaveInstanceState(outState);
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
            mapView.OnPause();
            if (locationEngine != null)
            {
                locationEngine.RemoveLocationUpdates(callback);
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            navigationView.OnStop();
            mapView.OnStop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            navigationView.OnDestroy();
            mapView.OnDestroy();
        }

        void OnLocationFound(Location location)
        {
            origin = Point.FromLngLat(location.Longitude, location.Latitude);
            if (!locationFound)
            {
                AnimateCamera(new LatLng(location.Latitude, location.Longitude));
                locationFound = true;
                UpdateLoadingTo(false);
            }
        }

        private void ExpandCollapse()
        {
            TransitionManager.BeginDelayedTransition(dualNavigationMap);
            ConstraintSet constraint;
            if (constraintChanged[0])
            {
                constraint = navigationMapConstraint;
            }
            else
            {
                constraint = navigationMapExpandedConstraint;
            }
            constraint.ApplyTo(dualNavigationMap);
            constraintChanged[0] = !constraintChanged[0];
        }

        private void FetchRoute()
        {
            NavigationRoute builder = NavigationRoute.InvokeBuilder(this)
              .AccessToken(GetString(Resource.String.mapbox_access_token))
              .Origin(origin)
              .Destination(destination)
              .Alternatives((Java.Lang.Boolean)true)
              .Build();
            builder.GetRoute(this);
        }

        private void LaunchNavigation()
        {
            launchNavigationFab.Hide();
            navigationView.Visibility = ViewStates.Visible;
            NavigationViewOptions.Builder options = NavigationViewOptions.InvokeBuilder()
              .NavigationListener(this)
              .DirectionsRoute(route);
            navigationView.StartNavigation(options.Build());
        }

        private void InitializeViews(Bundle savedInstanceState)
        {
            SetContentView(Resource.Layout.activity_dual_navigation_map);
            dualNavigationMap = FindViewById<ConstraintLayout>(Resource.Id.dualNavigationMap);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            navigationView = FindViewById<NavigationView>(Resource.Id.navigationView);
            loading = FindViewById<ProgressBar>(Resource.Id.loading);
            launchNavigationFab = FindViewById<FloatingActionButton>(Resource.Id.launchNavigation);
            navigationView.OnCreate(savedInstanceState);
            mapView.OnCreate(savedInstanceState);
            mapView.GetMapAsync(this);
        }

        private void UpdateLoadingTo(bool isVisible)
        {
            if (isVisible)
            {
                loading.Visibility = ViewStates.Visible;
            }
            else
            {
                loading.Visibility = ViewStates.Invisible;
            }
        }

        private bool ValidRouteResponse(Response response)
        {
            var body = response.Body() as DirectionsResponse;
            return response != null && body != null && body.Routes().Any();
        }

        private void InitializeLocationEngine()
        {
            locationEngine = LocationEngineProvider.GetBestLocationEngine(ApplicationContext);
            locationEngine.GetLastLocation(callback);
        }

        private LocationEngineRequest BuildEngineRequest()
        {
            return new LocationEngineRequest.Builder(UPDATE_INTERVAL_IN_MILLISECONDS)
              .SetPriority(LocationEngineRequest.PriorityHighAccuracy)
              .SetFastestInterval(FASTEST_UPDATE_INTERVAL_IN_MILLISECONDS)
              .Build();
        }

        private void InitializeLocationComponent(Style style)
        {
            LocationComponent locationComponent = mapboxMap.LocationComponent;
            locationComponent.ActivateLocationComponent(this, style, locationEngine);
            locationComponent.LocationComponentEnabled = true;
            locationComponent.RenderMode = RenderMode.Compass;
        }

        private void InitMapRoute()
        {
            mapRoute = new NavigationMapRoute(mapView, mapboxMap);
            mapRoute.SetOnRouteSelectionChangeListener(this);
        }

        private void SetCurrentMarkerPosition(LatLng position)
        {
            if (position != null)
            {
                if (currentMarker == null)
                {
                    MarkerOptions markerViewOptions = new MarkerOptions();
                    markerViewOptions.SetPosition(position);
                    currentMarker = mapboxMap.AddMarker(markerViewOptions);
                }
                else
                {
                    currentMarker.Position = position;
                }
            }
        }

        private void AnimateCamera(LatLng point)
        {
            mapboxMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(point, DEFAULT_CAMERA_ZOOM), CAMERA_ANIMATION_DURATION);
        }

        private class DualNavigationLocationCallback : Java.Lang.Object, ILocationEngineCallback
        {
            private WeakReference<DualNavigationMapActivity> activityWeakReference;

            public DualNavigationLocationCallback(DualNavigationMapActivity activity)
            {
                activityWeakReference = new WeakReference<DualNavigationMapActivity>(activity);
            }

            public void OnFailure(Java.Lang.Exception p0)
            {
                Timber.E(p0);
            }

            public void OnSuccess(Java.Lang.Object p0)
            {
                if (activityWeakReference.TryGetTarget(out DualNavigationMapActivity activity))
                {
                    var result = p0 as LocationEngineResult;
                    Location location = result.LastLocation;
                    if (location == null)
                    {
                        return;
                    }
                    activity.OnLocationFound(location);
                }
            }
        }

        private class LaunchNavigationFabClickListener : Java.Lang.Object, View.IOnClickListener
        {
            Action<View> _clickAction;

            public LaunchNavigationFabClickListener(Action<View> clickAction)
            {
                _clickAction = clickAction;
            }

            public void OnClick(View v)
            {
                _clickAction?.Invoke(v);
            }
        }

        private class MapboxMapSetStyleListener : Java.Lang.Object, Style.IOnStyleLoaded
        {
            Action<Style> action;

            public MapboxMapSetStyleListener(Action<Style> action)
            {
                this.action = action;
            }

            public void OnStyleLoaded(Style p0)
            {
                action.Invoke(p0);
            }
        }
    }
}
