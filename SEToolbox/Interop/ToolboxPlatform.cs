using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ParallelTasks;
using ProtoBuf.Meta;
using SEToolbox.Support;
using VRage;
using VRage.Analytics;
using VRage.Audio;
using VRage.FileSystem;
using VRage.Http;
using VRage.Input;
using VRage.Library.Threading;
using VRage.Scripting;
using VRage.Serialization;
using VRage.Utils;
using VRageRender;

//implemented generic dummy  implementations to satisfy the interface requirements 
namespace SEToolbox.Interop
{
    public class ToolboxPlatform : IVRagePlatform
    {
        public bool SessionReady { get; set; }

        public IVRageWindows Windows => throw new NotImplementedException();
        //public IVRageWindows Windows => new VRageWindowsImpl();
        public IVRageHttp Http => throw new NotImplementedException();
        //public IVRageHttp Http => new VRageHttpImpl();

        public IVRageSystem System { get; } = new VRageSystemImpl();
        public IVRageRender Render { get; } = new VRageRenderImpl();

        public IAnsel Ansel => throw new NotImplementedException();
        public IAfterMath AfterMath => throw new NotImplementedException();
        public IVRageInput Input => throw new NotImplementedException();
        public IVRageInput2 Input2 => throw new NotImplementedException();
        public IMyAudio Audio => throw new NotImplementedException();
        public IMyImeProcessor ImeProcessor => throw new NotImplementedException();
        public IMyCrashReporting CrashReporting => throw new NotImplementedException();
        public IVRageScripting Scripting => throw new NotImplementedException();

        IProtoTypeModel typeModel;

        public void Init()
        {
            typeModel = new DynamicTypeModel();
        }

        public bool CreateInput2() => throw new NotImplementedException();
        public IVideoPlayer CreateVideoPlayer() => throw new NotImplementedException();
        public void Done() => throw new NotImplementedException();

        public IProtoTypeModel GetTypeModel()
        {
            return typeModel;
        }

        public IMyAnalytics InitAnalytics(string projectId, string version) => throw new NotImplementedException();
        public IMyAnalytics InitAnalytics(string projectId, string version, bool idInited) => throw new NotImplementedException();
        public void InitScripting(IVRageScripting scripting) => throw new NotImplementedException();
        public void Update() => throw new NotImplementedException();
    }

    class VRageSystemImpl : IVRageSystem
    {
        private static Action _onSuspending;
        private static Action<string> _onSystemProtocolActivated;
        private static float? _forcedUiRatio;
        public float CPUCounter => throw new NotImplementedException();
        public float RAMCounter => throw new NotImplementedException();
        public float GCMemory => throw new NotImplementedException();
        public long RemainingMemoryForGame => throw new NotImplementedException();
        public long ProcessPrivateMemory => throw new NotImplementedException();

        public bool IsUsingGeforceNow => false;

        public bool IsScriptCompilationSupported => throw new NotImplementedException();

        string IVRageSystem.Clipboard
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool IsAllocationProfilingReady => throw new NotImplementedException(); //false;
        public bool IsSingleInstance => throw new NotImplementedException(); // false;
        public bool IsRemoteDebuggingSupported => throw new NotImplementedException(); //false;
        public SimulationQuality SimulationQuality => throw new NotImplementedException(); //SimulationQuality.VeryLow | SimulationQuality.Low | SimulationQuality.Normal;

        public bool IsDeprecatedOS => throw new NotImplementedException(); // false;
        public bool IsMemoryLimited => throw new NotImplementedException(); // false;
        public bool HasSwappedMouseButtons => false;
        public string ThreeLetterISORegionName => RegionInfo.CurrentRegion.ThreeLetterISORegionName;
        public string TwoLetterISORegionName => RegionInfo.CurrentRegion.TwoLetterISORegionName;
        public string RegionLatitude => throw new NotImplementedException();
        public string RegionLongitude => throw new NotImplementedException();
        public string TempPath => throw new NotImplementedException();
        public int? OptimalHavokThreadCount => throw new NotImplementedException();

        public PrioritizedScheduler.ExplicitWorkerSetup? ExplicitWorkerSetup => null;

        public bool AreEnterBackButtonsSwapped => false;

        public float? ForcedUiRatio
        {
            get
            {
                if (_forcedUiRatio == null)
                {
                    var primaryMonitorSize = System.Windows.Forms.SystemInformation.PrimaryMonitorSize;
                    _forcedUiRatio = primaryMonitorSize.Width / (float)primaryMonitorSize.Height;
                }

                return _forcedUiRatio;
            }
        }

        public event Action<string> OnSystemProtocolActivated
        {
            add => _onSystemProtocolActivated += value;
            remove => _onSystemProtocolActivated -= value;
        }


        public event Action LeaveSession
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event Action OnResuming
        {
            add => OnResuming += value;
            remove => OnResuming -= value;
        }


