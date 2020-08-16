
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
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Geojson;
using Mapbox.Mapboxsdk.Maps;
using Mapbox.Mapboxsdk.Camera;
using Mapbox.Mapboxsdk.Geometry;
using Mapbox.Services.Android.Navigation.UI.V5;
using Mapbox.Services.Android.Navigation.UI.V5.Listeners;
using Mapbox.Services.Android.Navigation.UI.V5.Voice;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Mapbox.Services.Android.Navigation.V5.Routeprogress;
using TimberLog;
using Android.Content.Res;
using Android.Text;
using Android.Text.Style;

namespace MapboxNavigation.UI.Droid.TestApp.Activity.NavigationUI
{
    [Activity(Label = "EmbeddedNavigationActivity")]
    public class EmbeddedNavigationActivity : AppCompatActivity, IOnNavigationReadyCallback, INavigationListener,
        IProgressChangeListener, IInstructionListListener, ISpeechAnnouncementListener, IBannerInstructionsListener
    {
        private static Point ORIGIN = Point.FromLngLat(-77.03194990754128, 38.909664963450105);
        private static Point DESTINATION = Point.FromLngLat(-77.0270025730133, 38.91057077063121);
        private static int INITIAL_ZOOM = 16;

        private Mapbox.Services.Android.Navigation.UI.V5.NavigationView navigationView;
        private View spacer;
        private TextView speedWidget;
        private FloatingActionButton fabNightModeToggle;
        private FloatingActionButton fabStyleToggle;

        private bool bottomSheetVisible = true;
        private bool instructionListShown = false;

        private StyleCycle styleCycle = new StyleCycle();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetTheme(Resource.Style.Theme_AppCompat_Light_NoActionBar);
            InitNightMode();
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_embedded_navigation);
            navigationView = FindViewById<Mapbox.Services.Android.Navigation.UI.V5.NavigationView>(Resource.Id.navigationView);
            fabNightModeToggle = FindViewById<FloatingActionButton>(Resource.Id.fabToggleNightMode);
            fabStyleToggle = FindViewById<FloatingActionButton>(Resource.Id.fabToggleStyle);
            speedWidget = FindViewById<TextView>(Resource.Id.speed_limit);
            spacer = FindViewById<View>(Resource.Id.spacer);
            SetSpeedWidgetAnchor(Resource.Id.summaryBottomSheet);

