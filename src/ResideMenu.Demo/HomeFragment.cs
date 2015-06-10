using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using SupportFragment = Android.Support.V4.App.Fragment;

namespace ResideMenu.Demo
{
    public class HomeFragment : SupportFragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var parentView = inflater.Inflate(Resource.Layout.home, container, false);
            MenuActivity parentActivity = Activity as MenuActivity;
            var resideMenu = parentActivity.ResideMenu;

            parentView.FindViewById(Resource.Id.btn_open_menu).Click += (s, e) => resideMenu.OpenMenu(global::AndroidResideMenu.ResideMenu.Direction.Left);

            FrameLayout ignoredView = parentView.FindViewById<FrameLayout>(Resource.Id.ignored_view);
            resideMenu.AddIgnoredView(ignoredView);
            return parentView;
        }
    }
}
