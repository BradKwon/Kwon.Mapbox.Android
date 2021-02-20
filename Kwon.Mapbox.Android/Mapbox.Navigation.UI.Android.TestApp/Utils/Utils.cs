using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using AndroidX.Core.Content.Resources;
using Mapbox.Api.Directions.V5.Models;
using Mapbox.Mapboxsdk.Annotations;
using Mapbox.Mapboxsdk.Geometry;
using TimberLog;

namespace MapboxNavigation.UI.Droid.TestApp
{
    public static class Utils
    {
        public static string GetMapboxAccessToken(Context context)
        {
            try
            {
                string token = Mapbox.Mapboxsdk.Mapbox.AccessToken;
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new ArgumentException();
                }
                return token;
            }
            catch (Exception ex)
            {
                // Use fallback on string resource, used for development
                int tokenResId = context.Resources.GetIdentifier("mapbox_access_token", "string", context.PackageName);
                return tokenResId != 0 ? context.GetString(tokenResId) : null;
            }
        }

        /**
        * Demonstrates converting any Drawable to an Icon, for use as a marker icon.
        */
        public static Mapbox.Mapboxsdk.Annotations.Icon DrawableToIcon(Context context, int id)
        {
            Drawable vectorDrawable = ResourcesCompat.GetDrawable(context.Resources, id, context.Theme);
            Bitmap bitmap = Bitmap.CreateBitmap(vectorDrawable.IntrinsicWidth,
              vectorDrawable.IntrinsicHeight, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(bitmap);
            vectorDrawable.SetBounds(0, 0, canvas.Width, canvas.Height);
            vectorDrawable.Draw(canvas);
            return IconFactory.GetInstance(context).FromBitmap(bitmap);
        }

        public static LatLng GetRandomLatLng(double[] bbox)
        {
            Random random = new Random();

            double randomLat = bbox[1] + (bbox[3] - bbox[1]) * random.NextDouble();
            double randomLon = bbox[0] + (bbox[2] - bbox[0]) * random.NextDouble();

            LatLng latLng = new LatLng(randomLat, randomLon);
            Timber.D("GetRandomLatLng: %s", latLng.ToString());
            return latLng;
        }

        public static string PRIMARY_ROUTE_BUNDLE_KEY = "myPrimaryRouteBundleKey";

        /**
           * Used by the example activities to get a DirectionsRoute from a bundle.
           *
           * @param bundle to get the DirectionsRoute from
           * @return a DirectionsRoute or null
           */
        public static DirectionsRoute GetRouteFromBundle(Bundle bundle)
        {
            try
            {
                if (bundle.ContainsKey(PRIMARY_ROUTE_BUNDLE_KEY))
                {
                    string routeAsJson = bundle.GetString(PRIMARY_ROUTE_BUNDLE_KEY);
                    return DirectionsRoute.FromJson(routeAsJson);
                }
            }
            catch (Exception ex)
            {
                Timber.I(ex.Message);
            }
            return null;
        }
    }
}
