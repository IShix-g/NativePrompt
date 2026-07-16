package com.ishix.nativeprompt;

import android.app.Activity;
import android.content.Context;
import android.content.res.ColorStateList;
import android.graphics.Color;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.text.TextUtils;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.WindowInsets;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.Space;
import android.widget.TextView;

import com.unity3d.player.UnityPlayer;

public final class NativeLoading {
    private static final int POSITION_CENTER = 0;
    private static final int POSITION_TOP_LEFT = 1;
    private static final int POSITION_TOP_RIGHT = 2;
    private static final int POSITION_BOTTOM_LEFT = 3;
    private static final int POSITION_BOTTOM_RIGHT = 4;
    private static final int SIZE_SMALL = 0;
    private static final int SIZE_LARGE = 2;
    private static final Handler MAIN_HANDLER = new Handler(Looper.getMainLooper());
    private static State state;

    private NativeLoading() {
    }

    public static void show(
            final String requestId,
            final String message,
            final boolean blocksInteraction,
            final boolean showsBackground,
            final float backgroundRed,
            final float backgroundGreen,
            final float backgroundBlue,
            final float backgroundOpacity,
            final int position,
            final int size,
            final float showDelaySeconds) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            throw new IllegalStateException("Unity activity is not available.");
        }

        activity.runOnUiThread(() -> showOnUiThread(
                activity,
                requestId,
                message,
                blocksInteraction,
                showsBackground,
                backgroundRed,
                backgroundGreen,
                backgroundBlue,
                backgroundOpacity,
                position,
                size,
                showDelaySeconds));
    }

    public static void dismiss(final String requestId) {
        MAIN_HANDLER.post(() -> {
            if (state != null && TextUtils.equals(state.requestId, requestId)) {
                state.remove();
                state = null;
            }
        });
    }

    public static void reset() {
        MAIN_HANDLER.post(() -> {
            if (state != null) {
                state.remove();
                state = null;
            }
        });
    }

    private static void showOnUiThread(
            Activity activity,
            String requestId,
            String message,
            boolean blocksInteraction,
            boolean showsBackground,
            float backgroundRed,
            float backgroundGreen,
            float backgroundBlue,
            float backgroundOpacity,
            int position,
            int size,
            float showDelaySeconds) {
        FrameLayout root = activity.findViewById(android.R.id.content);
        if (root == null) {
            throw new IllegalStateException("Unity content view is not available.");
        }

        if (state == null || state.root != root) {
            if (state != null) {
                state.remove();
            }
            state = new State(activity, root);
        }

        state.configure(
                requestId,
                message,
                blocksInteraction,
                showsBackground,
                backgroundRed,
                backgroundGreen,
                backgroundBlue,
                backgroundOpacity,
                position,
                size,
                showDelaySeconds);
    }

    private static int dp(Context context, int value) {
        return Math.round(TypedValue.applyDimension(
                TypedValue.COMPLEX_UNIT_DIP,
                value,
                context.getResources().getDisplayMetrics()));
    }

    private static int colorComponent(float value) {
        return Math.round(Math.max(0f, Math.min(1f, value)) * 255f);
    }

    private static final class State {
        private final Activity activity;
        private final FrameLayout root;
        private final BlockingFrameLayout overlay;
        private final View background;
        private LinearLayout content;
        private Runnable showVisuals;
        private String requestId;
        private int position;

        private State(Activity activity, FrameLayout root) {
            this.activity = activity;
            this.root = root;
            overlay = new BlockingFrameLayout(activity);
            overlay.setClipChildren(false);
            overlay.setClipToPadding(false);
            background = new View(activity);
            overlay.addView(background, new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.MATCH_PARENT));
            root.addView(overlay, new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.MATCH_PARENT));
            overlay.setOnApplyWindowInsetsListener((view, insets) -> {
                updateContentLayout(insets);
                return insets;
            });
        }

        private void configure(
                String requestId,
                String message,
                boolean blocksInteraction,
                boolean showsBackground,
                float backgroundRed,
                float backgroundGreen,
                float backgroundBlue,
                float backgroundOpacity,
                int position,
                int size,
                float showDelaySeconds) {
            cancelDelayedShow();
            this.requestId = requestId;
            this.position = position;
            overlay.setBlocksInteraction(blocksInteraction);
            overlay.bringToFront();

            background.setVisibility(View.GONE);
            background.setBackgroundColor(Color.argb(
                    colorComponent(backgroundOpacity),
                    colorComponent(backgroundRed),
                    colorComponent(backgroundGreen),
                    colorComponent(backgroundBlue)));

            if (content != null) {
                overlay.removeView(content);
            }
            content = createContent(message, size);
            content.setVisibility(View.GONE);
            overlay.addView(content, createContentLayoutParams(position));
            updateContentLayout(overlay.getRootWindowInsets());
            overlay.requestApplyInsets();

            showVisuals = () -> {
                if (!TextUtils.equals(this.requestId, requestId)) {
                    return;
                }
                background.setVisibility(showsBackground ? View.VISIBLE : View.GONE);
                content.setVisibility(View.VISIBLE);
                showVisuals = null;
            };

            long delayMillis = Math.max(0L, (long) (showDelaySeconds * 1000f));
            if (delayMillis == 0L) {
                showVisuals.run();
            } else {
                MAIN_HANDLER.postDelayed(showVisuals, delayMillis);
            }
        }

        private LinearLayout createContent(String message, int size) {
            LinearLayout group = new LinearLayout(activity);
            group.setOrientation(LinearLayout.HORIZONTAL);
            group.setGravity(Gravity.CENTER);

            int progressStyle = size == SIZE_SMALL
                    ? android.R.attr.progressBarStyleSmall
                    : size == SIZE_LARGE
                            ? android.R.attr.progressBarStyleLarge
                            : android.R.attr.progressBarStyle;
            ProgressBar spinner = new ProgressBar(activity, null, progressStyle);
            spinner.setIndeterminate(true);
            group.addView(spinner, new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WRAP_CONTENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT));

            if (!TextUtils.isEmpty(message)) {
                Space spacing = new Space(activity);
                group.addView(spacing, new LinearLayout.LayoutParams(
                        dp(activity, 8),
                        1));

                TextView label = new TextView(activity);
                label.setText(message);
                label.setTextAppearance(android.R.style.TextAppearance_Material_Body1);
                label.setGravity(Gravity.CENTER_VERTICAL);
                label.setMaxLines(4);
                label.setEllipsize(TextUtils.TruncateAt.END);
                label.setTag("NativePromptLoadingMessage");
                applyPrimaryTextColor(label);
                group.addView(label, new LinearLayout.LayoutParams(
                        ViewGroup.LayoutParams.WRAP_CONTENT,
                        ViewGroup.LayoutParams.WRAP_CONTENT));
            }

            return group;
        }

        private void applyPrimaryTextColor(TextView label) {
            TypedValue value = new TypedValue();
            if (!activity.getTheme().resolveAttribute(
                    android.R.attr.textColorPrimary,
                    value,
                    true)) {
                return;
            }

            if (value.resourceId != 0) {
                ColorStateList colors = activity.getResources().getColorStateList(
                        value.resourceId,
                        activity.getTheme());
                label.setTextColor(colors);
            } else if (value.type >= TypedValue.TYPE_FIRST_COLOR_INT &&
                    value.type <= TypedValue.TYPE_LAST_COLOR_INT) {
                label.setTextColor(value.data);
            }
        }

        private FrameLayout.LayoutParams createContentLayoutParams(int position) {
            FrameLayout.LayoutParams params = new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.WRAP_CONTENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT);
            params.gravity = gravityFor(position);
            return params;
        }

        private int gravityFor(int position) {
            switch (position) {
                case POSITION_TOP_LEFT:
                    return Gravity.TOP | Gravity.LEFT;
                case POSITION_TOP_RIGHT:
                    return Gravity.TOP | Gravity.RIGHT;
                case POSITION_BOTTOM_LEFT:
                    return Gravity.BOTTOM | Gravity.LEFT;
                case POSITION_BOTTOM_RIGHT:
                    return Gravity.BOTTOM | Gravity.RIGHT;
                case POSITION_CENTER:
                default:
                    return Gravity.CENTER;
            }
        }

        @SuppressWarnings("deprecation")
        private void updateContentLayout(WindowInsets insets) {
            if (content == null) {
                return;
            }

            int insetLeft = 0;
            int insetTop = 0;
            int insetRight = 0;
            int insetBottom = 0;
            if (insets != null) {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                    android.graphics.Insets systemBars =
                            insets.getInsets(WindowInsets.Type.systemBars());
                    insetLeft = systemBars.left;
                    insetTop = systemBars.top;
                    insetRight = systemBars.right;
                    insetBottom = systemBars.bottom;
                } else {
                    insetLeft = insets.getSystemWindowInsetLeft();
                    insetTop = insets.getSystemWindowInsetTop();
                    insetRight = insets.getSystemWindowInsetRight();
                    insetBottom = insets.getSystemWindowInsetBottom();
                }
            }

            int margin = dp(activity, 24);
            FrameLayout.LayoutParams params = (FrameLayout.LayoutParams) content.getLayoutParams();
            params.gravity = gravityFor(position);
            params.leftMargin = insetLeft + margin;
            params.topMargin = insetTop + margin;
            params.rightMargin = insetRight + margin;
            params.bottomMargin = insetBottom + margin;
            int maximumWidth = Math.max(
                    dp(activity, 80),
                    Math.max(overlay.getWidth(), root.getWidth()) -
                            insetLeft - insetRight - margin * 2);
            params.width = ViewGroup.LayoutParams.WRAP_CONTENT;
            for (int index = 0; index < content.getChildCount(); index++) {
                View child = content.getChildAt(index);
                if (child instanceof TextView &&
                        "NativePromptLoadingMessage".equals(child.getTag())) {
                    ((TextView) child).setMaxWidth(Math.max(
                            dp(activity, 48),
                            maximumWidth - dp(activity, 72)));
                }
            }
            content.setLayoutParams(params);
        }

        private void cancelDelayedShow() {
            if (showVisuals != null) {
                MAIN_HANDLER.removeCallbacks(showVisuals);
                showVisuals = null;
            }
        }

        private void remove() {
            cancelDelayedShow();
            requestId = null;
            root.removeView(overlay);
        }
    }

    private static final class BlockingFrameLayout extends FrameLayout {
        private boolean blocksInteraction;

        private BlockingFrameLayout(Context context) {
            super(context);
        }

        private void setBlocksInteraction(boolean blocksInteraction) {
            this.blocksInteraction = blocksInteraction;
            setClickable(blocksInteraction);
        }

        @Override
        public boolean onInterceptTouchEvent(MotionEvent event) {
            return blocksInteraction || super.onInterceptTouchEvent(event);
        }

        @Override
        public boolean onTouchEvent(MotionEvent event) {
            return blocksInteraction || super.onTouchEvent(event);
        }
    }
}
