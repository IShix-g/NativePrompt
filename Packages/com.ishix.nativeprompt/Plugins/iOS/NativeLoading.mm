#import <UIKit/UIKit.h>

@interface NativePromptLoadingOverlayView : UIView

@property(nonatomic, assign) BOOL blocksInteraction;

@end

@implementation NativePromptLoadingOverlayView

- (UIView *)hitTest:(CGPoint)point withEvent:(UIEvent *)event
{
    if (!self.blocksInteraction)
    {
        return nil;
    }

    UIView *hitView = [super hitTest:point withEvent:event];
    return hitView == nil ? self : hitView;
}

@end

@interface NativePromptLoadingState : NSObject

@property(nonatomic, copy) NSString *requestId;
@property(nonatomic, strong) UIWindow *window;
@property(nonatomic, strong) NativePromptLoadingOverlayView *overlay;
@property(nonatomic, strong) UIView *background;
@property(nonatomic, strong) UIStackView *content;
@property(nonatomic, copy) dispatch_block_t showVisualsBlock;

- (instancetype)initWithWindow:(UIWindow *)window;
- (void)configureRequestId:(NSString *)requestId
                   message:(NSString *)message
         blocksInteraction:(BOOL)blocksInteraction
           showsBackground:(BOOL)showsBackground
             backgroundRed:(CGFloat)backgroundRed
           backgroundGreen:(CGFloat)backgroundGreen
            backgroundBlue:(CGFloat)backgroundBlue
         backgroundOpacity:(CGFloat)backgroundOpacity
                  position:(NSInteger)position
                      size:(NSInteger)size
          showDelaySeconds:(NSTimeInterval)showDelaySeconds;
- (void)remove;

@end

static NativePromptLoadingState *NativePromptLoadingCurrentState;

static UIWindow *NativePromptLoadingKeyWindow(void)
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

@implementation NativePromptLoadingState

- (instancetype)initWithWindow:(UIWindow *)window
{
    self = [super init];
    if (self == nil)
    {
        return nil;
    }

    self.window = window;
    self.overlay = [NativePromptLoadingOverlayView new];
    self.overlay.translatesAutoresizingMaskIntoConstraints = NO;
    self.overlay.backgroundColor = UIColor.clearColor;
    self.overlay.userInteractionEnabled = YES;

    self.background = [UIView new];
    self.background.translatesAutoresizingMaskIntoConstraints = NO;
    self.background.userInteractionEnabled = NO;
    [self.overlay addSubview:self.background];
    [window addSubview:self.overlay];
    [NSLayoutConstraint activateConstraints:@[
        [self.overlay.leadingAnchor constraintEqualToAnchor:window.leadingAnchor],
        [self.overlay.trailingAnchor constraintEqualToAnchor:window.trailingAnchor],
        [self.overlay.topAnchor constraintEqualToAnchor:window.topAnchor],
        [self.overlay.bottomAnchor constraintEqualToAnchor:window.bottomAnchor],
        [self.background.leadingAnchor constraintEqualToAnchor:self.overlay.leadingAnchor],
        [self.background.trailingAnchor constraintEqualToAnchor:self.overlay.trailingAnchor],
        [self.background.topAnchor constraintEqualToAnchor:self.overlay.topAnchor],
        [self.background.bottomAnchor constraintEqualToAnchor:self.overlay.bottomAnchor],
    ]];
    return self;
}