        public event Action OnSuspending
        {
            add => _onSuspending += value;
            remove => _onSuspending -= value;
        }

        (string Name, uint MaxClock, uint Cores) m_cpuInfo;

        public string GetRootPath() => null;
        // {
        //     return AppDomain.CurrentDomain.BaseDirectory; // Returns the root path of the application
        // }

        public string GetAppDataPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // Returns the AppData path
        }

        public ulong GetGlobalAllocationsStamp() => throw new NotImplementedException();

        public string GetInfoCPU(out uint frequency, out uint physicalCores)
        {
            if (m_cpuInfo.Name == null)
            {
                try
                {
                    using var managementObjectSearcher = new ManagementObjectSearcher("select Name, MaxClockSpeed, NumberOfCores from Win32_Processor");
                    foreach (var item in managementObjectSearcher.Get().Cast<ManagementObject>())
                    {
                        m_cpuInfo.Name = $"{item["Name"]}";
                        m_cpuInfo.Cores = (uint)item["NumberOfCores"];
                        m_cpuInfo.MaxClock = (uint)item["MaxClockSpeed"];
                    }
                }
                catch (Exception)
                {
                    //m_log.WriteLine("Couldn't get cpu info: " + ex);
                    m_cpuInfo.Name = "UnknownCPU";
                    m_cpuInfo.Cores = 0u;
                    m_cpuInfo.MaxClock = 0u;
                }
            }

            frequency = m_cpuInfo.MaxClock;
            physicalCores = m_cpuInfo.Cores;

            return m_cpuInfo.Name;
        }

        public string GetOsName() => Environment.OSVersion.ToString();

        public List<string> GetProcessesLockingFile(string path)
        {
            var processes = new List<string>();
            var searchQuery = string.Format($"SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE Handle = '{path.Replace(@"\", @"\\")}'");
            var search = new ManagementObjectSearcher(searchQuery);
            var results = search.Get();
            foreach (ManagementObject result in results.Cast<ManagementObject>())
            {
                try
                {
                    var processId = result["ProcessId"];
                    var executablePath = result["ExecutablePath"];
                    processes.Add($"{processId} - {executablePath}");
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            return processes;
        }

        public ulong GetThreadAllocationStamp()
        {
            return (ulong)GC.CollectionCount(0);
        }

        public ulong GetTotalPhysicalMemory()
        {
            return (ulong)Process.GetCurrentProcess().WorkingSet64;
        }
        public void LogEnvironmentInformation()
        {
            //Log relevant environment information
            Console.WriteLine($"OS: {GetOsName()}, CPU: {GetInfoCPU(out uint freq, out uint cores)}");
            Debugger.Log(0, null, $"OS: {GetOsName()}, CPU: {GetInfoCPU(out freq, out cores)}"); // Log to external debugger.

        }
        public void LogToExternalDebugger(string message)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Log(0, null, message + Environment.NewLine);
            }
        }
        public bool OpenUrl(string url) => throw new NotImplementedException();
        public void ResetColdStartRegister() => throw new NotImplementedException();

        public void WriteLineToConsole(string msg)
        {
            Console.WriteLine(msg);
        }

        public void GetGCMemory(out float allocated, out float used)
        {
            allocated = GC.GetTotalMemory(false) / (1024f * 1024f);
            used = GC.GetTotalMemory(true) / (1024f * 1024f);
        }


        public void OnThreadpoolInitialized()
        {
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);


            minWorkerThreads = Environment.ProcessorCount * 2;
            maxWorkerThreads = Environment.ProcessorCount * 4;

            ThreadPool.SetMinThreads(minWorkerThreads, minCompletionPortThreads);
            ThreadPool.SetMaxThreads(maxWorkerThreads, maxCompletionPortThreads);

