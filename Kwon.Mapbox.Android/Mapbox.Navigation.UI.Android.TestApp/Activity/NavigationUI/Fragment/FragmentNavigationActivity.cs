
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using CheeseBind;

namespace MapboxNavigation.UI.Droid.TestApp.Activity.NavigationUI.Fragment
{
    [Activity(Label = "FragmentNavigationActivity")]
    public class FragmentNavigationActivity : AppCompatActivity
    {
        private static string FAB_VISIBLE_KEY = "restart_fab_visible";

        [BindView(Resource.Id.restart_navigation_fab)]
        FloatingActionButton restartNavigationFab;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_navigation_fragment);
            Cheeseknife.Bind(this);
            InitializeNavigationViewFragment(savedInstanceState);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutBoolean(FAB_VISIBLE_KEY, restartNavigationFab.Visibility == ViewStates.Visible);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
            bool isVisible = savedInstanceState.GetBoolean(FAB_VISIBLE_KEY);
            var visibility = isVisible ? ViewStates.Visible : ViewStates.Invisible;
            restartNavigationFab.Visibility = visibility;
        }

        [OnClick(Resource.Id.restart_navigation_fab)]
        public void OnClick(object sender, EventArgs args)
        {
            ReplaceFragment(new NavigationFragment());
            restartNavigationFab.Hide();
        }

        public void ShowNavigationFab()
        {
            restartNavigationFab.Show();
        }

        public void ShowPlaceholderFragment()
        {
            ReplaceFragment(new PlaceholderFragment());
        }

        private void InitializeNavigationViewFragment(Bundle savedInstanceState)
        {
            var fragmentManager = SupportFragmentManager;
            if (savedInstanceState == null)
            {
                var transaction = fragmentManager.BeginTransaction();
                transaction.DisallowAddToBackStack();
                transaction.Add(Resource.Id.navigation_fragment_frame, new NavigationFragment()).Commit();
            }
        }

        private void ReplaceFragment(AndroidX.Fragment.App.Fragment newFragment)
        {
            string tag = newFragment.Id.ToString();
            var transaction = SupportFragmentManager.BeginTransaction();
            transaction.DisallowAddToBackStack();
            transaction.SetTransition((int)FragmentTransit.FragmentOpen);
            int fadeInAnimId = Resource.Animation.abc_fade_in;
            int fadeOutAnimId = Resource.Animation.abc_fade_out;
            transaction.SetCustomAnimations(fadeInAnimId, fadeOutAnimId, fadeInAnimId, fadeOutAnimId);
            transaction.Replace(Resource.Id.navigation_fragment_frame, newFragment, tag);
            transaction.Commit();
        }
    }
}
