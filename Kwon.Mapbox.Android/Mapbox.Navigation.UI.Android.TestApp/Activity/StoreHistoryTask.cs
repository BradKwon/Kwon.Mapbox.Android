using System;
using Android.OS;
using Java.IO;
using TimberLog;

namespace MapboxNavigation.UI.Droid.TestApp.Activity
{
    public class StoreHistoryTask : AsyncTask
    {
        private static string EMPTY_HISTORY = "{}";
        private static string DRIVES_FOLDER = "/drives";
        private Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation navigation;
        private string filename;

        public StoreHistoryTask(Mapbox.Services.Android.Navigation.V5.Navigation.MapboxNavigation navigation, string filename)
        {
            this.navigation = navigation;
            this.filename = filename;
        }

        protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
        {
            if (IsExternalStorageWritable())
            {
                string history = navigation.RetrieveHistory();
                if (!history.Equals(EMPTY_HISTORY))
                {
                    File pathToExternalStorage = Android.OS.Environment.ExternalStorageDirectory;
                    File appDirectory = new File(pathToExternalStorage.AbsolutePath + DRIVES_FOLDER);
                    appDirectory.Mkdirs();
                    File saveFilePath = new File(appDirectory, filename);
                    Write(history, saveFilePath);
                }
            }
            return null;
        }

        private bool IsExternalStorageWritable()
        {
            string state = Android.OS.Environment.ExternalStorageState;
            if (Android.OS.Environment.MediaMounted.Equals(state))
            {
                return true;
            }
            return false;
        }

        private void Write(string history, File saveFilePath)
        {
            try
            {
                using (var file = new System.IO.StreamWriter(saveFilePath.AbsolutePath))
                {
                    file.WriteLine(history);
                }
            }
            catch (Exception exception)
            {
                Timber.E(exception.Message);
            }
        }
    }
}