            SConsole.WriteLine($"ThreadPool Min/Max Worker Threads: {minWorkerThreads}/{maxWorkerThreads}");
            SConsole.WriteLine($"ThreadPool Min/Max Completion Port Threads: {minCompletionPortThreads}/{maxCompletionPortThreads}");
        }
        public void LogRuntimeInfo(Action<string> log)
        {
            var runtime = typeof(string).Assembly.GetName();
            log($"Runtime: {runtime.Name} {runtime.Version}");
        }

        public void OnSessionStarted(SessionType sessionType) => throw new NotImplementedException();
        public void OnSessionUnloaded() => throw new NotImplementedException();

        public int? GetExperimentalPCULimit(int safePCULimit) => throw new NotImplementedException();


        public void DebuggerBreak()
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
        public void CollectGC(int generation, GCCollectionMode mode)
        {
            generation = Math.Min(generation, GC.MaxGeneration);
            GC.Collect(generation, mode);
        }

        public void CollectGC(int generation, GCCollectionMode mode, bool blocking, bool compacting)
        {
            generation = Math.Min(generation, GC.MaxGeneration);
            GC.Collect(generation, mode, blocking, compacting);
        }

        public bool OpenUrl(string url, bool predetermined = true) => throw new NotImplementedException(); //potential use: opening a mods url? or not?

        public ISharedCriticalSection CreateSharedCriticalSection(bool spinLock)
            => spinLock ? new MyCriticalSection_SpinLock() : new MyCriticalSection_Mutex();


        public DateTime GetNetworkTimeUTC() => throw new NotImplementedException();

        public string GetPlatformSpecificCrashReport() => null;

        public string GetModsCachePath() => MyFileSystem.ModsCachePath ?? null;
    }

    class VRageRenderImpl : IVRageRender
    {
        public bool UseParallelRenderInit => throw new NotImplementedException(); // true;
        public bool IsRenderOutputDebugSupported => throw new NotImplementedException(); // false;
        public bool ForceClearGBuffer => throw new NotImplementedException(); // false;



        public event Action OnResuming
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event Action OnSuspending
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }


        

        public void ApplyRenderSettings(MyRenderDeviceSettings? settings) => throw new NotImplementedException();
        public object CreateRenderAnnotation(object deviceContext) => throw new NotImplementedException();
        public void CreateRenderDevice(ref MyRenderDeviceSettings? settings, out object deviceInstance, out object swapChain) => throw new NotImplementedException();
        public void DisposeRenderDevice() => throw new NotImplementedException();
        public void FlushIndirectArgsFromComputeShader(object deviceContext) => throw new NotImplementedException();

        public ulong GetMemoryBudgetForStreamedResources() => 0;
        public ulong GetMemoryBudgetForGeneratedTextures() => 0;
        public ulong GetMemoryBudgetForVoxelTextureArrays() => 0;

        public MyAdapterInfo[] GetRenderAdapterList() => throw new NotImplementedException();
        public MyRenderPresetEnum GetRenderQualityHint() => throw new NotImplementedException();
        public void ResumeRenderContext() => throw new NotImplementedException();
        public void SetMemoryUsedForImprovedGFX(long bytes) => throw new NotImplementedException();
        public void SuspendRenderContext() => throw new NotImplementedException();
        public void RequestSuspendWait() => throw new NotImplementedException();
        public void CustomUpdateForDeferredBuffer(object deviceContext, object buffer) => throw new NotImplementedException();
        public void SubmitEmptyCustomContext(object deviceContext) => throw new NotImplementedException();
        public void FastVSSetConstantBuffer(object deviceContext, int slot, object buffer) => throw new NotImplementedException();
        public void FastGSSetConstantBuffer(object deviceContext, int slot, object buffer) => throw new NotImplementedException();
        public void FastPSSetConstantBuffer(object deviceContext, int slot, object buffer) => throw new NotImplementedException();
        public void FastCSSetConstantBuffer(object deviceContext, int slot, object buffer) => throw new NotImplementedException();
        public void FastVSSetConstantBuffers1(object deviceContext, int slot, object buffer, int offset, int size, ref object constantBindingsCache) => throw new NotImplementedException();
        public void FastPSSetConstantBuffers1(object deviceContext, int slot, object buffer, int offset, int size, ref object constantBindingsCache) => throw new NotImplementedException();
        public void SetDepthTextureHint(VRageRender_DepthTextureHintType hint, object deviceContext = null, object texture = null) => throw new NotImplementedException();
        public bool IsExclusiveTextureLoadRequired() => throw new NotImplementedException();
    }
    // Internal class copied from VRage.Platform.Windows
    class DynamicTypeModel : IProtoTypeModel
    {
        public TypeModel Model => m_typeModel;

        private RuntimeTypeModel m_typeModel;

        public DynamicTypeModel()
        {
            CreateTypeModel();
        }

        private void CreateTypeModel()
        {
            m_typeModel = RuntimeTypeModel.Create(true);
            m_typeModel.AutoAddMissingTypes = true;
            m_typeModel.UseImplicitZeroDefaults = false;
        }
        private static ushort Get16BitHash(string s)
        {
            using MD5 mD = MD5.Create();
            return BitConverter.ToUInt16(mD.ComputeHash(Encoding.UTF8.GetBytes(s)), 0);
        }

        public void RegisterTypes(IEnumerable<Type> types)
        {
            var registered = new HashSet<Type>();

            foreach (Type type in types)
                RegisterType(type);

            void RegisterType(Type protoType)
            {
                if (protoType.IsGenericType)
                    return;

                if (protoType.BaseType == typeof(object) || protoType.IsValueType)
                {
                    m_typeModel.Add(protoType, true);
                }
                else
                {
                    RegisterType(protoType.BaseType);

                    if (registered.Add(protoType))
                    {
                        int fieldNumber = Get16BitHash(protoType.Name) + 65535;
                        m_typeModel.Add(protoType, true);
                        m_typeModel[protoType.BaseType].AddSubType(fieldNumber, protoType);
                    }
                }
            }
        }

        public void FlushCaches() => CreateTypeModel();

    }
}
