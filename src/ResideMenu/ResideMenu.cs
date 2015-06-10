using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using System;
using System.Collections.Generic;
using JavaObject = Java.Lang.Object;
using Orientation = Android.Content.Res.Orientation;

namespace AndroidResideMenu
{
    public class ResideMenu : FrameLayout
    {
        public event EventHandler MenuOpened;
        public event EventHandler MenuClosed;

        private readonly ImageView _imageViewShadow;
        private readonly ImageView _imageViewBackground;
        private readonly LinearLayout _layoutLeftMenu;
        private readonly LinearLayout _layoutRightMenu;
        private readonly ScrollView _scrollViewLeftMenu;
        private readonly ScrollView _scrollViewRightMenu;
        private ScrollView _scrollViewMenu;

        /** Current attaching activity. */
        private Activity _activity;

        /** The DecorView of current activity. */
        private ViewGroup _viewDecor;

        private TouchDisableView _viewActivity;

        /** The flag of menu opening status. */
        public bool IsOpened { get; private set; }

        private float _shadowAdjustScaleX;
        private float _shadowAdjustScaleY;

        /** Views which need stop to intercept touch events. */
        private List<View> _ignoredViews;
        private List<ResideMenuItem> _leftMenuItems;
        private List<ResideMenuItem> _rightMenuItems;
        private DisplayMetrics _displayMetrics = new DisplayMetrics();
        private IOnMenuListener _menuListener;
        private float _lastRawX;
        private bool _isInIgnoredView;
        private global::AndroidResideMenu.ResideMenu.Direction _scaleDirection = Direction.Left;
        private PressedState _pressedState = PressedState.Down;
        private List<Direction> _disabledSwipeDirection = new List<Direction>();

        // Valid scale factor is between 0.0f and 1.0f.
        private float _scaleValue = 0.5f;
        IOnClickListener _clickListener;
        Animator.IAnimatorListener _animatorListener;

        public ResideMenu(Context context)
            : base(context)
        {
            LayoutInflater inflater = context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;
            inflater.Inflate(global::ResideMenu.Resource.Layout.residemenu, this);
            _scrollViewLeftMenu = FindViewById<ScrollView>(global::ResideMenu.Resource.Id.sv_left_menu);
            _scrollViewRightMenu = FindViewById<ScrollView>(global::ResideMenu.Resource.Id.sv_right_menu);
            _imageViewShadow = FindViewById<ImageView>(global::ResideMenu.Resource.Id.iv_shadow);
            _layoutLeftMenu = FindViewById<LinearLayout>(global::ResideMenu.Resource.Id.layout_left_menu);
            _layoutRightMenu = FindViewById<LinearLayout>(global::ResideMenu.Resource.Id.layout_right_menu);
            _imageViewBackground = FindViewById<ImageView>(global::ResideMenu.Resource.Id.iv_background);

            _clickListener = new ClickListener(this);
            _animatorListener = new AnimatorListener(this);
        }

        protected override bool FitSystemWindows(Rect insets)
        {
            SetPadding(_viewActivity.PaddingLeft + insets.Left, _viewActivity.PaddingTop + insets.Top, _viewActivity.PaddingRight + insets.Right, _viewActivity.PaddingBottom + insets.Bottom);
            insets.Left = insets.Top = insets.Right = insets.Bottom = 0;
            return true;
        }

        public void AttachToActivity(Activity activity)
        {
            _activity = activity;
            _leftMenuItems = new List<ResideMenuItem>();
            _rightMenuItems = new List<ResideMenuItem>();
            _ignoredViews = new List<View>();
            _viewDecor = activity.Window.DecorView as ViewGroup;
            _viewActivity = new TouchDisableView(activity);

            View mContent = _viewDecor.GetChildAt(0);
            _viewDecor.RemoveViewAt(0);
            _viewActivity.Content = mContent;
            AddView(_viewActivity);

            ViewGroup parent = _scrollViewLeftMenu.Parent as ViewGroup;
            parent.RemoveView(_scrollViewLeftMenu);
            parent.RemoveView(_scrollViewRightMenu);
            SetShadowAdjustScaleXByOrientation();
            _viewDecor.AddView(this, 0);
        }

        private void SetShadowAdjustScaleXByOrientation()
        {
            Orientation orientation = Resources.Configuration.Orientation;
            if (orientation == Orientation.Landscape)
            {
                _shadowAdjustScaleX = 0.034f;
                _shadowAdjustScaleY = 0.12f;
            }
            else if (orientation == Orientation.Portrait)
            {
                _shadowAdjustScaleX = 0.06f;
                _shadowAdjustScaleY = 0.07f;
            }
        }

