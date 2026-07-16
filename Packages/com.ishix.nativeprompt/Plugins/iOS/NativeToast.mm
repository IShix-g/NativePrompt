#import <UIKit/UIKit.h>

typedef void (*NativePromptToastDismissedCallback)(const char *requestId, int reason);
typedef void (*NativePromptToastShownCallback)(const char *requestId);

enum NativePromptToastDismissReason
{
    NativePromptToastDismissReasonTimedOut = 0,
    NativePromptToastDismissReasonTapped = 1,
};

@interface NativePromptToastState : NSObject

@property(nonatomic, copy) NSString *requestId;
@property(nonatomic, strong) UIView *view;
@property(nonatomic, copy) dispatch_block_t timeoutBlock;
@property(nonatomic, assign) NativePromptToastDismissedCallback dismissed;
@property(nonatomic, assign) BOOL completed;

- (void)completeWithReason:(NativePromptToastDismissReason)reason;
- (void)dismissWithoutCallback;
- (void)toastTapped:(UITapGestureRecognizer *)recognizer;

@end

static NSMutableDictionary<NSString *, NativePromptToastState *> *NativePromptToasts(void)
{
    static NSMutableDictionary<NSString *, NativePromptToastState *> *toasts;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        toasts = [NSMutableDictionary dictionary];
    });
    return toasts;
}

@implementation NativePromptToastState

- (void)completeWithReason:(NativePromptToastDismissReason)reason
{
    if (self.completed)
    {
        return;
    }

    self.completed = YES;
    if (self.timeoutBlock != nil)
    {
        dispatch_block_cancel(self.timeoutBlock);
        self.timeoutBlock = nil;
    }
    [NativePromptToasts() removeObjectForKey:self.requestId];
    [self.view removeFromSuperview];

    if (self.dismissed != NULL)
    {
        self.dismissed(self.requestId.UTF8String, (int)reason);
    }
}

- (void)dismissWithoutCallback
{
    if (self.completed)
    {
        return;
    }

    self.completed = YES;
    if (self.timeoutBlock != nil)
    {
        dispatch_block_cancel(self.timeoutBlock);
        self.timeoutBlock = nil;
    }
    [NativePromptToasts() removeObjectForKey:self.requestId];
    [self.view removeFromSuperview];
}

- (void)toastTapped:(UITapGestureRecognizer *)recognizer
{
    if (recognizer.state == UIGestureRecognizerStateEnded)
    {
        [self completeWithReason:NativePromptToastDismissReasonTapped];
    }
}

@end

static UIWindow *NativePromptToastKeyWindow(void)
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

extern "C" void NativePrompt_ShowToast(
    const char *requestIdValue,
    const char *messageValue,
    float duration,
    bool autoDismiss,
    bool dismissOnTap,
    int position,
    NativePromptToastShownCallback shown,
    NativePromptToastDismissedCallback dismissed)
{
    NSString *requestId = requestIdValue == NULL
        ? @""
        : [NSString stringWithUTF8String:requestIdValue];
    NSString *message = messageValue == NULL
        ? @""
        : [NSString stringWithUTF8String:messageValue];

    dispatch_async(dispatch_get_main_queue(), ^{
        UIWindow *window = NativePromptToastKeyWindow();
        if (window == nil)
        {
            if (dismissed != NULL)
            {
                dismissed(requestId.UTF8String, NativePromptToastDismissReasonTimedOut);
            }
            return;
        }

        NativePromptToastState *existing = NativePromptToasts()[requestId];
        [existing dismissWithoutCallback];

        UIView *toastView = [UIView new];
        toastView.translatesAutoresizingMaskIntoConstraints = NO;
        toastView.backgroundColor = [UIColor colorWithWhite:0.08 alpha:0.92];
        toastView.layer.cornerRadius = 12.0;
        toastView.layer.masksToBounds = YES;
        toastView.userInteractionEnabled = dismissOnTap;

        UILabel *label = [UILabel new];
        label.translatesAutoresizingMaskIntoConstraints = NO;
        label.text = message;
        label.textColor = UIColor.whiteColor;
        label.font = [UIFont preferredFontForTextStyle:UIFontTextStyleBody];
        label.textAlignment = NSTextAlignmentCenter;
        label.numberOfLines = 3;
        label.lineBreakMode = NSLineBreakByTruncatingTail;
        label.adjustsFontForContentSizeCategory = YES;
        [toastView addSubview:label];
        [window addSubview:toastView];

        UILayoutGuide *safeArea = window.safeAreaLayoutGuide;
        NSMutableArray<NSLayoutConstraint *> *constraints = [NSMutableArray arrayWithArray:@[
            [toastView.centerXAnchor constraintEqualToAnchor:safeArea.centerXAnchor],
            [toastView.leadingAnchor constraintGreaterThanOrEqualToAnchor:safeArea.leadingAnchor constant:16.0],
            [toastView.trailingAnchor constraintLessThanOrEqualToAnchor:safeArea.trailingAnchor constant:-16.0],
            [label.leadingAnchor constraintEqualToAnchor:toastView.leadingAnchor constant:16.0],
            [label.trailingAnchor constraintEqualToAnchor:toastView.trailingAnchor constant:-16.0],
            [label.topAnchor constraintEqualToAnchor:toastView.topAnchor constant:10.0],
            [label.bottomAnchor constraintEqualToAnchor:toastView.bottomAnchor constant:-10.0],
        ]];

        if (position == 0)
        {
            [constraints addObject:[toastView.topAnchor constraintEqualToAnchor:safeArea.topAnchor constant:16.0]];
        }
        else if (position == 1)
        {
            [constraints addObject:[toastView.centerYAnchor constraintEqualToAnchor:safeArea.centerYAnchor]];
        }
        else
        {
            [constraints addObject:[toastView.bottomAnchor constraintEqualToAnchor:safeArea.bottomAnchor constant:-16.0]];
        }
        [NSLayoutConstraint activateConstraints:constraints];

        NativePromptToastState *state = [NativePromptToastState new];
        state.requestId = requestId;
        state.view = toastView;
        state.dismissed = dismissed;
        NativePromptToasts()[requestId] = state;
        if (shown != NULL)
        {
            shown(requestId.UTF8String);
        }

        if (dismissOnTap)
        {
            UITapGestureRecognizer *tap = [[UITapGestureRecognizer alloc]
                initWithTarget:state
                action:@selector(toastTapped:)];
            [toastView addGestureRecognizer:tap];
        }

        if (autoDismiss)
        {
            dispatch_block_t timeoutBlock = dispatch_block_create((dispatch_block_flags_t)0, ^{
                [state completeWithReason:NativePromptToastDismissReasonTimedOut];
            });
            state.timeoutBlock = timeoutBlock;
            dispatch_after(
                dispatch_time(DISPATCH_TIME_NOW, (int64_t)(duration * NSEC_PER_SEC)),
                dispatch_get_main_queue(),
                timeoutBlock);
        }
    });
}

extern "C" void NativePrompt_DismissToast(const char *requestIdValue)
{
    NSString *requestId = requestIdValue == NULL
        ? @""
        : [NSString stringWithUTF8String:requestIdValue];
    dispatch_async(dispatch_get_main_queue(), ^{
        [NativePromptToasts()[requestId] dismissWithoutCallback];
    });
}

extern "C" void NativePrompt_ResetToasts(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        NSArray<NativePromptToastState *> *states = NativePromptToasts().allValues;
        [NativePromptToasts() removeAllObjects];
        for (NativePromptToastState *state in states)
        {
            [state dismissWithoutCallback];
        }
    });
}
