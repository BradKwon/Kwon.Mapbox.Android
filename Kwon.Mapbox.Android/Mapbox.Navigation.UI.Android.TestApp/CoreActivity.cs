using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using CheeseBind;
using MapboxNavigation.UI.Droid.TestApp.Core;

namespace MapboxNavigation.UI.Droid.TestApp
{
    [Activity(Label = "CoreActivity")]
    public class CoreActivity : AppCompatActivity
    {
        private LinearLayoutManager layoutManager;
        private ExamplesAdapter adapter;

        [BindView(Resource.Id.coreRecycler)]
        private RecyclerView coreRecycler;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_core);

            var sampleItemList = BuildSampleList();
            adapter = new ExamplesAdapter(this, (position) =>
            {
                StartActivity(new Intent(this, sampleItemList[position].Activity));
            });
            layoutManager = new LinearLayoutManager(this, (int)Orientation.Vertical, false);
            coreRecycler.SetLayoutManager(layoutManager);
            coreRecycler.AddItemDecoration(new DividerItemDecoration(this, DividerItemDecoration.Vertical));
            coreRecycler.SetAdapter(adapter);
            adapter.AddSampleItems(sampleItemList);
        }

        private List<SampleItem> BuildSampleList()
        {
            List<SampleItem> sampleItems = new List<SampleItem>
            {
                new SampleItem(
                    GetString(Resource.String.title_simple_navigation_kotlin),
                    GetString(Resource.String.description_simple_navigation_kotlin),
                    typeof(SimpleMapboxNavigationKt)
                ),
                //new SampleItem(
                //    GetString(Resource.String.title_guidance_view),
                //    GetString(Resource.String.description_guidance_view),
                //    typeof(GuidanceViewActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_junction_snapshot_sample),
                //    GetString(Resource.String.description_junction_snapshot_sample),
                //    typeof(JunctionSnapshotActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_reroute_view),
                //    GetString(Resource.String.description_reroute_view),
                //    typeof(ReRouteActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_basic_navigation_kotlin),
                //    GetString(Resource.String.description_basic_navigation_kotlin),
                //    typeof(BasicNavigationActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_basic_navigation_fragment),
                //    GetString(Resource.String.description_basic_navigation_fragment),
                //    typeof(BasicNavigationFragmentActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_basic_navigation_sdk_only_kotlin),
                //    GetString(Resource.String.description_basic_navigation_sdk_only_kotlin),
                //    typeof(BasicNavSdkOnlyActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_free_drive_kotlin),
                //    GetString(Resource.String.description_free_drive_kotlin),
                //    typeof(FreeDriveNavigationActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_replay_route),
                //    GetString(Resource.String.description_replay_route),
                //    typeof(ReplayActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_replay_history_kotlin),
                //    GetString(Resource.String.description_replay_history_kotlin),
                //    typeof(ReplayHistoryActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_replay_waypoints),
                //    GetString(Resource.String.description_replay_waypoints),
                //    typeof(ReplayWaypointsActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_silent_waypoints_reroute),
                //    GetString(Resource.String.description_silent_waypoints_reroute),
                //    typeof(SilentWaypointsRerouteActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_faster_route),
                //    GetString(Resource.String.description_faster_route),
                //    typeof(FasterRouteActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_instruction_view),
                //    GetString(Resource.String.description_instruction_view),
                //    typeof(InstructionViewActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_summary_bottom_sheet),
                //    GetString(Resource.String.description_summary_bottom_sheet),
                //    typeof(SummaryBottomSheetActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_feedback_button),
                //    GetString(Resource.String.description_feedback_button),
                //    typeof(FeedbackButtonActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_debug_navigation_kotlin),
                //    GetString(Resource.String.description_debug_navigation_kotlin),
                //    typeof(DebugMapboxNavigationKt)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_custom_route_styling_kotlin),
                //    GetString(Resource.String.description_custom_route_styling_kotlin),
                //    typeof(CustomRouteStylingActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_navigation_route_ui),
                //    GetString(Resource.String.description_navigation_route_ui),
                //    typeof(NavigationMapRouteActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_runtime_styling),
                //    GetString(Resource.String.description_runtime_styling),
                //    typeof(RuntimeRouteStylingActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_map_matching),
                //    GetString(Resource.String.description_map_matching),
                //    typeof(MapMatchingActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_route_alerts),
                //    GetString(Resource.String.description_route_alerts),
                //    typeof(RouteAlertsActivity)
                //),
                //new SampleItem(
                //    GetString(Resource.String.title_alternative_route_custom_click_padding),
                //    GetString(Resource.String.description_alternative_route_custom_click_padding),
                //    typeof(CustomAlternativeRouteClickPaddingActivity)
                //)
            };

            return sampleItems;
        }
    }
}   
