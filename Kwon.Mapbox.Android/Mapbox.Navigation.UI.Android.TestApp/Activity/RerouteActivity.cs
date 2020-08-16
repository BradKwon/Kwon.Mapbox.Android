
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using CheeseBind;
using Java.Lang;
using Mapbox.Android.Core.Location;
using Mapbox.Android.Gestures;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Geojson;
using Mapbox.Mapboxsdk.Annotations;
using Mapbox.Mapboxsdk.Camera;
using Mapbox.Mapboxsdk.Geometry;
using Mapbox.Mapboxsdk.Location;
using Mapbox.Mapboxsdk.Location.Modes;
using Mapbox.Mapboxsdk.Maps;
using Mapbox.Services.Android.Navigation.UI.V5.Instruction;
using Mapbox.Services.Android.Navigation.V5.Location.Replay;
using Mapbox.Services.Android.Navigation.V5.Milestone;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Mapbox.Services.Android.Navigation.V5.Offroute;
using Mapbox.Services.Android.Navigation.V5.Routeprogress;
using Square.Retrofit2;
using TimberLog;
using Point = Mapbox.Geojson.Point;

namespace MapboxNavigation.UI.Droid.TestApp.Activity
{
    [Activity(Label = "RerouteActivity")]
    public class RerouteActivity : HistoryActivity, IOnMapReadyCallback, Square.Retrofit2.ICallback,
        MapboxMap.IOnMapClickListener, INavigationEventListener, IOffRouteListener,
        IProgressChangeListener, IMilestoneEventListener
    {
        [BindView(Resource.Id.mapView)]
        MapView mapView;

        [BindView(Android.Resource.Id.Content)]
        View contentLayout;

        [BindView(Resource.Id.instructionView)]
        InstructionView instructionView;

        private Point origin = Point.FromLngLat(-0.358764, 39.494876);
        private Point destination = Point.FromLngLat(-0.383524, 39.497825);
        private Polyline polyline;

        private RerouteActivityLocationCallback callback;
        private Location lastLocation;
        private ReplayRouteLocationEngine mockLocationEngine;
        private Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation navigation;
        private MapboxMap mapboxMap;
        private bool running;
        private bool tracking;
        private bool wasInTunnel = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetTheme(Resource.Style.NavigationViewLight);
            base.OnCreate(savedInstanceState);

            callback = new RerouteActivityLocationCallback(this);

            SetContentView(Resource.Layout.activity_reroute);
            Cheeseknife.Bind(this);

            mapView.OnCreate(savedInstanceState);
            mapView.GetMapAsync(this);

            MapboxNavigationOptions options = MapboxNavigationOptions.InvokeBuilder().IsDebugLoggingEnabled(true).Build();
            navigation = new Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation(ApplicationContext, Mapbox.Mapboxsdk.Mapbox.AccessToken, options);
            navigation.AddNavigationEventListener(this);
            navigation.AddMilestoneEventListener(this);
            AddNavigationForHistory(navigation);

            instructionView.RetrieveSoundButton().Show();
            instructionView.RetrieveSoundButton().AddOnClickListener(new InstructionViewOnClickListener((v) =>
            {
                Toast.MakeText(this, "Sound button clicked!", ToastLength.Short).Show();
            }));
        }

        protected override void OnResume()
        {
            base.OnResume();
            mapView.OnResume();
        }

        protected override void OnStop()
        {
            base.OnStop();
            mapView.OnStop();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            mapView.OnSaveInstanceState(outState);
        }

        protected override void OnStart()
        {
            base.OnStart();
            mapView.OnStart();
        }

        protected override void OnPause()
        {
            base.OnPause();
            mapView.OnPause();
        }

        public override void OnLowMemory()
        {
            base.OnLowMemory();
            mapView.OnLowMemory();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            mapView.OnDestroy();
            ShutdownLocationEngine();
            ShutdownNavigation();
        }

        public void OnFailure(ICall p0, Throwable p1)
        {
            Timber.E(p1);
        }

        public bool OnMapClick(LatLng p0)
        {
            if (!running || mapboxMap == null || lastLocation == null)
            {
                return false;
            }

            var marker = new MarkerOptions();
            marker.SetPosition(p0);
            mapboxMap.AddMarker(marker);
            mapboxMap.RemoveOnMapClickListener(this);

            destination = Point.FromLngLat(p0.Longitude, p0.Latitude);
            ResetLocationEngine(destination);

            tracking = false;
            return false;
        }

        public void OnMapReady(MapboxMap p0)
        {
            this.mapboxMap = p0;
            this.mapboxMap.AddOnMapClickListener(this);
            mapboxMap.SetStyle(Style.Dark, new MapboxMapSetStyleListener((style) =>
            {
                LocationComponent locationComponent = mapboxMap.LocationComponent;
                locationComponent.ActivateLocationComponent(this, style);
                locationComponent.LocationComponentEnabled = true;
                locationComponent.RenderMode = RenderMode.Gps;

                mockLocationEngine = new ReplayRouteLocationEngine();
                GetRoute(origin, destination);
            }));
        }

        public void OnMilestoneEvent(RouteProgress p0, string p1, Milestone p2)
        {
            var instruction = p1;
            var milestone = p2;

            if (milestone is VoiceInstructionMilestone) {
                Snackbar.Make(contentLayout, instruction, Snackbar.LengthShort).Show();
            }
            instructionView.UpdateBannerInstructionsWith(milestone);
            Timber.D("onMilestoneEvent - Current Instruction: %s", instruction);
        }

