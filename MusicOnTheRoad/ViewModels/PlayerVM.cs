using MusicOnTheRoad.Data;
using MusicOnTheRoad.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	public sealed class PlayerVM : ObservableData, IDisposable
	{
		private readonly PersistentData _persistentData = null;
		public PersistentData PersistentData { get { return _persistentData; } }
		private readonly MediaPlayer _mediaPlayer = null;
		private volatile IMediaPlaybackSource _source = null;
		private readonly SemaphoreSlimSafeRelease _mediaSourceSemaphore = null;

		public IMediaPlaybackSource Source { get { return _source; } }
		private readonly SwitchableObservableCollection<FolderWithChildren> _foldersWithChildren = new SwitchableObservableCollection<FolderWithChildren>();
		public SwitchableObservableCollection<FolderWithChildren> FoldersWithChildren { get { return _foldersWithChildren; } }
		private string _lastMessage = null;
		public string LastMessage { get { return _lastMessage; } private set { _lastMessage = value; RaisePropertyChanged_UI(); } }
		private string _songTitle = null;
		public string SongTitle { get { return _songTitle; } private set { _songTitle = value; RaisePropertyChanged_UI(); } }
		private bool _isLoadingChildren = false;
		public bool IsLoadingChildren { get { return _isLoadingChildren; } private set { _isLoadingChildren = value; RaisePropertyChanged_UI(); } }

		public PlayerVM(MediaPlayer mediaPlayer)
		{
			_mediaSourceSemaphore = new SemaphoreSlimSafeRelease(1, 1);
			_mediaPlayer = mediaPlayer;
			AddMediaHandlers();

			_persistentData = PersistentData.GetInstance();
			RaisePropertyChanged(nameof(PersistentData));

			AddDataChangedHandlers();
			UpdateLastMessage();
			Task upd1 = UpdateFoldersWithChildrenAsync();
		}

		#region updaters
		private async Task UpdateFoldersWithChildrenAsync()
		{
			FolderWithChildren expandedRootFolder = null;
			await RunInUiThreadAsync(delegate
			{
				FoldersWithChildren.Clear();
				foreach (var folderPath in _persistentData.RootFolderPaths)
				{
					FoldersWithChildren.Add(new FolderWithChildren(folderPath));
				}
				if (string.IsNullOrWhiteSpace(_persistentData.ExpandedRootFolderPath)) return;
				expandedRootFolder = FoldersWithChildren.FirstOrDefault((fwc) => { return fwc.FolderPath == _persistentData.ExpandedRootFolderPath; });
			}).ConfigureAwait(false);
			if (expandedRootFolder == null) return;
			await ToggleExpandRootFolderAsync(expandedRootFolder).ConfigureAwait(false);
		}
		private void UpdateLastMessage()
		{
			LastMessage = _persistentData.LastMessage;
		}
		private void UpdateLastMessage(string message)
		{
			LastMessage = message;
		}

		private void UpdateAudioQuality(AudioTrack audioTrack)
		{
			if (audioTrack == null) return;
			try
			{
				var encodingProperties = audioTrack?.GetEncodingProperties();
				var supportInfo = audioTrack?.SupportInfo;
				if (supportInfo == null || encodingProperties == null) return;

				string audioQuality = $"{encodingProperties.ChannelCount} channels, {encodingProperties.BitsPerSample} bit, {encodingProperties.SampleRate} kHz, {encodingProperties.Subtype}, {encodingProperties.Bitrate} bits/sec, {supportInfo.DecoderStatus}";
				UpdateLastMessage(audioQuality);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
		}
		#endregion updaters

		#region user actions
		public async Task<bool> SetSourceFileAsync()
		{
			var file = await Utilz.Pickers.PickOpenFileAsync(ConstantData.Extensions, PickerLocationId.MusicLibrary);
			if (file == null) return false;

			try
			{
				var mediaPlaybackList = new MediaPlaybackList() { AutoRepeatEnabled = false, MaxPlayedItemsToKeepOpen = 1 };
				var mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file)) { AutoLoadedDisplayProperties = AutoLoadedDisplayPropertyKind.Music, CanSkip = true };
				mediaPlaybackList.Items.Add(mediaPlaybackItem);

				await _mediaSourceSemaphore.WaitAsync().ConfigureAwait(false);
				RemoveMediaHandlers();
				_source = mediaPlaybackList;
				AddMediaHandlers();
			}
			finally
			{
				SemaphoreSlimSafeRelease.TryRelease(_mediaSourceSemaphore);
			}

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

			try
			{
				await _mediaSourceSemaphore.WaitAsync().ConfigureAwait(false);
				RemoveMediaHandlers();
				_source = mediaPlaybackList;
				AddMediaHandlers();
			}
			finally
			{
				SemaphoreSlimSafeRelease.TryRelease(_mediaSourceSemaphore);
			}

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

		public async Task ToggleExpandRootFolderAsync(FolderWithChildren folderWithChildren)
		{
			FolderWithChildren toBeExpanded = null;
			bool isShrinking = false;
			await RunInUiThreadAsync(delegate
			{
				toBeExpanded = _foldersWithChildren.FirstOrDefault((fwc) => { return fwc.FolderPath == folderWithChildren.FolderPath; });
				bool isExpanded = toBeExpanded?.Children?.Count > 0;
				CollapseRootFolders();

				if (toBeExpanded == null || isExpanded)
				{
					_persistentData.ExpandedRootFolderPath = null;
					isShrinking = true;
					return;
				}

				IsLoadingChildren = true;
			}).ConfigureAwait(false);

			if (isShrinking) return;

			//Stopwatch sw = new Stopwatch();
			//sw.Start();
			//var sss = await StorageFolder.GetFolderFromPathAsync(folderWithChildren.FolderPath).AsTask().ConfigureAwait(false);
			//var childrenn = await sss.GetFoldersAsync().AsTask().ConfigureAwait(false);
			//sw.Stop();
			//Debug.WriteLine($"sw1 took {sw.ElapsedMilliseconds} msec");

			//sw.Restart();
			//sss = await StorageFolder.GetFolderFromPathAsync(folderWithChildren.FolderPath).AsTask().ConfigureAwait(false);
			//var query = sss.CreateFolderQuery();
			//var childrennn = await query.GetFoldersAsync().AsTask().ConfigureAwait(false);

			//sw.Stop();
			//Debug.WriteLine($"sw2 took {sw.ElapsedMilliseconds} msec");

			//sw.Restart();
			// LOLLO NOTE the StorageFolder methods are not faster
			string[] paths = System.IO.Directory.GetDirectories(folderWithChildren.FolderPath);
			List<NameAndPath> children = new List<NameAndPath>();
			//sw.Stop();
			//Debug.WriteLine($"sw3 took {sw.ElapsedMilliseconds} msec");

			await RunInUiThreadAsync(delegate
			{
				foreach (var path in paths)
				{
					children.Add(new NameAndPath() { Name = System.IO.Path.GetFileName(path), Path = path });
				}
				toBeExpanded.Children.AddRange(children);
				toBeExpanded.IsExpanded = true;
				_persistentData.ExpandedRootFolderPath = toBeExpanded.FolderPath;

				IsLoadingChildren = false;
			}).ConfigureAwait(false);
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
			var persistentData = _persistentData;
			if (persistentData != null)
			{
				persistentData.PropertyChanged += OnPersistentData_PropertyChanged;
			}
			_isDataChangedHandlersActive = true;
		}
		private void RemoveDataChangedHandlers()
		{
			SuspensionManager.Loaded -= OnSuspensionManager_Loaded;
			var persistentData = _persistentData;
			if (persistentData != null)
			{
				persistentData.PropertyChanged -= OnPersistentData_PropertyChanged;
			}
			_isDataChangedHandlersActive = false;
		}

		private void OnSuspensionManager_Loaded(object sender, bool e)
		{
			UpdateLastMessage();
			Task upd1 = UpdateFoldersWithChildrenAsync();
		}

		private void OnPersistentData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PersistentData.LastMessage))
			{
				UpdateLastMessage();
			}
		}
		#endregion data event handlers

		#region media event handlers
		private bool _isMediaHandlersActive = false;

		private void AddMediaHandlers()
		{
			if (_isMediaHandlersActive) return;

			var mediaPlaybackList = _source as MediaPlaybackList;
			if (mediaPlaybackList != null)
			{
				//_mediaPlaybackList.CurrentItemChanged += OnMediaPlaybackList_CurrentItemChanged;
				mediaPlaybackList.ItemFailed += OnMediaPlaybackList_ItemFailed;
				mediaPlaybackList.ItemOpened += OnMediaPlaybackList_ItemOpened;
			}

			_isMediaHandlersActive = true;
		}

		private void RemoveMediaHandlers()
		{
			var mediaPlaybackList = _source as MediaPlaybackList;
			if (mediaPlaybackList != null)
			{
				//_mediaPlaybackList.CurrentItemChanged -= OnMediaPlaybackList_CurrentItemChanged;
				mediaPlaybackList.ItemFailed -= OnMediaPlaybackList_ItemFailed;
				mediaPlaybackList.ItemOpened -= OnMediaPlaybackList_ItemOpened;
			}

			_isMediaHandlersActive = false;
		}

		private void OnMediaPlaybackList_ItemOpened(MediaPlaybackList sender, MediaPlaybackItemOpenedEventArgs args)
		{
			var currentAudioTrack = args?.Item?.AudioTracks?[0];
			UpdateAudioQuality(currentAudioTrack);
		}

		private void OnMediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
		{
			var message = args?.Error?.ExtendedError?.Message;
			UpdateLastMessage(message == null ? "media error" : message);
		}

		private void OnMediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
		{
			var currentAudioTrack = args?.NewItem?.AudioTracks?[0];
			UpdateAudioQuality(currentAudioTrack);
		}
		#endregion media event handlers

		#region IDisposable Support
		private bool isDisposed = false; // To detect redundant calls

		private /*virtual*/ void Dispose(bool isDisposing)
		{
			if (!isDisposed)
			{
				if (isDisposing)
				{
					// TODO: dispose managed state (managed objects).
					RemoveDataChangedHandlers();
					RemoveMediaHandlers();
					SemaphoreSlimSafeRelease.TryDispose(_mediaSourceSemaphore);
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