        public void SetBackground(int imageResource)
        {
            _imageViewBackground.SetImageResource(imageResource);
        }

        public void SetShadowVisible(bool isVisible)
        {
            if (isVisible)
                _imageViewShadow.SetBackgroundResource(global::ResideMenu.Resource.Drawable.shadow);
            else
                _imageViewShadow.SetBackgroundResource(0);
        }

        public void AddMenuItem(ResideMenuItem menuItem, Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    this._leftMenuItems.Add(menuItem);
                    _layoutLeftMenu.AddView(menuItem);
                    break;
                case Direction.Right:
                    this._rightMenuItems.Add(menuItem);
                    _layoutRightMenu.AddView(menuItem);
                    break;
                default:
                    throw new Exception();
            }
        }

        public void SetMenuItems(List<ResideMenuItem> menuItems, Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    this._leftMenuItems = menuItems;
                    break;
                case Direction.Right:
                    this._rightMenuItems = menuItems;
                    break;
                default:
                    break;
            }

            RebuildMenu();
        }

        private void RebuildMenu()
        {
            _layoutLeftMenu.RemoveAllViews();
            _layoutRightMenu.RemoveAllViews();
            foreach (ResideMenuItem leftMenuItem in _leftMenuItems)
                _layoutLeftMenu.AddView(leftMenuItem);
            foreach (ResideMenuItem rightMenuItem in _rightMenuItems)
                _layoutRightMenu.AddView(rightMenuItem);
        }

        public List<ResideMenuItem> GetMenuItems(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return _leftMenuItems;
                case Direction.Right:
                    return _rightMenuItems;
                default:
                    throw new Exception();
            }
        }

        public void SetMenuListener(IOnMenuListener menuListener)
        {
            this._menuListener = menuListener;
        }


        public IOnMenuListener GetMenuListener()
        {
            return _menuListener;
        }

        public void OpenMenu(Direction direction)
        {
            SetScaleDirection(direction);

            IsOpened = true;
            AnimatorSet scaleDown_activity = BuildScaleDownAnimation(_viewActivity, _scaleValue, _scaleValue);
            AnimatorSet scaleDown_shadow = BuildScaleDownAnimation(_imageViewShadow, _scaleValue + _shadowAdjustScaleX, _scaleValue + _shadowAdjustScaleY);
            AnimatorSet alpha_menu = BuildMenuAnimation(_scrollViewMenu, 1.0f);
            scaleDown_shadow.AddListener(_animatorListener);
            scaleDown_activity.PlayTogether(scaleDown_shadow);
            scaleDown_activity.PlayTogether(alpha_menu);
            scaleDown_activity.Start();
        }

        public void CloseMenu()
        {
            IsOpened = false;
            AnimatorSet scaleUp_activity = BuildScaleUpAnimation(_viewActivity, 1.0f, 1.0f);
            AnimatorSet scaleUp_shadow = BuildScaleUpAnimation(_imageViewShadow, 1.0f, 1.0f);
            AnimatorSet alpha_menu = BuildMenuAnimation(_scrollViewMenu, 0.0f);
            scaleUp_activity.AddListener(_animatorListener);
            scaleUp_activity.PlayTogether(scaleUp_shadow);
            scaleUp_activity.PlayTogether(alpha_menu);
            scaleUp_activity.Start();
        }

        public void SetSwipeDirectionDisable(Direction direction)
        {
            _disabledSwipeDirection.Add(direction);
        }

        private bool IsInDisableDirection(Direction direction)
        {
            return _disabledSwipeDirection.Contains(direction);
        }

        private void SetScaleDirection(Direction direction)
        {

            int screenWidth = GetScreenWidth();
            float pivotX;
            float pivotY = GetScreenHeight() * 0.5f;

            switch (direction)
            {
                case Direction.Left:
                    _scrollViewMenu = _scrollViewLeftMenu;
                    pivotX = screenWidth * 1.5f;
                    break;
                case Direction.Right:
                    _scrollViewMenu = _scrollViewRightMenu;
                    pivotX = screenWidth * -0.5f;
                    break;
                default:
                    throw new Exception();
            }

            _viewActivity.PivotX = pivotX;
            _viewActivity.PivotY = pivotY;
            _imageViewShadow.PivotX = pivotX;
            _imageViewShadow.PivotY = pivotY;
            _scaleDirection = direction;
        }

        private class ClickListener : JavaObject, IOnClickListener
        {
            private readonly ResideMenu _outerInstance;
            public ClickListener(ResideMenu outerInstance)
            {
                _outerInstance = outerInstance;
            }

            public void OnClick(View v)
            {
                if (_outerInstance.IsOpened)
                {
                    _outerInstance.CloseMenu();
                }
            }
        }

        private class AnimatorListener : JavaObject, Animator.IAnimatorListener
        {
            private readonly ResideMenu _outerInstance;
            public AnimatorListener(ResideMenu outerInstance)
            {
                _outerInstance = outerInstance;
            }

            public void OnAnimationCancel(Animator animation)
            {
                throw new NotImplementedException();
            }

            public void OnAnimationRepeat(Animator animation)
            {
                throw new NotImplementedException();
            }

            public void OnAnimationStart(Animator animation)
            {
                if (_outerInstance.IsOpened)
                {
                    _outerInstance.ShowScrollViewMenu(_outerInstance._scrollViewMenu);

                    if (_outerInstance._menuListener != null)
                    {
                        _outerInstance._menuListener.OpenMenu();
                    }

                    var handler = _outerInstance.MenuOpened;

                    if(handler != null)
                    {
                        handler(_outerInstance, EventArgs.Empty);
                    }
                }
            }

            public void OnAnimationEnd(Animator animation)
            {
                if (_outerInstance.IsOpened)
                {
                    _outerInstance._viewActivity.IsTouchDisabled = true;
                    _outerInstance._viewActivity.SetOnClickListener(_outerInstance._clickListener);
                }
                else
                {
                    _outerInstance._viewActivity.IsTouchDisabled = false;
                    _outerInstance._viewActivity.SetOnClickListener(null);
                    _outerInstance.HideScrollViewMenu(_outerInstance._scrollViewLeftMenu);
                    _outerInstance.HideScrollViewMenu(_outerInstance._scrollViewRightMenu);

                    if (_outerInstance._menuListener != null)
                    {
                        _outerInstance._menuListener.CloseMenu();
                    }

                    var handler = _outerInstance.MenuClosed;

                    if (handler != null)
                    {
                        handler(_outerInstance, EventArgs.Empty);
                    }
                }
            }
        }

        private AnimatorSet BuildScaleDownAnimation(View target, float targetScaleX, float targetScaleY)
        {
            AnimatorSet scaleDown = new AnimatorSet();
            scaleDown.PlayTogether(ObjectAnimator.OfFloat(target, "scaleX", targetScaleX), ObjectAnimator.OfFloat(target, "scaleY", targetScaleY));

            scaleDown.SetInterpolator(AnimationUtils.LoadInterpolator(_activity, Android.Resource.Animation.DecelerateInterpolator));
            scaleDown.SetDuration(250);
            return scaleDown;
        }

        private AnimatorSet BuildScaleUpAnimation(View target, float targetScaleX, float targetScaleY)
        {
            AnimatorSet scaleUp = new AnimatorSet();
            scaleUp.PlayTogether(ObjectAnimator.OfFloat(target, "scaleX", targetScaleX), ObjectAnimator.OfFloat(target, "scaleY", targetScaleY));
            scaleUp.SetDuration(250);
            return scaleUp;
        }

        private AnimatorSet BuildMenuAnimation(View target, float alpha)
        {

            AnimatorSet alphaAnimation = new AnimatorSet();
            alphaAnimation.PlayTogether(ObjectAnimator.OfFloat(target, "alpha", alpha));
            alphaAnimation.SetDuration(250);
            return alphaAnimation;
        }

        public void AddIgnoredView(View v)
        {
            _ignoredViews.Add(v);
        }

        public void RemoveIgnoredView(View v)
        {
            _ignoredViews.Remove(v);
        }

        public void ClearIgnoredViewList()
        {
            _ignoredViews.Clear();
        }

        private bool IsInIgnoredView(MotionEvent ev)
        {
            Rect rect = new Rect();
            foreach (View v in _ignoredViews)
            {
                v.GetGlobalVisibleRect(rect);
                if (rect.Contains((int)ev.GetX(), (int)ev.GetY()))
                    return true;
            }
            return false;
        }

        private void SetScaleDirectionByRawX(float currentRawX)
        {
            if (currentRawX < _lastRawX)
                SetScaleDirection(global::AndroidResideMenu.ResideMenu.Direction.Right);
            else
                SetScaleDirection(global::AndroidResideMenu.ResideMenu.Direction.Left);
        }

        private float GetTargetScale(float currentRawX)
        {
            float scaleFloatX = ((currentRawX - _lastRawX) / GetScreenWidth()) * 0.75f;
            scaleFloatX = _scaleDirection == Direction.Right ? -scaleFloatX : scaleFloatX;

            float targetScale = _viewActivity.ScaleX - scaleFloatX;
            targetScale = targetScale > 1.0f ? 1.0f : targetScale;
            targetScale = targetScale < 0.5f ? 0.5f : targetScale;
            return targetScale;
        }

        private float lastActionDownX, lastActionDownY;

        public override bool DispatchTouchEvent(MotionEvent ev)
        {
            float currentActivityScaleX = _viewActivity.ScaleX;
            if (currentActivityScaleX == 1.0f)
                SetScaleDirectionByRawX(ev.RawX);

            switch (ev.Action)
            {
                case MotionEventActions.Down:
                    lastActionDownX = ev.GetX();
                    lastActionDownY = ev.GetY();
                    _isInIgnoredView = IsInIgnoredView(ev) && !IsOpened;
                    _pressedState = PressedState.Down;
                    break;

                case MotionEventActions.Move:
                    if (_isInIgnoredView || IsInDisableDirection(_scaleDirection))
                        break;

                    if (_pressedState != PressedState.Down && _pressedState != PressedState.Horizontal)
                        break;

                    int xOffset = (int)(ev.GetX() - lastActionDownX);
                    int yOffset = (int)(ev.GetY() - lastActionDownY);

                    if (_pressedState == PressedState.Down)
                    {
                        if (yOffset > 25 || yOffset < -25)
                        {
                            _pressedState = PressedState.Vertical;
                            break;
                        }
                        if (xOffset < -50 || xOffset > 50)
                        {
                            _pressedState = PressedState.Horizontal;
                            ev.Action = MotionEventActions.Cancel;
                        }
                    }
                    else if (_pressedState == PressedState.Horizontal)
                    {
                        if (currentActivityScaleX < 0.95)
                            ShowScrollViewMenu(_scrollViewMenu);

                        float targetScale = GetTargetScale(ev.RawX);
                        _viewActivity.ScaleX = targetScale;
                        _viewActivity.ScaleY = targetScale;
                        _imageViewShadow.ScaleX = targetScale + _shadowAdjustScaleX;
                        _imageViewShadow.ScaleY = targetScale + _shadowAdjustScaleY;
                        _scrollViewMenu.Alpha = (1 - targetScale) * 2.0F;

                        _lastRawX = ev.RawX;
                        return true;
                    }

                    break;

                case MotionEventActions.Up:

                    if (_isInIgnoredView) break;
                    if (_pressedState != PressedState.Horizontal) break;

                    _pressedState = PressedState.Done;
                    if (IsOpened)
                    {
                        if (currentActivityScaleX > 0.56f)
                            CloseMenu();
                        else
                            OpenMenu(_scaleDirection);
                    }
                    else
                    {
                        if (currentActivityScaleX < 0.94f)
                        {
                            OpenMenu(_scaleDirection);
                        }
                        else
                        {
                            CloseMenu();
                        }
                    }

                    break;

            }
            _lastRawX = ev.RawX;
            return base.DispatchTouchEvent(ev);
        }

        public int GetScreenHeight()
        {
            _activity.WindowManager.DefaultDisplay.GetMetrics(_displayMetrics);
            return _displayMetrics.HeightPixels;
        }

        public int GetScreenWidth()
        {
            _activity.WindowManager.DefaultDisplay.GetMetrics(_displayMetrics);
            return _displayMetrics.WidthPixels;
        }

        public void SetScaleValue(float scaleValue)
        {
            _scaleValue = scaleValue;
        }

        private void ShowScrollViewMenu(ScrollView scrollViewMenu)
        {
            if (scrollViewMenu != null && scrollViewMenu.Parent == null)
            {
                AddView(scrollViewMenu);
            }
        }

        private void HideScrollViewMenu(ScrollView scrollViewMenu)
        {
            if (scrollViewMenu != null && scrollViewMenu.Parent != null)
            {
                RemoveView(scrollViewMenu);
            }
        }

        public enum Direction
        {
            Left,
            Right
        }

        private enum PressedState
        {
            Horizontal = 2,
            Down = 3,
            Done = 4,
            Vertical = 5
        }

        public interface IOnMenuListener
        {
            void OpenMenu();
            void CloseMenu();
        }
    }
}

