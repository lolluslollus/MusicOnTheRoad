using MusicOnTheRoad.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Utilz.Controlz;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MusicOnTheRoad.Views
{
	public sealed partial class Player : ObservableControl, IDisposable
	{
		private PlayerVM _vm = null;
		public PlayerVM VM { get { return _vm; } }

		#region lifecycle
		public Player()
		{
			this.InitializeComponent();
			mediaPlayerElement.TransportControls = new MediaTransportControls()
			{
				IsCompact = false,
				IsFastForwardButtonVisible = false,
				IsFastForwardEnabled = false,
				IsFastRewindButtonVisible = false,
				IsFastRewindEnabled = false,
				IsFullWindowButtonVisible = false,
				IsFullWindowEnabled = false,
				IsNextTrackButtonVisible = true,
				IsPlaybackRateButtonVisible = false,
				IsPlaybackRateEnabled = false,
				IsPreviousTrackButtonVisible = true,
				IsSeekBarVisible = true,
				IsSeekEnabled = true,
				IsSkipBackwardButtonVisible = false,
				IsSkipBackwardEnabled = false,
				IsSkipForwardButtonVisible = false,
				IsSkipForwardEnabled = false,
				IsStopButtonVisible = true,
				IsStopEnabled = true,
				IsVolumeButtonVisible = false,
				IsVolumeEnabled = false,
				IsZoomButtonVisible = false,
				IsZoomEnabled = false
			};
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			_vm = new PlayerVM(mediaPlayerElement.MediaPlayer);
            //_vm.PropertyChanged += OnVMPropertyChanged;
			RaisePropertyChanged_UI(nameof(VM));
		}

        //private void OnVMPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == nameof(VM.IsLoadingChildren)) {
        //        if (VM.IsLoadingChildren) ShowLoadingOverlay.Begin();
        //        else HideLoadingOverlay.Begin();
        //    }
        //}

        private void OnUnloaded(object sender, RoutedEventArgs e)
		{
            Dispose(true);
		}
        #endregion lifecycle

        private void OnPinnedFolderPathBorder_Tapped(object sender, TappedRoutedEventArgs e)
		{
			Task task = _vm.OpenOrToggleExpandPinnedFolderAsync((sender as FrameworkElement).DataContext as FolderWithChildren);
		}

		private void OnChildFolderBorder_Tapped(object sender, TappedRoutedEventArgs e)
		{
            Task task = _vm.SetSourceFolderAsync((sender as FrameworkElement).DataContext as NameAndPath);
		}

		private void OnRemovePinnedFolderIcon_Tapped(object sender, TappedRoutedEventArgs e)
		{
			e.Handled = true;
            Task task = _vm.RemovePinnedFolderAsync(((sender as FrameworkElement).DataContext as FolderWithChildren).FolderPath);
		}

        #region IDisposable Support
        private bool isDisposed = false; // To detect redundant calls

        void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    // TODO: dispose managed state (managed objects).
                    var vm = _vm;
                    if (vm != null)
                    {
                        //vm.PropertyChanged -= OnVMPropertyChanged;
                        vm.Dispose();
                    }
                    _vm = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                isDisposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Player() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
