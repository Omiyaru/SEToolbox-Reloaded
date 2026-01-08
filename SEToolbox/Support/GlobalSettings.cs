using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic;
using Microsoft.Win32;


namespace SEToolbox.Support
{
    public class GlobalSettings
    {
        #region Fields

        public static GlobalSettings Default = new();
        public static TimesStarted TimesStartedInfo = new();
        /// <summary>
        /// Temporary property to reprompt user to game installation path.
        /// </summary>
        public bool PromptUser;

        private bool _isLoaded;

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

        public struct TimesStarted(int? total, int? sinceLastReset, DateTime? lastReset, int? sinceGameUpdate, DateTime? lastGameUpdate)
        {
            public int? Total = total;
            public int? SinceLastReset = sinceLastReset;
            public DateTime? LastReset = lastReset;
            public int? SinceGameUpdate = sinceGameUpdate;
            public DateTime? LastGameUpdate = lastGameUpdate;

            public readonly TimesStarted TimesStartedInfo => new(Total, SinceLastReset, LastReset, SinceGameUpdate, LastGameUpdate);

            public TimesStarted UpdateTimesStartedInfo(DateTime? gameUpdateDate = null)
            {
                Total++;

                var today = DateTime.Now;

                if (gameUpdateDate.HasValue)
                {
                    var timeSinceUpdate = today.Subtract(LastGameUpdate.GetValueOrDefault()).Days;
                    SinceGameUpdate = timeSinceUpdate;
                    LastGameUpdate = gameUpdateDate;
                }
                if (LastReset.HasValue)
                {

                    var timeSinceLastReset = today.Subtract(LastReset.GetValueOrDefault()).Days;
                    SinceLastReset = timeSinceLastReset; ;
                    LastReset = today;
                }
                else
                {
                    SinceLastReset = 0;
                    LastReset = today;
                }
                return TimesStartedInfo;
            }
        }

        public struct WindowDimension(double? left, double? top, double? width, double? height)
        {
            public readonly double? Left => left;
            public readonly double? Top => top;
            public readonly double? Width => width;
            public readonly double? Height => height;
        }

        private readonly Dictionary<double?, WindowDimension> _windowDimensions = [];

        public Dictionary<double?, WindowDimension> WindowDimensions => _windowDimensions;

        public WindowDimension GetWindowDimension(double key)
        {
            return _windowDimensions.TryGetValue(key, out var dimension) ? dimension : default;
        }

        public WindowDimension SetWindowDimension(double? key, WindowDimension dimension = default)
        {
            if (_windowDimensions.TryGetValue(key, out var existingDimension))
            {
                dimension = existingDimension;
            }

            var properties = typeof(WindowDimension).GetProperties();
            foreach (var property in properties)
            {
                if (property.GetValue(dimension) is double doubleValue)
                {
                    var validatedValue = ValidateWindowDimension(doubleValue);
                    if (validatedValue.HasValue)
                    {
                        property.SetValue(dimension, validatedValue.Value);
                    }
                }
            }

            _windowDimensions[key] = dimension;
            return dimension;
        }

        private double? ValidateWindowDimension(double? value, WindowDimension dimension = default)
        {

            Log.WriteLine($"Validating Window Dimension: {value}");
            if (dimension.Width.HasValue && dimension.Height.HasValue)
            {     
                var primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
                var primaryScreenHeight = SystemParameters.PrimaryScreenHeight;
                value = value   switch
                {
                    < double.MinValue or < 0 => null,
                    _ when value > double.MaxValue || 
                           value > primaryScreenWidth || 
                           value > primaryScreenHeight => double.MaxValue,
                    _ => value
                };
            }
            return value;
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
                var properties = Settings.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var propertyDict = properties.ToDictionary(p => p.Name, p => p.GetValue(this));
                foreach (var property in properties)
                {
                    UpdateValue(key, property.Name, property.GetValue(this));
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
            if (!_isLoaded)
            {
                _isLoaded = true;
            }

            var key = Registry.CurrentUser.OpenSubKey(BaseKey, false);

            if (key == null)
            {
                Reset();
                return;
            }

            var properties = Settings.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var propertyDict = properties.ToDictionary(p => p.Name, p => p.GetValue(this));
            foreach (var property in properties)
            {
                var currentValue = propertyDict[property.Name];
                var typedValue = ReadValue(key, property.Name, currentValue);
                var converter = TypeDescriptor.GetConverter(property.PropertyType);
                Action _ = typedValue switch
                {
                    _ when ReferenceEquals(typedValue, currentValue) => _ = () => { },
                    _ when property.PropertyType.IsInstanceOfType(typedValue) => _ = () => property.SetValue(this, typedValue),
                    _ when converter.CanConvertFrom(typedValue.GetType()) => _ = () => property.SetValue(this, converter.ConvertFrom(typedValue)),
                    _ => _ = () => { },
                };
                    _();


                Log.WriteLine($"{property.Name}: {typedValue ?? null}");

                if (property.Name == nameof(LanguageCode))
                {
                    LanguageCode = ReadValue(key, nameof(LanguageCode), CultureInfo.CurrentUICulture.IetfLanguageTag);
                    Log.WriteLine($" {property.Name}: {LanguageCode}");
                }
                else if (property.Name == nameof(WindowDimension))
                {
                    foreach (double? dimension in WindowDimensions?.Keys)
                    {
                        SetWindowDimension(dimension ?? 0);
                    }
                }
            }
        }

        /// <summary>
        /// set all properties to their default value. Used for new application installs.
        /// </summary>
        public void Reset()
        {
            Log.WriteLine("Resetting GlobalSettings");
            var properties = typeof(GlobalSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            foreach (var property in properties)
            {

                var defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
                var setValue = property.CanWrite && property.DeclaringType != typeof(TimesStarted);
                object value = null;
                var newValue = setValue ? property.Name switch
                {
                    nameof(TimesStarted.LastReset) => null,
                    nameof(AlwaysCheckForUpdates) => true, // the space is intentional for any future updates

                    nameof(LanguageCode) => CultureInfo.CurrentUICulture.IetfLanguageTag,
                    _ => value,
                } : defaultValue;

                if (setValue && !ReferenceEquals(newValue, value))
                {
                    property.SetValue(this, newValue);
                }

                Log.WriteLine($"{property.Name}: {newValue ?? null}");
                Log.WriteLine($"{property.Name}: {property.GetValue(this)}");
            }
        }

        public static Version GetAppVersion(bool ignoreBuildRevision = false)
        {
            var assemblyVersion = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)
                                          .OfType<AssemblyFileVersionAttribute>()
                                          .FirstOrDefault();

            var version = assemblyVersion == null ? new Version() : new Version(assemblyVersion.Version);

            return ignoreBuildRevision ? new Version(version.Major, version.Minor, 0, 0) : version;
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
                TypeCode.Int32 or TypeCode.Boolean => RegistryValueKind.DWord,
                TypeCode.Double => RegistryValueKind.QWord,
                TypeCode.Empty => RegistryValueKind.None,
                _ => RegistryValueKind.String,
            };
            var convert = TypeDescriptor.GetConverter(targetType).ConvertToString(value);
            key.SetValue(subkey, typeCode == TypeCode.String ? value.ToString() : convert, kind);
        }

