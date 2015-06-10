using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using SupportFragment = Android.Support.V4.App.Fragment;

namespace ResideMenu.Demo
{
    public class CalendarFragment : SupportFragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View parentView = inflater.Inflate(Resource.Layout.calendar, container, false);
            ListView listView = parentView.FindViewById<ListView>(Resource.Id.listView);

            listView.Adapter = new ArrayAdapter<string>(Activity, Android.Resource.Layout.SimpleListItem1,
                new List<string>
                {
                    "New Year's Day",
                    "St. Valentine's Day",
                    "Easter Day",
                    "April Fool's Day",
                    "Mother's Day",
                    "Memorial Day",
                    "National Flag Day",
                    "Father's Day",
                    "Independence Day",
                    "Labor Day",
                    "Columbus Day",
                    "Halloween",
                    "All Soul's Day",
                    "Veterans Day",
                    "Thanksgiving Day",
                    "Election Day",
                    "Forefather's Day",
                    "Christmas Day",
                });

            listView.ItemClick += (s, e) => Toast.MakeText(Activity, "Clicked item!", ToastLength.Short).Show();

            return parentView;
        }
    }
}