- (void)configureRequestId:(NSString *)requestId
                   message:(NSString *)message
         blocksInteraction:(BOOL)blocksInteraction
           showsBackground:(BOOL)showsBackground
             backgroundRed:(CGFloat)backgroundRed
           backgroundGreen:(CGFloat)backgroundGreen
            backgroundBlue:(CGFloat)backgroundBlue
         backgroundOpacity:(CGFloat)backgroundOpacity
                  position:(NSInteger)position
                      size:(NSInteger)size
          showDelaySeconds:(NSTimeInterval)showDelaySeconds
{
    if (self.showVisualsBlock != nil)
    {
        dispatch_block_cancel(self.showVisualsBlock);
        self.showVisualsBlock = nil;
    }

    self.requestId = requestId;
    self.overlay.blocksInteraction = blocksInteraction;
    [self.window bringSubviewToFront:self.overlay];
    self.background.hidden = YES;
    self.background.backgroundColor = [UIColor
        colorWithRed:backgroundRed
        green:backgroundGreen
        blue:backgroundBlue
        alpha:backgroundOpacity];

    [self.content removeFromSuperview];
    UIActivityIndicatorViewStyle indicatorStyle = size == 2
        ? UIActivityIndicatorViewStyleLarge
        : UIActivityIndicatorViewStyleMedium;
    UIActivityIndicatorView *indicator = [[UIActivityIndicatorView alloc]
        initWithActivityIndicatorStyle:indicatorStyle];
    indicator.translatesAutoresizingMaskIntoConstraints = NO;
    indicator.userInteractionEnabled = NO;
    if (size == 0)
    {
        indicator.transform = CGAffineTransformMakeScale(0.75, 0.75);
    }
    [indicator startAnimating];

    NSMutableArray<UIView *> *views = [NSMutableArray arrayWithObject:indicator];
    if (message.length > 0)
    {
        UILabel *label = [UILabel new];
        label.translatesAutoresizingMaskIntoConstraints = NO;
        label.text = message;
        label.textColor = UIColor.labelColor;
        label.font = [UIFont preferredFontForTextStyle:UIFontTextStyleBody];
        label.adjustsFontForContentSizeCategory = YES;
        label.numberOfLines = 0;
        label.lineBreakMode = NSLineBreakByWordWrapping;
        label.userInteractionEnabled = NO;
        [views addObject:label];
    }

    self.content = [[UIStackView alloc] initWithArrangedSubviews:views];
    self.content.translatesAutoresizingMaskIntoConstraints = NO;
    self.content.axis = UILayoutConstraintAxisHorizontal;
    self.content.alignment = UIStackViewAlignmentCenter;
    self.content.spacing = 8.0;
    self.content.userInteractionEnabled = NO;
    self.content.hidden = YES;
    [self.overlay addSubview:self.content];

    UILayoutGuide *safeArea = self.overlay.safeAreaLayoutGuide;
    NSMutableArray<NSLayoutConstraint *> *constraints = [NSMutableArray arrayWithArray:@[
        [self.content.leadingAnchor constraintGreaterThanOrEqualToAnchor:safeArea.leadingAnchor constant:24.0],
        [self.content.trailingAnchor constraintLessThanOrEqualToAnchor:safeArea.trailingAnchor constant:-24.0],
    ]];
    switch (position)
    {
        case 1:
            [constraints addObject:[self.content.leadingAnchor constraintEqualToAnchor:safeArea.leadingAnchor constant:24.0]];
            [constraints addObject:[self.content.topAnchor constraintEqualToAnchor:safeArea.topAnchor constant:24.0]];
            break;
        case 2:
            [constraints addObject:[self.content.trailingAnchor constraintEqualToAnchor:safeArea.trailingAnchor constant:-24.0]];
            [constraints addObject:[self.content.topAnchor constraintEqualToAnchor:safeArea.topAnchor constant:24.0]];
            break;
        case 3:
            [constraints addObject:[self.content.leadingAnchor constraintEqualToAnchor:safeArea.leadingAnchor constant:24.0]];
            [constraints addObject:[self.content.bottomAnchor constraintEqualToAnchor:safeArea.bottomAnchor constant:-24.0]];
            break;
        case 4:
            [constraints addObject:[self.content.trailingAnchor constraintEqualToAnchor:safeArea.trailingAnchor constant:-24.0]];
            [constraints addObject:[self.content.bottomAnchor constraintEqualToAnchor:safeArea.bottomAnchor constant:-24.0]];
            break;
        case 0:
        default:
            [constraints addObject:[self.content.centerXAnchor constraintEqualToAnchor:safeArea.centerXAnchor]];
            [constraints addObject:[self.content.centerYAnchor constraintEqualToAnchor:safeArea.centerYAnchor]];
            break;
    }
    [NSLayoutConstraint activateConstraints:constraints];

    __weak NativePromptLoadingState *weakSelf = self;
    NSString *configuredRequestId = [requestId copy];
    dispatch_block_t showBlock = dispatch_block_create((dispatch_block_flags_t)0, ^{
        NativePromptLoadingState *strongSelf = weakSelf;
        if (strongSelf == nil ||
            ![strongSelf.requestId isEqualToString:configuredRequestId])
        {
            return;
        }
        strongSelf.background.hidden = !showsBackground;
        strongSelf.content.hidden = NO;
        strongSelf.showVisualsBlock = nil;
    });
    self.showVisualsBlock = showBlock;
    if (showDelaySeconds <= 0.0)
    {
        showBlock();
    }
    else
    {
        dispatch_after(
            dispatch_time(DISPATCH_TIME_NOW, (int64_t)(showDelaySeconds * NSEC_PER_SEC)),
            dispatch_get_main_queue(),
            showBlock);
    }
}

