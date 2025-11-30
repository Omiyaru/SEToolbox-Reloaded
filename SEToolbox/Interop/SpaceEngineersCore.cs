namespace SEToolbox.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Sandbox;
    using Sandbox.Engine.Networking;
    using Sandbox.Engine.Utils;
    using Sandbox.Game;
    using Sandbox.Game.Entities.Planet;
    using Sandbox.Game.GameSystems;
    using SEToolbox.Models;
    using SEToolbox.Support;
    using SpaceEngineers.Game;
    using VRage;
    using VRage.Collections;
    using VRage.FileSystem;
    using VRage.Game;
    using VRage.GameServices;
    using VRage.Plugins;
    using VRage.Steam;
    using VRage.Utils;
    using VRageRender;

    /// <summary>
    /// Core interop for loading up Space Engineers content.
    /// </summary>
    public class SpaceEngineersCore
    {
        public static SEResources Resources
        {
            get => _singleton._worldResource?.Resources ?? _singleton._stockDefinitions;
        }

        public static Dictionary<string, byte> MaterialIndex
        {
            get => Resources.MaterialIndex;
        }

        public static WorldResource WorldResource
        {
            get => _singleton?._worldResource;
            set => _singleton?._worldResource = value;
        }

        public static List<string> ManageDeleteVoxelList
        {
            get => _singleton._manageDeleteVoxelList;
        }


        private static readonly Dictionary<string, Func<SpaceEngineersCore, string>> _propertyCache = new();

        public static string GetDataPathOrDefault(string key, string defaultValue)
        {
            if (_propertyCache.TryGetValue(key, out var propertyGetter))
            {
                return propertyGetter(_singleton) ?? defaultValue;
            }
            if (UserDataPath.PathMap.TryGetValue(key, out var propertyName))
            {
                var singletonType = typeof(SpaceEngineersCore);
                var propertyInfo = singletonType.GetProperty(propertyName);
                _propertyCache.Add(key, p => propertyInfo.GetValue(p) as string);
                return propertyInfo.GetValue(_singleton) as string ?? defaultValue;
            }
            return defaultValue;
        }

        //private static bool _isInitialized = false;

        public void SpaceEngineersCoreLoader()
        {
            if (_singleton != null)
            {
                SConsole.Init();

                InitializePaths();
                InitializeSteamService();
                FetchSteamMods();
                ShutDownSteamService();
                LoadSandboxGame();
            }
        }
        /// <summary>
        /// Forces static Ctor to load stock definitions.
        /// </summary>
        public static void LoadDefinitions()
        {
            SConsole.WriteLine("Init MyTexts.");

            typeof(MyTexts).TypeInitializer.Invoke(null, null); // For tests

            _singleton = new();

        }
        static SpaceEngineersCore _singleton;
        private WorldResource _worldResource;
        SEResources _stockDefinitions;
        List<string> _manageDeleteVoxelList;
        MyCommonProgramStartup _startup = new([]);
        private IMyGameService _steamService = MySteamGameService.Create(Sandbox.Engine.Platform.Game.IsDedicated, AppIdGame);
        const uint testAppId = 480; //steams spacewar test app id , Testing - im not sure if this is needed so setoolbox doesnt intefere with Space Engineers playtime
        const uint AppIdGame = 244850; // Game
        const uint AppIdDedicatedServer = 298740; // Dedicated Server

        public void InitializePaths()
        {
            if (_startup != null)
            {
                string contentPath = ToolboxUpdater.GetApplicationContentPath();
                string userDataPath = SpaceEngineersConsts.BaseLocalPath.DataPath;
                string userModsPath = SpaceEngineersConsts.BaseLocalPath.ModsPath;
                string shadersBasePath = SpaceEngineersConsts.BaseLocalPath.ShaderPath;
                string modsCachePath = SpaceEngineersConsts.BaseLocalPath.ModsCache;


                MFS.ExePath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(FastResourceLock)).Location);
                MyLog.Default = MySandboxGame.Log;
                SpaceEngineersGame.SetupBasicGameInfo();
                _startup = new MyCommonProgramStartup([]);
                _ = _startup.GetAppDataPath();
                //MyInitializer.InvokeBeforeRun(AppIdGame, MyPerGameSettings.BasicGameInfo.ApplicationName + "SEToolbox", userDataPath);
                //MyInitializer.InitContent(contentPath, userDataPath, userModsPath, shadersBasePath, modsCachePath); 
                //MyInitializer.InvokeBeforeRun(AppIdGame, MyPerGameSettings.BasicGameInfo.ApplicationName + "SEToolbox", userModsPath, userDataPath, true, -1, null, modsCachePath);
                //MyChecksumVerifier.Verify(MyChecksums.Items, )

                MFS.Reset();
                //MFS.Init(contentPath, userDataPath);
                MFS.Init(contentPath, userDataPath, userModsPath);
                //MFS.Init(contentPath, userDataPath,userModsPath, null, null);
                //MFS.Init(contentPath, userDataPath, userModsPath, shadersBasePath, modsCachePath);
            }


            // This will start the Steam Service, and Steam will think SE is running.
            // TODO: we don't want to be doing this all the while SEToolbox is running,
            // perhaps a once off during load to fetch of mods then disconnect/Dispose.
            _steamService = MySteamGameService.Create(Sandbox.Engine.Platform.Game.IsDedicated, AppId);// CreateSteamService();
            MyServiceManager.Instance.AddService(_steamService);

                SConsole.WriteLine("Initializing VRage platform.");
                MyVRage.Init(new ToolboxPlatform());
                MyVRage.Platform.Init();
            }
        

        private void FetchSteamMods()
        {
            if (_steamService == null)
            {
                throw new InvalidOperationException("_steamService is null");
            }

            try
            {
                IMyUGCService ugc = MySteamUgcService.Create(AppIdGame, _steamService) ?? throw new InvalidOperationException("UGC service is null");
                MyServiceManager.Instance.AddService(ugc);
                MyGameService.WorkshopService.AddAggregate(ugc);
                MyServiceManager.Instance.AddService(MyGameService.WorkshopService);

                SConsole.WriteLine("Fetching user specific data.");

                MFS.InitUserSpecific(_steamService.UserId.ToString(), SpaceEngineersConsts.Folders.SavesFolder);
                MyGameService.WorkshopService.Update();
            }
            catch (Exception ex)
            {
                SConsole.WriteLine($"Error fetching Steam mods: {ex.Message}");
            }
        }

        private void ShutDownSteamService()
        {
            if (_steamService != null)
            {
                IMyGameService steamGameService = MyServiceManager.Instance.GetService<IMyGameService>();
                steamGameService.ShutDown();
            }
        }

        //todo ??
        // public void VerifyChecksums() 
        // {    

        //     var verifier = new MyChecksumVerifier(new MyChecksums(),string);
        //     using (Stream stream = File.OpenRead(filename))
        //     {
        //         verifier.Verify(filename, stream);
        //     }
        // }


        private void LoadSandboxGame()
        {
           
            var myConfig = new MyConfig("SpaceEngineers.cfg");
            SConsole.WriteLine($"Loading config: {myConfig.Path}" );

            if (myConfig != null)
            {
                MySandboxGame.Config = myConfig;
                MySandboxGame.Config.Load();
            }

            SConsole.WriteLine("Setting Up MyPerGameSettings.");

            SpaceEngineersGame.SetupPerGameSettings();
            MyPerGameSettings.UpdateOrchestratorType = null;
            //MySandboxGame.InitMultithreading();
            SConsole.WriteLine("Initializing Multithreading.");
            InitMultithreading();
            SConsole.WriteLine("Initializing MyRenderProxy.");

            // Needed for MyRenderProxy.Log access in MyFont.LogWriteLine() and likely other things.
            //Todo Static patching

            MyRenderProxy.Initialize(new MyNullRender());
            InitSandboxGame();

            SConsole.WriteLine("loading localization.");

            // Reapply CurrentUICulture after MySandboxGame creation
            string languageCode = GlobalSettings.Default.LanguageCode;
            if (!string.IsNullOrWhiteSpace(languageCode))
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfoByIetfLanguageTag(languageCode);
            SpaceEngineersApi.LoadLocalization();


            SConsole.WriteLine("Initializing MySession.");
            // Create an empty instance of MySession for use by low level code.
            SGW_MySession session = (SGW_MySession)GetUninitializedObject(typeof(SGW_MySession));

            // Required as the above code doesn't populate it during Ctor of MySession.
            ReflUtil.ConstructField(session, "CreativeTools");
            ReflUtil.ConstructField(session, "m_sessionComponents");
            ReflUtil.ConstructField(session, "m_sessionComponentsForUpdate");

            session.Settings = new MyObjectBuilder_SessionSettings { EnableVoxelDestruction = true };
            // Register the required components


            // ??Change for the Clone() method to use XML cloning instead of Protobuf because of issues with MyObjectBuilder_CubeGrid.Clone()??
            // ENABLE_PROTOBUFFERS_CLONING is a static readonly field. 
            // Setting these via reflection is not guaranteed to work and is blocked in newer runtimes.
            EnableProtobufCloning();

            // Assign the instance back to the static.
            SGW_MySession.Static = session;
            MyHeightMapLoadingSystem heightmapSystem = new();
            MyPlanets planets = new();

            session.RegisterComponent(heightmapSystem, heightmapSystem.UpdateOrder, heightmapSystem.Priority);
            session.RegisterComponent(planets, planets.UpdateOrder, planets.Priority);

            heightmapSystem.LoadData();
            planets.LoadData();

            //session.RegisterComponent(voxelDestructionSystem, voxelDestructionSystem.UpdateOrder, voxelDestructionSystem.Priority);
            // Load the definitions 
            SConsole.WriteLine("Loading definitions.");
            var stockDefinitions = new SEResources();
            stockDefinitions.LoadDefinitions();


            // Store the variables for later use
            _stockDefinitions = stockDefinitions;
            _manageDeleteVoxelList = [];
            // EnableProtobufCloning();
        }

        static void ResetProtobufCloning()
        {
            EnableProtobufCloning(false);
        }

        static void EnableProtobufCloning(bool setValue = true)
        {
            ReflUtil.SetFieldValue<MOBSerializerKeen>("ENABLE_PROTOBUFFERS_CLONING", false);
            FieldInfo _protobufCloningField = ReflUtil.GetField<MOBSerializerKeen>( "ENABLE_PROTOBUFFERS_CLONING", BindingFlags.NonPublic | BindingFlags.Static, false);
            var _protobufCloningOriginalValue = (bool?)null;
            if (_protobufCloningField != null)
            {
                var originalBoolValue = (bool?)_protobufCloningField.GetValue(null);
                if (originalBoolValue.HasValue)
                {
                    if (setValue)
                    {
                        _protobufCloningField.SetValue(null, true);
                    }
                    else
                    {
                        _protobufCloningField.SetValue(null, originalBoolValue.Value);
                    }
                }
                else if (setValue)
                {

                    // Save the original value to reset it later
                    _protobufCloningField.SetValue(null, true);
                }
                else

                {
                    _protobufCloningField.SetValue(null, _protobufCloningOriginalValue.Value);
                }
            }
        }

        // Re-implement most of MySteamService constructor (with adjustments) to
        // bypass the call to Environment.Exit
        static IMyGameService CreateSteamService()
        {
            //return MySteamGameService.Create(Sandbox.Engine.Platform.Game.IsDedicated, AppId);

            var appIdStr = AppId.ToString();
            Environment.SetEnvironmentVariable("SteamAppId", appIdStr);
            Environment.SetEnvironmentVariable("SteamGameId", appIdStr);

            // isDedicated: true skips the initialization that is done here instead.
            var service = MySteamGameService.Create(isDedicated: true, AppId);
            var serviceType = service.GetType();

            var steamAppId = (AppId_t)AppId;

            //if (SteamAPI.RestartAppIfNecessary(steamAppId))
            //    Log.Error("SteamAPI.RestartAppIfNecessary returned true.");

            bool isActive = SteamAPI.Init();
            serviceType.GetProperty(nameof(MySteamGameService.IsActive)).SetValue(service, isActive);

            if (!isActive)
                Log.Error("Failed to initialize Steam service.");

            IMyInventoryService serviceInstance;

            const ulong OFFLINE_STEAM_ID = 1234567891011uL;

            if (isActive)
            {
                var steamUserId = SteamUser.GetSteamID();
                var userName = "\ue030" + SteamFriends.GetPersonaName();
                var hasLicenseResult = SteamUser.UserHasLicenseForApp(steamUserId, steamAppId);
                var ownsGame = hasLicenseResult == EUserHasLicenseForAppResult.k_EUserHasLicenseResultHasLicense;
                var userUniverse = (MyGameServiceUniverse)SteamUtils.GetConnectedUniverse();
                var branchName = SteamApps.GetCurrentBetaName(out var pchName, 512) ? pchName : "default";

                var propertyInfos = serviceType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);
                var fieldInfos = serviceType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

                foreach (var propertyInfo in propertyInfos)
                {
                    var value = propertyInfo.GetValue(service);
                    propertyInfo.SetValue(service, value);
                }

                foreach (var fieldInfo in fieldInfos)
                {
                    var value = fieldInfo.GetValue(service);
                    fieldInfo.SetValue(service, value);
                }

                SteamUserStats.RequestCurrentStats();
                serviceType.GetMethod(nameof(MySteamGameService.RegisterCallbacks), BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, []);

                var remoteStorage = Activator.CreateInstance(Type.GetType(nameof(MySteamRemoteStorage), VRage.Steam));

                serviceType.GetField(nameof(m_remoteStorage), BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service, remoteStorage);

                serviceInstance = (IMyInventoryService)Activator.CreateInstance(Type.GetType(nameof(MySteamInventoryService), VRage.Steam.MySteamInventory, VRage.Steam), [service]);
            }
            else
            {
                service.UserId = OFFLINE_STEAM_ID;
                serviceInstance = new MyNullInventoryService();
            }

            MyServiceManager.Instance.AddService(serviceInstance);

            if (!isActive)
            {
                var errMsg = """
                    Failed to initialize Steam API. Please ensure Steam is running and re-launch SEToolbox.
                    You can try to continue anyway but modded worlds may not work correctly.
                    """;
                MessageBox.Show(errMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return service;

        }
        static void InitMultithreading()
        {
            //MySandboxGame.InitMultithreading();
            ParallelTasks.Parallel.Scheduler = new ParallelTasks.PrioritizedScheduler(Math.Max(Environment.ProcessorCount / 2, 1), amd: true, setup: null);
        }

        static void InitSandboxGame()
        {
            SConsole.WriteLine("Initializing SandboxGame");

            // If this is causing an exception then there is a missing dependency.
            // gameTemp instance gets captured in MySandboxGame.Static
            //MySandboxGame gameTemp = new DerivedGame(["-skipintro"]);

            // Required for definitions to work properly
            MySandboxGame.Static = (MySandboxGame)GetUninitializedObject(typeof(MySandboxGame));

            MySandboxGame game = MySandboxGame.Static;

            var myInvokeDataType = ReflUtil.Ntypeof<MySandboxGame>("MyInvokeData", BindingFlags.NonPublic);
            var myConcurrentQueueType = typeof(MyConcurrentQueue<>).MakeGenericType(myInvokeDataType);
            var iq1 = Activator.CreateInstance(myConcurrentQueueType, 32);
            var iq2 = Activator.CreateInstance(myConcurrentQueueType, 32);

            ReflUtil.GetField<MySandboxGame>("m_invokeQueue", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(game, iq1);
            ReflUtil.GetField<MySandboxGame>("m_invokeQueueExecuting", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(game, iq2);

            RegisterAssemblies();

            SConsole.WriteLine("Initializing MyGlobalTypeMetadata.");

            VRage.Game.ObjectBuilder.MyGlobalTypeMetadata.Static.Init();

            SConsole.WriteLine("Preallocate");

            Preallocate();
        }

        static object GetUninitializedObject(Type type)
        {
#if NET
            return RuntimeHelpers.GetUninitializedObject(type);
#else
            return FormatterServices.GetUninitializedObject(type);
#endif
        }

        static void RegisterAssemblies()
        {
            SConsole.WriteLine("Registering Assemblies");
            MyPlugins.RegisterGameAssemblyFile(MyPerGameSettings.GameModAssembly);

            if (MyPerGameSettings.GameModBaseObjBuildersAssembly != null)
                MyPlugins.RegisterBaseGameObjectBuildersAssemblyFile(MyPerGameSettings.GameModBaseObjBuildersAssembly);
            MyPlugins.RegisterGameObjectBuildersAssemblyFile(MyPerGameSettings.GameModObjBuildersAssembly);
            MyPlugins.RegisterSandboxAssemblyFile(MyPerGameSettings.SandboxAssembly);
            MyPlugins.RegisterSandboxGameAssemblyFile(MyPerGameSettings.SandboxGameAssembly);
            MyPlugins.Load();
        }

        static void Preallocate()
        {
            Type[] types = [
                typeof(Sandbox.Game.Entities.MyEntities),
                typeof(VRage.ObjectBuilders.MyObjectBuilder_Base),
                typeof(MyTransparentGeometry),
                ReflUtil.Rtypeof<MyPlugins>("MyCubeGridDeformationTables"), // typeof(Sandbox.Game.Entities.MyCubeGridDeformationTables),
                typeof(VRageMath.MyMath),
                typeof(MySimpleObjectDraw)
            ];

            PreloadTypesFrom(MyPlugins.GameAssembly);
            PreloadTypesFrom(MyPlugins.SandboxAssembly);
            ForceStaticCtor(types);
            PreloadTypesFrom(typeof(MySandboxGame).Assembly);


            static void PreloadTypesFrom(Assembly assembly)
            {
                if (assembly == null)
                    return;

                IEnumerable<Type> types = from type in assembly.GetTypes()
                                          where Attribute.IsDefined(type, typeof(PreloadRequiredAttribute))
                                          select type;

                SConsole.WriteLine($"Preloading {types.Count().GetType()} types from {assembly.FullName}");
                ForceStaticCtor(types);
            }


            static void ForceStaticCtor(IEnumerable<Type> types)
            {
                if (types != null)
                {
                    foreach (Type type in types)
                    {
                        if (type != null)
                            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                    }
                }
            }
            //class DerivedGame(string[] commandlineArgs)
            //    : MySandboxGame(commandlineArgs, default)
            //{
            //    protected override void InitializeRender(IntPtr windowHandle) { }
            //}
        }
    }
}

