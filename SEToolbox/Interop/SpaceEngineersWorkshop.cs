using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using SEToolbox.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text;
using System.Threading;
using System.Threading.Tasks;

using VRage.Game;
using VRage.GameServices;
using static VRage.Game.MyObjectBuilder_Checkpoint;
using SEConsts = SEToolbox.Interop.SpaceEngineersConsts;

namespace SEToolbox.Interop
{
    public class SpaceEngineersWorkshop
    {
        public static MyGuiScreenDownloadMods m_downloadScreen;
        public static Dictionary<WorkshopId, MyWorkshopItem> m_workshopItems = new();
        public static List<ModItem> m_modItems = new();
        public List<WorkshopId> m_workshopIds = new();
        public static IMyGameServer m_gameServer;
        //GetWorkshopIdFromLocalMod

        public static void GetLocalModsBlocking(string userModsPath, List<ModItem> mods)
        {
            var modDict = new Dictionary<WorkshopId, MyWorkshopItem>();
            foreach (var mod in mods)
            {
                modDict[new WorkshopId(mod.PublishedFileId, mod.PublishedServiceName)] = null;
            }

            GetLocalModsBlockingInternal(userModsPath, modDict);
        } //Result GetLocalModsBlocking(string userModsPath, List<ModItem> mods, MyWorkshop.CancelToken cancelToken)

        public static void GetLocalModsBlockingInternal(string userModsPath, Dictionary<WorkshopId, MyWorkshopItem> mods)
        {
            if (!Directory.Exists(userModsPath) || userModsPath != null || !string.IsNullOrEmpty(userModsPath))
            {
                MyWorkshop.GetItemsBlockingUGC([.. mods.Keys], [.. mods.Values]);
            }
        }

        public static MyWorkshop.ResultData DownloadWorldModsBlocking(List<ModItem> mods, MyWorkshop.CancelToken cancelToken)
        {
            if (!MyGameService.IsActive)
                return default;

            MyWorkshop.ResultData ret = default;

            Task task = Task.Factory.StartNew(() => 
                ret = DownloadWorldModsBlockingInternal(mods, cancelToken)
            );

            while (!task.IsCompleted)
            {
                MyGameService.Update();
                Thread.Sleep(10);
            }

            return ret;
        }
        //MyWorkshop.DownloadModsResult(List<MyObjectBuilder_Checkpoint.ModItem> mods, Action<MyGameServiceCallResult> onFinishedCallback, MyWorkshop.CancelToken cancelToken)

        public static MyWorkshop.ResultData DownloadWorldModsBlockingInternal(List<ModItem> mods, MyWorkshop.CancelToken cancelToken)
        {
            SConsole.WriteLine("Starting to download world mods:");
            MySandboxGame.Log.IncreaseIndent();

            MyWorkshop.ResultData resultData = default;
            resultData.Result = MyGameServiceCallResult.OK;

            if (mods == null || mods.Count == 0)
            {
                SConsole.WriteLine("No mods to download");
                resultData.Result = MyGameServiceCallResult.OK;

                return resultData;
            }

            FixModServiceName(mods);

            var availableServices = MyGameService.WorkshopService.GetAggregates().ToDictionary(x => x.ServiceName, x => x);
            var modDict = new Dictionary<ModItem, IMyGameService>();
            var modList = modDict.Select(modsList => modsList.Key).Select(mod => new WorkshopId(mod.PublishedFileId, mod.PublishedServiceName)).ToList();
            var failedDownloads = mods.Where(mod => !availableServices.TryGetValue(mod.PublishedServiceName, out var aggregate) || !aggregate.IsConsoleCompatible).Select(mod => new WorkshopId(mod.PublishedFileId, mod.PublishedServiceName)).ToList();

            foreach (var mod in mods)
            {
                if (availableServices.TryGetValue(mod.PublishedServiceName, out var aggregate))
                {
                    if (aggregate.IsConsoleCompatible)
                    {

                        modList.Add(new WorkshopId(mod.PublishedFileId, mod.PublishedServiceName));
                    }
                    else if (Sandbox.Engine.Platform.Game.IsDedicated && MySandboxGame.ConfigDedicated.AutodetectDependencies)
                    {
                        SConsole.WriteLine("Local mods are not allowed in multiplayer.");
                        failedDownloads.Add(new WorkshopId(mod.PublishedFileId, mod.PublishedServiceName));
                    }
                }
                else
                {
                    failedDownloads.Add(new WorkshopId(mod.PublishedFileId, mod.PublishedServiceName));
                }
            }
            if (failedDownloads.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Failed to download the following mods:");

                foreach (var failedDownload in failedDownloads)
                {
                    sb.AppendLine(failedDownload.ToString());
                    SConsole.WriteLine(sb.ToString());
                }
                if (availableServices.Count == 0 || availableServices.All(x => !x.Value.IsConsoleCompatible || modList.Count == 0))
                {
                    resultData.Result = MyGameServiceCallResult.Fail;
                    return resultData;
                }
            }

            modList.RemoveAll(item => !availableServices.TryGetValue(item.ServiceName, out var aggregate) || !aggregate.IsConsoleCompatible);

            if (modList.Count == 0)
            {
                resultData.Result = MyGameServiceCallResult.Fail;
                return resultData;
            }

            if (Sync.IsServer)
            {
                AddModDependencies(mods, modList);
            }

            resultData = DownloadModsBlocking(mods, modList, cancelToken);

            SConsole.WriteLine("Finished downloading world mods");

            if (cancelToken != null)
                resultData.Cancel |= cancelToken.Cancel;

            return resultData;
        }


