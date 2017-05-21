using MusicOnTheRoad.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
	public sealed partial class Player : UserControl
	{
		private readonly PlayerVM _vm = null;
		public PlayerVM VM { get { return _vm; } }
		public Player()
		{
			this.InitializeComponent();
			mediaPlayerElement.TransportControls = new MediaTransportControls()
			{
				IsFastForwardButtonVisible = false,
				IsFastForwardEnabled = false,
				IsFastRewindButtonVisible = false,
				IsFastRewindEnabled = false,
				IsNextTrackButtonVisible = true,
				IsPreviousTrackButtonVisible = true,
				IsSkipBackwardButtonVisible = true,
				IsSkipBackwardEnabled = true,
				IsSkipForwardButtonVisible = true,
				IsSkipForwardEnabled = true,
				IsVolumeEnabled = true,
				IsStopButtonVisible = true,
				IsStopEnabled = true
			};
			_vm = new PlayerVM(mediaPlayerElement.MediaPlayer);
		}

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

		private void OnRootFolderPathBorder_Tapped(object sender, TappedRoutedEventArgs e)
		{
			_vm.ToggleExpandRootFolder((sender as FrameworkElement).DataContext as FolderWithChildren);
		}

		private async void OnChildFolderBorder_Tapped(object sender, TappedRoutedEventArgs e)
		{
			await _vm.SetSourceFolderAsync((sender as FrameworkElement).DataContext as NameAndPath).ConfigureAwait(false);
		}
	}
}
