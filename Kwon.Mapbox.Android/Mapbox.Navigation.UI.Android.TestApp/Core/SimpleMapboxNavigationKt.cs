
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.BottomSheet;
using KotlinX.Coroutines.Channels;
using Mapbox.Android.Core.Location;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Mapboxsdk.Location;
using Mapbox.Mapboxsdk.Maps;
using Mapbox.Mapboxsdk.Plugins.Annotation;
using Mapbox.Navigation.Core.Replay;
using Mapbox.Navigation.Ui.Map;
using Mapbox.Navigation.Ui.Voice;

namespace MapboxNavigation.UI.Droid.TestApp.Core
{
    [Activity(Label = "SimpleMapboxNavigationKt")]
    public class SimpleMapboxNavigationKt : AppCompatActivity
    {
        private const string VOICE_INSTRUCTION_CACHE = "voice-instruction-cache";
        private readonly long startTimeInMillis = 5000;
        private readonly int countdownInterval = 10;
        private float maxProgress;
        private ILocationEngineCallback locationEngineCallback;
        private object restartSessionEventChannel;
        private object restartTripSessionAction;

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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            maxProgress = startTimeInMillis / countdownInterval;
            locationEngineCallback = new MyLocationEngineCallback(this);
            restartSessionEventChannel = ChannelKt(1);
            fasterRoutes = new List<DirectionsRoute>();
            var mapboxReplayer = new MapboxReplayer();
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
