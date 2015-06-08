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

        private readonly ImageView _imageViewShadow;
        private readonly ImageView _imageViewBackground;
        private readonly LinearLayout _layoutLeftMenu;
        private readonly LinearLayout _layoutRightMenu;
        private readonly ScrollView _scrollViewLeftMenu;
        private readonly ScrollView _scrollViewRightMenu;
        private ScrollView scrollViewMenu;
        /** Current attaching activity. */
        private Activity activity;
        /** The DecorView of current activity. */
        private ViewGroup viewDecor;
        private TouchDisableView viewActivity;
        /** The flag of menu opening status. */
        public bool isOpened { get; private set; }
        private float shadowAdjustScaleX;
        private float shadowAdjustScaleY;
        /** Views which need stop to intercept touch events. */
        private List<View> ignoredViews;
        private List<ResideMenuItem> leftMenuItems;
        private List<ResideMenuItem> rightMenuItems;
        private DisplayMetrics displayMetrics = new DisplayMetrics();
        private IOnMenuListener menuListener;
        private float lastRawX;
        private bool _isInIgnoredView;
        private global::AndroidResideMenu.ResideMenu.Direction scaleDirection = Direction.Left;
        private PressedState pressedState = PressedState.Down;
        private List<Direction> disabledSwipeDirection = new List<Direction>();
        // Valid scale factor is between 0.0f and 1.0f.
        private float mScaleValue = 0.5f;
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
            SetPadding(viewActivity.PaddingLeft + insets.Left, viewActivity.PaddingTop + insets.Top, viewActivity.PaddingRight + insets.Right, viewActivity.PaddingBottom + insets.Bottom);
            insets.Left = insets.Top = insets.Right = insets.Bottom = 0;
            return true;
        }

        public void AttachToActivity(Activity activity)
        {
            this.activity = activity;
            leftMenuItems = new List<ResideMenuItem>();
            rightMenuItems = new List<ResideMenuItem>();
            ignoredViews = new List<View>();
            viewDecor = activity.Window.DecorView as ViewGroup;
            viewActivity = new TouchDisableView(activity);

            View mContent = viewDecor.GetChildAt(0);
            viewDecor.RemoveViewAt(0);
            viewActivity.Content = mContent;
            AddView(viewActivity);

            ViewGroup parent = _scrollViewLeftMenu.Parent as ViewGroup;
            parent.RemoveView(_scrollViewLeftMenu);
            parent.RemoveView(_scrollViewRightMenu);
            SetShadowAdjustScaleXByOrientation();
            viewDecor.AddView(this, 0);
        }

        private void SetShadowAdjustScaleXByOrientation()
        {
            Orientation orientation = Resources.Configuration.Orientation;
            if (orientation == Orientation.Landscape)
            {
                shadowAdjustScaleX = 0.034f;
                shadowAdjustScaleY = 0.12f;
            }
            else if (orientation == Orientation.Portrait)
            {
                shadowAdjustScaleX = 0.06f;
                shadowAdjustScaleY = 0.07f;
            }
        }

        public void setBackground(int imageResource)
        {
            _imageViewBackground.SetImageResource(imageResource);
        }

        public void setShadowVisible(bool isVisible)
        {
            if (isVisible)
                _imageViewShadow.SetBackgroundResource(global::ResideMenu.Resource.Drawable.shadow);
            else
                _imageViewShadow.SetBackgroundResource(0);
        }

        public void addMenuItem(ResideMenuItem menuItem, Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    this.leftMenuItems.Add(menuItem);
                _layoutLeftMenu.AddView(menuItem);
                    break;
                case Direction.Right:
                    this.rightMenuItems.Add(menuItem);
                _layoutRightMenu.AddView(menuItem);
                    break;
                default:
                    throw new Exception();
            }
        }

        public void setMenuItems(List<ResideMenuItem> menuItems, Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    this.leftMenuItems = menuItems;
                    break;
                case Direction.Right:
                    this.rightMenuItems = menuItems;
                    break;
                default:
                    break;
            }

            rebuildMenu();
        }

        private void rebuildMenu()
        {
            _layoutLeftMenu.RemoveAllViews();
            _layoutRightMenu.RemoveAllViews();
            foreach (ResideMenuItem leftMenuItem in leftMenuItems)
                _layoutLeftMenu.AddView(leftMenuItem);
            foreach (ResideMenuItem rightMenuItem in rightMenuItems)
                _layoutRightMenu.AddView(rightMenuItem);
        }

        public List<ResideMenuItem> GetMenuItems(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return leftMenuItems;
                case Direction.Right:
                    return rightMenuItems;
                default:
                    throw new Exception();
            }
        }

        public void SetMenuListener(IOnMenuListener menuListener)
        {
            this.menuListener = menuListener;
        }


        public IOnMenuListener GetMenuListener()
        {
            return menuListener;
        }

        public void OpenMenu(Direction direction)
        {
            SetScaleDirection(direction);

            isOpened = true;
            AnimatorSet scaleDown_activity = BuildScaleDownAnimation(viewActivity, mScaleValue, mScaleValue);
            AnimatorSet scaleDown_shadow = BuildScaleDownAnimation(_imageViewShadow, mScaleValue + shadowAdjustScaleX, mScaleValue + shadowAdjustScaleY);
            AnimatorSet alpha_menu = BuildMenuAnimation(scrollViewMenu, 1.0f);
            scaleDown_shadow.AddListener(_animatorListener);
            scaleDown_activity.PlayTogether(scaleDown_shadow);
            scaleDown_activity.PlayTogether(alpha_menu);
            scaleDown_activity.Start();
        }

        public void CloseMenu()
        {
            isOpened = false;
            AnimatorSet scaleUp_activity = BuildScaleUpAnimation(viewActivity, 1.0f, 1.0f);
            AnimatorSet scaleUp_shadow = BuildScaleUpAnimation(_imageViewShadow, 1.0f, 1.0f);
            AnimatorSet alpha_menu = BuildMenuAnimation(scrollViewMenu, 0.0f);
            scaleUp_activity.AddListener(_animatorListener);
            scaleUp_activity.PlayTogether(scaleUp_shadow);
            scaleUp_activity.PlayTogether(alpha_menu);
            scaleUp_activity.Start();
        }

        public void SetSwipeDirectionDisable(Direction direction)
        {
            disabledSwipeDirection.Add(direction);
        }

        private bool IsInDisableDirection(Direction direction)
        {
            return disabledSwipeDirection.Contains(direction);
        }

        private void SetScaleDirection(Direction direction)
        {

            int screenWidth = GetScreenWidth();
            float pivotX;
            float pivotY = GetScreenHeight() * 0.5f;

            switch (direction)
            {
                case Direction.Left:
                    scrollViewMenu = _scrollViewLeftMenu;
                    pivotX = screenWidth * 1.5f;
                    break;
                case Direction.Right:
                    scrollViewMenu = _scrollViewRightMenu;
                    pivotX = screenWidth * -0.5f;
                    break;
                default:
                    throw new Exception();
            }

            viewActivity.PivotX = pivotX;
            viewActivity.PivotY = pivotY;
            _imageViewShadow.PivotX = pivotX;
            _imageViewShadow.PivotY = pivotY;
            scaleDirection = direction;
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
                if (_outerInstance.isOpened)
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
                if (_outerInstance.isOpened)
                {
                    _outerInstance.ShowScrollViewMenu(_outerInstance.scrollViewMenu);

                    if (_outerInstance.menuListener != null)
                    {
                        _outerInstance.menuListener.OpenMenu();
                    }
                }
            }

            public void OnAnimationEnd(Animator animation)
            {
                if (_outerInstance.isOpened)
                {
                    _outerInstance.viewActivity.IsTouchDisabled = true;
                    _outerInstance.viewActivity.SetOnClickListener(_outerInstance._clickListener);
                }
                else
                {
                    _outerInstance.viewActivity.IsTouchDisabled = false;
                    _outerInstance.viewActivity.SetOnClickListener(null);
                    _outerInstance.HideScrollViewMenu(_outerInstance._scrollViewLeftMenu);
                    _outerInstance.HideScrollViewMenu(_outerInstance._scrollViewRightMenu);
                    if (_outerInstance.menuListener != null)
                    {
                        _outerInstance.menuListener.CloseMenu();
                    }
                }
            }
        }

        private AnimatorSet BuildScaleDownAnimation(View target, float targetScaleX, float targetScaleY)
        {

            AnimatorSet scaleDown = new AnimatorSet();
            scaleDown.PlayTogether(ObjectAnimator.OfFloat(target, "scaleX", targetScaleX), ObjectAnimator.OfFloat(target, "scaleY", targetScaleY));

            scaleDown.SetInterpolator(AnimationUtils.LoadInterpolator(activity, Android.Resource.Animation.DecelerateInterpolator));
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
            ignoredViews.Add(v);
        }

        public void RemoveIgnoredView(View v)
        {
            ignoredViews.Remove(v);
        }

        public void ClearIgnoredViewList()
        {
            ignoredViews.Clear();
        }

        private bool IsInIgnoredView(MotionEvent ev)
        {
            Rect rect = new Rect();
            foreach (View v in ignoredViews)
            {
                v.GetGlobalVisibleRect(rect);
                if (rect.Contains((int)ev.GetX(), (int)ev.GetY()))
                    return true;
            }
            return false;
        }

        private void SetScaleDirectionByRawX(float currentRawX)
        {
            if (currentRawX < lastRawX)
                SetScaleDirection(global::AndroidResideMenu.ResideMenu.Direction.Right);
            else
                SetScaleDirection(global::AndroidResideMenu.ResideMenu.Direction.Left);
        }

        private float GetTargetScale(float currentRawX)
        {
            float scaleFloatX = ((currentRawX - lastRawX) / GetScreenWidth()) * 0.75f;
            scaleFloatX = scaleDirection == Direction.Right ? -scaleFloatX : scaleFloatX;

            float targetScale = viewActivity.ScaleX - scaleFloatX;
            targetScale = targetScale > 1.0f ? 1.0f : targetScale;
            targetScale = targetScale < 0.5f ? 0.5f : targetScale;
            return targetScale;
        }

        private float lastActionDownX, lastActionDownY;

        public override bool DispatchTouchEvent(MotionEvent ev)
        {
            float currentActivityScaleX = viewActivity.ScaleX;
            if (currentActivityScaleX == 1.0f)
                SetScaleDirectionByRawX(ev.RawX);

            switch (ev.Action)
            {
                case MotionEventActions.Down:
                    lastActionDownX = ev.GetX();
                    lastActionDownY = ev.GetY();
                    _isInIgnoredView = IsInIgnoredView(ev) && !isOpened;
                    pressedState = PressedState.Down;
                    break;

                case MotionEventActions.Move:
                    if (_isInIgnoredView || IsInDisableDirection(scaleDirection))
                        break;

                    if (pressedState != PressedState.Down && pressedState != PressedState.Horizontal)
                        break;

                    int xOffset = (int)(ev.GetX() - lastActionDownX);
                    int yOffset = (int)(ev.GetY() - lastActionDownY);

                    if (pressedState == PressedState.Down)
                    {
                        if (yOffset > 25 || yOffset < -25)
                        {
                            pressedState = PressedState.Vertical;
                            break;
                        }
                        if (xOffset < -50 || xOffset > 50)
                        {
                            pressedState = PressedState.Horizontal;
                            ev.Action = MotionEventActions.Cancel;
                        }
                    }
                    else if (pressedState == PressedState.Horizontal)
                    {
                        if (currentActivityScaleX < 0.95)
                            ShowScrollViewMenu(scrollViewMenu);

                        float targetScale = GetTargetScale(ev.RawX);
                        viewActivity.ScaleX = targetScale;
                        viewActivity.ScaleY = targetScale;
                        _imageViewShadow.ScaleX = targetScale + shadowAdjustScaleX;
                        _imageViewShadow.ScaleY = targetScale + shadowAdjustScaleY;
                        scrollViewMenu.Alpha = (1 - targetScale) * 2.0F;

                        lastRawX = ev.RawX;
                        return true;
                    }

                    break;

                case MotionEventActions.Up:

                    if (_isInIgnoredView) break;
                    if (pressedState != PressedState.Horizontal) break;

                    pressedState = PressedState.Done;
                    if (isOpened)
                    {
                        if (currentActivityScaleX > 0.56f)
                            CloseMenu();
                        else
                            OpenMenu(scaleDirection);
                    }
                    else
                    {
                        if (currentActivityScaleX < 0.94f)
                        {
                            OpenMenu(scaleDirection);
                        }
                        else
                        {
                            CloseMenu();
                        }
                    }

                    break;

            }
            lastRawX = ev.RawX;
            return base.DispatchTouchEvent(ev);
        }

        public int GetScreenHeight()
        {
            activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            return displayMetrics.HeightPixels;
        }

        public int GetScreenWidth()
        {
            activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            return displayMetrics.WidthPixels;
        }

        public void SetScaleValue(float scaleValue)
        {
            this.mScaleValue = scaleValue;
        }

        public interface IOnMenuListener
        {
            void OpenMenu();
            void CloseMenu();
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
    }
}

