
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
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.ConstraintLayout.Widget;
using CheeseBind;
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
using Mapbox.Services.Android.Navigation.UI.V5.Route;
using Mapbox.Services.Android.Navigation.V5.Instruction;
using Mapbox.Services.Android.Navigation.V5.Location.Replay;
using Mapbox.Services.Android.Navigation.V5.Milestone;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Mapbox.Services.Android.Navigation.V5.Offroute;
using Mapbox.Services.Android.Navigation.V5.Routeprogress;
using Mapbox.Turf;
using MapboxNavigation.UI.Droid.TestApp.Legacy.Activity.Notification;
using Square.Retrofit2;
using TimberLog;

namespace MapboxNavigation.UI.Droid.TestApp.Legacy.Activity
{
    [Activity(Label = "MockNavigationActivity")]
    public class MockNavigationActivity : AppCompatActivity,
        IOnMapReadyCallback, MapboxMap.IOnMapClickListener, IProgressChangeListener, INavigationEventListener,
        IMilestoneEventListener, IOffRouteListener, IRefreshCallback
    {
        private static int BEGIN_ROUTE_MILESTONE = 1001;
        private static double TWENTY_FIVE_METERS = 25d;

        [BindView(Resource.Id.mapView)]
        MapView mapView;

        [BindView(Resource.Id.newLocationFab)]
        FloatingActionButton newLocationFab;

        [BindView(Resource.Id.startRouteButton)]
        Button startRouteButton;

        private MapboxMap mapboxMap;

        // Navigation related variables
        private ILocationEngine locationEngine;
        private Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation navigation;
        private DirectionsRoute route;
        private NavigationMapRoute navigationMapRoute;
        private Point destination;
        private Point waypoint;
        private RouteRefresh routeRefresh;
        private bool isRefreshing = false;

        private class MyBroadcastReceiver : BroadcastReceiver
        {
            private WeakReference<Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation> weakNavigation;

            public MyBroadcastReceiver(Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation navigation)
            {
                weakNavigation = new WeakReference<Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation>(navigation);
            }

            public override void OnReceive(Context context, Intent intent)
            {
                if (weakNavigation.TryGetTarget(out var navigation))
                {
                    navigation.StopNavigation();
                }
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_mock_navigation);
            Cheeseknife.Bind(this);
            routeRefresh = new RouteRefresh(Mapbox.Mapboxsdk.Mapbox.AccessToken, this);

            mapView.OnCreate(savedInstanceState);
            mapView.GetMapAsync(this);

            Context context = ApplicationContext;
            CustomNavigationNotification customNotification = new CustomNavigationNotification(context);
            MapboxNavigationOptions options = MapboxNavigationOptions.InvokeBuilder()
              .NavigationNotification(customNotification)
              .Build();

            navigation = new Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation(this, Mapbox.Mapboxsdk.Mapbox.AccessToken, options);

            var builder = new RouteMilestone.Builder();
            builder.SetIdentifier(BEGIN_ROUTE_MILESTONE);
            builder.SetInstruction(new BeginRouteInstruction());
            builder.SetTrigger(
                Trigger.All(
                  Trigger.Lt(TriggerProperty.StepIndex, 3),
                  Trigger.Gt(TriggerProperty.StepDistanceTotalMeters, 200),
                  Trigger.Gte(TriggerProperty.StepDistanceTraveledMeters, 75)
                )
              );

            navigation.AddMilestone(builder.Build());
            customNotification.Register(new MyBroadcastReceiver(navigation), context);
        }

        [OnClick(Resource.Id.startRouteButton)]
        public void OnStartRouteClick(object sender, EventArgs args)
        {
            bool isValidNavigation = navigation != null;
            bool isValidRoute = route != null && (double)route.Distance() > TWENTY_FIVE_METERS;
            if (isValidNavigation && isValidRoute)
            {
                // Hide the start button
                startRouteButton.Visibility = ViewStates.Invisible;

                // Attach all of our navigation listeners.
                navigation.AddNavigationEventListener(this);
                navigation.AddProgressChangeListener(this);
                navigation.AddMilestoneEventListener(this);
                navigation.AddOffRouteListener(this);

                ((ReplayRouteLocationEngine)locationEngine).Assign(route);
                navigation.LocationEngine = locationEngine;
                mapboxMap.LocationComponent.LocationComponentEnabled = true;
                navigation.StartNavigation(route);
                mapboxMap.RemoveOnMapClickListener(this);
            }
        }

        [OnClick(Resource.Id.newLocationFab)]
        public void OnNewLocationClick(object sender, EventArgs args)
        {
            NewOrigin();
        }

        private void NewOrigin()
        {
            if (mapboxMap != null)
            {
                LatLng latLng = Utils.GetRandomLatLng(new double[] { -77.1825, 38.7825, -76.9790, 39.0157 });
                ((ReplayRouteLocationEngine)locationEngine).AssignLastLocation(
                  Point.FromLngLat(latLng.Longitude, latLng.Latitude)
                );
                mapboxMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(latLng, 12));
            }
        }

        public void OnError(RefreshError p0)
        {
            isRefreshing = false;
        }

        public bool OnMapClick(LatLng p0)
        {
            var point = p0;

            if (destination == null)
            {
                destination = Point.FromLngLat(point.Longitude, point.Latitude);
            }
            else if (waypoint == null)
            {
                waypoint = Point.FromLngLat(point.Longitude, point.Latitude);
            }
            else
            {
                Toast.MakeText(this, "Only 2 waypoints supported", ToastLength.Long).Show();
            }

            var marker = new MarkerOptions();
            marker.SetPosition(point);
            mapboxMap.AddMarker(marker);
            CalculateRoute();
            return false;
        }

