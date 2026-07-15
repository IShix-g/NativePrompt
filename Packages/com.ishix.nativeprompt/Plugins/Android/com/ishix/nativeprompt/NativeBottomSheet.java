package com.ishix.nativeprompt;

import android.app.Activity;
import android.app.Dialog;
import android.graphics.Color;
import android.graphics.drawable.ColorDrawable;
import android.graphics.drawable.GradientDrawable;
import android.os.Build;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowInsets;
import android.view.WindowManager;
import android.view.animation.DecelerateInterpolator;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

import com.unity3d.player.UnityPlayer;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.atomic.AtomicBoolean;

public final class NativeBottomSheet {
    public interface Callback {
        void onActionSelected(String requestId, String actionId);

        void onCancelled(String requestId);
    }

    private static final Map<String, State> STATES = new HashMap<>();

    private NativeBottomSheet() {
    }

    public static void show(
            final String requestId,
            final String payload,
            final Callback callback) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            throw new IllegalStateException("Unity activity is not available.");
        }

        activity.runOnUiThread(() -> showOnUiThread(activity, requestId, payload, callback));
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
            String payloadValue,
            Callback callback) {
        try {
            JSONObject payload = new JSONObject(payloadValue);
            Dialog dialog = new Dialog(activity);
            dialog.requestWindowFeature(Window.FEATURE_NO_TITLE);
            dialog.setCancelable(true);
            dialog.setCanceledOnTouchOutside(true);

            LinearLayout content = createContent(activity, payload);
            dialog.setContentView(content);

            State state = new State(requestId, dialog, callback);
            STATES.put(requestId, state);
            bindActions(content, payload, state);
            dialog.setOnCancelListener(ignored -> state.completeCancelled());
            dialog.setOnDismissListener(ignored -> STATES.remove(requestId));
            dialog.show();

            Window window = dialog.getWindow();
            if (window == null) {
                state.completeCancelled();
                return;
            }

            window.setBackgroundDrawable(new ColorDrawable(Color.TRANSPARENT));
            window.setLayout(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT);
            window.setGravity(Gravity.BOTTOM);
            window.addFlags(WindowManager.LayoutParams.FLAG_DIM_BEHIND);
            WindowManager.LayoutParams attributes = window.getAttributes();
            attributes.dimAmount = 0.32f;
            window.setAttributes(attributes);

            applyBottomInsets(content);
            content.post(() -> {
                content.setTranslationY(content.getHeight());
                content.animate()
                        .translationY(0f)
                        .setDuration(220L)
                        .setInterpolator(new DecelerateInterpolator())
                        .start();
            });
        } catch (Exception exception) {
            STATES.remove(requestId);
            callback.onCancelled(requestId);
        }
    }

    private static LinearLayout createContent(
            Activity activity,
            JSONObject payload) throws Exception {
        LinearLayout content = new LinearLayout(activity);
        content.setOrientation(LinearLayout.VERTICAL);
        int horizontalPadding = dp(activity, 20);
        int verticalPadding = dp(activity, 12);
        content.setPadding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);

        GradientDrawable background = new GradientDrawable();
        background.setColor(resolveColor(activity, android.R.attr.colorBackground, Color.WHITE));
        background.setCornerRadius(dp(activity, 16));
        content.setBackground(background);

        String title = optionalString(payload, "title");
        if (title != null) {
            TextView titleView = new TextView(activity);
            titleView.setText(title);
            titleView.setTextSize(TypedValue.COMPLEX_UNIT_SP, 20f);
            titleView.setTextColor(resolveColor(
                    activity,
                    android.R.attr.textColorPrimary,
                    Color.BLACK));
            titleView.setPadding(0, dp(activity, 6), 0, dp(activity, 4));
            content.addView(titleView, matchWrap());
        }

        String body = optionalString(payload, "content");
        if (body != null) {
            TextView bodyView = new TextView(activity);
            bodyView.setText(body);
            bodyView.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14f);
            bodyView.setTextColor(resolveColor(
                    activity,
                    android.R.attr.textColorSecondary,
                    Color.DKGRAY));
            bodyView.setPadding(0, dp(activity, 2), 0, dp(activity, 8));
            content.addView(bodyView, matchWrap());
        }

        return content;
    }

    private static void bindActions(
            LinearLayout content,
            JSONObject payload,
            State state) throws Exception {
        Activity activity = (Activity) content.getContext();
        JSONArray actions = payload.getJSONArray("actions");
        for (int index = 0; index < actions.length(); index++) {
            JSONObject action = actions.getJSONObject(index);
            String actionId = action.getString("id");
            Button button = createButton(activity, action.getString("text"));
            button.setEnabled(action.getBoolean("enabled"));
            if (action.getInt("style") == 1) {
                button.setTextColor(Color.rgb(176, 0, 32));
            }
            button.setOnClickListener(ignored -> state.completeAction(actionId));
            content.addView(button, matchWrap());
        }

        Button cancelButton = createButton(activity, payload.getString("cancelButtonText"));
        LinearLayout.LayoutParams cancelLayout = matchWrap();
        cancelLayout.topMargin = dp(activity, 4);
        cancelButton.setOnClickListener(ignored -> state.completeCancelled());
        content.addView(cancelButton, cancelLayout);
    }

    private static Button createButton(Activity activity, String text) {
        Button button = new Button(activity);
        button.setText(text);
        button.setAllCaps(false);
        button.setGravity(Gravity.START | Gravity.CENTER_VERTICAL);
        button.setMinHeight(dp(activity, 48));
        return button;
    }

    private static void applyBottomInsets(View content) {
        int left = content.getPaddingLeft();
        int top = content.getPaddingTop();
        int right = content.getPaddingRight();
        int bottom = content.getPaddingBottom();
        content.setOnApplyWindowInsetsListener((view, insets) -> {
            int insetBottom;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                insetBottom = insets.getInsets(WindowInsets.Type.systemBars()).bottom;
            } else {
                insetBottom = insets.getSystemWindowInsetBottom();
            }
            view.setPadding(left, top, right, bottom + insetBottom);
            return insets;
        });
        content.requestApplyInsets();
    }

    private static String optionalString(JSONObject payload, String key) {
        if (!payload.has(key) || payload.isNull(key)) {
            return null;
        }
        String value = payload.optString(key, null);
        return value == null || value.isEmpty() ? null : value;
    }

    private static int resolveColor(Activity activity, int attributeId, int fallback) {
        TypedValue value = new TypedValue();
        if (!activity.getTheme().resolveAttribute(attributeId, value, true)) {
            return fallback;
        }
        if (value.resourceId != 0) {
            return activity.getColor(value.resourceId);
        }
        return value.data;
    }

    private static int dp(Activity activity, int value) {
        return Math.round(TypedValue.applyDimension(
                TypedValue.COMPLEX_UNIT_DIP,
                value,
                activity.getResources().getDisplayMetrics()));
    }

    private static LinearLayout.LayoutParams matchWrap() {
        return new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT);
    }

    private static final class State {
        private final String requestId;
        private final Dialog dialog;
        private final Callback callback;
        private final AtomicBoolean completed = new AtomicBoolean();

        private State(String requestId, Dialog dialog, Callback callback) {
            this.requestId = requestId;
            this.dialog = dialog;
            this.callback = callback;
        }

        private void completeAction(String actionId) {
            if (!completed.compareAndSet(false, true)) {
                return;
            }
            STATES.remove(requestId);
            dialog.dismiss();
            callback.onActionSelected(requestId, actionId);
        }

        private void completeCancelled() {
            if (!completed.compareAndSet(false, true)) {
                return;
            }
            STATES.remove(requestId);
            dialog.dismiss();
            callback.onCancelled(requestId);
        }

        private void dismissWithoutCallback() {
            completed.set(true);
            dialog.dismiss();
        }
    }
}