            CameraPosition initialPosition = new CameraPosition.Builder()
              .Target(new LatLng(ORIGIN.Latitude(), ORIGIN.Longitude()))
              .Zoom(INITIAL_ZOOM)
              .Build();
            navigationView.OnCreate(savedInstanceState);
            navigationView.Initialize(this, initialPosition);
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
            if (IsFinishing)
            {
                SaveNightModeToPreferences(AppCompatDelegate.ModeNightAuto);
                AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightAuto;
            }
        }

        public void OnCancelNavigation()
        {
            // Navigation canceled, finish the activity
            Finish();
        }

        public void OnInstructionListVisibilityChanged(bool p0)
        {
            var shown = p0;
            instructionListShown = shown;
            speedWidget.Visibility = shown ? ViewStates.Gone : ViewStates.Visible;
            if (instructionListShown)
            {
                fabNightModeToggle.Hide();
            }
            else if (bottomSheetVisible)
            {
                fabNightModeToggle.Show();
            }
        }

        public void OnNavigationFinished()
        {
            // Intentionally empty
        }

        public void OnNavigationReady(bool p0)
        {
            FetchRoute();
        }

        public void OnNavigationRunning()
        {
            // Intentionally empty
        }

        public void OnProgressChange(Location p0, RouteProgress p1)
        {
            SetSpeed(p0);
        }

        public BannerInstructions WillDisplay(BannerInstructions p0)
        {
            return p0;
        }

        public SpeechAnnouncement WillVoice(SpeechAnnouncement p0)
        {
            return SpeechAnnouncement.InvokeBuilder().Announcement("All announcements will be the same.").Build();
        }

        private void StartNavigation(DirectionsRoute directionsRoute)
        {
            NavigationViewOptions.Builder options =
              NavigationViewOptions.InvokeBuilder()
                .NavigationListener(this)
                .DirectionsRoute(directionsRoute)
                .ShouldSimulateRoute(true)
                .ProgressChangeListener(this)
                .InstructionListListener(this)
                .SpeechAnnouncementListener(this)
                .BannerInstructionsListener(this)
                .OfflineRoutingTilesPath(ObtainOfflineDirectory())
                .OfflineRoutingTilesVersion(ObtainOfflineTileVersion());
            SetBottomSheetCallback(options);
            SetupStyleFab();
            SetupNightModeFab();

            navigationView.StartNavigation(options.Build());
        }

        private string ObtainOfflineDirectory()
        {
            var offline = Android.OS.Environment.GetExternalStoragePublicDirectory("Offline");
            if (!offline.Exists())
            {
                Timber.D("Offline directory does not exist");
                offline.Mkdirs();
            }
            return offline.AbsolutePath;
        }

        private string ObtainOfflineTileVersion()
        {
            return PreferenceManager.GetDefaultSharedPreferences(this)
              .GetString(GetString(Resource.String.offline_version_key), "");
        }

        private void FetchRoute()
        {
            NavigationRoute.InvokeBuilder(this)
              .AccessToken(Mapbox.Mapboxsdk.Mapbox.AccessToken)
              .Origin(ORIGIN)
              .Destination(DESTINATION)
              .Alternatives((Java.Lang.Boolean)true)
              .Build()
              .GetRoute(new SimplifiedCallback((res) =>
              {
                  var body = res.Body() as DirectionsResponse;
                  if (body != null && body.Routes().Any())
                  {
                      var directionsRoute = body.Routes()[0];
                      StartNavigation(directionsRoute);
                  }
              }));
        }

        /**
        * Sets the anchor of the spacer for the speed widget, thus setting the anchor for the speed widget
        * (The speed widget is anchored to the spacer, which is there because padding between items and
        * their anchors in CoordinatorLayouts is finicky.
        *
        * @param res resource for view of which to anchor the spacer
        */
        private void SetSpeedWidgetAnchor(int res)
        {
            CoordinatorLayout.LayoutParams layoutParams = (CoordinatorLayout.LayoutParams)spacer.LayoutParameters;
            layoutParams.AnchorId = res;
            spacer.LayoutParameters = layoutParams;
        }

        private void SetBottomSheetCallback(NavigationViewOptions.Builder options)
        {
            options.BottomSheetCallback(new MyBottomSheetCallback((bottomSheet, newState) =>
            {
                switch (newState)
                {
                    case BottomSheetBehavior.StateHidden:
                        bottomSheetVisible = false;
                        fabNightModeToggle.Hide();
                        SetSpeedWidgetAnchor(Resource.Id.recenterBtn);
                        break;
                    case BottomSheetBehavior.StateExpanded:
                        bottomSheetVisible = true;
                        break;
                    case BottomSheetBehavior.StateSettling:
                        if (!bottomSheetVisible)
                        {
                            // View needs to be anchored to the bottom sheet before it is finished expanding
                            // because of the animation
                            fabNightModeToggle.Show();
                            SetSpeedWidgetAnchor(Resource.Id.summaryBottomSheet);
                        }
                        break;
                    default:
                        return;
                }
            }));
        }

        private void SetupNightModeFab()
        {
            fabNightModeToggle.SetOnClickListener(new FabNightModeToggleClickListener((view) =>
            {
                ToggleNightMode();
            }));
        }

        private void SetupStyleFab()
        {
            fabStyleToggle.SetOnClickListener(new FabStyleToggleClickListener((view) =>
            {
                navigationView.RetrieveNavigationMapboxMap().RetrieveMap().SetStyle(styleCycle.GetNextStyle());
            }));
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

        private void ToggleNightMode()
        {
            int currentNightMode = GetCurrentNightMode();
            AlternateNightMode(currentNightMode);
        }

        private void InitNightMode()
        {
            int nightMode = RetrieveNightModeFromPreferences();
            AppCompatDelegate.DefaultNightMode = nightMode;
        }

        private int GetCurrentNightMode()
        {
            return (int)(Resources.Configuration.UiMode & UiMode.NightMask);
        }

        private void AlternateNightMode(int currentNightMode)
        {
            int newNightMode;
            if (currentNightMode == (int)UiMode.NightYes)
            {
                newNightMode = AppCompatDelegate.ModeNightNo;
            }
            else
            {
                newNightMode = AppCompatDelegate.ModeNightYes;
            }
            SaveNightModeToPreferences(newNightMode);
            Recreate();
        }

        private int RetrieveNightModeFromPreferences()
        {
            var preferences = PreferenceManager.GetDefaultSharedPreferences(this);
            return preferences.GetInt(GetString(Resource.String.current_night_mode), AppCompatDelegate.ModeNightAuto);
        }

        private void SaveNightModeToPreferences(int nightMode)
        {
            var preferences = PreferenceManager.GetDefaultSharedPreferences(this);
            var editor = preferences.Edit();
            editor.PutInt(GetString(Resource.String.current_night_mode), nightMode);
            editor.Apply();
        }

        private void SetSpeed(Location location)
        {
            string str = $"{(int)(location.Speed * 2.2369)}\nMPH";
            int mphTextSize = Resources.GetDimensionPixelSize(Resource.Dimension.mph_text_size);
            int speedTextSize = Resources.GetDimensionPixelSize(Resource.Dimension.speed_text_size);

            SpannableString spannableString = new SpannableString(str);
            spannableString.SetSpan(new AbsoluteSizeSpan(mphTextSize),
              str.Length - 4, str.Length, SpanTypes.InclusiveInclusive);

            spannableString.SetSpan(new AbsoluteSizeSpan(speedTextSize),
              0, str.Length - 3, SpanTypes.InclusiveInclusive);

            speedWidget.TextFormatted = spannableString;
            if (!instructionListShown)
            {
                speedWidget.Visibility = ViewStates.Visible;
            }
        }

        private class FabStyleToggleClickListener : Java.Lang.Object, View.IOnClickListener
        {
            Action<View> clickAction;

            public FabStyleToggleClickListener(Action<View> clickAction)
            {
                this.clickAction = clickAction;
            }

            public void OnClick(View v)
            {
                clickAction.Invoke(v);
            }
        }

        private class FabNightModeToggleClickListener : Java.Lang.Object, View.IOnClickListener
        {
            Action<View> clickAction;

            public FabNightModeToggleClickListener(Action<View> clickAction)
            {
                this.clickAction = clickAction;
            }

            public void OnClick(View v)
            {
                clickAction.Invoke(v);
            }
        }

        private class MyBottomSheetCallback : BottomSheetBehavior.BottomSheetCallback
        {
            Action<View, int> stateChangedAction;

            public MyBottomSheetCallback(Action<View, int> stateChangedAction)
            {
                this.stateChangedAction = stateChangedAction;
            }

            public override void OnSlide(View bottomSheet, float slideOffset)
            {
            }

            public override void OnStateChanged(View bottomSheet, int newState)
            {
                stateChangedAction.Invoke(bottomSheet, newState);
            }
        }
    }
}
