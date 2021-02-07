using Kotlin.Jvm.Internal;
using Mapbox.Navigation.Base.Metrics;

namespace Mapbox.Navigation.Metrics
{
    public sealed partial class MapboxMetricsReporter
    {
        private static volatile IMetricsObserver metricsObserver;

        public void SetMetricsObserver(global::Mapbox.Navigation.Base.Metrics.IMetricsObserver metricsObserver)
        {
            Intrinsics.CheckNotNullParameter(metricsObserver as Java.Lang.Object, "metricsObserver");
            MapboxMetricsReporter.metricsObserver = metricsObserver;
        }
    }
}