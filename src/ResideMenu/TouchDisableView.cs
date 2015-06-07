using Android.Content;
using Android.Util;
using Android.Views;

namespace AndroidResideMenu
{
    class TouchDisableView : ViewGroup
    {
        public bool IsTouchDisabled { get; set; }

        public TouchDisableView(Context context)
            : this(context, null)
        {
        }

        public TouchDisableView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
        }

        private View _content;
        public View Content
        {
            get { return _content; }
            set
            {
                if (_content != null)
                {
                    RemoveView(_content);
                }

                _content = value;
                AddView(_content);
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            int width = GetDefaultSize(0, widthMeasureSpec);
            int height = GetDefaultSize(0, heightMeasureSpec);
            SetMeasuredDimension(width, height);

            int contentWidth = GetChildMeasureSpec(widthMeasureSpec, 0, width);
            int contentHeight = GetChildMeasureSpec(heightMeasureSpec, 0, height);
            Content.Measure(contentWidth, contentHeight);
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            int width = r - l;
            int height = b - t;
            Content.Layout(0, 0, width, height);
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            return IsTouchDisabled;
        }
    }
}