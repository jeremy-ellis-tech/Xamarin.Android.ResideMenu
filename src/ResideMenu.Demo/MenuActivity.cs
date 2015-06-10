using Android.App;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using AndroidResideMenu;
using System;
using Fragment = Android.Support.V4.App.Fragment;

namespace ResideMenu.Demo
{
    [Activity(MainLauncher = true, Label = "@string/app_name", Theme = "@android:style/Theme.Light.NoTitleBar")]
    public class MenuActivity : FragmentActivity, View.IOnClickListener//, global::AndroidResideMenu.ResideMenu.IOnMenuListener
    {

        public global::AndroidResideMenu.ResideMenu ResideMenu { get; private set; }

        private MenuActivity _context;
        private ResideMenuItem _itemHome;
        private ResideMenuItem _itemProfile;
        private ResideMenuItem _itemCalendar;
        private ResideMenuItem _itemSettings;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.main);
            _context = this;
            SetupMenu();
            if (savedInstanceState == null)
                ChangeFragment(new HomeFragment());
        }

        private void SetupMenu()
        {
            ResideMenu = new global::AndroidResideMenu.ResideMenu(this);
            ResideMenu.SetBackground(Resource.Drawable.menu_background);
            ResideMenu.AttachToActivity(this);

            //Now done with MenuOpened/Closed handlers!
            //ResideMenu.SetMenuListener(this);

            ResideMenu.MenuOpened += OnMenuOpened;
            ResideMenu.MenuClosed += OnMenuClosed;

            ResideMenu.SetScaleValue(0.6F);

            // create menu items;
            _itemHome = new ResideMenuItem(this, Resource.Drawable.icon_home, "Home");
            _itemProfile = new ResideMenuItem(this, Resource.Drawable.icon_profile, "Profile");
            _itemCalendar = new ResideMenuItem(this, Resource.Drawable.icon_calendar, "Calendar");
            _itemSettings = new ResideMenuItem(this, Resource.Drawable.icon_settings, "Settings");

            _itemHome.SetOnClickListener(this);
            _itemProfile.SetOnClickListener(this);
            _itemCalendar.SetOnClickListener(this);
            _itemSettings.SetOnClickListener(this);

            ResideMenu.AddMenuItem(_itemHome, global::AndroidResideMenu.ResideMenu.Direction.Left);
            ResideMenu.AddMenuItem(_itemProfile, global::AndroidResideMenu.ResideMenu.Direction.Left);
            ResideMenu.AddMenuItem(_itemCalendar, global::AndroidResideMenu.ResideMenu.Direction.Right);
            ResideMenu.AddMenuItem(_itemSettings, global::AndroidResideMenu.ResideMenu.Direction.Right);

            // You can disable a direction by setting ->
            // resideMenu.setSwipeDirectionDisable(ResideMenu.DIRECTION_RIGHT);

            FindViewById(Resource.Id.title_bar_left_menu).Click += (s, e) => { ResideMenu.OpenMenu(global::AndroidResideMenu.ResideMenu.Direction.Left); };
            FindViewById(Resource.Id.title_bar_right_menu).Click += (s, e) => { ResideMenu.OpenMenu(global::AndroidResideMenu.ResideMenu.Direction.Right); };
        }

        public override bool DispatchTouchEvent(MotionEvent ev)
        {
            return ResideMenu.DispatchTouchEvent(ev);
        }

        public void OnClick(View view)
        {
            if (view == _itemHome)
            {
                ChangeFragment(new HomeFragment());
            }
            else if (view == _itemProfile)
            {
                ChangeFragment(new ProfileFragment());
            }
            else if (view == _itemCalendar)
            {
                ChangeFragment(new CalendarFragment());
            }
            else if (view == _itemSettings)
            {
                ChangeFragment(new SettingsFragment());
            }

            ResideMenu.CloseMenu();
        }

        private void ChangeFragment(Fragment targetFragment)
        {
            ResideMenu.ClearIgnoredViewList();

            SupportFragmentManager
                    .BeginTransaction()
                    .Replace(Resource.Id.main_fragment, targetFragment, "fragment")
                    .SetTransitionStyle(global::Android.Support.V4.App.FragmentTransaction.TransitFragmentFade)
                    .Commit();
        }

        private void OnMenuOpened(object sender, EventArgs e)
        {
            Toast.MakeText(this, "Menu is opened!", ToastLength.Short).Show();
        }

        private void OnMenuClosed(object sender, EventArgs e)
        {
            Toast.MakeText(this, "Menu is closed!", ToastLength.Short).Show();
        }

        //Old interface callbacks
        //public void OpenMenu()
        //{
        //    Toast.MakeText(this, "Menu is opened!", ToastLength.Short).Show();
        //}

        //public void CloseMenu()
        //{
        //    Toast.MakeText(this, "Menu is closed!", ToastLength.Short).Show();
        //}
    }
}
