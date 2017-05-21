using MusicOnTheRoad.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utilz;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MusicOnTheRoad.Services
{
	public sealed class SuspensionManager
	{
		private const string SessionDataFilename = "LolloSessionData.xml";

		// LOLLO NOTE important! The Mutex can work across AppDomains (ie across main app and background task) but only if you give it a name!
		// Also, if you declare initially owned true, the second thread trying to cross it will stay locked forever. So, declare it false.
		// All this is not well documented.
		public static async Task LoadAsync()
		{
			string errorMessage = string.Empty;

			try
			{
				StorageFile file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(SessionDataFilename, CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);

				//string ssss = null; //this is useful when you debug and want to see the file as a string
				//using (IInputStream inStream = await file.OpenSequentialReadAsync())
				//{
				//    using (StreamReader streamReader = new StreamReader(inStream.AsStreamForRead()))
				//    {
				//      ssss = streamReader.ReadToEnd();
				//    }
				//}

				using (IInputStream inStream = await file.OpenSequentialReadAsync().AsTask().ConfigureAwait(false))
				{
					using (var iinStream = inStream.AsStreamForRead())
					{
						DataContractSerializer serializer = new DataContractSerializer(typeof(PersistentData));
						iinStream.Position = 0;
						PersistentData newPersistentData = (PersistentData)(serializer.ReadObject(iinStream));
						await iinStream.FlushAsync().ConfigureAwait(false);

						PersistentData.SetInstanceProperties(newPersistentData);
						//await PersistentData.SetInstanceProperties(newPersistentData).ConfigureAwait(false);
					}
				}
				Debug.WriteLine("ended reading settings");
			}
			catch (System.Xml.XmlException ex)
			{
				errorMessage = "could not restore the settings";
				await Logger.AddAsync(ex.ToString(), Logger.FileErrorLogFilename);
			}
			catch (Exception ex)
			{
				errorMessage = "could not restore the settings";
				await Logger.AddAsync(ex.ToString(), Logger.FileErrorLogFilename);
			}

			if (!string.IsNullOrWhiteSpace(errorMessage)) PersistentData.GetInstance().LastMessage = errorMessage;
		}

		public static async Task SaveAsync(PersistentData allDataOriginal)
		{
			try
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					DataContractSerializer sessionDataSerializer = new DataContractSerializer(typeof(PersistentData));
					// DataContractSerializer serializer = new DataContractSerializer(typeof(PersistentData), new DataContractSerializerSettings() { SerializeReadOnlyTypes = true });
					// DataContractSerializer sessionDataSerializer = new DataContractSerializer(typeof(PersistentData), _knownTypes);
					// DataContractSerializer sessionDataSerializer = new DataContractSerializer(typeof(PersistentData), new DataContractSerializerSettings() { KnownTypes = _knownTypes, SerializeReadOnlyTypes = true, PreserveObjectReferences = true });
					sessionDataSerializer.WriteObject(memoryStream, allDataOriginal);

					StorageFile sessionDataFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
						SessionDataFilename, CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);
					using (Stream fileStream = await sessionDataFile.OpenStreamForWriteAsync().ConfigureAwait(false))
					{
						memoryStream.Seek(0, SeekOrigin.Begin);
						await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
						await memoryStream.FlushAsync().ConfigureAwait(false);
						await fileStream.FlushAsync().ConfigureAwait(false);
					}
				}
				Debug.WriteLine("ended saving settings");
			}
			catch (Exception ex)
			{
				Logger.Add_TPL(ex.ToString(), Logger.FileErrorLogFilename);
			}
		}
	}
}