        static void AddModDependencies(List<ModItem> mods, List<WorkshopId> workshopIds)
        {
           

            var modsToProcess = mods.Where(x => !x.IsDependency && x.PublishedFileId != 0L)
                                    .Select(x => new WorkshopId(x.PublishedFileId, x.PublishedServiceName))
                                    .ToHashSet();
            var modsToAdd = MyWorkshop.GetModsDependencyHiearchy(modsToProcess, out _)
                                      .Where(x => !mods.Any(y => y.PublishedFileId == x.Id && y.PublishedServiceName == x.ServiceName))
                                      .Select(x => new ModItem(x.Id, x.ServiceName, isDependency: true)
                                      { FriendlyName = x.Title });
            foreach (var mod in modsToAdd)
            {
                mods.Add(mod);
                if (!workshopIds.Contains(new WorkshopId(mod.PublishedFileId, mod.PublishedServiceName)))
                    workshopIds.Add(new WorkshopId(mod.PublishedFileId, mod.PublishedServiceName));
            }
        }

        static MyWorkshop.ResultData DownloadModsBlocking(List<ModItem> mods, List<WorkshopId> workshopIds, MyWorkshop.CancelToken cancelToken)
        {
            if (mods == null)
                throw new ArgumentNullException(nameof(mods));
            if (workshopIds == null)
                throw new ArgumentNullException(nameof(workshopIds));


            var modsToDownload = mods.Where(x => workshopIds.ToDictionary(x => x.Id)
                                     .TryGetValue(x.PublishedFileId, out _))
                                     .ToList();

            if (modsToDownload.Count == 0)
            {
                SConsole.WriteLine("No mods to download");
                return new(MyGameServiceCallResult.OK, false);
            }

            var items = new List<MyWorkshopItem>(modsToDownload.Count);
            if (!MyWorkshop.GetItemsBlockingUGC(workshopIds, items) || items.Count != modsToDownload.Count)
            {
                SConsole.WriteLine("Failed to obtain workshop item details");
                return new(MyGameServiceCallResult.Fail, false);
            }

            m_downloadScreen.MessageText = new StringBuilder(VRage.MyTexts.GetString(MyCommonTexts.ProgressTextDownloadingMods))
                                                                          .Append($"Downloading mods: {0} of {items.Count}");

            var result = MyWorkshop.DownloadModsBlockingUGC(items, cancelToken);

            if (result.Result != MyGameServiceCallResult.OK)
            {
                SConsole.WriteLine($"Downloading mods failed, Result: {result.Result}");
            }
            else
            {
                for (int i = 0; i < modsToDownload.Count; i++)
                {
                    var mod = modsToDownload[i];
                    var item = items[i];
                    mod.FriendlyName = item.Title;
                    mod.SetModData(item);
                }
            }
            return result;
        }


        static void FixModServiceName(List<ModItem> mods)
        {
            if (mods == null) throw new ArgumentNullException(nameof(mods));

            string serviceName = MyGameService.GetDefaultUGC().ServiceName;
            for (int i = 0; i < mods.Count; i++)
            {
                var value = mods[i];

                if (string.IsNullOrEmpty(value.PublishedServiceName))
                {
                    value.PublishedServiceName = serviceName;
                    mods[i] = value;
                }
            }
        }

        // Todo getsubscribedworkshopitems
        //GetSubscribedWorkshopItems

        //     private static async Task<MyGameServiceCallResult> GetSubscribedItemsBlockingUGCInternalAsync(string serviceName, List<MyWorkshopItem> results, IEnumerable<string> tags, CancellationToken cancellationToken)
        //     {
        //         if (!MyGameService.IsActive || !MyGameService.IsOnline)
        //         {
        //             return MyGameServiceCallResult.NoUser;
        //         }

        //         MyWorkshopQuery myWorkshopQuery = MyGameService.CreateWorkshopQuery(serviceName);
        //         myWorkshopQuery.UserId = Sync.MyId;
        //         myWorkshopQuery.RequiredTags = tags == null ? null : tags.ToList();

        //         try
        //         {
        //             myWorkshopQuery.Run();
        //             myWorkshopQuery.Stop();
        //             var pMyWorkshopQuery = typeof(MyWorkshopQuery).GetMethod("MyWorkshopQuery", BindingFlags.NonPublic);
        //             var myWorkshopQueryType = typeof(MyWorkshopQuery).GetMethod("OnQueryCompleted");
        //             if (myWorkshopQueryType != null && pMyWorkshopQuery != null)
        //             {
        //                 results.AddRange(myWorkshopQuery.Items);
        //             }
        //         }
        //         catch (OperationCanceledException)
        //         {
        //             return MyGameServiceCallResult.AccessDenied;
        //         }
        //         catch (Exception ex)
        //         {
        //             MySandboxGame.Log.WriteLine($"Error while querying workshop items: {ex.Message}");
        //             return MyGameServiceCallResult.Fail;
        //         }

        //         return GetSubscribedWorkshopItemsAsync(results, tags, cancellationToken).Result.Item1;
        //     }


        //     public static async Task<(MyGameServiceCallResult, List<MyWorkshopItem>)> GetSubscribedWorkshopItemsAsync(List<MyWorkshopItem> results, IEnumerable<string> subscribedItems, CancellationToken cancellationToken)
        //     {
        //         subscribedItems = ["world", "mod", "scenario", "blueprint", "ingameScript"];

        //         var tasks = subscribedItems.Select(tag => GetSubscribedItemsBlockingUGCInternalAsync(tag, results, null, cancellationToken)).ToList();
        //         await Task.WhenAll(tasks);

        //         var success = tasks.All(t => t.Result == MyGameServiceCallResult.OK);
        //         return success ? (MyGameServiceCallResult.OK, results) : (MyGameServiceCallResult.Fail, results);
        //     }



    }
}