        public void OnProgressChange(Location p0, RouteProgress p1)
        {
            var location = p0;
            var routeProgress = p1;

            bool isInTunnel = routeProgress.InTunnel();
            lastLocation = location;
            if (!wasInTunnel && isInTunnel)
            {
                wasInTunnel = true;
                Snackbar.Make(contentLayout, "Enter tunnel!", Snackbar.LengthShort).Show();
            }
            if (wasInTunnel && !isInTunnel)
            {
                wasInTunnel = false;
                Snackbar.Make(contentLayout, "Exit tunnel!", Snackbar.LengthShort).Show();
            }
            if (tracking)
            {
                mapboxMap.LocationComponent.ForceLocationUpdate(location);
                CameraPosition cameraPosition = new CameraPosition.Builder()
                  .Zoom(15)
                  .Target(new LatLng(location.Latitude, location.Longitude))
                  .Bearing(location.Bearing)
                  .Build();
                mapboxMap.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition), 2000);
            }
            instructionView.UpdateDistanceWith(routeProgress);
        }

        public void OnResponse(ICall p0, Response p1)
        {
            var call = p0;
            var response = p1;

            Timber.D(call.Request().Url().ToString());
            if (response.Body() != null)
            {
                var body = response.Body() as DirectionsResponse;

                if (body.Routes().Any())
                {
                    DirectionsRoute route = body.Routes()[0];
                    DrawRoute(route);
                    ResetLocationEngine(route);
                    navigation.StartNavigation(route);
                    mapboxMap.AddOnMapClickListener(this);
                    tracking = true;
                }
            }
        }

        public void OnRunning(bool p0)
        {
            this.running = p0;
            if (running)
            {
                navigation.AddOffRouteListener(this);
                navigation.AddProgressChangeListener(this);
            }
        }

        public void UserOffRoute(Location p0)
        {
            origin = Point.FromLngLat(lastLocation.Longitude, lastLocation.Latitude);
            GetRoute(origin, destination);
            Snackbar.Make(contentLayout, "User Off Route", Snackbar.LengthShort).Show();
            var marker = new MarkerOptions();
            marker.SetPosition(new LatLng(p0.Latitude, p0.Longitude));
            mapboxMap.AddMarker(marker);
        }

        void UpdateLocation(Location location)
        {
            if (!tracking)
            {
                mapboxMap.LocationComponent.ForceLocationUpdate(location);
            }
        }

        private void GetRoute(Point origin, Point destination)
        {
            NavigationRoute.InvokeBuilder(this)
              .Origin(origin)
              .Destination(destination)
              .AccessToken(Mapbox.Mapboxsdk.Mapbox.AccessToken)
              .Build().GetRoute(this);
        }

        private void DrawRoute(DirectionsRoute route)
        {
            List<LatLng> points = new List<LatLng>();
            List<Point> coords = LineString.FromPolyline(route.Geometry(), Mapbox.Core.Constants.Constants.Precision6).Coordinates().ToList();

            foreach (Point point in coords)
            {
                points.Add(new LatLng(point.Latitude(), point.Longitude()));
            }

            if (points.Any())
            {
                if (polyline != null)
                {
                    mapboxMap.RemovePolyline(polyline);
                }

                Java.Util.ArrayList all = new Java.Util.ArrayList(points);
                polyline = mapboxMap.AddPolyline(new PolylineOptions()
                  .AddAll(all)
                  .InvokeColor(Color.ParseColor(GetString(Resource.String.blue)))
                  .InvokeWidth(5));
            }
        }

        private void ResetLocationEngine(Point point)
        {
            mockLocationEngine.MoveTo(point);
            navigation.LocationEngine = mockLocationEngine;
        }

        private void ResetLocationEngine(DirectionsRoute directionsRoute)
        {
            mockLocationEngine.Assign(directionsRoute);
            navigation.LocationEngine = mockLocationEngine;
        }

        private void ShutdownLocationEngine()
        {
            if (mockLocationEngine != null)
            {
                mockLocationEngine.RemoveLocationUpdates(callback);
            }
        }

        private void ShutdownNavigation()
        {
            navigation.RemoveNavigationEventListener(this);
            navigation.RemoveProgressChangeListener(this);
            navigation.OnDestroy();
        }

        private class InstructionViewOnClickListener : Java.Lang.Object, View.IOnClickListener
        {
            Action<View> clickAction;

            public InstructionViewOnClickListener(Action<View> action)
            {
                clickAction = action;
            }

            public void OnClick(View v)
            {
                clickAction.Invoke(v);
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

        private class RerouteActivityLocationCallback : Java.Lang.Object, ILocationEngineCallback
        {
            private WeakReference<RerouteActivity> activityWeakReference;

            public RerouteActivityLocationCallback(RerouteActivity activity)
            {
                activityWeakReference = new WeakReference<RerouteActivity>(activity);
            }

            public void OnFailure(Java.Lang.Exception p0)
            {
                Timber.E(p0);
            }

            public void OnSuccess(Java.Lang.Object p0)
            {
                if (activityWeakReference.TryGetTarget(out var activity))
                {
                    var result = p0 as LocationEngineResult;

                    Location location = result.LastLocation;
                    if (location == null)
                    {
                        return;
                    }
                    activity.UpdateLocation(location);
                }
            }
        }
    }
}
