﻿using MusicOnTheRoad.Data;
using MusicOnTheRoad.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilz;
using Utilz.Data;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MusicOnTheRoad.ViewModels
{
    public sealed class PlayerVM : ObservableData, IDisposable
    {
        #region properties
        public const int MaxPlaylistItems = 200;

        private PersistentData _persistentData = null;
        public PersistentData PersistentData { get { return _persistentData; } }
        private readonly MediaPlayer _mediaPlayer = null;
        private volatile IMediaPlaybackSource _source = null;
        private readonly SemaphoreSlimSafeRelease _mediaSourceSemaphore = null;

        public IMediaPlaybackSource Source { get { return _source; } }
        private readonly SwitchableObservableCollection<FolderWithChildren> _pinnedFolders = new SwitchableObservableCollection<FolderWithChildren>();
        public SwitchableObservableCollection<FolderWithChildren> PinnedFolders { get { return _pinnedFolders; } }
        private string _lastMessage = null;
        public string LastMessage { get { return _lastMessage; } private set { _lastMessage = value; RaisePropertyChanged(); } }
        private string _albumTitle = null;
        public string AlbumTitle { get { return _albumTitle; } private set { _albumTitle = value; RaisePropertyChanged(); } }
        private string _songTitle = null;
        public string SongTitle { get { return _songTitle; } private set { _songTitle = value; RaisePropertyChanged(); } }
        private bool _isBusy = false;
        public bool IsBusy { get { return _isBusy; } private set { if (_isBusy != value) { _isBusy = value; RaisePropertyChanged_UI(); } } }
        # endregion properties

        public PlayerVM(MediaPlayer mediaPlayer)
        {
            _mediaSourceSemaphore = new SemaphoreSlimSafeRelease(1, 1);
            _mediaPlayer = mediaPlayer;
            AddMediaHandlers();

            SuspensionManager.Loaded += OnSuspensionManager_Loaded;
            UpdateFromPersistentData();
        }

        #region updaters
        private void UpdateFromPersistentData()
        {
            if (SuspensionManager.IsLoaded)
            {
                var pd = _persistentData = PersistentData.GetInstance();
                RaisePropertyChanged_UI(nameof(PersistentData));

                if (pd != null) pd.PropertyChanged += OnPersistentData_PropertyChanged;

                Task upd0 = UpdateLastMessageAsync();
                Task upd1 = UpdatePinnedFoldersAsync();
                Task upd2 = UpdateKeepAliveAsync();
            }
            else
            {
                var pd = _persistentData;
                if (pd != null) pd.PropertyChanged -= OnPersistentData_PropertyChanged;
            }
        }

        private async Task UpdatePinnedFoldersAsync()
        {
            var pd = PersistentData;
            if (pd == null) return;

            await RunInUiThreadAsyncT(async delegate
            {
                _pinnedFolders.Clear();

                foreach (var folderPath in pd.PinnedFolderPaths)
                {
                    var folder = await Pickers.GetPreviouslyPickedFolderAsync(folderPath, new System.Threading.CancellationToken(false));
                    if (folder == null) continue;
                    _pinnedFolders.Add(new FolderWithChildren(folder.Name, folder.Path));
                    //_pinnedFolders.Add(new FolderWithChildren(System.IO.Path.GetFileName(folderPath), folderPath));
                }
            }).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pd.ExpandedPinnedFolderPath)) return;
            var expandedPinnedFolder = _pinnedFolders.FirstOrDefault((fwc) => { return fwc.FolderPath == pd.ExpandedPinnedFolderPath; });
            if (expandedPinnedFolder == null) return;

            await ToggleExpandPinnedFolderAsync(expandedPinnedFolder).ConfigureAwait(false);
        }
        private Task UpdateLastMessageAsync()
        {
            var pd = PersistentData;
            if (pd == null) return Task.CompletedTask;

            return RunInUiThreadAsync(delegate
            {
                LastMessage = pd.LastMessage;
            });
        }
        private Task UpdateLastMessageAsync(string message)
        {
            return RunInUiThreadAsync(delegate
            {
                LastMessage = message;
            });
        }
        private Task UpdateKeepAliveAsync()
        {
            var pd = PersistentData;
            if (pd == null) return Task.CompletedTask;

            bool isKeepAlive = pd.IsKeepAlive;
            return RunInUiThreadAsync(delegate
            {
                KeepAlive.UpdateKeepAlive(isKeepAlive);
            });
        }
        private Task UpdateSongTitleAsync(MusicDisplayProperties displayProperties)
        {
            return RunInUiThreadAsync(delegate
            {
                if (displayProperties == null)
                {
                    AlbumTitle = "Error with the album title";
                    SongTitle = "Error with the song title";
                    return;
                }

                string artist = displayProperties.Artist;
                string songTitle = string.Empty;
                if (displayProperties.TrackNumber > 0 && displayProperties.AlbumTrackCount > 0)
                {
                    songTitle = $"´{artist}{displayProperties.Title} - {displayProperties.TrackNumber} of {displayProperties.AlbumTrackCount}";
                }
                else
                {
                    songTitle = $"{artist}{displayProperties.Title}";
                }

                if (!string.IsNullOrWhiteSpace(artist) && !string.IsNullOrWhiteSpace(songTitle))
                    SongTitle = $"{artist} - {songTitle}";
                else if (!string.IsNullOrWhiteSpace(artist))
                    SongTitle = artist;
                else if (!string.IsNullOrWhiteSpace(songTitle))
                    SongTitle = songTitle;
                else
                    SongTitle = "Unknown track";

                string albumTitle = string.Empty;
                if (!string.IsNullOrWhiteSpace(displayProperties.AlbumArtist))
                    albumTitle = displayProperties.AlbumArtist;
                //else if (string.IsNullOrWhiteSpace(displayProperties.Artist))
                //    albumTitle = displayProperties.Artist;

                if (!string.IsNullOrWhiteSpace(albumTitle) && !string.IsNullOrWhiteSpace(displayProperties.AlbumTitle))
                    albumTitle += " - ";

                albumTitle += displayProperties.AlbumTitle;

                AlbumTitle = albumTitle;
            });
        }
        private void UpdateAudioQuality(AudioTrack audioTrack)
        {
            if (audioTrack == null) return;
            try
            {
                var encodingProperties = audioTrack.GetEncodingProperties();
                var supportInfo = audioTrack.SupportInfo;
                if (supportInfo == null || encodingProperties == null) return;

                string audioQuality = $"{encodingProperties.ChannelCount} channels, {encodingProperties.BitsPerSample} bit, {encodingProperties.SampleRate} kHz, {encodingProperties.Subtype}, {encodingProperties.Bitrate} bits/sec, {supportInfo.DecoderStatus}";
                UpdateLastMessageAsync(audioQuality);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        #endregion updaters

        #region user actions
        public async Task PickSourceFileAsync()
        {
            var file = await Pickers.PickOpenFileAsync(ConstantData.Extensions, Pickers.PICKED_OPEN_FILE_TOKEN, PickerLocationId.MusicLibrary);
            if (file == null) return;

            List<string> songTitles = new List<string>();
            var mediaPlaybackList = new MediaPlaybackList() { AutoRepeatEnabled = false, MaxPlayedItemsToKeepOpen = 1 };
            var mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file)) { AutoLoadedDisplayProperties = AutoLoadedDisplayPropertyKind.Music, CanSkip = true };

            var displayProperties = mediaPlaybackItem.GetDisplayProperties();
            displayProperties.Type = MediaPlaybackType.Music;
            if (String.IsNullOrWhiteSpace(displayProperties.MusicProperties.Title)) displayProperties.MusicProperties.Title = file.Name;
            displayProperties.MusicProperties.TrackNumber = 1;
            displayProperties.MusicProperties.AlbumTrackCount = 1;
            mediaPlaybackItem.ApplyDisplayProperties(displayProperties);

            mediaPlaybackList.Items.Add(mediaPlaybackItem);

            try
            {
                await _mediaSourceSemaphore.WaitAsync().ConfigureAwait(false);

                RemoveMediaHandlers();
                _source = mediaPlaybackList;
                AddMediaHandlers();
                RaisePropertyChanged_UI(nameof(Source));
            }
            finally
            {
                SemaphoreSlimSafeRelease.TryRelease(_mediaSourceSemaphore);
            }
        }
        public async Task PickSourceFolderAsync()
        {
            var folder = await Pickers.PickFolderAsync(ConstantData.Extensions, Pickers.PICKED_FOLDER_TOKEN, PickerLocationId.MusicLibrary).ConfigureAwait(false);
            if (folder == null) return;

            await SetSourceFolderAsync(folder).ConfigureAwait(false);
        }
        public async Task SetSourceFolderAsync(NameAndPath nameAndPath)
        {
            if (nameAndPath == null || nameAndPath.Path == null) return;

            var folder = await StorageFolder.GetFolderFromPathAsync(nameAndPath.Path).AsTask().ConfigureAwait(false);
            if (folder == null) return;

            await SetSourceFolderAsync(folder).ConfigureAwait(false);
        }
        private async Task SetSourceFolderAsync(StorageFolder folder)
        {
            if (folder == null) return;

            try
            {
                IsBusy = true;
                var mediaPlaybackList = new MediaPlaybackList() { AutoRepeatEnabled = false, MaxPlayedItemsToKeepOpen = 1 };
                bool isFolderWithMusic = await IsFolderWithMusicAsync(folder.Path).ConfigureAwait(false);

                Debug.WriteLine("CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess = " + Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess);
                if (isFolderWithMusic)
                {
                    await SetSourceFolderShallowAsync(folder, mediaPlaybackList, true).ConfigureAwait(false);
                }
                else
                {
                    var childFolders = await folder.GetFoldersAsync().AsTask().ConfigureAwait(false);
                    foreach (var childFolder in childFolders)
                    {
                        if (mediaPlaybackList.Items.Count > MaxPlaylistItems) break;
                        await SetSourceFolderShallowAsync(childFolder, mediaPlaybackList, false).ConfigureAwait(false);
                    }
                    Debug.WriteLine("mediaPlaybackList has " + mediaPlaybackList.Items.Count + " items");
                }

                if (mediaPlaybackList.Items.Count < 1)
                {
                    Task upd = UpdateLastMessageAsync("No music found");
                    return;
                }
                else if (mediaPlaybackList.Items.Count > MaxPlaylistItems)
                {
                    Task upd = UpdateLastMessageAsync($"Only the first {mediaPlaybackList.Items.Count} songs will be played");
                }

                try
                {
                    await _mediaSourceSemaphore.WaitAsync().ConfigureAwait(false);

                    RemoveMediaHandlers();
                    _source = mediaPlaybackList;
                    AddMediaHandlers();
                    RaisePropertyChanged_UI(nameof(Source));
                }
                finally
                {
                    SemaphoreSlimSafeRelease.TryRelease(_mediaSourceSemaphore);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SetSourceFolderShallowAsync(StorageFolder folder, MediaPlaybackList mediaPlaybackList, bool isTrackCountEnabled)
        {
            if (folder == null || mediaPlaybackList == null) return;

            var files = await folder.GetFilesAsync();

            var musicFiles = files.Where((fi) => { return ConstantData.Extensions.Any((ext) => { return ext == fi.FileType; }); });
            uint trackCount = 0;
            uint albumTrackCount = musicFiles != null ? Convert.ToUInt32(musicFiles.Count()) : 0;
            foreach (var file in musicFiles)
            {
                //if (mediaPlaybackList.Items.Count == MaxPlaylistItems) break; // NO! too slow, it's really slow!
                var mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file)) { AutoLoadedDisplayProperties = AutoLoadedDisplayPropertyKind.Music, CanSkip = true };

                var displayProperties = mediaPlaybackItem.GetDisplayProperties();
                displayProperties.Type = MediaPlaybackType.Music;
                if (String.IsNullOrWhiteSpace(displayProperties.MusicProperties.AlbumTitle)) displayProperties.MusicProperties.AlbumTitle = folder.Name;
                if (String.IsNullOrWhiteSpace(displayProperties.MusicProperties.Title)) displayProperties.MusicProperties.Title = file.Name;
                trackCount++;
                if (isTrackCountEnabled)
                {
                    displayProperties.MusicProperties.TrackNumber = trackCount;
                    displayProperties.MusicProperties.AlbumTrackCount = albumTrackCount;
                }
                else
                {
                    displayProperties.MusicProperties.TrackNumber = 0;
                    displayProperties.MusicProperties.AlbumTrackCount = 0;
                }

                mediaPlaybackItem.ApplyDisplayProperties(displayProperties);

                mediaPlaybackList.Items.Add(mediaPlaybackItem);
            }
        }

        public async Task PinFolderAsync()
        {
            var folder = await Pickers.PickFolderAsync(ConstantData.Extensions, Pickers.PICKED_FOLDER_TOKEN, PickerLocationId.MusicLibrary);
            if (string.IsNullOrWhiteSpace(folder?.Path)) return;

            Pickers.SetPickedFolder(folder, folder.Path); // set the token equal to the path, for later retrieval
            await RunInUiThreadAsync(delegate
            {
                PersistentData.AddPinnedFolderPath(folder.Path);

                if (_pinnedFolders.Any((fwc) => fwc.FolderPath == folder.Path)) return;
                _pinnedFolders.Add(new FolderWithChildren(folder.Name, folder.Path));
            }).ConfigureAwait(false);
        }
        public Task UnpinFoldersAsync()
        {
            return RunInUiThreadAsync(delegate
            {
                PersistentData.ClearPinnedFolderPaths();
                _pinnedFolders.Clear();
            });
        }
        private void CollapsePinnedFolders_UI()
        {
            foreach (var item in _pinnedFolders)
            {
                item.Children.Clear();
                if (item.ExpandedMode == ExpandedModes.Expanded) item.ExpandedMode = ExpandedModes.NotExpanded;
            }
        }

        public async Task OpenOrToggleExpandPinnedFolderAsync(FolderWithChildren pinnedFolder)
        {
            if (await IsFolderWithMusicAsync(pinnedFolder.FolderPath))
            {
                pinnedFolder.ExpandedMode = ExpandedModes.NotExpandable;
                await SetSourceFolderAsync(new NameAndPath(pinnedFolder.FolderName, pinnedFolder.FolderPath)).ConfigureAwait(false);
                return;
            }
            await ToggleExpandPinnedFolderAsync(pinnedFolder).ConfigureAwait(false);
        }

        private async Task ToggleExpandPinnedFolderAsync(FolderWithChildren folderWithChildren)
        {
            try
            {
                FolderWithChildren toBeExpanded = null;
                bool isShrinking = false;
                await RunInUiThreadAsync(delegate
                {
                    IsBusy = true;
                    toBeExpanded = _pinnedFolders.FirstOrDefault((fwc) => { return fwc.FolderPath == folderWithChildren.FolderPath; });
                    bool isExpanded = toBeExpanded?.Children?.Count > 0;
                    CollapsePinnedFolders_UI();

                    if (toBeExpanded == null || isExpanded)
                    {
                        PersistentData.ExpandedPinnedFolderPath = null;
                        isShrinking = true;
                    }
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

                List<NameAndPath> children = new List<NameAndPath>();
                await Task.Run(delegate
                {
                    //Debug.WriteLine("CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess = " + Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess);
                    string[] paths = System.IO.Directory.GetDirectories(folderWithChildren.FolderPath);

                    //sw.Stop();
                    //Debug.WriteLine($"sw3 took {sw.ElapsedMilliseconds} msec");

                    foreach (var path in paths)
                    {
                        children.Add(new NameAndPath(System.IO.Path.GetFileName(path), path));
                    }
                }).ConfigureAwait(false);

                await RunInUiThreadAsync(delegate
                {
                    toBeExpanded.Children.AddRange(children);
                    toBeExpanded.ExpandedMode = ExpandedModes.Expanded;
                    PersistentData.ExpandedPinnedFolderPath = toBeExpanded.FolderPath;
                }).ConfigureAwait(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public Task RemovePinnedFolderAsync(string folderPath)
        {
            return RunInUiThreadAsync(delegate
            {
                PersistentData.RemovePinnedFolderPath(folderPath);

                var toBeRemoved = _pinnedFolders.FirstOrDefault((folderWithChildren) => { return folderWithChildren.FolderPath == folderPath; });
                if (toBeRemoved == null) return;
                _pinnedFolders.Remove(toBeRemoved);
            });
        }
        #endregion user actions

        #region services
        private Task<bool> IsFolderWithMusicAsync(string folderPath)
        {
            return Task.Run(delegate
            {
                if (string.IsNullOrWhiteSpace(folderPath)) return false;
                foreach (var ext in ConstantData.Extensions)
                {
                    if (System.IO.Directory.EnumerateFiles(folderPath, $"*{ext}").Any()) return true;
                }
                return false;
            });
        }
        #endregion services

        #region data event handlers
        private void OnSuspensionManager_Loaded(object sender, bool isLoaded)
        {
            Task upd = RunInUiThreadAsync(() =>
            {
                UpdateFromPersistentData();
            });
        }

        private async void OnPersistentData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PersistentData.LastMessage))
            {
                await UpdateLastMessageAsync().ConfigureAwait(false);
            }
            else if (e.PropertyName == nameof(PersistentData.IsKeepAlive))
            {
                await UpdateKeepAliveAsync().ConfigureAwait(false);
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
                mediaPlaybackList.CurrentItemChanged += OnMediaPlaybackList_CurrentItemChanged;
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
                mediaPlaybackList.CurrentItemChanged -= OnMediaPlaybackList_CurrentItemChanged;
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
            UpdateLastMessageAsync(message == null ? "Media error" : message);
        }

        private void OnMediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            var currentAudioTrack = args?.NewItem?.AudioTracks?[0];
            UpdateSongTitleAsync(currentAudioTrack?.PlaybackItem?.GetDisplayProperties()?.MusicProperties);
        }
        #endregion media event handlers

        #region IDisposable Support
        private bool isDisposed = false; // To detect redundant calls

        private /*virtual*/ void Dispose(bool isDisposing)
        {
            if (isDisposed) return;

            if (isDisposing)
            {
                // TODO: dispose managed state (managed objects).
                SuspensionManager.Loaded -= OnSuspensionManager_Loaded;
                var pd = _persistentData;
                if (pd != null) pd.PropertyChanged -= OnPersistentData_PropertyChanged;
                RemoveMediaHandlers();
                Task stopKeepAlive = RunInUiThreadAsync(() => KeepAlive.StopKeepAlive());
                SemaphoreSlimSafeRelease.TryDispose(_mediaSourceSemaphore);
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            isDisposed = true;
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

    public enum ExpandedModes { DontKnow, NotExpanded, Expanded, NotExpandable }

    public class FolderWithChildren : ObservableData
    {
        private string _folderName = null;
        public string FolderName { get { return _folderName; } private set { _folderName = value; RaisePropertyChanged(); } }
        private string _folderPath = null;
        public string FolderPath { get { return _folderPath; } private set { _folderPath = value; RaisePropertyChanged(); } }
        private readonly SwitchableObservableCollection<NameAndPath> _children = new SwitchableObservableCollection<NameAndPath>();
        public SwitchableObservableCollection<NameAndPath> Children { get { return _children; } }
        private ExpandedModes _isExpanded = ExpandedModes.DontKnow;
        public ExpandedModes ExpandedMode { get { return _isExpanded; } set { _isExpanded = value; RaisePropertyChanged(); } }

        public FolderWithChildren(string folderName, string folderPath)
        {
            FolderName = folderName;
            FolderPath = folderPath;
        }
    }

    public class NameAndPath : ObservableData
    {
        private string _name = null;
        public string Name { get { return _name; } private set { _name = value; RaisePropertyChanged(); } }
        private string _path = null;
        public string Path { get { return _path; } private set { _path = value; RaisePropertyChanged(); } }

        public NameAndPath(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }
}
