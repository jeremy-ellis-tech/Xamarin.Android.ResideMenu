using Android;
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
using Orientation = Android.Content.Res.Orientation;
using JavaObject = Java.Lang.Object;

namespace AndroidResideMenu
{
    public class ResideMenu : FrameLayout
    {
        public static int DIRECTION_LEFT = 0;
        public static int DIRECTION_RIGHT = 1;

        public enum Direction
        {
            Left,
            Right
        }


        private static int PRESSED_MOVE_HORIZONTAL = 2;
        private static int PRESSED_DOWN = 3;
        private static int PRESSED_DONE = 4;
        private static int PRESSED_MOVE_VERTICAL = 5;

        private ImageView imageViewShadow;
        private ImageView imageViewBackground;
        private LinearLayout layoutLeftMenu;
        private LinearLayout layoutRightMenu;
        private ScrollView scrollViewLeftMenu;
        private ScrollView scrollViewRightMenu;
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
        private int pressedState = PRESSED_DOWN;
        private List<Direction> disabledSwipeDirection = new List<Direction>();
        // Valid scale factor is between 0.0f and 1.0f.
        private float mScaleValue = 0.5f;
        IOnClickListener _clickListener;
        Animator.IAnimatorListener _animatorListener;

        public ResideMenu(Context context)
            : base(context)
        {
            initViews(context);
        }

        private void initViews(Context context)
        {
            LayoutInflater inflater = context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;
            inflater.Inflate(global::ResideMenu.Resource.Layout.residemenu, this);
            scrollViewLeftMenu = FindViewById<ScrollView>(global::ResideMenu.Resource.Id.sv_left_menu);
            scrollViewRightMenu = FindViewById<ScrollView>(global::ResideMenu.Resource.Id.sv_right_menu);
            imageViewShadow = FindViewById<ImageView>(global::ResideMenu.Resource.Id.iv_shadow);
            layoutLeftMenu = FindViewById<LinearLayout>(global::ResideMenu.Resource.Id.layout_left_menu);
            layoutRightMenu = FindViewById<LinearLayout>(global::ResideMenu.Resource.Id.layout_right_menu);
            imageViewBackground = FindViewById<ImageView>(global::ResideMenu.Resource.Id.iv_background);

            _clickListener = new ClickListener(this);
            _animatorListener = new AnimatorListener(this);
        }

        protected override bool FitSystemWindows(Rect insets)
        {
            SetPadding(viewActivity.PaddingLeft + insets.Left, viewActivity.PaddingTop + insets.Top, viewActivity.PaddingRight + insets.Right, viewActivity.PaddingBottom + insets.Bottom);
            insets.Left = insets.Top = insets.Right = insets.Bottom = 0;
            return true;
        }

        public void attachToActivity(Activity activity)
        {
            initValue(activity);
            SetShadowAdjustScaleXByOrientation();
            viewDecor.AddView(this, 0);
        }

        private void initValue(Activity activity)
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

            ViewGroup parent = scrollViewLeftMenu.Parent as ViewGroup;
            parent.RemoveView(scrollViewLeftMenu);
            parent.RemoveView(scrollViewRightMenu);
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
            imageViewBackground.SetImageResource(imageResource);
        }

        public void setShadowVisible(bool isVisible)
        {
            if (isVisible)
                imageViewShadow.SetBackgroundResource(global::ResideMenu.Resource.Drawable.shadow);
            else
                imageViewShadow.SetBackgroundResource(0);
        }

        [Obsolete]
        public void addMenuItem(ResideMenuItem menuItem)
        {
            this.leftMenuItems.Add(menuItem);
            layoutLeftMenu.AddView(menuItem);
        }

        public void addMenuItem(ResideMenuItem menuItem, int direction)
        {
            if (direction == DIRECTION_LEFT)
            {
                this.leftMenuItems.Add(menuItem);
                layoutLeftMenu.AddView(menuItem);
            }
            else
            {
                this.rightMenuItems.Add(menuItem);
                layoutRightMenu.AddView(menuItem);
            }
        }

