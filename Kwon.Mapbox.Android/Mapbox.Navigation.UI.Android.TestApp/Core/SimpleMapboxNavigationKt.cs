
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.ConstraintLayout.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Snackbar;
using Java.IO;
using Java.Lang;
using Java.Util;
using Mapbox.Android.Core.Location;
using Mapbox.Api.Directions.V5;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Geojson;
using Mapbox.Mapboxsdk.Annotations;
using Mapbox.Mapboxsdk.Camera;
using Mapbox.Mapboxsdk.Geometry;
using Mapbox.Mapboxsdk.Location;
using Mapbox.Mapboxsdk.Location.Modes;
using Mapbox.Mapboxsdk.Maps;
using Mapbox.Mapboxsdk.Plugins.Annotation;
using Mapbox.Navigation.Base.Options;
using Mapbox.Navigation.Base.Trip.Model;
using Mapbox.Navigation.Core.Directions.Session;
using Mapbox.Navigation.Core.Fasterroute;
using Mapbox.Navigation.Core.Replay;
using Mapbox.Navigation.Core.Replay.Route;
using Mapbox.Navigation.Core.Telemetry.Events;
using Mapbox.Navigation.Core.Trip.Session;
using Mapbox.Navigation.Ui.Camera;
using Mapbox.Navigation.Ui.Map;
using Mapbox.Navigation.Ui.Route;
using Mapbox.Navigation.Ui.Voice;
using Square.OkHttp3;
using TimberLog;

