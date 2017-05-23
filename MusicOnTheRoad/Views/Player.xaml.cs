using MusicOnTheRoad.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
	public sealed partial class Player : ObservableControl
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
            var vm = _vm;
            if (vm != null)
            {
                //vm.PropertyChanged -= OnVMPropertyChanged;
                vm.Dispose();
            }
			_vm = null;
		}
		#endregion lifecycle

		private void OnListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			//_vm.RemoveRootFolder(e.ClickedItem.ToString());
		}

		private void OnListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//var selectedItem = (sender as ListView).SelectedItem;
			//var selectedValue = (sender as ListView).SelectedValue;
			//_vm.ExpandRootFolder(selectedValue as FolderWithChildren);
		}
		private void OnChildListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			//_vm.RemoveRootFolder(e.ClickedItem.ToString());
		}

		private async void OnChildListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//var selectedItem = (sender as ListView).SelectedItem;
			//var selectedValue = (sender as ListView).SelectedValue;
			//await _vm.SetSourceFolderAsync(selectedValue.ToString()).ConfigureAwait(false);
		}

		private async void OnPickFolderButton_Click(object sender, RoutedEventArgs e)
		{
			await _vm.SetSourceFolderAsync().ConfigureAwait(false);
		}

		private async void OnRootFolderPathBorder_Tapped(object sender, TappedRoutedEventArgs e)
		{
			await _vm.ToggleExpandRootFolderAsync((sender as FrameworkElement).DataContext as FolderWithChildren).ConfigureAwait(false);
		}

		private async void OnChildFolderBorder_Tapped(object sender, TappedRoutedEventArgs e)
		{
			await _vm.SetSourceFolderAsync((sender as FrameworkElement).DataContext as NameAndPath).ConfigureAwait(false);
		}

		private void OnRemoveRootFolderIcon_Tapped(object sender, TappedRoutedEventArgs e)
		{
			e.Handled = true;
			_vm.RemoveRootFolder(((sender as FrameworkElement).DataContext as FolderWithChildren).FolderPath);
		}
	}
}
