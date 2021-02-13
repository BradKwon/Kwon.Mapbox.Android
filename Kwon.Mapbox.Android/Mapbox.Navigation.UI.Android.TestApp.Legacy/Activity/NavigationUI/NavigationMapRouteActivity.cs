
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using CheeseBind;
using Java.Lang;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Geojson;
using Mapbox.Mapboxsdk.Annotations;
using Mapbox.Mapboxsdk.Geometry;
using Mapbox.Mapboxsdk.Location;
using Mapbox.Mapboxsdk.Location.Modes;
using Mapbox.Mapboxsdk.Maps;
using Mapbox.Services.Android.Navigation.UI.V5.Route;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Square.Retrofit2;
using TimberLog;

namespace MapboxNavigation.UI.Droid.TestApp.Legacy.Activity.NavigationUI
{
    [Activity(Label = "NavigationMapRouteActivity")]
    public class NavigationMapRouteActivity : AppCompatActivity, IOnMapReadyCallback, MapboxMap.IOnMapLongClickListener,
        Square.Retrofit2.ICallback
    {
        private static int ONE_HUNDRED_MILLISECONDS = 100;

        [BindView(Resource.Id.mapView)]
        MapView mapView;
        [BindView(Resource.Id.routeLoadingProgressBar)]
        ProgressBar routeLoading;
        [BindView(Resource.Id.fabRemoveRoute)]
        FloatingActionButton fabRemoveRoute;

        private MapboxMap mapboxMap;
        private NavigationMapRoute navigationMapRoute;
        private StyleCycle styleCycle = new StyleCycle();

        private Marker originMarker;
        private Marker destinationMarker;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_navigation_map_route);
            Cheeseknife.Bind(this);

            mapView.OnCreate(savedInstanceState);
            mapView.GetMapAsync(this);
        }

        [OnClick(Resource.Id.fabStyles)]
        public void OnStyleFabClick(object sender, EventArgs args)
        {
            if (mapboxMap != null)
            {
                mapboxMap.SetStyle(styleCycle.GetNextStyle());
            }
        }

        [OnClick(Resource.Id.fabRemoveRoute)]
        public void OnRemoveRouteClick(object sender, EventArgs args)
        {
            RemoveRouteAndMarkers();
            fabRemoveRoute.Visibility = ViewStates.Invisible;
        }


        public void OnFailure(ICall p0, Throwable p1)
        {
            Timber.E(p1);
        }

        public bool OnMapLongClick(LatLng p0)
        {
            HandleClicked(p0);
            return true;
        }

        public void OnMapReady(MapboxMap p0)
        {
            var mapboxMap = p0;
            this.mapboxMap = mapboxMap;
            mapboxMap.SetStyle(styleCycle.GetStyle(), new MapboxMapSetStyleListener((style) =>
            {
                InitializeLocationComponent(mapboxMap);
                navigationMapRoute = new NavigationMapRoute(null, mapView, mapboxMap);
                mapboxMap.AddOnMapLongClickListener(this);
                Snackbar.Make(mapView, "Long press to select route", Snackbar.LengthShort).Show();
            }));
        }

        public void OnResponse(ICall p0, Response p1)
        {
            var response = p1;
            var body = response.Body() as DirectionsResponse;

            if (response.IsSuccessful && body != null && body.Routes().Any())
            {
                List<DirectionsRoute> routes = body.Routes().ToList();
                navigationMapRoute.AddRoutes(routes);
                routeLoading.Visibility = ViewStates.Invisible;
                fabRemoveRoute.Visibility = ViewStates.Visible;
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            mapView.OnResume();
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
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            mapView.OnSaveInstanceState(outState);
        }

        private void InitializeLocationComponent(MapboxMap mapboxMap)
        {
            LocationComponent locationComponent = mapboxMap.LocationComponent;
            locationComponent.ActivateLocationComponent(this, mapboxMap.Style);
            locationComponent.LocationComponentEnabled = true;
            locationComponent.RenderMode = RenderMode.Compass;
            locationComponent.CameraMode = CameraMode.Tracking;
            locationComponent.ZoomWhileTracking(10d);
        }

        private void HandleClicked(LatLng point)
        {
            Vibrate();
            if (originMarker == null)
            {
                var marker = new MarkerOptions();
                marker.SetPosition(point);

                originMarker = mapboxMap.AddMarker(marker);
                Snackbar.Make(mapView, "Origin selected", Snackbar.LengthShort).Show();
            }
            else if (destinationMarker == null)
            {
                var marker = new MarkerOptions();
                marker.SetPosition(point);
                destinationMarker = mapboxMap.AddMarker(marker);
                Point originPoint = Point.FromLngLat(
                  originMarker.Position.Longitude, originMarker.Position.Latitude);
                Point destinationPoint = Point.FromLngLat(
                  destinationMarker.Position.Longitude, destinationMarker.Position.Latitude);
                Snackbar.Make(mapView, "Destination selected", Snackbar.LengthShort).Show();
                FindRoute(originPoint, destinationPoint);
                routeLoading.Visibility = ViewStates.Visible;
            }
        }

        private void Vibrate()
        {
            Vibrator vibrator = (Vibrator)GetSystemService(Context.VibratorService);
            if (vibrator == null)
            {
                return;
            }
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                vibrator.Vibrate(VibrationEffect.CreateOneShot(ONE_HUNDRED_MILLISECONDS, VibrationEffect.DefaultAmplitude));
            }
            else
            {
                vibrator.Vibrate(ONE_HUNDRED_MILLISECONDS);
            }
        }

        private void RemoveRouteAndMarkers()
        {
            mapboxMap.RemoveMarker(originMarker);
            originMarker = null;
            mapboxMap.RemoveMarker(destinationMarker);
            destinationMarker = null;
            navigationMapRoute.RemoveRoute();
        }

        public void FindRoute(Point origin, Point destination)
        {
            NavigationRoute.InvokeBuilder(this)
              .AccessToken(Mapbox.Mapboxsdk.Mapbox.AccessToken)
              .Origin(origin)
              .Destination(destination)
              .Alternatives((Java.Lang.Boolean)true)
              .Build()
              .GetRoute(this);
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

        private class StyleCycle
        {
            private static string[] STYLES = new string[] {
                Style.MapboxStreets,
                Style.Outdoors,
                Style.TrafficDay,
                Style.Dark,
                Style.SatelliteStreets
            };

            private int index;

            public string GetNextStyle()
            {
                index++;
                if (index == STYLES.Length)
                {
                    index = 0;
                }
                return GetStyle();
            }

            public string GetStyle()
            {
                return STYLES[index];
            }
        }
    }
}