namespace MapboxNavigation.UI.Droid.TestApp.Core
{
    [Activity(Label = "SimpleMapboxNavigationKt")]
    public class SimpleMapboxNavigationKt : AppCompatActivity, IOnMapReadyCallback,
        Style.IOnStyleLoaded, IOnRouteSelectionChangeListener,
        IVoiceInstructionsObserver, IRouteProgressObserver, IRoutesObserver,
        IFasterRouteObserver, ITripSessionStateObserver
    {
        private const string VOICE_INSTRUCTION_CACHE = "voice-instruction-cache";
        private readonly long startTimeInMillis = 5000;
        private readonly int countdownInterval = 10;
        private float maxProgress;
        private ILocationEngineCallback locationEngineCallback;

        private MapboxMap mapboxMap;
        private LocationComponent locationComponent;
        private SymbolManager symbolManager;
        private List<DirectionsRoute> fasterRoutes;
        private DirectionsRoute originalRoute;

        private Mapbox.Navigation.Core.MapboxNavigation mapboxNavigation;
        private ILocationEngine localLocationEngine;
        private BottomSheetBehavior bottomSheetBehavior;
        private NavigationMapboxMap navigationMapboxMap;
        private NavigationSpeechPlayer speechPlayer;
        private MapboxReplayer mapboxReplayer;
        private MapView mapView;
        AppCompatButton startNavigation;
        ConstraintLayout container;
        ConstraintLayout bottomSheetFasterRoute;
        ProgressBar fasterRouteAcceptProgress;
        FasterRouteSelectionTimer fasterRouteSelectionTimer;

        public void OnMapReady(MapboxMap p0)
        {
            mapboxMap = p0;
            mapboxMap.MoveCamera(CameraUpdateFactory.ZoomTo(15.0));
            mapboxMap.AddOnMapLongClickListener(new MyOnMapLongClickListener(
                locationComponent, mapboxNavigation, originalRoute,
                navigationMapboxMap, symbolManager));
            mapboxMap.SetStyle(Style.MapboxStreets, this);
            InitializeSpeechPlayer();
        }

        void InitializeSpeechPlayer()
        {
            var cache = new Cache(new File(Application.CacheDir,
                VOICE_INSTRUCTION_CACHE), 10 * 1024 * 1024);
            var voiceInstructionLoader =
                new VoiceInstructionLoader(Application,
                    Utils.GetMapboxAccessToken(this), cache);
            var speechPlayerProvider =
                new SpeechPlayerProvider(Application, Locale.Us.Language,
                    true, voiceInstructionLoader);
            speechPlayer = new NavigationSpeechPlayer(speechPlayerProvider);
        }

        class FasterRouteSelectionTimer : CountDownTimer
        {
            private readonly Action<long> onTickAction;
            private readonly Action onFinishAction;

            FasterRouteSelectionTimer(long millisInFuture, long countDownInterval)
                : base(millisInFuture, countDownInterval)
            {
            }

            public FasterRouteSelectionTimer(long millisInFuture, long countDownInterval,
                Action<long> onTickAction, Action onFinishAction)
                    : this(millisInFuture, countDownInterval)
            {
                this.onTickAction = onTickAction;
                this.onFinishAction = onFinishAction;
            }

            public override void OnFinish()
            {
                onFinishAction?.Invoke();
            }

            public override void OnTick(long millisUntilFinished)
            {
                onTickAction?.Invoke(millisUntilFinished);
            }
        }

        class MyOnMapLongClickListener : Java.Lang.Object,
            MapboxMap.IOnMapLongClickListener, IRoutesRequestCallback
        {
            private readonly LocationComponent locationComponent;
            private readonly Mapbox.Navigation.Core.MapboxNavigation mapboxNavigation;
            private DirectionsRoute originalRoute;
            private readonly NavigationMapboxMap navigationMapboxMap;
            private readonly SymbolManager symbolManager;

            internal MyOnMapLongClickListener(LocationComponent locationComponent,
                Mapbox.Navigation.Core.MapboxNavigation mapboxNavigation,
                DirectionsRoute originalRoute,
                NavigationMapboxMap navigationMapboxMap,
                SymbolManager symbolManager)
            {
                this.locationComponent = locationComponent;
                this.mapboxNavigation = mapboxNavigation;
                this.originalRoute = originalRoute;
                this.navigationMapboxMap = navigationMapboxMap;
                this.symbolManager = symbolManager;
            }

            public bool OnMapLongClick(LatLng p0)
            {
                var location = locationComponent?.LastKnownLocation;
                if (location != null)
                {
                    var routeOptions = RouteOptions.InvokeBuilder()
                        .AccessToken(Utils.GetMapboxAccessToken(Application.Context))
                        .Coordinates(new List<Point> {
                            Point.FromLngLat(location.Longitude, location.Latitude),
                            null,
                            Point.FromLngLat(p0.Longitude, p0.Latitude)
                        })
                        .Alternatives((Java.Lang.Boolean)true)
                        .Profile(DirectionsCriteria.ProfileDrivingTraffic)
                        .Build();

                    mapboxNavigation.RequestRoutes(routeOptions, this);

                    symbolManager?.DeleteAll();
                    symbolManager?.Create(
                        new SymbolOptions()
                            .WithIconImage("marker")
                            .WithGeometry(Point.FromLngLat(p0.Longitude, p0.Latitude))
                    );
                }

                return false;
            }

            public void OnRoutesReady(IList<DirectionsRoute> routes)
            {
                originalRoute = routes[0];
                navigationMapboxMap.DrawRoutes(routes);
                Timber.D($"route request success {routes}");
            }

            public void OnRoutesRequestCanceled(RouteOptions routeOptions)
            {
                symbolManager?.DeleteAll();
                Timber.D("route request canceled");
            }

            public void OnRoutesRequestFailure(Throwable throwable, RouteOptions routeOptions)
            {
                symbolManager?.DeleteAll();
                Timber.E($"route request failure {throwable}");
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_simple_mapbox_navigation);

            maxProgress = startTimeInMillis / countdownInterval;
            locationEngineCallback = new MyLocationEngineCallback(this);
            fasterRoutes = new List<DirectionsRoute>();
            mapboxReplayer = new MapboxReplayer();
            fasterRouteSelectionTimer = new FasterRouteSelectionTimer(
                startTimeInMillis, countdownInterval,
                onTickAction: (millisUntilFinished) =>
                {
                    Timber.D($"FASTER_ROUTE: millisUntilFinished {millisUntilFinished}");
                    fasterRouteAcceptProgress.Progress =
                        (int)(maxProgress - millisUntilFinished / countdownInterval);
                },
                onFinishAction: () =>
                {
                    Timber.D("FASTER_ROUTE: finished");
                    fasterRoutes = new List<DirectionsRoute>();
                    bottomSheetBehavior.State = BottomSheetBehavior.StateCollapsed;
                });

            FindViewById<Button>(Resource.Id.btn_send_user_feedback).Click += (s, e) =>
                {
                    mapboxNavigation.PostUserFeedback(FeedbackEvent.GeneralIssue,
                        $"User feedback test at: ${DateTime.Now.ToShortTimeString()}",
                        FeedbackEvent.Ui, null, null, null);
                };

            FindViewById<Button>(Resource.Id.btn_add_original_route).Click += (s, e) =>
            {
                if (originalRoute != null)
                {
                    var routes = mapboxNavigation.Routes;

                    if (routes.Any())
                        routes.Insert(0, originalRoute);

                    mapboxNavigation.Routes = routes;
                }
            };

            FindViewById<Button>(Resource.Id.btn_clear_routes).Click += (s, e) =>
            {
                mapboxNavigation.Routes = new List<DirectionsRoute>();
            };

            fasterRouteAcceptProgress = FindViewById<ProgressBar>(
                Resource.Id.fasterRouteAcceptProgress);
            bottomSheetFasterRoute = FindViewById<ConstraintLayout>(
                Resource.Id.bottomSheetFasterRoute);
            container = FindViewById<ConstraintLayout>(Resource.Id.container);
            startNavigation = FindViewById<AppCompatButton>(Resource.Id.startNavigation);
            mapView = FindViewById<MapView>(Resource.Id.mapView);
            mapView.OnCreate(savedInstanceState);
            mapView.GetMapAsync(this);
            localLocationEngine = LocationEngineProvider.GetBestLocationEngine(this);

            var mapboxNavigationOptions = Mapbox.Navigation.Core.MapboxNavigation
                .DefaultNavigationOptionsBuilder(this, Utils.GetMapboxAccessToken(this));
            mapboxNavigation = GetMapboxNavigation(mapboxNavigationOptions);
            InitViews();
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

            mapboxNavigation.RegisterVoiceInstructionsObserver(this);
            mapboxNavigation.StartTripSession();

            mapboxNavigation.RegisterRouteProgressObserver(this);
            mapboxNavigation.RegisterRoutesObserver(this);
            mapboxNavigation.RegisterTripSessionStateObserver(this);
            mapboxNavigation.AttachFasterRouteObserver(this);
        }

        protected override void OnStop()
        {
            base.OnStop();
            mapView.OnStop();

            mapboxNavigation.UnregisterRouteProgressObserver(this);
            mapboxNavigation.UnregisterRoutesObserver(this);
            mapboxNavigation.UnregisterTripSessionStateObserver(this);
            mapboxNavigation.DetachFasterRouteObserver();
            StopLocationUpdates();

            if (!mapboxNavigation.Routes.Any() &&
                mapboxNavigation.TripSessionState == TripSessionState.Started)
            {
                // use this to kill the service and hide the notification when going into the background in the Free Drive state,
                // but also ensure to restart Free Drive when coming back from background by using the channel
                mapboxNavigation.UnregisterVoiceInstructionsObserver(this);
                mapboxNavigation.StopTripSession();
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

            mapboxReplayer.Finish();
            mapboxNavigation.UnregisterVoiceInstructionsObserver(this);
            mapboxNavigation.StopTripSession();
            mapboxNavigation.OnDestroy();

            speechPlayer.OnDestroy();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            mapView.OnSaveInstanceState(outState);

            // This is not the most efficient way to preserve the route on a device rotation.
            // This is here to demonstrate that this event needs to be handled in order to
            // redraw the route line after a rotation.
            if (originalRoute != null)
            {
                outState.PutString(Utils.PRIMARY_ROUTE_BUNDLE_KEY, originalRoute.ToJson());
            }
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
            originalRoute = Utils.GetRouteFromBundle(savedInstanceState);
        }

        void InitViews()
        {
            bottomSheetBehavior = BottomSheetBehavior.From(bottomSheetFasterRoute);
            bottomSheetBehavior.PeekHeight = 0;
            fasterRouteAcceptProgress.Max = (int)maxProgress;
        }

        Mapbox.Navigation.Core.MapboxNavigation GetMapboxNavigation(
            NavigationOptions.Builder builder)
        {
            Mapbox.Navigation.Core.MapboxNavigation mapboxNavigation = null;

            if (ShouldSimulateRoute())
            {
                builder.LocationEngine(new ReplayLocationEngine(mapboxReplayer));
                mapboxNavigation = new Mapbox.Navigation.Core.MapboxNavigation(
                    builder.Build());
                mapboxNavigation.RegisterRouteProgressObserver(
                    new ReplayProgressObserver(mapboxReplayer));

                if (originalRoute == null)
                {
                    mapboxReplayer.PushRealLocation(Application.Context, 0.0);
                    mapboxReplayer.Play();
                }
            }
            else
            {
                mapboxNavigation = new Mapbox.Navigation.Core.MapboxNavigation(
                    builder.Build());
            }

            return mapboxNavigation;
        }

        public void OnStyleLoaded(Style style)
        {
            locationComponent = mapboxMap.LocationComponent;
            locationComponent.ActivateLocationComponent(
                LocationComponentActivationOptions
                    .InvokeBuilder(this, style)
                    .UseDefaultLocationEngine(false)
                    .Build()
                    );
            locationComponent.CameraMode = CameraMode.Tracking;
            locationComponent.LocationComponentEnabled = true;

            symbolManager = new SymbolManager(mapView, mapboxMap, style);
            style.AddImage("marker", IconFactory.GetInstance(this).DefaultMarker().Bitmap);

            navigationMapboxMap = new NavigationMapboxMap(mapView, mapboxMap, this, true);
            navigationMapboxMap.SetCamera(new DynamicCamera(mapboxMap));
            navigationMapboxMap.AddProgressChangeListener(mapboxNavigation);
            navigationMapboxMap.SetOnRouteSelectionChangeListener(this);

            InitNavigationButton();

            if (originalRoute == null)
            {
                if (ShouldSimulateRoute())
                {
                    mapboxNavigation.RegisterRouteProgressObserver(
                        new ReplayProgressObserver(mapboxReplayer));
                    mapboxReplayer.PushRealLocation(this, 0.0);
                    mapboxReplayer.Play();
                }

                Snackbar.Make(container,
                    Resource.String.msg_long_press_map_to_place_waypoint,
                    (int)ToastLength.Short).Show();
            }
            else
            {
                RestoreNavigation();
            }
        }

        void StartLocationUpdates()
        {
            var request = new LocationEngineRequest.Builder(1000L)
                .SetFastestInterval(500L)
                .SetPriority(LocationEngineRequest.PriorityHighAccuracy)
                .Build();

            try
            {
                localLocationEngine.RequestLocationUpdates(
                    request,
                    locationEngineCallback,
                    Looper.MainLooper
                );
                if (originalRoute == null)
                {
                    localLocationEngine.GetLastLocation(locationEngineCallback);
                }
            }
            catch (SecurityException exception)
            {
                Timber.E(exception);
            }
        }

        void StopLocationUpdates()
        {
            localLocationEngine.RemoveLocationUpdates(locationEngineCallback);
        }



        void RestoreNavigation()
        {
            if (originalRoute != null)
            {
                mapboxNavigation.Routes = new List<DirectionsRoute> { originalRoute };
                navigationMapboxMap.AddProgressChangeListener(mapboxNavigation);
                navigationMapboxMap.StartCamera(mapboxNavigation.Routes[0]);
                UpdateCameraOnNavigationStateChange(true);
                mapboxNavigation.StartTripSession();
            }
        }

        bool ShouldSimulateRoute()
        {
            return PreferenceManager
                .GetDefaultSharedPreferences(Application.Context)
                .GetBoolean(GetString(Resource.String.simulate_route_key), false);
        }

        public void OnNewPrimaryRouteSelected(DirectionsRoute route)
        {
            mapboxNavigation.Routes.Insert(0, route);
        }

        void InitNavigationButton()
        {
            startNavigation.Click += (s, e) =>
            {
                UpdateCameraOnNavigationStateChange(true);
                mapboxNavigation.RegisterVoiceInstructionsObserver(this);
                mapboxNavigation.StartTripSession();
                var routes = mapboxNavigation.Routes;
                if (routes.Any())
                {
                    InitDynamicCamera(routes[0]);
                }
                navigationMapboxMap.ShowAlternativeRoutes(false);
            };
        }

        void InitDynamicCamera(DirectionsRoute route)
        {
            navigationMapboxMap.StartCamera(route);
        }

        void UpdateCameraOnNavigationStateChange(bool navigationStarted)
        {
            if (navigationStarted)
            {
                navigationMapboxMap.UpdateCameraTrackingMode(
                    NavigationCamera.NavigationTrackingModeGps);
                navigationMapboxMap.UpdateLocationLayerRenderMode(RenderMode.Gps);
            }
            else
            {
                symbolManager?.DeleteAll();
                navigationMapboxMap.UpdateCameraTrackingMode(
                    NavigationCamera.NavigationTrackingModeNone);
                navigationMapboxMap.UpdateLocationLayerRenderMode(RenderMode.Compass);
            }
        }

        public void OnNewVoiceInstructions(VoiceInstructions voiceInstructions)
        {
            speechPlayer.Play(voiceInstructions);
        }

        public void OnRouteProgressChanged(RouteProgress routeProgress)
        {
            Timber.D($"route progress {routeProgress}");
            navigationMapboxMap.OnNewRouteProgress(routeProgress);
        }

        public void OnRoutesChanged(IList<DirectionsRoute> routes)
        {
            navigationMapboxMap.DrawRoutes(routes);
            if (!routes.Any())
            {
                Toast.MakeText(this, "Empty routes", ToastLength.Short).Show();
            }
            else
            {
                if (mapboxNavigation.TripSessionState == TripSessionState.Started)
                {
                    InitDynamicCamera(routes[0]);
                }
            }
            Timber.D($"route changed {routes}");
        }

        public void OnFasterRoute(DirectionsRoute currentRoute,
            IList<DirectionsRoute> alternatives, bool isAlternativeFaster)
        {
            if (isAlternativeFaster)
            {
                fasterRoutes = alternatives.ToList();
                fasterRouteSelectionTimer.Start();
                bottomSheetBehavior.State = BottomSheetBehavior.StateExpanded;
            }
        }

        public long RestartAfterMillis()
        {
            return startTimeInMillis;
        }

        public void OnSessionStateChanged(TripSessionState tripSessionState)
        {
            if (tripSessionState == TripSessionState.Started)
            {
                StopLocationUpdates();
                startNavigation.Visibility = ViewStates.Gone;
            }
            else if (tripSessionState == TripSessionState.Stopped)
            {
                StartLocationUpdates();
                startNavigation.Visibility = ViewStates.Visible;
                UpdateCameraOnNavigationStateChange(false);
            }
        }

        private class MyLocationEngineCallback : Java.Lang.Object, ILocationEngineCallback
        {
            WeakReference<SimpleMapboxNavigationKt> activityRef;

            internal MyLocationEngineCallback(SimpleMapboxNavigationKt activity)
            {
                this.activityRef = new WeakReference<SimpleMapboxNavigationKt>(activity);
            }

            public void OnFailure(Java.Lang.Exception p0)
            {
            }

            public void OnSuccess(Java.Lang.Object p0)
            {
                if (p0 is LocationEngineResult result)
                {
                    var location = result?.Locations?.FirstOrDefault();
                    if (location != null)
                    {
                        if (activityRef.TryGetTarget(out SimpleMapboxNavigationKt activity))
                        {
                            activity.locationComponent?.ForceLocationUpdate(location);
                        }
                    }
                }
            }
        }
    }
}
