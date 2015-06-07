
using Android.OS;
using Android.Support.V4.App;
using Android.Views;

namespace ResideMenu.Demo
{
    public class SettingsFragment : Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.settings, container, false);
        }
    }
}

