using Microsoft.Maui.Devices;

namespace MauiApp2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) 
            { 
                Title = "QuadTech - Inventory Management System"
            };

#if WINDOWS
            // Only maximize window on desktop Windows, not on mobile/tablet
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                window.Created += (s, e) =>
                {
                    if (s is Window w)
                    {
                        // Use a small delay to ensure the window is fully initialized
                        Task.Delay(100).ContinueWith(_ =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                try
                                {
                                    Platforms.Windows.WindowExtensions.MaximizeWindow(w);
                                }
                                catch
                                {
                                    // If maximization fails, window will still open normally
                                }
                            });
                        });
                    }
                };
            }
#endif

            return window;
        }
    }
}
