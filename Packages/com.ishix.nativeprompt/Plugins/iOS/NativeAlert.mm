#import <UIKit/UIKit.h>

typedef void (*NativePromptAlertCompletedCallback)(const char *requestId, int result);
typedef void (*NativePromptAlertOpenedCallback)(const char *requestId);

enum NativePromptAlertResult
{
    NativePromptAlertResultYes = 0,
    NativePromptAlertResultNo = 1,
    NativePromptAlertResultClosed = 2,
};

@interface NativePromptAlertState : NSObject

@property(nonatomic, copy) NSString *requestId;
@property(nonatomic, weak) UIAlertController *controller;
@property(nonatomic, assign) NativePromptAlertCompletedCallback completed;
@property(nonatomic, assign) BOOL hasCompleted;

- (void)completeWithResult:(NativePromptAlertResult)result;
- (void)dismissWithoutCallback;

@end

static NSMutableDictionary<NSString *, NativePromptAlertState *> *NativePromptAlerts(void)
{
    static NSMutableDictionary<NSString *, NativePromptAlertState *> *alerts;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        alerts = [NSMutableDictionary dictionary];
    });
    return alerts;
}

@implementation NativePromptAlertState

- (void)completeWithResult:(NativePromptAlertResult)result
{
    if (self.hasCompleted)
    {
        return;
    }

    self.hasCompleted = YES;
    [NativePromptAlerts() removeObjectForKey:self.requestId];
    if (self.completed != NULL)
    {
        self.completed(self.requestId.UTF8String, (int)result);
    }
}

- (void)dismissWithoutCallback
{
    if (self.hasCompleted)
    {
        return;
    }

    self.hasCompleted = YES;
    [NativePromptAlerts() removeObjectForKey:self.requestId];
    [self.controller dismissViewControllerAnimated:NO completion:nil];
}

@end

static UIWindow *NativePromptAlertKeyWindow(void)
{
    if (@available(iOS 13.0, *))
    {
        for (UIScene *scene in UIApplication.sharedApplication.connectedScenes)
        {
            if (scene.activationState != UISceneActivationStateForegroundActive ||
                ![scene isKindOfClass:UIWindowScene.class])
            {
                continue;
            }

            for (UIWindow *window in ((UIWindowScene *)scene).windows)
            {
                if (window.isKeyWindow)
                {
                    return window;
                }
            }
        }
    }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    return UIApplication.sharedApplication.keyWindow;
#pragma clang diagnostic pop
}

static UIViewController *NativePromptAlertTopViewController(void)
{
    UIViewController *controller = NativePromptAlertKeyWindow().rootViewController;
    while (controller != nil)
    {
        if (controller.presentedViewController != nil &&
            !controller.presentedViewController.isBeingDismissed)
        {
            controller = controller.presentedViewController;
        }
        else if ([controller isKindOfClass:UINavigationController.class])
        {
            controller = ((UINavigationController *)controller).visibleViewController;
        }
        else if ([controller isKindOfClass:UITabBarController.class])
        {
            controller = ((UITabBarController *)controller).selectedViewController;
        }
        else
        {
            break;
        }
    }
    return controller;
}

static NSString *NativePromptAlertString(const char *value)
{
    return value == NULL ? nil : [NSString stringWithUTF8String:value];
}

extern "C" void NativePrompt_ShowAlert(
    const char *requestIdValue,
    const char *titleValue,
    const char *contentValue,
    const char *yesButtonTextValue,
    const char *noButtonTextValue,
    const char *closeButtonTextValue,
    NativePromptAlertOpenedCallback opened,
    NativePromptAlertCompletedCallback completed)
{
    NSString *requestId = NativePromptAlertString(requestIdValue) ?: @"";
    NSString *title = NativePromptAlertString(titleValue);
    NSString *content = NativePromptAlertString(contentValue) ?: @"";
    NSString *yesButtonText = NativePromptAlertString(yesButtonTextValue);
    NSString *noButtonText = NativePromptAlertString(noButtonTextValue);
    NSString *closeButtonText = NativePromptAlertString(closeButtonTextValue);

    dispatch_async(dispatch_get_main_queue(), ^{
        UIViewController *presenter = NativePromptAlertTopViewController();
        if (presenter == nil)
        {
            if (completed != NULL)
            {
                completed(requestId.UTF8String, NativePromptAlertResultClosed);
            }
            return;
        }

        UIAlertController *controller = [UIAlertController
            alertControllerWithTitle:title
            message:content
            preferredStyle:UIAlertControllerStyleAlert];
        NativePromptAlertState *state = [NativePromptAlertState new];
        state.requestId = requestId;
        state.controller = controller;
        state.completed = completed;
        NativePromptAlerts()[requestId] = state;

        if (yesButtonText != nil)
        {
            UIAlertAction *yesAction = [UIAlertAction
                actionWithTitle:yesButtonText
                style:UIAlertActionStyleDefault
                handler:^(__unused UIAlertAction *action) {
                    [state completeWithResult:NativePromptAlertResultYes];
                }];
            [controller addAction:yesAction];
        }

        if (noButtonText != nil)
        {
            UIAlertAction *noAction = [UIAlertAction
                actionWithTitle:noButtonText
                style:UIAlertActionStyleCancel
                handler:^(__unused UIAlertAction *action) {
                    [state completeWithResult:NativePromptAlertResultNo];
                }];
            [controller addAction:noAction];
        }

        if (yesButtonText == nil && noButtonText == nil)
        {
            UIAlertAction *closeAction = [UIAlertAction
                actionWithTitle:closeButtonText
                style:UIAlertActionStyleCancel
                handler:^(__unused UIAlertAction *action) {
                    [state completeWithResult:NativePromptAlertResultClosed];
                }];
            [controller addAction:closeAction];
        }

        [presenter presentViewController:controller animated:YES completion:^{
            if (!state.hasCompleted && opened != NULL)
            {
                opened(requestId.UTF8String);
            }
        }];
    });
}

extern "C" void NativePrompt_DismissAlert(const char *requestIdValue)
{
    NSString *requestId = NativePromptAlertString(requestIdValue) ?: @"";
    dispatch_async(dispatch_get_main_queue(), ^{
        [NativePromptAlerts()[requestId] dismissWithoutCallback];
    });
}

extern "C" void NativePrompt_ResetAlerts(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        NSArray<NativePromptAlertState *> *states = NativePromptAlerts().allValues;
        [NativePromptAlerts() removeAllObjects];
        for (NativePromptAlertState *state in states)
        {
            [state dismissWithoutCallback];
        }
    });
}