        private void CalculateRoute()
        {
            locationEngine.GetLastLocation(new MyLocationEngineCallback((result) => FindRouteWith(result)));
        }

        private void FindRouteWith(LocationEngineResult result)
        {
            Location userLocation = result.LastLocation;
            if (userLocation == null)
            {
                Timber.D("calculateRoute: User location is null, therefore, origin can't be set.");
                return;
            }
            Point origin = Point.FromLngLat(userLocation.Longitude, userLocation.Latitude);
            if (TurfMeasurement.Distance(origin, destination, TurfConstants.UnitMeters) < 50)
            {
                startRouteButton.Visibility = ViewStates.Gone;
                return;
            }

            NavigationRoute.Builder navigationRouteBuilder = NavigationRoute.InvokeBuilder(this)
                .AccessToken(Mapbox.Mapboxsdk.Mapbox.AccessToken);
            navigationRouteBuilder.Origin(origin);
            navigationRouteBuilder.Destination(destination);
            if (waypoint != null)
            {
                navigationRouteBuilder.AddWaypoint(waypoint);
            }
            navigationRouteBuilder.EnableRefresh(true);
            navigationRouteBuilder.Build().GetRoute(new GetRouteCallback((response) =>
            {
                if (response != null && response.Routes().Any())
                {
                    route = response.Routes()[0];
                    navigationMapRoute.AddRoutes(response.Routes());
                    startRouteButton.Visibility = ViewStates.Visible;
                }
            }));
        }

        public void OnMapReady(MapboxMap p0)
        {
            this.mapboxMap = p0;
            this.mapboxMap.AddOnMapClickListener(this);

            mapboxMap.SetStyle(Style.MapboxStreets, new MapboxMapSetStyleListener((style) =>
            {
                LocationComponent locationComponent = mapboxMap.LocationComponent;
                locationComponent.ActivateLocationComponent(this, style);
                locationComponent.RenderMode = RenderMode.Gps;
                locationComponent.LocationComponentEnabled = false;
                navigationMapRoute = new NavigationMapRoute(navigation, mapView, mapboxMap);
                Snackbar.Make(FindViewById<ConstraintLayout>(Resource.Id.container), "Tap map to place waypoint",
                  BaseTransientBottomBar.LengthLong).Show();
                locationEngine = new ReplayRouteLocationEngine();
                NewOrigin();
            }));
        }

        public void OnMilestoneEvent(RouteProgress p0, string p1, Milestone p2)
        {
            Timber.D("Milestone Event Occurred with id: %d", p2.Identifier);
            Timber.D("Voice instruction: %s", p1);
        }

        public void OnProgressChange(Location p0, RouteProgress p1)
        {
            mapboxMap.LocationComponent.ForceLocationUpdate(p0);
            if (!isRefreshing)
            {
                isRefreshing = true;
                routeRefresh.Refresh(p1);
            }
            Timber.D("onProgressChange: fraction of route traveled: %f", p1.FractionTraveled());
        }

        protected override void OnResume()
        {
            base.OnResume();
            mapView.OnResume();
        }

        protected override void OnPause()
        {
            base.OnPause();
            mapView.OnPause();
        }

        protected override void OnStart()
        {
            base.OnStart();
            mapView.OnStart();
        }

        protected override void OnStop()
        {
            base.OnStop();
            mapView.OnStop();
        }

        public override void OnLowMemory()
        {
            base.OnLowMemory();
            mapView.OnLowMemory();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            navigation.OnDestroy();
            if (mapboxMap != null)
            {
                mapboxMap.RemoveOnMapClickListener(this);
            }
            mapView.OnDestroy();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            mapView.OnSaveInstanceState(outState);
        }

        public void OnRefresh(DirectionsRoute p0)
        {
            navigation.StartNavigation(p0);
            isRefreshing = false;
        }

        public void OnRunning(bool p0)
        {
            if (p0)
            {
                Timber.D("onRunning: Started");
            }
            else
            {
                Timber.D("onRunning: Stopped");
            }
        }

        public void UserOffRoute(Location p0)
        {
            Toast.MakeText(this, "off-route called", ToastLength.Long).Show();
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

        private class MyLocationEngineCallback : Java.Lang.Object, ILocationEngineCallback
        {
            Action<LocationEngineResult> action;

            public MyLocationEngineCallback(Action<LocationEngineResult> action)
            {
                this.action = action;
            }

            public void OnFailure(Java.Lang.Exception p0)
            {
                Timber.E(p0);
            }

            public void OnSuccess(Java.Lang.Object p0)
            {
                var result = p0 as LocationEngineResult;
                action.Invoke(result);
            }
        }

        private class GetRouteCallback : Java.Lang.Object, Square.Retrofit2.ICallback
        {
            Action<DirectionsResponse> successAction;

            public GetRouteCallback(Action<DirectionsResponse> successAction)
            {
                this.successAction = successAction;
            }

            public void OnFailure(ICall p0, Throwable p1)
            {
                Timber.E(p1, "onFailure: navigation.getRoute()");
            }

            public void OnResponse(ICall p0, Response p1)
            {
                Timber.D("Url: %s", p0.Request().Url().ToString());
                var body = p1.Body() as DirectionsResponse;
                successAction.Invoke(body);
            }
        }

        private class BeginRouteInstruction : Instruction
        {
            public override string BuildInstruction(RouteProgress p0)
            {
                return "Have a safe trip!";
            }
        }
    }
}
