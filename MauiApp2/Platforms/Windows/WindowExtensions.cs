#if WINDOWS
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI;

namespace MauiApp2.Platforms.Windows
{
    public static class WindowExtensions
    {
        public static void MaximizeWindow(this Microsoft.Maui.Controls.Window window)
        {
            if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            {
                try
                {
                    var appWindow = nativeWindow.AppWindow;
                    if (appWindow != null)
                    {
                        // Get the display area for the current monitor
                        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest);
                        if (displayArea != null)
                        {
                            // Maximize to fill the entire work area (full screen)
                            appWindow.MoveAndResize(displayArea.WorkArea);
                        }
                    }
                }
                catch
                {
                    // If we can't maximize, at least activate the window
                    try
                    {
                        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window xamlWindow)
                        {
                            xamlWindow.Activate();
                        }
                    }
                    catch
                    {
                        // Silently fail if window activation doesn't work
                    }
                }
            }
        }
    }
}
#endif

