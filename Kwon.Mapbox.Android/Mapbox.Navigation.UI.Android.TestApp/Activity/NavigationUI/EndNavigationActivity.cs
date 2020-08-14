
using System;
using System.Linq;

using Android.App;
using Android.Locations;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.ConstraintLayout.Widget;
using Java.Lang;
using Mapbox.Api.Directions.V5;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Geojson;
using Mapbox.Mapboxsdk.Annotations;
using Mapbox.Mapboxsdk.Geometry;
using Mapbox.Services.Android.Navigation.UI.V5;
using Mapbox.Services.Android.Navigation.UI.V5.Listeners;
using Mapbox.Services.Android.Navigation.V5.Navigation;
using Mapbox.Services.Android.Navigation.V5.Routeprogress;
using Square.Retrofit2;
using NavigationView = Mapbox.Services.Android.Navigation.UI.V5.NavigationView;

namespace MapboxNavigation.UI.Droid.TestApp.Activity.NavigationUI
{
    [Activity(Label = "EndNavigationActivity")]
    public class EndNavigationActivity : AppCompatActivity, IOnNavigationReadyCallback, INavigationListener, IProgressChangeListener, ICallback
    {
        private NavigationView navigationView;
        private ProgressBar loading;
        private TextView message;
        private FloatingActionButton launchNavigationFab;
        private Point origin = Point.FromLngLat(-122.423579, 37.761689);
        private Point pickup = Point.FromLngLat(-122.423877, 37.760578);
        private Point middlePickup = Point.FromLngLat(-122.428604, 37.763559);
        private Point destination = Point.FromLngLat(-122.426183, 37.760872);
        private DirectionsRoute route;
        private bool paellaPickedUp = false;
        private Marker paella;
        private bool isNavigationRunning;
        private ConstraintLayout endNavigationLayout;

        public void OnCancelNavigation()
        {
            navigationView.StopNavigation();
            UpdateUiNavigationFinished();
        }

        public void OnFailure(ICall p0, Throwable p1)
        {
        }

        public void OnNavigationFinished()
        {
        }

        public void OnNavigationReady(bool p0)
        {
            isNavigationRunning = p0;
            FetchRoute();
        }

        public void OnNavigationRunning()
        {
        }

        public void OnProgressChange(Location p0, RouteProgress p1)
        {
            bool isCurrentStepArrival = p1.CurrentLegProgress().CurrentStep().Maneuver().Type()
                .Contains(NavigationConstants.StepManeuverTypeArrive);

            if (isCurrentStepArrival && !paellaPickedUp)
            {
                UpdateUiDelivering();
            }
            else if (isCurrentStepArrival && paellaPickedUp)
            {
                UpdateUiDelivered();
            }
        }

        private void FetchRoute()
        {
            NavigationRoute builder = NavigationRoute.InvokeBuilder(this)
              .AccessToken(Mapbox.Mapboxsdk.Mapbox.AccessToken)
              .Origin(origin)
              .Profile(DirectionsCriteria.ProfileDriving)
              .AddWaypoint(pickup)
              .AddWaypoint(middlePickup)
              .Destination(destination)
              //.AddWaypointIndices(new Integer(0), new Integer(2), new Integer(3))
              .Alternatives((Java.Lang.Boolean)true)
              .Build();
            builder.GetRoute(this);
            UpdateLoadingTo(true);
        }

        public void OnResponse(ICall p0, Response p1)
        {
            if (ValidRouteResponse(p1))
            {
                UpdateLoadingTo(false);
                message.Text = "Launch Navigation";
                launchNavigationFab.Visibility = ViewStates.Visible;
                launchNavigationFab.Show();
                route = (p1.Body() as DirectionsResponse).Routes()[0];
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

            InitializeViews(savedInstanceState);
            navigationView.Initialize(this);
            launchNavigationFab.SetOnClickListener(new LaunchNavigationFabClickListener((v) => LaunchNavigation()));
        }

        private void LaunchNavigation()
        {
            launchNavigationFab.Hide();
            DrawPaella();
            navigationView.Visibility = ViewStates.Visible;
            int height = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 80, Resources.DisplayMetrics);
            message.LayoutParameters.Height = height;
            ConstraintSet constraintSet = new ConstraintSet();
            constraintSet.Clone(endNavigationLayout);
            constraintSet.Connect(Resource.Id.message, ConstraintSet.Bottom, ConstraintSet.ParentId, ConstraintSet.Bottom, 0);
            constraintSet.Connect(Resource.Id.message, ConstraintSet.End, ConstraintSet.ParentId, ConstraintSet.End, 0);
            constraintSet.Connect(Resource.Id.message, ConstraintSet.Start, ConstraintSet.ParentId, ConstraintSet.Start, 0);
            constraintSet.ApplyTo(endNavigationLayout);
            NavigationViewOptions.Builder options = NavigationViewOptions.InvokeBuilder()
              .NavigationListener(this)
              .ProgressChangeListener(this)
              .DirectionsRoute(route)
              .ShouldSimulateRoute(true);
            navigationView.StartNavigation(options.Build());
            UpdateUiPickingUp();
        }

        private void DrawPaella()
        {
            Icon paellaIcon = IconFactory.GetInstance(this).FromResource(Resource.Drawable.paella_icon);
            var markerOptions = new MarkerOptions();
            markerOptions.SetPosition(new LatLng(37.760615, -122.424306));
            markerOptions.SetIcon(paellaIcon);
            paella = navigationView.RetrieveNavigationMapboxMap().RetrieveMap().AddMarker(markerOptions);
        }

        private void InitializeViews(Bundle savedInstanceState)
        {
            SetContentView(Resource.Layout.activity_end_navigation);
            endNavigationLayout = FindViewById<ConstraintLayout>(Resource.Id.endNavigationLayout);
            navigationView = FindViewById<NavigationView>(Resource.Id.navigationView);
            loading = FindViewById<ProgressBar>(Resource.Id.loading);
            message = FindViewById<TextView>(Resource.Id.message);
            launchNavigationFab = FindViewById<FloatingActionButton>(Resource.Id.launchNavigation);
            navigationView.OnCreate(savedInstanceState);
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

        private bool ValidRouteResponse(Response p1)
        {
            var response = p1.Body() as DirectionsResponse;
            return response != null && response.Routes().Any();
        }

        private void UpdateUiPickingUp()
        {
            message.Text = "Picking the paella up...";
        }

        private void UpdateUiDelivering()
        {
            paellaPickedUp = true;
            message.Text = "Delivering...";
        }

        private void UpdateUiDelivered()
        {
            message.Text = "Delicious paella delivered!";
        }

        private void UpdateUiNavigationFinished()
        {
            navigationView.RetrieveNavigationMapboxMap().RetrieveMap().RemoveMarker(paella);
            navigationView.Visibility = ViewStates.Gone;
            message.Text = "Launch Navigation";
            message.LayoutParameters = new ConstraintLayout.LayoutParams(ConstraintLayout.LayoutParams.MatchParent, ConstraintLayout.LayoutParams.MatchParent);
            launchNavigationFab.Show();
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
    }
}
