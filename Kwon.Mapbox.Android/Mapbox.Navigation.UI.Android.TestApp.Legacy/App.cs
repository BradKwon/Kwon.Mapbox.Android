﻿using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Text;

namespace MapboxNavigation.UI.Droid.TestApp.Legacy
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
            SetupTimber();
            //SetupStrictMode();
            SetupCanary();
            SetupMapbox();
        }

        private void SetupTimber()
        {
#if DEBUG
            TimberLog.Timber.Plant(new TimberLog.Timber.DebugTree());
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
                TimberLog.Timber.W("Mapbox access token isn't set!");
            }
            Mapbox.Mapboxsdk.Mapbox.GetInstance(ApplicationContext, mapboxAccessToken);
        }
    }
}
