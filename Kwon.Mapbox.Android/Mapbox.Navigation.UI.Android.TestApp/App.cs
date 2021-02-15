using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Text;
using AndroidX.AppCompat.App;
using Mapbox.Common.Logger;
using Mapbox.Navigation.Navigator.Internal;
using TimberLog;

namespace MapboxNavigation.UI.Droid.TestApp
{
    [Application()]
    public class App : Application
    {
        protected App(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            AppCompatDelegate.CompatVectorFromResourcesEnabled = true;
            SetupTimber();
            SetupStrictMode();
            SetupCanary();
            SetupMapbox();
        }

        private void SetupTimber()
        {
#if DEBUG
            Timber.Plant(new Timber.DebugTree());
#endif
        }

        private void SetupStrictMode()
        {
#if DEBUG
            StrictMode.SetThreadPolicy(new StrictMode.ThreadPolicy.Builder()
                .DetectAll()
                .PenaltyLog()
                .Build());
            StrictMode.SetVmPolicy(new StrictMode.VmPolicy.Builder()
                .DetectAll()
                .PenaltyLog()
                .Build());
#endif
        }

        private void SetupCanary()
        {
            if (Square.LeakCanary.LeakCanaryXamarin.IsInAnalyzerProcess(this))
            {
                // This process is dedicated to LeakCanary for heap analysis.
                // You should not init your app in this process.
                return;
            }
            Square.LeakCanary.LeakCanaryXamarin.Install(this);
        }

        private void SetupMapbox()
        {
            var mapboxAccessToken = Utils.GetMapboxAccessToken(ApplicationContext);
            if (TextUtils.IsEmpty(mapboxAccessToken))
            {
                MapboxLogger.Instance.W(new Mapbox.Base.Common.Logger.Model.Message("Mapbox access token isn't set!"));
            }
            Mapbox.Mapboxsdk.Mapbox.GetInstance(ApplicationContext, mapboxAccessToken);
        }
    }
}