        [Obsolete("Will be removed from v2.0")]
        public void setMenuItems(List<ResideMenuItem> menuItems)
        {
            this.leftMenuItems = menuItems;
            rebuildMenu();
        }

        public void setMenuItems(List<ResideMenuItem> menuItems, int direction)
        {
            if (direction == DIRECTION_LEFT)
                this.leftMenuItems = menuItems;
            else
                this.rightMenuItems = menuItems;
            rebuildMenu();
        }

        private void rebuildMenu()
        {
            layoutLeftMenu.RemoveAllViews();
            layoutRightMenu.RemoveAllViews();
            foreach (ResideMenuItem leftMenuItem in leftMenuItems)
                layoutLeftMenu.AddView(leftMenuItem);
            foreach (ResideMenuItem rightMenuItem in rightMenuItems)
                layoutRightMenu.AddView(rightMenuItem);
        }

        [Obsolete("Will be removed v2.0")]
        public List<ResideMenuItem> getMenuItems()
        {
            return leftMenuItems;
        }

        public List<ResideMenuItem> getMenuItems(int direction)
        {
            if (direction == DIRECTION_LEFT)
                return leftMenuItems;
            else
                return rightMenuItems;
        }

        public void setMenuListener(IOnMenuListener menuListener)
        {
            this.menuListener = menuListener;
        }


        public IOnMenuListener getMenuListener()
        {
            return menuListener;
        }

        public void OpenMenu(Direction direction)
        {
            SetScaleDirection(direction);

            isOpened = true;
            AnimatorSet scaleDown_activity = buildScaleDownAnimation(viewActivity, mScaleValue, mScaleValue);
            AnimatorSet scaleDown_shadow = buildScaleDownAnimation(imageViewShadow, mScaleValue + shadowAdjustScaleX, mScaleValue + shadowAdjustScaleY);
            AnimatorSet alpha_menu = buildMenuAnimation(scrollViewMenu, 1.0f);
            scaleDown_shadow.AddListener(_animatorListener);
            scaleDown_activity.PlayTogether(scaleDown_shadow);
            scaleDown_activity.PlayTogether(alpha_menu);
            scaleDown_activity.Start();
        }

        public void CloseMenu()
        {
            isOpened = false;
            AnimatorSet scaleUp_activity = buildScaleUpAnimation(viewActivity, 1.0f, 1.0f);
            AnimatorSet scaleUp_shadow = buildScaleUpAnimation(imageViewShadow, 1.0f, 1.0f);
            AnimatorSet alpha_menu = buildMenuAnimation(scrollViewMenu, 0.0f);
            scaleUp_activity.AddListener(_animatorListener);
            scaleUp_activity.PlayTogether(scaleUp_shadow);
            scaleUp_activity.PlayTogether(alpha_menu);
            scaleUp_activity.Start();
        }

        [Obsolete("Will be removed in v2.0")]
        public void setDirectionDisable(Direction direction)
        {
            disabledSwipeDirection.Add(direction);
        }

        public void setSwipeDirectionDisable(Direction direction)
        {
            disabledSwipeDirection.Add(direction);
        }

        private bool isInDisableDirection(Direction direction)
        {
            return disabledSwipeDirection.Contains(direction);
        }

