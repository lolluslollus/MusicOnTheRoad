using MusicOnTheRoad.Data;
using MusicOnTheRoad.Services;
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
	public class PlayerVM : ObservableData, IDisposable
	{
		private readonly PersistentData _persistentData = null;
		public PersistentData PersistentData { get { return _persistentData; } }
		private readonly MediaPlayer _mediaPlayer = null;
		private IMediaPlaybackSource _source = null;
		public IMediaPlaybackSource Source { get { return _source; } }
		private readonly SwitchableObservableCollection<FolderWithChildren> _foldersWithChildren = new SwitchableObservableCollection<FolderWithChildren>();
		public SwitchableObservableCollection<FolderWithChildren> FoldersWithChildren { get { return _foldersWithChildren; } }
		private string _lastMessage = null;
		public string LastMessage { get { return _lastMessage; } private set { _lastMessage = value; RaisePropertyChanged(); } }

		public PlayerVM(MediaPlayer mediaPlayer)
		{
			_mediaPlayer = mediaPlayer;

			_persistentData = PersistentData.GetInstance();
			RaisePropertyChanged(nameof(PersistentData));

			AddDataChangedHandlers();
			Task upd0 = UpdateLastMessage();
			Task upd1 = UpdateFoldersWithChildren();
		}

		private Task UpdateFoldersWithChildren()
		{
			return RunInUiThreadAsync(delegate
			{
				FoldersWithChildren.Clear();
				foreach (var folderPath in _persistentData.RootFolderPaths)
				{
					FoldersWithChildren.Add(new FolderWithChildren(folderPath));
				}
			});
		}
		private Task UpdateLastMessage()
		{
			return RunInUiThreadAsync(delegate
			{
				LastMessage = _persistentData.LastMessage;
			});
		}

		#region user actions
		public async Task<bool> SetSourceFileAsync()
		{
			var file = await Utilz.Pickers.PickOpenFileAsync(ConstantData.Extensions, PickerLocationId.MusicLibrary);
			if (file == null) return false;
			_source = MediaSource.CreateFromStorageFile(file);
			RaisePropertyChanged_UI(nameof(Source));
			return true;
		}
		public async Task<bool> SetSourceFolderAsync(NameAndPath nameAndPath = null)
		{
			StorageFolder folder = null;
			if (nameAndPath == null || String.IsNullOrWhiteSpace(nameAndPath.Path)) folder = await Utilz.Pickers.PickDirectoryAsync(ConstantData.Extensions, PickerLocationId.MusicLibrary);
			else folder = await StorageFolder.GetFolderFromPathAsync(nameAndPath.Path);
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
			string[] paths = System.IO.Directory.GetDirectories(folderWithChildren.FolderPath);
			List<NameAndPath> children = new List<NameAndPath>();
			foreach (var path in paths)
			{
				children.Add(new NameAndPath() { Name = System.IO.Path.GetFileName(path), Path = path });
			}
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
		#endregion user actions

		#region data event handlers
		private bool _isDataChangedHandlersActive = false;
		private void AddDataChangedHandlers()
		{
			if (_isDataChangedHandlersActive) return;
			SuspensionManager.Loaded += OnSuspensionManager_Loaded;
			_persistentData.PropertyChanged += OnPersistentData_PropertyChanged;
			_isDataChangedHandlersActive = true;
		}
		private void RemoveDataChangedHandlers()
		{
			SuspensionManager.Loaded -= OnSuspensionManager_Loaded;
			_persistentData.PropertyChanged -= OnPersistentData_PropertyChanged;
			_isDataChangedHandlersActive = false;
		}

		private void OnSuspensionManager_Loaded(object sender, bool e)
		{
			Task upd0 = UpdateLastMessage();
			Task upd1 = UpdateFoldersWithChildren();
		}

		private void OnPersistentData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PersistentData.LastMessage))
			{
				Task upd = UpdateLastMessage();
			}
		}
		#endregion data event handlers

		#region media event handlers
		private bool _isMediaHandlersActive = false;

		private void AddMediaHandlers()
		{
			if (_isMediaHandlersActive) return;
			_mediaPlayer.MediaEnded += OnMediaPlayer_MediaEnded;
			_mediaPlayer.MediaFailed += OnMediaPlayer_MediaFailed;
			_mediaPlayer.MediaOpened += OnMediaPlayer_MediaOpened;
			_isMediaHandlersActive = true;
		}

		private void RemoveMediaHandlers()
		{
			_mediaPlayer.MediaEnded -= OnMediaPlayer_MediaEnded;
			_mediaPlayer.MediaFailed -= OnMediaPlayer_MediaFailed;
			_mediaPlayer.MediaOpened -= OnMediaPlayer_MediaOpened;
			_isMediaHandlersActive = false;
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
		#endregion media event handlers

		#region IDisposable Support
		private bool isDisposed = false; // To detect redundant calls

		protected virtual void Dispose(bool isDisposing)
		{
			if (!isDisposed)
			{
				if (isDisposing)
				{
					// TODO: dispose managed state (managed objects).
					RemoveDataChangedHandlers();
					RemoveMediaHandlers();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				isDisposed = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~PlayerVM() {
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

		/// <summary>
		/// Finalizes an instance of the <see cref="PlayerVM"/> class. 
		/// Releases unmanaged resources and performs other cleanup operations before the <see cref="PlayerVM"/>
		/// is reclaimed by garbage collection. Will run only if the
		/// Dispose method does not get called.
		/// </summary>
		//~PlayerVM()
		//{
		//	this.Dispose(false);
		//}
		#endregion
	}

	public class FolderWithChildren : ObservableData
	{
		private string _folderPath = null;
		public string FolderPath { get { return _folderPath; } set { _folderPath = value; RaisePropertyChanged(); } }
		private readonly SwitchableObservableCollection<NameAndPath> _children = new SwitchableObservableCollection<NameAndPath>();
		public SwitchableObservableCollection<NameAndPath> Children { get { return _children; } }
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
