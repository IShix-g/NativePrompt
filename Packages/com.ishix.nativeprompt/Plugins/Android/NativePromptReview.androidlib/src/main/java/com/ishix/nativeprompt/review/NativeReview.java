package com.ishix.nativeprompt.review;

import android.app.Activity;
import android.util.Log;

import com.google.android.play.core.review.ReviewInfo;
import com.google.android.play.core.review.ReviewManager;
import com.google.android.play.core.review.ReviewManagerFactory;
import com.google.android.gms.tasks.Task;

import java.util.concurrent.atomic.AtomicBoolean;

public final class NativeReview {
    private static final String TAG = "NativePrompt";
    private static final AtomicBoolean REQUEST_IN_FLIGHT = new AtomicBoolean(false);

    private NativeReview() {
    }

    public static void request(Activity activity) {
        if (activity == null) {
            Log.w(TAG, "Store Review request ignored because the Unity Activity is unavailable.");
            return;
        }
        if (!REQUEST_IN_FLIGHT.compareAndSet(false, true)) {
            Log.w(TAG, "Store Review request ignored because another request is in progress.");
            return;
        }

        try {
            activity.runOnUiThread(() -> requestOnUiThread(activity));
        } catch (Throwable throwable) {
            logFailure("Store Review request could not reach the Android UI thread.", throwable);
            REQUEST_IN_FLIGHT.set(false);
        }
    }

    private static void requestOnUiThread(Activity activity) {
        try {
            ReviewManager manager = ReviewManagerFactory.create(activity);
            Task<ReviewInfo> requestTask = manager.requestReviewFlow();
            requestTask.addOnCompleteListener(task -> {
                if (!task.isSuccessful()) {
                    logFailure("Store Review API information request failed.", task.getException());
                    REQUEST_IN_FLIGHT.set(false);
                    return;
                }

                launchReviewFlow(activity, manager, task.getResult());
            });
        } catch (Throwable throwable) {
            logFailure("Store Review request could not be started.", throwable);
            REQUEST_IN_FLIGHT.set(false);
        }
    }

    private static void launchReviewFlow(
            Activity activity,
            ReviewManager manager,
            ReviewInfo reviewInfo) {
        try {
            Task<Void> launchTask = manager.launchReviewFlow(activity, reviewInfo);
            launchTask.addOnCompleteListener(task -> {
                if (task.isSuccessful()) {
                    Log.d(
                            TAG,
                            "Store Review flow request completed; dialog display and review " +
                                    "submission are not reported by Google Play.");
                } else {
                    logFailure("Store Review flow launch failed.", task.getException());
                }
                REQUEST_IN_FLIGHT.set(false);
            });
        } catch (Throwable throwable) {
            logFailure("Store Review flow could not be launched.", throwable);
            REQUEST_IN_FLIGHT.set(false);
        }
    }

    private static void logFailure(String message, Throwable throwable) {
        if (throwable == null) {
            Log.w(TAG, message);
        } else {
            Log.w(TAG, message, throwable);
        }
    }
}
