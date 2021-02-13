using System;
using Java.Lang;
using Square.Retrofit2;
using TimberLog;

namespace MapboxNavigation.UI.Droid.TestApp.Legacy.Activity.NavigationUI
{
    /**
     * Helper class to reduce redundant logging code when no other action is taken in onFailure
     */
    public class SimplifiedCallback : Java.Lang.Object, Square.Retrofit2.ICallback
    {
        Action<Response> responseAction;

        public SimplifiedCallback(Action<Response> responseAction)
        {
            this.responseAction = responseAction;
        }

        public void OnFailure(ICall p0, Throwable p1)
        {
            Timber.E(p1, p1.Message);
        }

        public void OnResponse(ICall p0, Response p1)
        {
            responseAction.Invoke(p1);
        }
    }
}
