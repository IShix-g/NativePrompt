package com.ishix.nativeprompt;

import android.app.Activity;
import android.app.Dialog;
import android.content.res.Configuration;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.ColorDrawable;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.RippleDrawable;
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
        void onOpened(String requestId);

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
            callback.onOpened(requestId);

            Window window = dialog.getWindow();
            if (window == null) {
                state.completeCancelled();
                return;
            }

            window.setBackgroundDrawable(new ColorDrawable(Color.TRANSPARENT));
            window.setLayout(
                    Math.min(activity.getResources().getDisplayMetrics().widthPixels,
                            dp(activity, 536)),
                    ViewGroup.LayoutParams.WRAP_CONTENT);
            window.setGravity(Gravity.BOTTOM | Gravity.CENTER_HORIZONTAL);
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
        int padding = dp(activity, 8);
        content.setPadding(padding, padding, padding, padding);

        boolean dark = isDarkMode(activity);
        int surface = dark ? Color.rgb(44, 44, 46) : Color.rgb(242, 242, 247);
        int secondaryText = dark ? Color.rgb(174, 174, 178) : Color.rgb(99, 99, 102);

        LinearLayout group = new LinearLayout(activity);
        group.setOrientation(LinearLayout.VERTICAL);
        group.setBackground(roundedBackground(activity, surface));
        group.setClipToOutline(true);
        content.addView(group, matchWrap());

        String title = optionalString(payload, "title");
        String body = optionalString(payload, "content");
        if (title != null) {
            TextView titleView = new TextView(activity);
            titleView.setText(title);
            titleView.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14f);
            titleView.setTextColor(secondaryText);
            titleView.setGravity(Gravity.CENTER);
            titleView.setPadding(dp(activity, 16), dp(activity, 16),
                    dp(activity, 16), body == null ? dp(activity, 16) : dp(activity, 4));
            group.addView(titleView, matchWrap());
        }

        if (body != null) {
            TextView bodyView = new TextView(activity);
            bodyView.setText(body);
            bodyView.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14f);
            bodyView.setTextColor(secondaryText);
            bodyView.setGravity(Gravity.CENTER);
            bodyView.setPadding(dp(activity, 16), title == null ? dp(activity, 16) : 0,
                    dp(activity, 16), dp(activity, 16));
            group.addView(bodyView, matchWrap());
        }

        return content;
    }

    private static void bindActions(
            LinearLayout content,
            JSONObject payload,
            State state) throws Exception {
        Activity activity = (Activity) content.getContext();
        LinearLayout group = (LinearLayout) content.getChildAt(0);
        boolean dark = isDarkMode(activity);
        int tint = dark ? Color.rgb(10, 132, 255) : Color.rgb(0, 122, 255);
        int destructive = dark ? Color.rgb(255, 69, 58) : Color.rgb(255, 59, 48);
        int disabled = dark ? Color.rgb(99, 99, 102) : Color.rgb(174, 174, 178);
        int pressed = dark ? Color.rgb(72, 72, 74) : Color.rgb(209, 209, 214);
        int separator = dark ? Color.rgb(84, 84, 88) : Color.rgb(198, 198, 200);
        JSONArray actions = payload.getJSONArray("actions");
        for (int index = 0; index < actions.length(); index++) {
            JSONObject action = actions.getJSONObject(index);
            String actionId = action.getString("id");
            if (group.getChildCount() > 0) {
                addSeparator(activity, group, separator);
            }
            Button button = createAction(activity, action.getString("text"), pressed);
            button.setEnabled(action.getBoolean("enabled"));
            button.setTextColor(!button.isEnabled() ? disabled
                    : action.getInt("style") == 1 ? destructive : tint);
            button.setOnClickListener(ignored -> state.completeAction(actionId));
            group.addView(button, matchWrap());
        }

        Button cancelButton = createAction(
                activity, payload.getString("cancelButtonText"), pressed);
        cancelButton.setTextColor(tint);
        cancelButton.setBackground(roundedBackground(
                activity, dark ? Color.rgb(44, 44, 46) : Color.rgb(242, 242, 247)));
        cancelButton.setClipToOutline(true);
        LinearLayout.LayoutParams cancelLayout = matchWrap();
        cancelLayout.topMargin = dp(activity, 8);
        cancelButton.setOnClickListener(ignored -> state.completeCancelled());
        content.addView(cancelButton, cancelLayout);
    }

    private static Button createAction(Activity activity, String text, int pressed) {
        Button button = new Button(activity);
        button.setText(text);
        button.setAllCaps(false);
        button.setTypeface(Typeface.DEFAULT);
        button.setTextSize(TypedValue.COMPLEX_UNIT_SP, 20f);
        button.setGravity(Gravity.CENTER);
        button.setMinHeight(dp(activity, 58));
        button.setMinWidth(0);
        button.setPadding(dp(activity, 16), dp(activity, 10),
                dp(activity, 16), dp(activity, 10));
        button.setBackground(new ColorDrawable(Color.TRANSPARENT));
        button.setBackgroundTintList(null);
        button.setStateListAnimator(null);
        button.setForeground(new RippleDrawable(
                android.content.res.ColorStateList.valueOf(pressed), null, null));
        return button;
    }

    private static void addSeparator(Activity activity, LinearLayout group, int color) {
        View separator = new View(activity);
        separator.setBackgroundColor(color);
        group.addView(separator, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, dp(activity, 1)));
    }

    private static GradientDrawable roundedBackground(Activity activity, int color) {
        GradientDrawable background = new GradientDrawable();
        background.setColor(color);
        background.setCornerRadius(dp(activity, 14));
        return background;
    }

    private static boolean isDarkMode(Activity activity) {
        int mode = activity.getResources().getConfiguration().uiMode;
        return (mode & Configuration.UI_MODE_NIGHT_MASK) == Configuration.UI_MODE_NIGHT_YES;
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
