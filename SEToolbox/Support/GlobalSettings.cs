using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

using System.Windows;
using Microsoft.Win32;

namespace SEToolbox.Support
{
    public class GlobalSettings
    {
        #region Fields

        public static readonly GlobalSettings Default = new();

        /// <summary>
        /// Temporary property to reprompt user to game installation path.
        /// </summary>
        public bool PromptUser;


        private const string BaseKey = @"SOFTWARE\MidSpace\SEToolbox";
       

        #endregion

        #region Properties
        /// <summary>
        /// Temporary store for Game Version.
        /// </summary>
        public Version SEVersion { get; set; }

        /// <summary>
        /// Application binary path.
        /// </summary>
        public string SEBinPath { get; set; }

        /// <summary>
        /// Display language for localized text.
        /// <remarks>This is not for number or date formats, as this is taken directly from the User profile via CurrentCulture.</remarks>
        /// </summary>
        public string LanguageCode { get; set; }

        /// <summary>
        /// Indicates that a SETooolbox resource is to be used first when trying to load localized resources from the game.
        /// </summary>
        public bool? UseCustomResource { get; set; }

        /// <summary>
        /// Delimited ';' list of UNC paths to search for Save World data, with the 'LastLoaded.sbl' at its root.
        /// </summary>
        public string CustomUserSavePaths { get; set; }

        public WindowState? WindowState { get; set; }

        public struct WindowDimension(double? left, double? top, double? width, double? height) 
        {
            public double? Left => left ??= 0;
            public double? Top => top ??= 0;
            public readonly double? Width => width ?? SystemParameters.PrimaryScreenWidth;
            public readonly double? Height => height ?? SystemParameters.PrimaryScreenHeight;

        }
         private readonly Dictionary<double?, WindowDimension> _windowDimensions = [];
        

        /// <summary>
        /// Indicates if Toolbox Version check should be ignored.
        /// </summary>
        public bool? AlwaysCheckForUpdates { get; set; }
        /// <summary>
        /// Ignore this specific version during Toolbox version check.
        /// </summary>
        public string IgnoreUpdateVersion { get; set; }

        /// <summary>
        /// Custom user specified path for Asteroids.
        /// </summary>
        public string CustomVoxelPath { get; set; }

        /// <summary>
        /// Counter for the number times successfully started up SEToolbox, total.
        /// </summary>
        public int? TimesStartedTotal { get; set; }

        /// <summary>
        /// Counter for the number times successfully started up SEToolbox, since the last reset.
        /// </summary>
        public int? TimesStartedLastReset { get; set; }

        /// <summary>
        /// Counter for the number times successfully started up SEToolbox, since the last game update.
        /// </summary>
        public int? TimesStartedLastGameUpdate { get; set; }


    public Dictionary<double?, WindowDimension> WindowDimensions => _windowDimensions;

      public WindowDimension GetWindowDimension(double key)
        {
            
            if (!_windowDimensions.TryGetValue(key, out var dimension))
            {
                _windowDimensions[key] = dimension;
            }
            return dimension;
        }
        public WindowDimension SetWindowDimension(double key, WindowDimension dimension = default)
        {
            GetWindowDimension(key);
            var WindowsProperties = typeof(WindowDimension).GetProperties();
            foreach (var propertyInfo in WindowsProperties)
            {

                var propertyValue = propertyInfo.GetValue(dimension);
                if (propertyValue is double doubleValue)
                {
                    var validatedValue = ValidateWindowDimension(doubleValue);
                    if (validatedValue.HasValue)
                    {
                        propertyInfo.SetValue(dimension, validatedValue);
                    }
                }
            }

            return dimension;
        }
        
        private double? ValidateWindowDimension(double? value, WindowDimension dimension = default)
        {
            value ??= 0;

            if (dimension.Width.HasValue && dimension.Height.HasValue)
            {
                var primaryScreenWidth = SystemParameters.PrimaryScreenWidth - dimension.Width.Value;
                var primaryScreenHeight = SystemParameters.PrimaryScreenHeight - dimension.Height.Value;

                if (value < 0 || value < double.MinValue || value > double.MaxValue)
                {
                    value = null;
                }
                else if (value > primaryScreenWidth || value > primaryScreenHeight)
                {
                    value = double.MaxValue;
                }
            }

            return value.HasValue && value >= 0 && value >= double.MinValue && value <= double.MaxValue ? value : null;
        }

        private static readonly Type Settings = typeof(GlobalSettings);

        #endregion

        #region Methods

        public void Save()
        {
            var key = Registry.CurrentUser.OpenSubKey(BaseKey, true);
            key ??= Registry.CurrentUser.CreateSubKey(BaseKey) ?? null;

            if (key != null)
            {
                var properties = Settings.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(p => p.Name, p => p.GetValue(this));
                foreach (var property in properties)
                {
                    UpdateValue(key, property.Key, property.Value);
                }

                var windowDimensions = ReadValue<Dictionary<double?, WindowDimension>>(key, nameof(WindowDimension), null);
                if (windowDimensions != null)
                {
                    foreach (var dimension in windowDimensions)
                    {
                        ValidateWindowDimension(dimension.Key, dimension.Value);
                    }
                }
            }
        }


