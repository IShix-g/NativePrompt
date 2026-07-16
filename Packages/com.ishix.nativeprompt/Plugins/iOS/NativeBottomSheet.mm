#import <UIKit/UIKit.h>
#import <objc/runtime.h>

typedef void (*NativePromptActionSelectedCallback)(const char *requestId, const char *actionId);
typedef void (*NativePromptCancelledCallback)(const char *requestId);
typedef void (*NativePromptBottomSheetOpenedCallback)(const char *requestId);

static const void *NativePromptBottomSheetDelegateKey = &NativePromptBottomSheetDelegateKey;

@interface NativePromptBottomSheetDelegate : NSObject <UIPopoverPresentationControllerDelegate, UIGestureRecognizerDelegate>

@property(nonatomic, copy) NSString *requestId;
@property(nonatomic, weak) UIAlertController *controller;
@property(nonatomic, assign) NativePromptActionSelectedCallback actionSelected;
@property(nonatomic, assign) NativePromptCancelledCallback cancelled;
@property(nonatomic, assign) BOOL completed;

- (void)completeWithActionId:(NSString *)actionId;
- (void)completeCancellationAndDismiss:(BOOL)dismiss;

@end

static NSMutableDictionary<NSString *, NativePromptBottomSheetDelegate *> *NativePromptBottomSheets(void)
{
    static NSMutableDictionary<NSString *, NativePromptBottomSheetDelegate *> *bottomSheets;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        bottomSheets = [NSMutableDictionary dictionary];
    });
    return bottomSheets;
}

@implementation NativePromptBottomSheetDelegate

- (void)completeWithActionId:(NSString *)actionId
{
    if (self.completed)
    {
        return;
    }

    self.completed = YES;
    [NativePromptBottomSheets() removeObjectForKey:self.requestId];
    if (self.actionSelected != NULL)
    {
        self.actionSelected(self.requestId.UTF8String, actionId.UTF8String);
    }
}

- (void)completeCancellationAndDismiss:(BOOL)dismiss
{
    if (self.completed)
    {
        return;
    }

    self.completed = YES;
    [NativePromptBottomSheets() removeObjectForKey:self.requestId];
    UIAlertController *controller = self.controller;
    if (dismiss && controller.presentingViewController != nil)
    {
        [controller dismissViewControllerAnimated:YES completion:nil];
    }

    if (self.cancelled != NULL)
    {
        self.cancelled(self.requestId.UTF8String);
    }
}

- (void)presentationControllerDidDismiss:(UIPresentationController *)presentationController
{
    [self completeCancellationAndDismiss:NO];
}

- (BOOL)gestureRecognizer:(UIGestureRecognizer *)gestureRecognizer
       shouldReceiveTouch:(UITouch *)touch
{
    UIView *controllerView = self.controller.view;
    return controllerView == nil || ![touch.view isDescendantOfView:controllerView];
}

- (void)backgroundTapped:(UITapGestureRecognizer *)recognizer
{
    if (recognizer.state == UIGestureRecognizerStateEnded)
    {
        [self completeCancellationAndDismiss:YES];
    }
}

@end

static UIWindow *NativePromptKeyWindow(void)
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