        private static T ReadValue<T>(RegistryKey key, string subkey, T defaultValue = default)
        {
            var item = key?.GetValue(subkey);
            item ??= defaultValue;

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var rangeMin = double.MinValue;
            var rangeMax = double.MaxValue;
            var typeInfo = targetType.GetTypeInfo();
            var minAttr = typeInfo.GetCustomAttribute<RangeAttribute>();
            try
            {
                if (targetType.IsAssignableFrom(typeof(IComparable)) && minAttr != null)
                {
                    rangeMin = minAttr.Minimum;
                    rangeMax = minAttr.Maximum;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine($"Error while reading range for {typeof(T)}: {ex.Message}");
            }
            try
            {
                item = item switch
                {
                    T t => t,
                    string itemString when targetType == typeof(Version) => new Version(itemString),
                    string itemString when targetType.IsEnum => Enum.Parse(targetType, itemString, true),
                    object when targetType.IsValueType || targetType.IsClass => Convert.ChangeType(item, targetType, CultureInfo.InvariantCulture),
                    string itemString when targetType == typeof(Guid) => new Guid(itemString),
                    null => defaultValue,
                    _ => defaultValue,
                };
            }
            catch (Exception ex)
            {
                var message = ex.Message;

                switch (ex)
                {
                    case ArgumentOutOfRangeException when message.Contains("Value out of range"):
                    case OverflowException when message.Contains("Value was too large") || message.Contains("Value too small"):
                        Log.WriteLine($"Value out of range for registry key '{subkey}'. Expected range: {rangeMin} to {rangeMax}. Actual value: {item}: {ex.Message}");
                        break;
                    case InvalidCastException when message.Contains("Cannot cast from source type") || message.Contains("Invalid cast"):
                        Log.WriteLine($"Type mismatch for registry key '{subkey}'. Expected type: {typeof(T).Name}, Actual type: {item?.GetType().Name}: {message}");
                        break;
                    case FormatException when message.Contains("Input string was not in a correct format"):
                        Log.WriteLine($"Invalid format for registry key '{subkey}'. Expected format: {typeof(T).Name}, Actual format: {item?.GetType().Name}: {message}");


                        break;
                    case NotSupportedException when message.Contains($"Platform '{Environment.OSVersion.Platform}' is not supported"):
                    case ArgumentException argEx when argEx.ParamName == nameof(key) || argEx.ParamName == nameof(subkey) || message.Contains("Value cannot be null"):
                    case FormatException when message.Contains("Input string was not in a correct format"):
                    case KeyNotFoundException when message.Contains("Key not found"):
                    case ArgumentException when message.Contains("Invalid Argument"):
                    default:
                        Log.WriteLine($"{ex.GetType().Name} occurred while reading registry key '{subkey}': {message}{Environment.NewLine}{ex.StackTrace}");
                        throw new Exception(message, ex);
                }
                Log.WriteLine($"{ex.GetType().Name} occurred while reading registry key '{subkey}': {message}{Environment.NewLine}{ex.StackTrace}");
                Debug.WriteLine($"{ex.GetType().Name} occurred while reading registry key '{subkey}': {message}{Environment.NewLine}{ex.StackTrace}");

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