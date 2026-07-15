package com.ishix.nativeprompt;

import android.app.Activity;
import android.graphics.Color;
import android.graphics.drawable.GradientDrawable;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.text.TextUtils;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.ViewGroup;
import android.view.WindowInsets;
import android.widget.FrameLayout;
import android.widget.TextView;

import com.unity3d.player.UnityPlayer;

import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.atomic.AtomicBoolean;

public final class NativeToast {
    public interface Callback {
        void onDismissed(String requestId, int reason);
    }

    private static final int REASON_TIMED_OUT = 0;
    private static final int REASON_TAPPED = 1;
    private static final int POSITION_TOP = 0;
    private static final int POSITION_CENTER = 1;
    private static final Handler MAIN_HANDLER = new Handler(Looper.getMainLooper());
    private static final Map<String, State> STATES = new HashMap<>();

    private NativeToast() {
    }

    public static void show(
            final String requestId,
            final String message,
            final float durationSeconds,
            final boolean autoDismiss,
            final boolean dismissOnTap,
            final int position,
            final Callback callback) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            throw new IllegalStateException("Unity activity is not available.");
        }

        activity.runOnUiThread(() -> showOnUiThread(
                activity,
                requestId,
                message,
                durationSeconds,
                autoDismiss,
                dismissOnTap,
                position,
                callback));
    }

    public static void dismiss(final String requestId) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            return;
        }

        activity.runOnUiThread(() -> {
            State state = STATES.get(requestId);
            if (state != null) {
                state.dismissWithoutCallback();
            }
        });
    }

    public static void reset() {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            return;
        }

        activity.runOnUiThread(() -> {
            State[] states = STATES.values().toArray(new State[0]);
            STATES.clear();
            for (State state : states) {
                state.dismissWithoutCallback();
            }
        });
    }

    private static void showOnUiThread(
            Activity activity,
            String requestId,
            String message,
            float durationSeconds,
            boolean autoDismiss,
            boolean dismissOnTap,
            int position,
            Callback callback) {
        State existing = STATES.get(requestId);
        if (existing != null) {
            existing.dismissWithoutCallback();
        }

        FrameLayout root = activity.findViewById(android.R.id.content);
        if (root == null) {
            callback.onDismissed(requestId, REASON_TIMED_OUT);
            return;
        }

        TextView toast = createToastView(activity, message, dismissOnTap);
        FrameLayout.LayoutParams params = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WRAP_CONTENT,
                ViewGroup.LayoutParams.WRAP_CONTENT);
        params.gravity = position == POSITION_TOP
                ? Gravity.TOP | Gravity.CENTER_HORIZONTAL
                : position == POSITION_CENTER
                        ? Gravity.CENTER
                        : Gravity.BOTTOM | Gravity.CENTER_HORIZONTAL;

        State state = new State(requestId, root, toast, params, callback);
        STATES.put(requestId, state);
        applyInsets(activity, state, position);
        root.addView(toast, params);

        if (dismissOnTap) {
            toast.setOnClickListener(ignored -> state.complete(REASON_TAPPED));
        }

        if (autoDismiss) {
            long delayMillis = Math.max(1L, (long) (durationSeconds * 1000f));
            state.timeout = () -> state.complete(REASON_TIMED_OUT);
            MAIN_HANDLER.postDelayed(state.timeout, delayMillis);
        }
    }

    private static TextView createToastView(
            Activity activity,
            String message,
            boolean dismissOnTap) {
        TextView toast = new TextView(activity);
        toast.setText(message);
        toast.setTextColor(Color.WHITE);
        toast.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16f);
        toast.setGravity(Gravity.CENTER);
        toast.setMaxLines(3);
        toast.setEllipsize(TextUtils.TruncateAt.END);
        toast.setMaxWidth(activity.getResources().getDisplayMetrics().widthPixels - dp(activity, 32));
        int horizontalPadding = dp(activity, 16);
        int verticalPadding = dp(activity, 10);
        toast.setPadding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
        toast.setClickable(dismissOnTap);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            toast.setElevation(dp(activity, 6));
        }

        GradientDrawable background = new GradientDrawable();
        background.setColor(Color.argb(235, 20, 20, 20));
        background.setCornerRadius(dp(activity, 12));
        toast.setBackground(background);
        return toast;
    }

    private static void applyInsets(Activity activity, State state, int position) {
        int spacing = dp(activity, 16);
        state.params.leftMargin = spacing;
        state.params.rightMargin = spacing;

        state.view.setOnApplyWindowInsetsListener((view, insets) -> {
            updateVerticalMargins(state.params, insets, position, spacing);
            view.setLayoutParams(state.params);
            return insets;
        });

        WindowInsets currentInsets = state.root.getRootWindowInsets();
        updateVerticalMargins(state.params, currentInsets, position, spacing);
        state.view.requestApplyInsets();
    }

    private static void updateVerticalMargins(
            FrameLayout.LayoutParams params,
            WindowInsets insets,
            int position,
            int spacing) {
        int insetTop = 0;
        int insetBottom = 0;
        if (insets != null) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                android.graphics.Insets systemBars = insets.getInsets(WindowInsets.Type.systemBars());
                insetTop = systemBars.top;
                insetBottom = systemBars.bottom;
            } else {
                insetTop = insets.getSystemWindowInsetTop();
                insetBottom = insets.getSystemWindowInsetBottom();
            }
        }

        params.topMargin = position == POSITION_TOP ? spacing + insetTop : 0;
        params.bottomMargin = position == POSITION_CENTER ? 0 : spacing + insetBottom;
    }

    private static int dp(Activity activity, int value) {
        return Math.round(TypedValue.applyDimension(
                TypedValue.COMPLEX_UNIT_DIP,
                value,
                activity.getResources().getDisplayMetrics()));
    }

    private static final class State {
        private final String requestId;
        private final FrameLayout root;
        private final TextView view;
        private final FrameLayout.LayoutParams params;
        private final Callback callback;
        private final AtomicBoolean completed = new AtomicBoolean();
        private Runnable timeout;

        private State(
                String requestId,
                FrameLayout root,
                TextView view,
                FrameLayout.LayoutParams params,
                Callback callback) {
            this.requestId = requestId;
            this.root = root;
            this.view = view;
            this.params = params;
            this.callback = callback;
        }

        private void complete(int reason) {
            if (!completed.compareAndSet(false, true)) {
                return;
            }

            remove();
            callback.onDismissed(requestId, reason);
        }

        private void dismissWithoutCallback() {
            if (!completed.compareAndSet(false, true)) {
                return;
            }

            remove();
        }

        private void remove() {
            if (timeout != null) {
                MAIN_HANDLER.removeCallbacks(timeout);
                timeout = null;
            }
            if (STATES.get(requestId) == this) {
                STATES.remove(requestId);
            }
            root.removeView(view);
        }
    }
}
