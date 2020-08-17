
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Transitions;
using CheeseBind;
using Java.IO;
using Java.Util;
using Mapbox.Android.Core.Location;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Geojson;
using Mapbox.Mapboxsdk.Camera;
using Mapbox.Mapboxsdk.Geometry;
using Mapbox.Mapboxsdk.Location.Modes;
using Mapbox.Mapboxsdk.Maps;
using Mapbox.Services.Android.Navigation.UI.V5.Camera;
using Mapbox.Services.Android.Navigation.UI.V5.Instruction;
using Mapbox.Services.Android.Navigation.UI.V5.Map;
using Mapbox.Services.Android.Navigation.UI.V5.Voice;
using Mapbox.Services.Android.Navigation.V5.Milestone;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Mapbox.Services.Android.Navigation.V5.Offroute;
using Mapbox.Services.Android.Navigation.V5.Routeprogress;
using Square.OkHttp3;

namespace MapboxNavigation.UI.Droid.TestApp.Activity.NavigationUI
{
    [Activity(Label = "ComponentNavigationActivity")]
    public class ComponentNavigationActivity : HistoryActivity, IOnMapReadyCallback, MapboxMap.IOnMapLongClickListener,
        IProgressChangeListener, IMilestoneEventListener, IOffRouteListener
    {
        private static int FIRST = 0;
        private static int ONE_HUNDRED_MILLISECONDS = 100;
        private static int BOTTOMSHEET_PADDING_MULTIPLIER = 4;
        private static int TWO_SECONDS_IN_MILLISECONDS = 2000;
        private static double BEARING_TOLERANCE = 90d;
        private static string LONG_PRESS_MAP_MESSAGE = "Long press the map to select a destination.";
        private static string SEARCHING_FOR_GPS_MESSAGE = "Searching for GPS...";
        private static string COMPONENT_NAVIGATION_INSTRUCTION_CACHE = "component-navigation-instruction-cache";
        private static long TEN_MEGABYTE_CACHE_SIZE = 10 * 1024 * 1024;
        private static int ZERO_PADDING = 0;
        private static double DEFAULT_ZOOM = 12.0;
        private static double DEFAULT_TILT = 0d;
        private static double DEFAULT_BEARING = 0d;
        private static long UPDATE_INTERVAL_IN_MILLISECONDS = 1000;
        private static long FASTEST_UPDATE_INTERVAL_IN_MILLISECONDS = 500;

        [BindView(Resource.Id.componentNavigationLayout)]
        ConstraintLayout navigationLayout;

        [BindView(Resource.Id.mapView)]
        MapView mapView;

        [BindView(Resource.Id.instructionView)]
        InstructionView instructionView;

        [BindView(Resource.Id.startNavigationFab)]
        FloatingActionButton startNavigationFab;

        [BindView(Resource.Id.cancelNavigationFab)]
        FloatingActionButton cancelNavigationFab;

        [BindView(Resource.Id.sendAnomalyFab)]
        FloatingActionButton sendAnomalyFab;

        private ComponentActivityLocationCallback callback;
        private ILocationEngine locationEngine;
        private Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation navigation;
        private NavigationSpeechPlayer speechPlayer;
        private NavigationMapboxMap navigationMap;
        private Location lastLocation;
        private DirectionsRoute route;
        private Point destination;
        private MapState mapState;

        private enum MapState
        {
            INFO,
            NAVIGATION
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // For styling the InstructionView
            SetTheme(Resource.Style.CustomInstructionView);
            base.OnCreate(savedInstanceState);

            callback = new ComponentActivityLocationCallback(this);

            SetContentView(Resource.Layout.activity_component_navigation);
            Cheeseknife.Bind(this);
            mapView.OnCreate(savedInstanceState);

            // Will call onMapReady
            mapView.GetMapAsync(this);
        }

        public bool OnMapLongClick(LatLng p0)
        {
            var point = p0;

            // Only reverse geocode while we are not in navigation
            if (mapState.Equals(MapState.NAVIGATION))
            {
                return false;
            }

            // Fetch the route with this given point
            destination = Point.FromLngLat(point.Longitude, point.Latitude);
            CalculateRouteWith(destination, false);

            // Clear any existing markers and add new one
            navigationMap.ClearMarkers();
            navigationMap.AddMarker(this, destination);

            // Update camera to new destination
            MoveCameraToInclude(destination);
            Vibrate();
            return false;
        }

        [OnClick(Resource.Id.startNavigationFab)]
        public void OnStartNavigationClick(object sender, EventArgs args)
        {
            var floatingActionButton = sender as FloatingActionButton;
            floatingActionButton.Hide();
            QuickStartNavigation();
        }

        [OnClick(Resource.Id.cancelNavigationFab)]
        public void OnCancelNavigationClick(object sender, EventArgs args)
        {
            var floatingActionButton = sender as FloatingActionButton;

            navigationMap.UpdateCameraTrackingMode(NavigationCamera.NavigationTrackingModeNone);
            // Transition to info state
            mapState = MapState.INFO;

            floatingActionButton.Hide();

            // Hide the InstructionView
            TransitionManager.BeginDelayedTransition(navigationLayout);
            instructionView.Visibility = ViewStates.Invisible;

            // Reset map camera and pitch
            ResetMapAfterNavigation();

            // Add back regular location listener
            AddLocationEngineListener();
        }

        [OnClick(Resource.Id.sendAnomalyFab)]
        public void OnSendAnomalyClick(object sender, EventArgs args)
        {
            AddEventToHistoryFile("anomaly");
        }

        public void OnMapReady(MapboxMap p0)
        {
            var mapboxMap = p0;
            mapboxMap.SetStyle(new Style.Builder().FromUrl(GetString(Resource.String.navigation_guidance_day)), new MapboxMapSetStyleListener((Style) =>
            {
                mapState = MapState.INFO;
                navigationMap = new NavigationMapboxMap(mapView, mapboxMap);

                // For Location updates
                InitializeLocationEngine();

                // For navigation logic / processing
                InitializeNavigation(mapboxMap);
                navigationMap.UpdateCameraTrackingMode(NavigationCamera.NavigationTrackingModeNone);
                navigationMap.UpdateLocationLayerRenderMode(RenderMode.Gps);

                // For voice instructions
                InitializeSpeechPlayer();
            }));
        }

        /*
        * Navigation listeners
        */

        public void OnMilestoneEvent(RouteProgress p0, string p1, Milestone p2)
        {
            PlayAnnouncement(p2);

            // Update InstructionView banner instructions
            instructionView.UpdateBannerInstructionsWith(p2);
        }

        public void OnProgressChange(Location p0, RouteProgress p1)
        {
            var location = p0;
            var routeProgress = p1;

            // Cache "snapped" Locations for re-route Directions API requests
            UpdateLocation(location);

            // Update InstructionView data from RouteProgress
            instructionView.UpdateDistanceWith(routeProgress);
        }

        public void UserOffRoute(Location p0)
        {
            CalculateRouteWith(destination, true);
        }

        /*
       * Activity lifecycle methods
       */
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
            if (navigationMap != null)
            {
                navigationMap.OnStart();
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            mapView.OnSaveInstanceState(outState);
        }

        protected override void OnStop()
        {
            base.OnStop();
            mapView.OnStop();
            if (navigationMap != null)
            {
                navigationMap.OnStop();
            }
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

            // Ensure proper shutdown of the SpeechPlayer
            if (speechPlayer != null)
            {
                speechPlayer.OnDestroy();
            }

            // Prevent leaks
            RemoveLocationEngineListener();

            ((DynamicCamera)navigation.CameraEngine).ClearMap();
            // MapboxNavigation will shutdown the LocationEngine
            navigation.OnDestroy();
        }

        void CheckFirstUpdate(Location location)
        {
            if (lastLocation == null)
            {
                MoveCameraTo(location);
                // Allow navigationMap clicks now that we have the current Location
                navigationMap.RetrieveMap().AddOnMapLongClickListener(this);
                ShowSnackbar(LONG_PRESS_MAP_MESSAGE, BaseTransientBottomBar.LengthLong);
            }
        }

        void UpdateLocation(Location location)
        {
            lastLocation = location;
            navigationMap.UpdateLocation(location);
        }

        private void InitializeSpeechPlayer()
        {
            System.String english = Locale.Us.Language;
            Cache cache = new Cache(new File(Application.CacheDir, COMPONENT_NAVIGATION_INSTRUCTION_CACHE),
              TEN_MEGABYTE_CACHE_SIZE);
            VoiceInstructionLoader voiceInstructionLoader = new VoiceInstructionLoader(Application,
              Mapbox.Mapboxsdk.Mapbox.AccessToken, cache);
            SpeechPlayerProvider speechPlayerProvider = new SpeechPlayerProvider(Application, english, true,
              voiceInstructionLoader);
            speechPlayer = new NavigationSpeechPlayer(speechPlayerProvider);
        }

        private void InitializeLocationEngine()
        {
            locationEngine = LocationEngineProvider.GetBestLocationEngine(ApplicationContext);
            LocationEngineRequest request = BuildEngineRequest();
            locationEngine.RequestLocationUpdates(request, callback, null);
            ShowSnackbar(SEARCHING_FOR_GPS_MESSAGE, BaseTransientBottomBar.LengthShort);
        }

        private void InitializeNavigation(MapboxMap mapboxMap)
        {
            navigation = new Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation(this, Mapbox.Mapboxsdk.Mapbox.AccessToken);
            navigation.LocationEngine = locationEngine;
            sendAnomalyFab.Show();
            navigation.CameraEngine = new DynamicCamera(mapboxMap);
            navigation.AddProgressChangeListener(this);
            navigation.AddMilestoneEventListener(this);
            navigation.AddOffRouteListener(this);
            navigationMap.AddProgressChangeListener(navigation);
            AddNavigationForHistory(navigation);
        }

        private void ShowSnackbar(System.String text, int duration)
        {
            Snackbar.Make(navigationLayout, text, duration).Show();
        }

        private void PlayAnnouncement(Milestone milestone)
        {
            if (milestone is VoiceInstructionMilestone) {
                SpeechAnnouncement announcement = SpeechAnnouncement.InvokeBuilder()
                  .VoiceInstructionMilestone((VoiceInstructionMilestone)milestone)
                  .Build();
                speechPlayer.Play(announcement);
            }
        }

        private void MoveCameraTo(Location location)
        {
            CameraPosition cameraPosition = BuildCameraPositionFrom(location, location.Bearing);
            navigationMap.RetrieveMap().AnimateCamera(
              CameraUpdateFactory.NewCameraPosition(cameraPosition), TWO_SECONDS_IN_MILLISECONDS
            );
        }

        private void MoveCameraToInclude(Point destination)
        {
            LatLng origin = new LatLng(lastLocation);
            LatLngBounds bounds = new LatLngBounds.Builder()
              .Include(origin)
              .Include(new LatLng(destination.Latitude(), destination.Longitude()))
              .Build();
            var resources = Resources;
            int routeCameraPadding = (int)resources.GetDimension(Resource.Dimension.component_navigation_route_camera_padding);
            int[] padding = { routeCameraPadding, routeCameraPadding, routeCameraPadding, routeCameraPadding };
            CameraPosition cameraPosition = navigationMap.RetrieveMap().GetCameraForLatLngBounds(bounds, padding);
            navigationMap.RetrieveMap().AnimateCamera(
              CameraUpdateFactory.NewCameraPosition(cameraPosition), TWO_SECONDS_IN_MILLISECONDS
            );
        }

        private void MoveCameraOverhead()
        {
            if (lastLocation == null)
            {
                return;
            }
            CameraPosition cameraPosition = BuildCameraPositionFrom(lastLocation, DEFAULT_BEARING);
            navigationMap.RetrieveMap().AnimateCamera(
              CameraUpdateFactory.NewCameraPosition(cameraPosition), TWO_SECONDS_IN_MILLISECONDS
            );
        }

        private ICameraUpdate CameraOverheadUpdate()
        {
            if (lastLocation == null)
            {
                return null;
            }
            CameraPosition cameraPosition = BuildCameraPositionFrom(lastLocation, DEFAULT_BEARING);
            return CameraUpdateFactory.NewCameraPosition(cameraPosition);
        }

        private CameraPosition BuildCameraPositionFrom(Location location, double bearing)
        {
            return new CameraPosition.Builder()
              .Zoom(DEFAULT_ZOOM)
              .Target(new LatLng(location.Latitude, location.Longitude))
              .Bearing(bearing)
              .Tilt(DEFAULT_TILT)
              .Build();
        }

        private void AdjustMapPaddingForNavigation()
        {
            Resources resources = Resources;
            int mapViewHeight = mapView.Height;
            int bottomSheetHeight = (int)resources.GetDimension(Resource.Dimension.component_navigation_bottomsheet_height);
            int topPadding = mapViewHeight - (bottomSheetHeight * BOTTOMSHEET_PADDING_MULTIPLIER);
            navigationMap.RetrieveMap().SetPadding(ZERO_PADDING, topPadding, ZERO_PADDING, ZERO_PADDING);
        }

        private void ResetMapAfterNavigation()
        {
            navigationMap.RemoveRoute();
            navigationMap.ClearMarkers();
            AddEventToHistoryFile("cancel_navigation");
            ExecuteStoreHistoryTask();
            navigation.StopNavigation();
            MoveCameraOverhead();
        }

        private void RemoveLocationEngineListener()
        {
            if (locationEngine != null)
            {
                locationEngine.RemoveLocationUpdates(callback);
            }
        }

        private void AddLocationEngineListener()
        {
            if (locationEngine != null)
            {
                LocationEngineRequest request = BuildEngineRequest();
                locationEngine.RequestLocationUpdates(request, callback, null);
            }
        }

        private void CalculateRouteWith(Point destination, bool isOffRoute)
        {
            Point origin = Point.FromLngLat(lastLocation.Longitude, lastLocation.Latitude);
            double bearing = (double)lastLocation.Bearing;
            NavigationRoute.InvokeBuilder(this)
              .AccessToken(Mapbox.Mapboxsdk.Mapbox.AccessToken)
              .Origin(origin, (Java.Lang.Double)bearing, (Java.Lang.Double)BEARING_TOLERANCE)
              .Destination(destination)
              .Build()
              .GetRoute(new SimplifiedCallback((response) =>
              {
                  HandleRoute(response, isOffRoute);
              }));
        }

        private void QuickStartNavigation()
        {
            // Transition to navigation state
            mapState = MapState.NAVIGATION;

            cancelNavigationFab.Show();

            // Show the InstructionView
            TransitionManager.BeginDelayedTransition(navigationLayout);
            instructionView.Visibility = ViewStates.Visible;

            AdjustMapPaddingForNavigation();
            // Updates camera with last location before starting navigating,
            // making sure the route information is updated
            // by the time the initial camera tracking animation is fired off
            // Alternatively, NavigationMapboxMap#startCamera could be used here,
            // centering the map camera to the beginning of the provided route
            navigationMap.ResumeCamera(lastLocation);
            navigation.StartNavigation(route);
            AddEventToHistoryFile("start_navigation");

            // Location updates will be received from ProgressChangeListener
            RemoveLocationEngineListener();

            // TODO remove example usage
            navigationMap.ResetCameraPositionWith(NavigationCamera.NavigationTrackingModeGps);
            ICameraUpdate cameraUpdate = CameraOverheadUpdate();
            if (cameraUpdate != null)
            {
                NavigationCameraUpdate navUpdate = new NavigationCameraUpdate(cameraUpdate);
                navigationMap.RetrieveCamera().Update(navUpdate);
            }
        }

        private void HandleRoute(Square.Retrofit2.Response response, bool isOffRoute)
        {
            var body = response.Body() as DirectionsResponse;
            if (body == null) return;

            List<DirectionsRoute> routes = body.Routes().ToList();
            if (routes.Any())
            {
                route = routes[FIRST];
                navigationMap.DrawRoute(route);
                if (isOffRoute)
                {
                    navigation.StartNavigation(route);
                }
                else
                {
                    startNavigationFab.Show();
                }
            }
        }

        private void Vibrate()
        {
            Vibrator vibrator = (Vibrator)GetSystemService(Context.VibratorService);
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                vibrator.Vibrate(VibrationEffect.CreateOneShot(ONE_HUNDRED_MILLISECONDS, VibrationEffect.DefaultAmplitude));
            }
            else
            {
                vibrator.Vibrate(ONE_HUNDRED_MILLISECONDS);
            }
        }

        private void AddEventToHistoryFile(string type)
        {
            double secondsFromEpoch = new Java.Util.Date().Time / 1000.0;
            navigation.AddHistoryEvent(type, secondsFromEpoch.ToString());
        }

        private LocationEngineRequest BuildEngineRequest()
        {
            return new LocationEngineRequest.Builder(UPDATE_INTERVAL_IN_MILLISECONDS)
              .SetPriority(LocationEngineRequest.PriorityHighAccuracy)
              .SetFastestInterval(FASTEST_UPDATE_INTERVAL_IN_MILLISECONDS)
              .Build();
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

        private class ComponentActivityLocationCallback : Java.Lang.Object, ILocationEngineCallback
        {
            private WeakReference<ComponentNavigationActivity> activityWeakReference;

            public ComponentActivityLocationCallback(ComponentNavigationActivity activityWeakReference)
            {
                this.activityWeakReference = new WeakReference<ComponentNavigationActivity>(activityWeakReference);
            }

            public void OnFailure(Java.Lang.Exception p0)
            {
                throw new NotImplementedException();
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
                    activity.CheckFirstUpdate(location);
                    activity.UpdateLocation(location);
                }
            }
        }
    }
}
