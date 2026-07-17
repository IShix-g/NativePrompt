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
    private static final int CONTENT_SPACING_DP = 8;
    private static final int CORNER_MESSAGE_MAX_LINES = 2;
    private static final int CENTER_MESSAGE_MAX_LINES = 4;
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
            final float spinnerRed,
            final float spinnerGreen,
            final float spinnerBlue,
            final float spinnerAlpha,
            final float messageRed,
            final float messageGreen,
            final float messageBlue,
            final float messageAlpha,
            final float messageFontSize,
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
                spinnerRed,
                spinnerGreen,
                spinnerBlue,
                spinnerAlpha,
                messageRed,
                messageGreen,
                messageBlue,
                messageAlpha,
                messageFontSize,
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
            float spinnerRed,
            float spinnerGreen,
            float spinnerBlue,
            float spinnerAlpha,
            float messageRed,
            float messageGreen,
            float messageBlue,
            float messageAlpha,
            float messageFontSize,
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
                spinnerRed,
                spinnerGreen,
                spinnerBlue,
                spinnerAlpha,
                messageRed,
                messageGreen,
                messageBlue,
                messageAlpha,
                messageFontSize,
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
                float spinnerRed,
                float spinnerGreen,
                float spinnerBlue,
                float spinnerAlpha,
                float messageRed,
                float messageGreen,
                float messageBlue,
                float messageAlpha,
                float messageFontSize,
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
            content = createContent(
                    message,
                    spinnerRed,
                    spinnerGreen,
                    spinnerBlue,
                    spinnerAlpha,
                    messageRed,
                    messageGreen,
                    messageBlue,
                    messageAlpha,
                    messageFontSize,
                    position,
                    size);
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

        private LinearLayout createContent(
                String message,
                float spinnerRed,
                float spinnerGreen,
                float spinnerBlue,
                float spinnerAlpha,
                float messageRed,
                float messageGreen,
                float messageBlue,
                float messageAlpha,
                float messageFontSize,
                int position,
                int size) {
            LinearLayout group = new LinearLayout(activity);
            boolean hasMessage = !TextUtils.isEmpty(message);
            boolean usesHorizontalLayout = hasMessage && position != POSITION_CENTER;
            boolean placesSpinnerOnRight =
                    position == POSITION_TOP_RIGHT || position == POSITION_BOTTOM_RIGHT;
            group.setOrientation(usesHorizontalLayout
                    ? LinearLayout.HORIZONTAL
                    : LinearLayout.VERTICAL);
            group.setGravity(Gravity.CENTER);
            if (usesHorizontalLayout) {
                group.setLayoutDirection(View.LAYOUT_DIRECTION_LTR);
            }

            int progressStyle = size == SIZE_SMALL
                    ? android.R.attr.progressBarStyleSmall
                    : size == SIZE_LARGE
                            ? android.R.attr.progressBarStyleLarge
                            : android.R.attr.progressBarStyle;
            ProgressBar spinner = new ProgressBar(activity, null, progressStyle);
            spinner.setIndeterminate(true);
            spinner.setIndeterminateTintList(ColorStateList.valueOf(Color.argb(
                    colorComponent(spinnerAlpha),
                    colorComponent(spinnerRed),
                    colorComponent(spinnerGreen),
                    colorComponent(spinnerBlue))));
            LinearLayout.LayoutParams spinnerParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WRAP_CONTENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT);

            if (hasMessage) {
                TextView label = new TextView(activity);
                label.setText(message);
                label.setTextAppearance(android.R.style.TextAppearance_Material_Body1);
                label.setGravity(Gravity.CENTER);
                label.setTextDirection(View.TEXT_DIRECTION_FIRST_STRONG);
                label.setMaxLines(usesHorizontalLayout
                        ? CORNER_MESSAGE_MAX_LINES
                        : CENTER_MESSAGE_MAX_LINES);
                label.setEllipsize(TextUtils.TruncateAt.END);
                label.setTag("NativePromptLoadingMessage");
                label.setTextColor(Color.argb(
                        colorComponent(messageAlpha),
                        colorComponent(messageRed),
                        colorComponent(messageGreen),
                        colorComponent(messageBlue)));
                label.setTextSize(TypedValue.COMPLEX_UNIT_SP, messageFontSize);
                LinearLayout.LayoutParams labelParams = new LinearLayout.LayoutParams(
                        ViewGroup.LayoutParams.WRAP_CONTENT,
                        ViewGroup.LayoutParams.WRAP_CONTENT);
                if (usesHorizontalLayout) {
                    if (placesSpinnerOnRight) {
                        labelParams.rightMargin = dp(activity, CONTENT_SPACING_DP);
                        group.addView(label, labelParams);
                        group.addView(spinner, spinnerParams);
                    } else {
                        labelParams.leftMargin = dp(activity, CONTENT_SPACING_DP);
                        group.addView(spinner, spinnerParams);
                        group.addView(label, labelParams);
                    }
                } else {
                    labelParams.topMargin = dp(activity, CONTENT_SPACING_DP);
                    group.addView(spinner, spinnerParams);
                    group.addView(label, labelParams);
                }
            } else {
                group.addView(spinner, spinnerParams);
            }

            return group;
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
            int maximumMessageWidth = maximumWidth;
            if (position != POSITION_CENTER) {
                for (int index = 0; index < content.getChildCount(); index++) {
                    View child = content.getChildAt(index);
                    if (child instanceof ProgressBar) {
                        child.measure(
                                View.MeasureSpec.makeMeasureSpec(
                                        0,
                                        View.MeasureSpec.UNSPECIFIED),
                                View.MeasureSpec.makeMeasureSpec(
                                        0,
                                        View.MeasureSpec.UNSPECIFIED));
                        maximumMessageWidth -=
                                child.getMeasuredWidth() + dp(activity, CONTENT_SPACING_DP);
                        break;
                    }
                }
            }
            params.width = ViewGroup.LayoutParams.WRAP_CONTENT;
            for (int index = 0; index < content.getChildCount(); index++) {
                View child = content.getChildAt(index);
                if (child instanceof TextView &&
                        "NativePromptLoadingMessage".equals(child.getTag())) {
                    ((TextView) child).setMaxWidth(Math.max(
                            dp(activity, 48),
                            maximumMessageWidth));
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