- (void)remove
{
    if (self.showVisualsBlock != nil)
    {
        dispatch_block_cancel(self.showVisualsBlock);
        self.showVisualsBlock = nil;
    }
    self.requestId = nil;
    [self.content removeFromSuperview];
    [self.overlay removeFromSuperview];
    self.content = nil;
    self.background = nil;
    self.overlay = nil;
    self.window = nil;
}

@end

extern "C" void NativePrompt_ShowLoading(
    const char *requestIdValue,
    const char *messageValue,
    bool blocksInteraction,
    bool showsBackground,
    float backgroundRed,
    float backgroundGreen,
    float backgroundBlue,
    float backgroundOpacity,
    int position,
    int size,
    float showDelaySeconds)
{
    NSString *requestId = requestIdValue == NULL
        ? @""
        : [NSString stringWithUTF8String:requestIdValue];
    NSString *message = messageValue == NULL
        ? nil
        : [NSString stringWithUTF8String:messageValue];
    dispatch_async(dispatch_get_main_queue(), ^{
        UIWindow *window = NativePromptLoadingKeyWindow();
        if (window == nil)
        {
            return;
        }

        if (NativePromptLoadingCurrentState == nil ||
            NativePromptLoadingCurrentState.window != window)
        {
            [NativePromptLoadingCurrentState remove];
            NativePromptLoadingCurrentState = [[NativePromptLoadingState alloc]
                initWithWindow:window];
        }
        [NativePromptLoadingCurrentState
            configureRequestId:requestId
            message:message
            blocksInteraction:blocksInteraction
            showsBackground:showsBackground
            backgroundRed:backgroundRed
            backgroundGreen:backgroundGreen
            backgroundBlue:backgroundBlue
            backgroundOpacity:backgroundOpacity
            position:position
            size:size
            showDelaySeconds:showDelaySeconds];
    });
}

extern "C" void NativePrompt_DismissLoading(const char *requestIdValue)
{
    NSString *requestId = requestIdValue == NULL
        ? @""
        : [NSString stringWithUTF8String:requestIdValue];
    dispatch_async(dispatch_get_main_queue(), ^{
        if ([NativePromptLoadingCurrentState.requestId isEqualToString:requestId])
        {
            [NativePromptLoadingCurrentState remove];
            NativePromptLoadingCurrentState = nil;
        }
    });
}

extern "C" void NativePrompt_ResetLoading(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        [NativePromptLoadingCurrentState remove];
        NativePromptLoadingCurrentState = nil;
    });
}
