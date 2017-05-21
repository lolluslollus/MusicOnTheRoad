using MusicOnTheRoad.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilz;
using Utilz.Data;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace MusicOnTheRoad.ViewModels
{
	public class PlayerVM : ObservableData
	{
		private readonly PersistentData _persistentData = null;
		public PersistentData PersistentData { get { return _persistentData; } }
		private readonly MediaPlayer _mediaPlayer = null;
		private IMediaPlaybackSource _source = null;
		public IMediaPlaybackSource Source { get { return _source; } }
		private readonly SwitchableObservableCollection<FolderWithChildren> _foldersWithChildren = new SwitchableObservableCollection<FolderWithChildren>();
		public SwitchableObservableCollection<FolderWithChildren> FoldersWithChildren { get { return _foldersWithChildren; } }

		public PlayerVM(MediaPlayer mediaPlayer)
		{
			_mediaPlayer = mediaPlayer;
			_persistentData = PersistentData.GetInstance();
			//RaisePropertyChanged(nameof(PersistentData));
			foreach (var folderPath in _persistentData.RootFolderPaths)
			{
				FoldersWithChildren.Add(new FolderWithChildren(folderPath));
			}
		}
		public async Task<bool> SetSourceFileAsync()
		{
			var file = await Utilz.Pickers.PickOpenFileAsync(ConstantData.Extensions, PickerLocationId.MusicLibrary);
			if (file == null) return false;
			_source = MediaSource.CreateFromStorageFile(file);
			RaisePropertyChanged_UI(nameof(Source));
			return true;
		}
		public async Task<bool> SetSourceFolderAsync(string folderPath = null)
		{
			StorageFolder folder = null;
			if (String.IsNullOrWhiteSpace(folderPath)) folder = await Utilz.Pickers.PickDirectoryAsync(ConstantData.Extensions, PickerLocationId.MusicLibrary);
			else folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
			if (folder == null) return false;

			var files = await folder.GetFilesAsync();
			MediaPlaybackList mediaPlaybackList = null;
			foreach (var file in files)
			{
				if (ConstantData.Extensions.Any((ext) => { return ext == file.FileType; }))
				{
					if (mediaPlaybackList == null) mediaPlaybackList = new MediaPlaybackList() { AutoRepeatEnabled = false, MaxPlayedItemsToKeepOpen = 1 };
					var mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file)) { AutoLoadedDisplayProperties = AutoLoadedDisplayPropertyKind.Music, CanSkip = true };
					mediaPlaybackList.Items.Add(mediaPlaybackItem);
				}
			}

			_source = mediaPlaybackList;
			RaisePropertyChanged_UI(nameof(Source));
			return true;
		}
		public async Task AddRootFolderAsync()
		{
			var folder = await Utilz.Pickers.PickDirectoryAsync(ConstantData.Extensions, PickerLocationId.MusicLibrary);
			if (folder == null) return;

			_persistentData.AddRootFolderPath(folder.Path);

			if (FoldersWithChildren.Any((fwc) => { return fwc.FolderPath == folder.Path; })) return;
			FoldersWithChildren.Add(new FolderWithChildren(folder.Path));
		}
		public void ClearRootFolders()
		{
			_persistentData.ClearRootFolders();
			FoldersWithChildren.Clear();
		}
		public void CollapseRootFolders()
		{
			foreach (var item in FoldersWithChildren)
			{
				item.Children.Clear();
				item.IsExpanded = false;
			}
		}

		public void ToggleExpandRootFolder(FolderWithChildren folderWithChildren)
		{
			var toBeExpanded = FoldersWithChildren.FirstOrDefault((fwc) => { return fwc.FolderPath == folderWithChildren.FolderPath; });
			bool isExpanded = toBeExpanded?.Children.Count > 0;
			CollapseRootFolders();

			if (toBeExpanded == null || isExpanded) return;

			// LOLLO TODO put this away in a separate thread				
			string[] children = System.IO.Directory.GetDirectories(folderWithChildren.FolderPath);
			toBeExpanded.Children.AddRange(children);
			toBeExpanded.IsExpanded = true;
		}

		public void RemoveRootFolder(string folderPath)
		{
			_persistentData.RemoveRootFolderPath(folderPath);

			var toBeRemoved = FoldersWithChildren.FirstOrDefault((folderWithChildren) => { return folderWithChildren.FolderPath == folderPath; });
			if (toBeRemoved == null) return;
			FoldersWithChildren.Remove(toBeRemoved);
		}

		#region event handlers
		private bool _isHandlersActive = false;

		private void _addHandlers()
		{
			if (_isHandlersActive) return;
			_mediaPlayer.MediaEnded += OnMediaPlayer_MediaEnded;
			_mediaPlayer.MediaFailed += OnMediaPlayer_MediaFailed;
			_mediaPlayer.MediaOpened += OnMediaPlayer_MediaOpened;
			_isHandlersActive = true;
		}

		private void _removeHandlers()
		{
			_mediaPlayer.MediaEnded -= OnMediaPlayer_MediaEnded;
			_mediaPlayer.MediaFailed -= OnMediaPlayer_MediaFailed;
			_mediaPlayer.MediaOpened -= OnMediaPlayer_MediaOpened;
			_isHandlersActive = false;
		}

		private void OnMediaPlayer_MediaOpened(Windows.Media.Playback.MediaPlayer sender, object args)
		{
			throw new NotImplementedException();
		}

		private void OnMediaPlayer_MediaFailed(Windows.Media.Playback.MediaPlayer sender, Windows.Media.Playback.MediaPlayerFailedEventArgs args)
		{
			throw new NotImplementedException();
		}

		private void OnMediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
		{
			throw new NotImplementedException();
		}
		#endregion event handlers

		#region utils
		#endregion utils
	}

	public class FolderWithChildren : ObservableData
	{
		private string _folderPath = null;
		public string FolderPath { get { return _folderPath; } set { _folderPath = value; RaisePropertyChanged(); } }
		private readonly SwitchableObservableCollection<string> _children = new SwitchableObservableCollection<string>();
		public SwitchableObservableCollection<string> Children { get { return _children; } }
		private bool _isExpanded = false;
		public bool IsExpanded { get { return _isExpanded; } set { _isExpanded = value; RaisePropertyChanged(); } }

		public FolderWithChildren(string folderPath)
		{
			FolderPath = folderPath;
		}
	}

	public class NameAndPath : ObservableData
	{
		private string _name = null;
		public string Name { get { return _name; } set { _name = value; RaisePropertyChanged(); } }
		private string _path = null;
		public string Path { get { return _path; } set { _path = value; RaisePropertyChanged(); } }
	}
}
