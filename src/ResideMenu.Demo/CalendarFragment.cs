using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Fragment = Android.Support.V4.App.Fragment;

namespace ResideMenu.Demo
{
    public class CalendarFragment : Fragment
    {

        private View parentView;
        private ListView listView;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            parentView = inflater.Inflate(Resource.Layout.calendar, container, false);
            listView = parentView.FindViewById<ListView>(Resource.Id.listView);
            initView();
            return parentView;
        }

        private void initView()
        {
            ArrayAdapter<string> arrayAdapter = new ArrayAdapter<string>(Activity, Android.Resource.Layout.SimpleListItem1, getCalendarData());
            listView.Adapter = arrayAdapter;
            listView.ItemClick += (s, e) => Toast.MakeText(Activity, "Clicked item!", ToastLength.Short).Show();
        }

        private List<string> getCalendarData()
        {
            List<string> calendarList = new List<string>();
            calendarList.Add("New Year's Day");
            calendarList.Add("St. Valentine's Day");
            calendarList.Add("Easter Day");
            calendarList.Add("April Fool's Day");
            calendarList.Add("Mother's Day");
            calendarList.Add("Memorial Day");
            calendarList.Add("National Flag Day");
            calendarList.Add("Father's Day");
            calendarList.Add("Independence Day");
            calendarList.Add("Labor Day");
            calendarList.Add("Columbus Day");
            calendarList.Add("Halloween");
            calendarList.Add("All Soul's Day");
            calendarList.Add("Veterans Day");
            calendarList.Add("Thanksgiving Day");
            calendarList.Add("Election Day");
            calendarList.Add("Forefather's Day");
            calendarList.Add("Christmas Day");
            return calendarList;
        }
    }
}