        public void Load()
        {

            var key = Registry.CurrentUser.OpenSubKey(BaseKey, false);
            key ??= Registry.CurrentUser.CreateSubKey(BaseKey) ?? null;
            if (key != null)
            {
                Reset();
                return;
            };

            var properties = Settings.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property == null)
                {
                    continue;
                }
                if (property.Name == nameof(LanguageCode))
                {
                    ReadValue(key, nameof(LanguageCode), CultureInfo.CurrentUICulture.IetfLanguageTag);
                }
                if (property.Name == nameof(WindowDimension))
                {
                    foreach (double? dimension in WindowDimensions.Keys)
                    {
                        SetWindowDimension(dimension ?? 0);
                    }
                }
                var currentValue = property.GetValue(this);
                var value = ReadValue(key, property.Name, currentValue);
                if (value != null)
                {
                    property.SetValue(this, Convert.ChangeType(value, property.PropertyType));
                }
                else
                {
                    property.SetValue(this, currentValue);
                }
            }
        }

        /// <summary>
        /// set all properties to their default value. Used for new application installs.
        /// </summary>
        public void Reset()
        {
            var properties = typeof(GlobalSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var property in properties)
            {
                if (property.CanWrite)
                {
                    var defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
                    if (property.Name != nameof(TimesStartedTotal) && property.Name != nameof(TimesStartedLastGameUpdate))
                    {
                        property.SetValue(this, defaultValue);
                    }
                }
                if (property.Name == nameof(LanguageCode))
                {
                    property.SetValue(this, CultureInfo.CurrentUICulture.IetfLanguageTag); //// Display language (only applied on multi lingual deployment of Windows OS).
                }
            }
        }

        public static Version GetAppVersion(bool ignoreBuildRevision = false)
        {
            var assemblyVersion = Assembly.GetExecutingAssembly()
              .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)
              .OfType<AssemblyFileVersionAttribute>()
              .FirstOrDefault();

            var version = assemblyVersion == null ? new Version() : new Version(assemblyVersion.Version);

            if (ignoreBuildRevision)
                return new Version(version.Major, version.Minor, 0, 0);
            
            return version;
        }

        #endregion

        #region Helpers

        private static void UpdateValue(RegistryKey key, string subkey, object value) => UpdateValue<object>(key, subkey, value);

        private static void UpdateValue<T>(RegistryKey key, string subkey, object value)
        {
            if (value == null)
            {
                key.DeleteValue(subkey, false);
                return;
            }
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var underlyingType = targetType.IsGenericType ? targetType.GetGenericTypeDefinition() : targetType;
            var typeCode = Type.GetTypeCode(underlyingType);
            var kind = typeCode switch
            {
                TypeCode.Int32 => RegistryValueKind.DWord,
                TypeCode.Double => RegistryValueKind.QWord,
                _ => RegistryValueKind.String,
            };
            var convert = TypeDescriptor.GetConverter(targetType).ConvertToString(value);
            key.SetValue(subkey, typeCode == TypeCode.String ? value.ToString() : convert, kind);
        }

        private static T ReadValue<T>(RegistryKey key, string subkey, T defaultValue = default)
        {
            var item = key?.GetValue(subkey);
            if (item == null)
                return defaultValue;

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var rangeMin = double.MinValue;
            var rangeMax = double.MaxValue;

            try
            {
                if (targetType.IsAssignableFrom(typeof(IComparable)))
                {
                    var typeInfo = targetType.GetTypeInfo();
                    var minAttr = typeInfo.GetCustomAttribute<RangeAttribute>();
                    if (minAttr != null)
                    {
                        rangeMin = minAttr.Minimum;
                        rangeMax = minAttr.Maximum;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while reading range for {typeof(T)}: {ex.Message}");
            }

            try
            {
                switch (item)
                {
                    case T t:
                        return t;
                    case string itemString when targetType == typeof(Version):
                        return (T)(object)new Version(itemString);
                    case string itemString when targetType.IsEnum:
                        return (T)Enum.Parse(targetType, itemString, true);
                    case object when targetType.IsValueType || targetType.IsClass:
                        return (T)Convert.ChangeType(item, targetType, CultureInfo.InvariantCulture);    
                    default:
                        break;
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                switch (ex)
                {
                    case ArgumentOutOfRangeException when message.Contains("Value out of range"):
                    case OverflowException when message.Contains("Value was too large") || message.Contains("Value too small"):
                        SConsole.WriteLine($"Value out of range for registry key '{subkey}'. Expected range: {rangeMin} to {rangeMax}. Actual value: {item}: {ex.Message}" + Environment.NewLine + ex.StackTrace);
                        break;
                    case InvalidCastException when message.Contains("Cannot cast from source type") || message.Contains("Invalid cast"):
                        SConsole.WriteLine($"Type mismatch for registry key '{subkey}'. Expected type: {typeof(T).Name}, Actual type: {item?.GetType().Name}: {message}" + Environment.NewLine + ex.StackTrace);
                        break;
                    case ArgumentException argEx when argEx.ParamName == nameof(key) || argEx.ParamName == nameof(subkey) || message.Contains("Value cannot be null"):
                    case FormatException when message.Contains("Input string was not in a correct format"):
                    case KeyNotFoundException when message.Contains("Key not found"):
                    case ArgumentException when message.Contains("Invalid Argument"):
                    default:

                        SConsole.WriteLine($"{ex.GetType().Name} occurred while reading registry key '{subkey}': {message}" + Environment.NewLine + ex.StackTrace);
                        throw new Exception(message, ex);
                }
            }
            return defaultValue;
        }


        [AttributeUsage(AttributeTargets.Parameter)]
        public class RangeAttribute(int minimum, int maximum) : Attribute
        {
            public int Minimum { get; } = minimum;
            public int Maximum { get; } = maximum;

        }

        #endregion
    }
}