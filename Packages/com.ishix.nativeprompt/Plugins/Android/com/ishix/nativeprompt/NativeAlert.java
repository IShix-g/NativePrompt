package com.ishix.nativeprompt;

import android.app.Activity;
import android.app.AlertDialog;

import com.unity3d.player.UnityPlayer;

import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.atomic.AtomicBoolean;

public final class NativeAlert {
    public interface Callback {
        void onCompleted(String requestId, int result);
    }

    private static final int RESULT_YES = 0;
    private static final int RESULT_NO = 1;
    private static final int RESULT_CLOSED = 2;
    private static final Map<String, State> STATES = new HashMap<>();

    private NativeAlert() {
    }

    public static void show(
            final String requestId,
            final String title,
            final String content,
            final String yesButtonText,
            final String noButtonText,
            final String closeButtonText,
            final Callback callback) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            throw new IllegalStateException("Unity activity is not available.");
        }

        activity.runOnUiThread(() -> showOnUiThread(
                activity,
                requestId,
                title,
                content,
                yesButtonText,
                noButtonText,
                closeButtonText,
                callback));
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
            String title,
            String content,
            String yesButtonText,
            String noButtonText,
            String closeButtonText,
            Callback callback) {
        try {
            AlertDialog.Builder builder = new AlertDialog.Builder(activity)
                    .setMessage(content)
                    .setCancelable(false);
            if (title != null) {
                builder.setTitle(title);
            }

            final State[] stateHolder = new State[1];
            if (yesButtonText != null) {
                builder.setPositiveButton(
                        yesButtonText,
                        (dialog, which) -> stateHolder[0].complete(RESULT_YES));
            }
            if (noButtonText != null) {
                builder.setNegativeButton(
                        noButtonText,
                        (dialog, which) -> stateHolder[0].complete(RESULT_NO));
            }
            if (yesButtonText == null && noButtonText == null) {
                builder.setPositiveButton(
                        closeButtonText,
                        (dialog, which) -> stateHolder[0].complete(RESULT_CLOSED));
            }

            AlertDialog dialog = builder.create();
            dialog.setCanceledOnTouchOutside(false);
            State state = new State(requestId, dialog, callback);
            stateHolder[0] = state;
            STATES.put(requestId, state);
            dialog.setOnDismissListener(ignored -> STATES.remove(requestId));
            dialog.show();
        } catch (Exception exception) {
            STATES.remove(requestId);
            callback.onCompleted(requestId, RESULT_CLOSED);
        }
    }

    private static final class State {
        private final String requestId;
        private final AlertDialog dialog;
        private final Callback callback;
        private final AtomicBoolean completed = new AtomicBoolean();

        private State(String requestId, AlertDialog dialog, Callback callback) {
            this.requestId = requestId;
            this.dialog = dialog;
            this.callback = callback;
        }

        private void complete(int result) {
            if (!completed.compareAndSet(false, true)) {
                return;
            }

            STATES.remove(requestId);
            dialog.dismiss();
            callback.onCompleted(requestId, result);
        }

        private void dismissWithoutCallback() {
            completed.set(true);
            dialog.dismiss();
        }
    }
}