        private void SetScaleDirection(Direction direction)
        {

            int screenWidth = getScreenWidth();
            float pivotX;
            float pivotY = getScreenHeight() * 0.5f;

            switch (direction)
            {
                case Direction.Left:
                    scrollViewMenu = scrollViewLeftMenu;
                    pivotX = screenWidth * 1.5f;
                    break;
                case Direction.Right:
                    scrollViewMenu = scrollViewRightMenu;
                    pivotX = screenWidth * -0.5f;
                    break;
                default:
                    throw new Exception();
            }

            viewActivity.PivotX = pivotX;
            viewActivity.PivotY = pivotY;
            imageViewShadow.PivotX = pivotX;
            imageViewShadow.PivotY = pivotY;
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
                        _outerInstance.menuListener.openMenu();
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
                    _outerInstance.HideScrollViewMenu(_outerInstance.scrollViewLeftMenu);
                    _outerInstance.HideScrollViewMenu(_outerInstance.scrollViewRightMenu);
                    if (_outerInstance.menuListener != null)
                    {
                        _outerInstance.menuListener.closeMenu();
                    }
                }
            }
        }

        private AnimatorSet buildScaleDownAnimation(View target, float targetScaleX, float targetScaleY)
        {

            AnimatorSet scaleDown = new AnimatorSet();
            scaleDown.PlayTogether(ObjectAnimator.OfFloat(target, "scaleX", targetScaleX), ObjectAnimator.OfFloat(target, "scaleY", targetScaleY));

            scaleDown.SetInterpolator(AnimationUtils.LoadInterpolator(activity, Android.Resource.Animation.DecelerateInterpolator));
            scaleDown.SetDuration(250);
            return scaleDown;
        }

        private AnimatorSet buildScaleUpAnimation(View target, float targetScaleX, float targetScaleY)
        {

            AnimatorSet scaleUp = new AnimatorSet();
            scaleUp.PlayTogether(ObjectAnimator.OfFloat(target, "scaleX", targetScaleX), ObjectAnimator.OfFloat(target, "scaleY", targetScaleY));
            scaleUp.SetDuration(250);
            return scaleUp;
        }

        private AnimatorSet buildMenuAnimation(View target, float alpha)
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

        /**
         * Remove a view from ignored views;
         * @param v
         */
        public void removeIgnoredView(View v)
        {
            ignoredViews.Remove(v);
        }

        /**
         * Clear the ignored view list;
         */
        public void clearIgnoredViewList()
        {
            ignoredViews.Clear();
        }

        private bool isInIgnoredView(MotionEvent ev)
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
            float scaleFloatX = ((currentRawX - lastRawX) / getScreenWidth()) * 0.75f;
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
                    _isInIgnoredView = isInIgnoredView(ev) && !isOpened;
                    pressedState = PRESSED_DOWN;
                    break;

                case MotionEventActions.Move:
                    if (_isInIgnoredView || isInDisableDirection(scaleDirection))
                        break;

                    if (pressedState != PRESSED_DOWN &&
                            pressedState != PRESSED_MOVE_HORIZONTAL)
                        break;

                    int xOffset = (int)(ev.GetX() - lastActionDownX);
                    int yOffset = (int)(ev.GetY() - lastActionDownY);

                    if (pressedState == PRESSED_DOWN)
                    {
                        if (yOffset > 25 || yOffset < -25)
                        {
                            pressedState = PRESSED_MOVE_VERTICAL;
                            break;
                        }
                        if (xOffset < -50 || xOffset > 50)
                        {
                            pressedState = PRESSED_MOVE_HORIZONTAL;
                            ev.Action = MotionEventActions.Cancel;
                        }
                    }
                    else if (pressedState == PRESSED_MOVE_HORIZONTAL)
                    {
                        if (currentActivityScaleX < 0.95)
                            ShowScrollViewMenu(scrollViewMenu);

                        float targetScale = GetTargetScale(ev.RawX);
                        viewActivity.ScaleX = targetScale;
                        viewActivity.ScaleY = targetScale;
                        imageViewShadow.ScaleX = targetScale + shadowAdjustScaleX;
                        imageViewShadow.ScaleY = targetScale + shadowAdjustScaleY;
                        scrollViewMenu.Alpha = (1 - targetScale) * 2.0F;

                        lastRawX = ev.RawX;
                        return true;
                    }

                    break;

                case MotionEventActions.Up:

                    if (_isInIgnoredView) break;
                    if (pressedState != PRESSED_MOVE_HORIZONTAL) break;

                    pressedState = PRESSED_DONE;
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

        public int getScreenHeight()
        {
            activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            return displayMetrics.HeightPixels;
        }

        public int getScreenWidth()
        {
            activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            return displayMetrics.WidthPixels;
        }

        public void setScaleValue(float scaleValue)
        {
            this.mScaleValue = scaleValue;
        }

        public interface IOnMenuListener
        {
            void openMenu();
            void closeMenu();
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

