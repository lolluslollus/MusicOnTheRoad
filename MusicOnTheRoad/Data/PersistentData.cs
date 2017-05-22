using System;
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
			if (from == null) return; // Task.CompletedTask;
			try
			{
				//return from.RunInUiThreadAsync(delegate
				//{
				var dataToBeChanged = GetInstance();
				//I must clone memberwise, otherwise the current event handlers get lost
				CloneProperties(from, ref dataToBeChanged);
				//});
			}
			catch (Exception ex)
			{
				//return 
				Logger.Add_TPL(ex.ToString(), Logger.PersistentDataLogFilename);
			}
		}
		private static void CloneProperties(PersistentData source, ref PersistentData target)
		{
			if (source == null || target == null) return;

			target.LastMessage = source.LastMessage;
			target.RootFolderPaths.Clear();
			target.RootFolderPaths.AddRange(source.RootFolderPaths);
		}
		#endregion lifecycle

		private readonly SwitchableObservableCollection<string> _rootFolderPaths = new SwitchableObservableCollection<string>();
		public SwitchableObservableCollection<string> RootFolderPaths { get { return _rootFolderPaths; } }

		public void AddRootFolderPath(string folderPath)
		{
			if (_rootFolderPaths.Any((record) => { return record == folderPath; })) return;
			_rootFolderPaths.Add(folderPath);
		}
		public void RemoveRootFolderPath(string folderPath)
		{
			var existingRecord = _rootFolderPaths.FirstOrDefault((record) => { return record == folderPath; });
			if (existingRecord == null) return;
			_rootFolderPaths.Remove(existingRecord);
		}
		public void ClearRootFolders()
		{
			_rootFolderPaths.Clear();
		}
		//private string _rootFolderPath = null;
		//public string RootFolderPath { get { return _rootFolderPath; } set { if (_rootFolderPath!= value) { _rootFolderPath = value; RaisePropertyChanged(); } } }

		private string _lastMessage = string.Empty;
		public string LastMessage { get { return _lastMessage; } set { _lastMessage = value; RaisePropertyChanged_UI(); } }

	}
}
