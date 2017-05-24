﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilz;
using Utilz.Data;

namespace MusicOnTheRoad.Data
{
    public class PersistentData : ObservableData
    {
        #region lifecycle
        private static PersistentData _instance;
        private static readonly object _instanceLock = new object();
        public static PersistentData GetInstance()
        {
            lock (_instanceLock)
            {
                return _instance ?? (_instance = new PersistentData());
            }
        }

        public static void SetInstanceProperties(PersistentData from)
        {
            if (from == null) return;
            try
            {
                var dataToBeChanged = GetInstance();
                //I must clone memberwise, otherwise the current event handlers get lost
                CloneProperties(from, ref dataToBeChanged);
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.PersistentDataLogFilename);
            }
        }
        private static void CloneProperties(PersistentData source, ref PersistentData target)
        {
            if (source == null || target == null) return;

            target.ExpandedPinnedFolderPath = source.ExpandedPinnedFolderPath;
            target.LastMessage = source.LastMessage;
            target.PinnedFolderPaths.Clear();
            target.PinnedFolderPaths.AddRange(source.PinnedFolderPaths);
        }
        #endregion lifecycle

        #region properties
        private readonly SwitchableObservableCollection<string> _pinnedFolderPaths = new SwitchableObservableCollection<string>();
        public SwitchableObservableCollection<string> PinnedFolderPaths { get { return _pinnedFolderPaths; } }

        public void AddPinnedFolderPath(string folderPath)
        {
            if (_pinnedFolderPaths.Any((record) => { return record == folderPath; })) return;
            _pinnedFolderPaths.Add(folderPath);
        }
        public void RemovePinnedFolderPath(string folderPath)
        {
            var existingRecord = _pinnedFolderPaths.FirstOrDefault((record) => { return record == folderPath; });
            if (existingRecord == null) return;
            _pinnedFolderPaths.Remove(existingRecord);
        }
        public void ClearFolders()
        {
            _pinnedFolderPaths.Clear();
        }

        private string _lastMessage = string.Empty;
        public string LastMessage { get { return _lastMessage; } set { _lastMessage = value; RaisePropertyChanged(); } }

        private string _expandedPinnedFolderPath = null;
        public string ExpandedPinnedFolderPath { get { return _expandedPinnedFolderPath; } set { _expandedPinnedFolderPath = value; RaisePropertyChanged(); } }
        #endregion properties
    }
}
