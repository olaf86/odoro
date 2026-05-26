#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

extern UIViewController *UnityGetGLViewController(void);

extern "C"
{
    void OdoroShareFile(const char *path)
    {
        if (path == NULL)
        {
            return;
        }

        NSString *filePath = [NSString stringWithUTF8String:path];
        if (filePath.length == 0 || ![[NSFileManager defaultManager] fileExistsAtPath:filePath])
        {
            return;
        }

        dispatch_async(dispatch_get_main_queue(), ^{
            NSURL *fileURL = [NSURL fileURLWithPath:filePath];
            UIActivityViewController *activityViewController =
                [[UIActivityViewController alloc] initWithActivityItems:@[fileURL] applicationActivities:nil];

            UIViewController *rootViewController = UnityGetGLViewController();
            if (rootViewController == nil)
            {
                rootViewController = UIApplication.sharedApplication.keyWindow.rootViewController;
            }

            if (rootViewController == nil)
            {
                return;
            }

            UIPopoverPresentationController *popover = activityViewController.popoverPresentationController;
            if (popover != nil)
            {
                popover.sourceView = rootViewController.view;
                popover.sourceRect = CGRectMake(
                    CGRectGetMidX(rootViewController.view.bounds),
                    CGRectGetMidY(rootViewController.view.bounds),
                    1.0,
                    1.0
                );
                popover.permittedArrowDirections = 0;
            }

            [rootViewController presentViewController:activityViewController animated:YES completion:nil];
        });
    }
}