static UIViewController *NativePromptTopViewController(void)
{
    UIViewController *controller = NativePromptKeyWindow().rootViewController;
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

static NSString *NativePromptOptionalString(id value)
{
    return [value isKindOfClass:NSString.class] ? value : nil;
}

extern "C" void NativePrompt_ShowBottomSheet(
    const char *requestIdValue,
    const char *payloadValue,
    NativePromptBottomSheetOpenedCallback opened,
    NativePromptActionSelectedCallback actionSelected,
    NativePromptCancelledCallback cancelled)
{
    NSString *requestId = requestIdValue == NULL
        ? @""
        : [NSString stringWithUTF8String:requestIdValue];
    NSData *payloadData = payloadValue == NULL
        ? nil
        : [[NSString stringWithUTF8String:payloadValue] dataUsingEncoding:NSUTF8StringEncoding];
    NSDictionary *payload = payloadData == nil
        ? nil
        : [NSJSONSerialization JSONObjectWithData:payloadData options:0 error:nil];

    dispatch_async(dispatch_get_main_queue(), ^{
        UIViewController *presenter = NativePromptTopViewController();
        if (presenter == nil || ![payload isKindOfClass:NSDictionary.class])
        {
            if (cancelled != NULL)
            {
                cancelled(requestId.UTF8String);
            }
            return;
        }

        NSString *title = NativePromptOptionalString(payload[@"title"]);
        NSString *content = NativePromptOptionalString(payload[@"content"]);
        UIAlertController *controller = [UIAlertController
            alertControllerWithTitle:title
            message:content
            preferredStyle:UIAlertControllerStyleActionSheet];

        NativePromptBottomSheetDelegate *delegate = [NativePromptBottomSheetDelegate new];
        delegate.requestId = requestId;
        delegate.controller = controller;
        delegate.actionSelected = actionSelected;
        delegate.cancelled = cancelled;
        NativePromptBottomSheets()[requestId] = delegate;
        objc_setAssociatedObject(
            controller,
            NativePromptBottomSheetDelegateKey,
            delegate,
            OBJC_ASSOCIATION_RETAIN_NONATOMIC);

        NSArray *actions = [payload[@"actions"] isKindOfClass:NSArray.class]
            ? payload[@"actions"]
            : @[];
        for (NSDictionary *actionPayload in actions)
        {
            if (![actionPayload isKindOfClass:NSDictionary.class])
            {
                continue;
            }

            NSString *actionId = NativePromptOptionalString(actionPayload[@"id"]);
            NSString *text = NativePromptOptionalString(actionPayload[@"text"]);
            UIAlertActionStyle style = [actionPayload[@"style"] integerValue] == 1
                ? UIAlertActionStyleDestructive
                : UIAlertActionStyleDefault;
            UIAlertAction *action = [UIAlertAction
                actionWithTitle:text
                style:style
                handler:^(__unused UIAlertAction *selectedAction) {
                    [delegate completeWithActionId:actionId];
                }];
            action.enabled = [actionPayload[@"enabled"] boolValue];
            [controller addAction:action];
        }

        NSString *cancelText = NativePromptOptionalString(payload[@"cancelButtonText"]);
        UIAlertAction *cancelAction = [UIAlertAction
            actionWithTitle:cancelText
            style:UIAlertActionStyleCancel
            handler:^(__unused UIAlertAction *selectedAction) {
                [delegate completeCancellationAndDismiss:NO];
            }];
        [controller addAction:cancelAction];

        UIPopoverPresentationController *popover = controller.popoverPresentationController;
        if (popover != nil)
        {
            // Compact action sheets adapt to an alert presentation controller that
            // rejects delegate changes. Only popovers need dismissal delegation.
            popover.delegate = delegate;
            UIView *anchorView = presenter.view;
            CGRect safeBounds = UIEdgeInsetsInsetRect(anchorView.bounds, anchorView.safeAreaInsets);
            CGFloat anchorY = MAX(
                CGRectGetMinY(safeBounds),
                CGRectGetMaxY(safeBounds) - 1.0);
            popover.sourceView = anchorView;
            popover.sourceRect = CGRectMake(
                CGRectGetMidX(safeBounds),
                anchorY,
                1.0,
                1.0);
            popover.permittedArrowDirections = UIPopoverArrowDirectionAny;
        }

        [presenter presentViewController:controller animated:YES completion:^{
            if (!delegate.completed && opened != NULL)
            {
                opened(requestId.UTF8String);
            }
            UIView *container = controller.presentationController.containerView;
            if (container != nil)
            {
                UITapGestureRecognizer *backgroundTap = [[UITapGestureRecognizer alloc]
                    initWithTarget:delegate
                    action:@selector(backgroundTapped:)];
                backgroundTap.delegate = delegate;
                backgroundTap.cancelsTouchesInView = NO;
                [container addGestureRecognizer:backgroundTap];
            }
        }];
    });
}

extern "C" void NativePrompt_DismissBottomSheet(const char *requestIdValue)
{
    NSString *requestId = requestIdValue == NULL
        ? @""
        : [NSString stringWithUTF8String:requestIdValue];
    dispatch_async(dispatch_get_main_queue(), ^{
        NativePromptBottomSheetDelegate *delegate = NativePromptBottomSheets()[requestId];
        if (delegate != nil && !delegate.completed)
        {
            delegate.completed = YES;
            [NativePromptBottomSheets() removeObjectForKey:requestId];
            [delegate.controller dismissViewControllerAnimated:NO completion:nil];
        }
    });
}

extern "C" void NativePrompt_ResetBottomSheets(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        NSArray<NativePromptBottomSheetDelegate *> *delegates =
            NativePromptBottomSheets().allValues;
        [NativePromptBottomSheets() removeAllObjects];
        for (NativePromptBottomSheetDelegate *delegate in delegates)
        {
            delegate.completed = YES;
            [delegate.controller dismissViewControllerAnimated:NO completion:nil];
        }
    });
}
