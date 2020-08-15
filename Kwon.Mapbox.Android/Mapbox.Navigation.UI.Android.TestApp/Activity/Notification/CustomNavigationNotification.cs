using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Mapbox.Services.Android.Navigation.V5.Navigation.Notification;
using Mapbox.Services.Android.Navigation.V5.Routeprogress;

namespace MapboxNavigation.UI.Droid.TestApp.Activity.Notification
{
    public class CustomNavigationNotification : Java.Lang.Object, INavigationNotification
    {
        private static int CUSTOM_NOTIFICATION_ID = 91234821;
        private static string STOP_NAVIGATION_ACTION = "stop_navigation_action";
        private static string CUSTOM_CHANNEL_ID = "custom_channel_id";
        private static string CUSTOM_CHANNEL_NAME = "custom_channel_name";

        private Android.App.Notification customNotification;
        private NotificationCompat.Builder customNotificationBuilder;
        private NotificationManager notificationManager;
        private BroadcastReceiver stopNavigationReceiver;
        private int numberOfUpdates;

        public CustomNavigationNotification(Context applicationContext)
        {
            notificationManager = (NotificationManager)applicationContext.GetSystemService(Context.NotificationService);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel notificationChannel = new NotificationChannel(
                  CUSTOM_CHANNEL_ID, CUSTOM_CHANNEL_NAME, NotificationImportance.Low
                );
                notificationManager.CreateNotificationChannel(notificationChannel);
            }

            customNotificationBuilder = new NotificationCompat.Builder(applicationContext, CUSTOM_CHANNEL_ID)
              .SetSmallIcon(Resource.Drawable.ic_navigation)
              .SetContentTitle("Custom Navigation Notification")
              .SetContentText("Display your own content here!")
              .SetContentIntent(CreatePendingStopIntent(applicationContext));

            customNotification = customNotificationBuilder.Build();
        }

        public Android.App.Notification Notification => customNotification;

        public int NotificationId => CUSTOM_NOTIFICATION_ID;

        public void OnNavigationStopped(Context p0)
        {
            p0.UnregisterReceiver(stopNavigationReceiver);
            notificationManager.Cancel(CUSTOM_NOTIFICATION_ID);
        }

        public void UpdateNotification(RouteProgress p0)
        {
            // Update the builder with a new number of updates
            customNotificationBuilder.SetContentText("Number of updates: " + numberOfUpdates++);

            notificationManager.Notify(CUSTOM_NOTIFICATION_ID, customNotificationBuilder.Build());
        }

        public void Register(BroadcastReceiver stopNavigationReceiver, Context applicationContext)
        {
            this.stopNavigationReceiver = stopNavigationReceiver;
            applicationContext.RegisterReceiver(stopNavigationReceiver, new IntentFilter(STOP_NAVIGATION_ACTION));
        }

        private PendingIntent CreatePendingStopIntent(Context context)
        {
            Intent stopNavigationIntent = new Intent(STOP_NAVIGATION_ACTION);
            return PendingIntent.GetBroadcast(context, 0, stopNavigationIntent, 0);
        }
    }
}
