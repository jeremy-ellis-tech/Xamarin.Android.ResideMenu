using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Fragment = Android.Support.V4.App.Fragment;

namespace ResideMenu.Demo
{
    public class HomeFragment : Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var _parentView = inflater.Inflate(Resource.Layout.home, container, false);
            MenuActivity parentActivity = Activity as MenuActivity;
            var _resideMenu = parentActivity.ResideMenu;

            _parentView.FindViewById(Resource.Id.btn_open_menu).Click += (s, e) => _resideMenu.OpenMenu(global::AndroidResideMenu.ResideMenu.Direction.Left);

            FrameLayout ignored_view = _parentView.FindViewById<FrameLayout>(Resource.Id.ignored_view);
            _resideMenu.AddIgnoredView(ignored_view);
            return _parentView;
        }
    }
}
