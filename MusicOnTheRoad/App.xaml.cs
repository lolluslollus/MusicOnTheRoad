using MusicOnTheRoad.Data;
using MusicOnTheRoad.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Utilz;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicOnTheRoad
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.UnhandledException += OnUnhandledException;
            this.InitializeComponent();
            this.Resuming += OnResuming;
            this.Suspending += OnSuspending;
            this.EnteredBackground += OnEnteredBackground;
            this.LeavingBackground += OnLeavingBackground;
            Logger.Add_TPL("App ctor ended OK", Logger.AppEventsLogFilename, Logger.Severity.Info, false);
        }

        /// <summary>
        /// newly, the best place to save the app data. Mind that it fires when opening a picker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnEnteredBackground(object sender, EnteredBackgroundEventArgs args)
        {
            //var deferral = args.GetDeferral();
            //try
            //{
            //	Logger.Add_TPL("App OnEnteredBackground", Logger.AppEventsLogFilename, Logger.Severity.Info, false);
            //	await SuspensionManager.SaveAsync().ConfigureAwait(false);
            //}
            //finally
            //{
            //	deferral.Complete();
            //}
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Logger.Add_TPL($"OnLaunched started with arguments = {args.Arguments} and kind = {args.Kind.ToString()} and prelaunch activated = {args.PrelaunchActivated} and prev exec state = {args.PreviousExecutionState.ToString()}",
                Logger.AppEventsLogFilename,
                Logger.Severity.Info,
                false);

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                //if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                //{
                //    //MS TODO: Load state from previously suspended application
                //}
                SuspensionManager.LoadAsync().Wait();

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (args.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), args.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }
        /// <summary>
        /// newly, the best place to load the app data. Mind that it fires when returning from a picker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLeavingBackground(object sender, LeavingBackgroundEventArgs args)
        {
            //var deferral = args.GetDeferral();
            //try
            //{
            //	Logger.Add_TPL("App OnLeavingBackground", Logger.AppEventsLogFilename, Logger.Severity.Info, false);
            //	await SuspensionManager.LoadAsync().ConfigureAwait(false);
            //}
            //finally
            //{
            //	deferral.Complete();
            //}
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private async void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            await Logger.AddAsync(e.Exception.ToString(), Logger.AppEventsLogFilename);
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnResuming(object sender, object e)
        {
            Logger.Add_TPL("App OnResuming", Logger.AppEventsLogFilename, Logger.Severity.Info, false);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            Logger.Add_TPL("App OnSuspending", Logger.AppEventsLogFilename, Logger.Severity.Info, false);
            //MS TODO: Save application state and stop any background activity
            SuspensionManager.SaveAsync().Wait();

            deferral.Complete();
        }

        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // this does not always work when the device force-shuts the app
            await Logger.AddAsync("UnhandledException: " + e.Exception.ToString(), Logger.AppExceptionLogFilename);
        }
    }
}
