using Android.Content;
using Android.Views;
using Android.Widget;

namespace AndroidResideMenu
{
    public class ResideMenuItem : LinearLayout
    {
        private ImageView _icon;
        private TextView _title;

        public ResideMenuItem(Context context)
            : base(context)
        {
            Init(context);
        }

        public ResideMenuItem(Context context, int icon, int title)
            : base(context)
        {
            Init(context);
            _title.SetText(title);
            _icon.SetImageResource(icon);
        }

        public ResideMenuItem(Context context, int icon, string title)
            : base(context)
        {
            Init(context);
            _title.Text = title;
            _icon.SetImageResource(icon);
        }

        private void Init(Context context)
        {
            LayoutInflater inflater = context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;
            inflater.Inflate(global::ResideMenu.Resource.Layout.residemenu_item, this);
            _icon = FindViewById<ImageView>(global::ResideMenu.Resource.Id.iv_icon);
            _title = FindViewById<TextView>(global::ResideMenu.Resource.Id.tv_title);
        }

        public void SetIcon(int icon)
        {
            _icon.SetImageResource(icon);
        }

        public void SetTitle(int title)
        {
            _title.SetText(title);
        }

        public void SetTitle(string title)
        {
            _title.Text = title;
        }
    }
}
