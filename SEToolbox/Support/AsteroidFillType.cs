using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using SEToolbox.Models;

namespace SEToolbox.Support
{
    public interface IAsteroidFillType
    {
        string Name { get; }
        int Id { get; }
    }

    public class AsteroidFillType : IAsteroidFillType

    {
        private static readonly BaseModel _baseModel = new();
        private  List<object> _data ;
        private int _id;
        private int _customId;
        private string _name;
        private int _count;


        private static Dictionary<AsteroidFillType, List<object>> _customFillTypesRegistry ;
        private static ConcurrentDictionary<AsteroidFillType, List<object>> _fillTypesRegistry;


        /// <summary>
        /// Enumerates the types of fills that can be applied to an asteroid
        /// </summary>
        public enum AsteroidFills
        {
            Custom = -1,
            None = 0,
            ByteFiller = 1,
            SeedFiller = 2,
            //RandomFiller = 3

        }
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public int Id
        {
            get => _id;
            set => _id = value;
        }
        public int CustomId
        {
            get => _customId;
            private set => _customId = value;
        }
        public int Count
        {
            get => _count;
            private set => _count = value;
        }


        private static readonly Dictionary<int, AsteroidFills> FillTypeIdMap = Enum.GetValues(typeof(AsteroidFills))
                                                                                   .Cast<AsteroidFills>()
                                                                                   .ToDictionary(fill => (int)fill, fill => fill);


        private static readonly Dictionary<int, AsteroidFills> IdToFillTypeMap = FillTypeIdMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        private static readonly Dictionary<string, AsteroidFills> NameToFillTypeMap = Enum.GetNames(typeof(AsteroidFills))
                                                                                          .Zip(Enum.GetValues(typeof(AsteroidFills))
                                                                                          .Cast<AsteroidFills>(), (name, fill) => new { name, fill })
                                                                                          .ToDictionary(kvp => kvp.name, kvp => kvp.fill);

        public static AsteroidFills GetByIdOrName(int id, int customId, string name)
        {
            id = AsteroidFills.Custom == (AsteroidFills)id ? customId : id ;

            return id == 0 ? AsteroidFills.None : IdToFillTypeMap.TryGetValue(id, out var fill) || NameToFillTypeMap.TryGetValue(name, out fill) ? fill : AsteroidFills.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsteroidFillType"/> class
        /// </summary>
        /// <param name="id">The id of the fill type</param>
        /// <param name="name">The name of the fill type</param>
        public AsteroidFillType(int id, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), "Name cannot be null or empty");
            }

            Id = id;
            Name = name;
        }
        
        private static string GetFilePath()
        {   
            var fileName = "AsteroidFillData_*.json";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileName);
        }

        private static string GetFilePath(string filename) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), filename);
        
        private static readonly Dictionary<(int, string), (AsteroidFillType, List<object>)> FillTypeRegistry = new(EqualityComparer<(int, string)>.Default);
        public static Dictionary<AsteroidFillType, List<object>> CustomFillTypesRegistry
        {
            get => _customFillTypesRegistry;
            private set => _baseModel.SetProperty( _customFillTypesRegistry, value, nameof(CustomFillTypesRegistry));
        }

        public static Dictionary<(int, string), (AsteroidFillType, List<object>)> CustomFillTypesUnion
        {
            get
            {
                var union = new Dictionary<(int, string), (AsteroidFillType, List<object>)>(FillTypeRegistry);

                if (_customFillTypesRegistry != null)
                {
                    foreach (var kvp in _customFillTypesRegistry)
                    {
                        var key = (kvp.Key.Id ,kvp.Key.Name);
                        
                        union[key] = (kvp.Key, kvp.Value);
                    }
                }
                return new Dictionary<(int, string), (AsteroidFillType, List<object>)>(union);
            }
        }


        public static AsteroidFillType Register(int id, string name, params List<object> data)
        {
            if (FillTypeRegistry.ContainsKey((id, name)))
            {
                throw new ArgumentException($"AsteroidFillType with id {id} already exists: {name}");
            }

            var type = new AsteroidFillType(id, name);
            FillTypeRegistry.Add((id, name), (type, data));
            return type;
        }

        // todo create new filltypes using parameters 
        //todo invalid  
        public static IEnumerable<AsteroidFillType> GetAll() => FillTypeRegistry.Select(kvp => kvp.Value.Item1);

        public static void Clear() => FillTypeRegistry.Clear();

        public static bool IsCustomFill(IAsteroidFillType type, int customId = 0)
        {
            return (customId != 0 && type.Id == customId) || type.Id == -1;

        }

        public static void RegisterCustomType(bool isCustomFill, AsteroidFillType type, AsteroidFillType id, params List<object> data)
        {
            if (isCustomFill && IsCustomFill(type) && 
                CustomFillTypesRegistry.ContainsKey(type) || 
                CustomFillTypesRegistry.Any(t => t.Key.Name == type.Name || t.Key.Id == id.Id))
                {
                    throw new ArgumentException($"AsteroidFillType {type.Name} already exists.");
                }

                CustomFillTypesRegistry[type] = data;
        }

        public static void RegisterNewId(AsteroidFillType type, AsteroidFillType newId)
        {
            if (CustomFillTypesRegistry.TryGetValue(type, out var data))
            {
                CustomFillTypesRegistry[newId] = data;
            }
        }
   
        public static void LoadCustomFillData(IDictionary<AsteroidFillType, List<object>> customFillTypes, AsteroidFillType type, string fileName)
        {
            fileName ??= $"AsteroidFillData_*.json";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileName);
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<List<object>>(json);
                    customFillTypes[type] = data;
                }
                else
                {
                    Log.WriteLine($"No custom fill data found for {type.Name}");
                }
            }
            catch (JsonException ex) 
            {
                Log.WriteLine($"Failed to load custom fill data for {type.Name}: {ex.Message}");
            }
        }
        public static void EnumerateCustomFillData()
        {
            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            var files = Directory.GetFiles(folderPath, "AsteroidFillData_*.json");
            foreach (var file in files)
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                var fillType = FillTypeRegistry.Values.FirstOrDefault(kvp => kvp.Item1.Name == filename);
                if (fillType != (null, null))
                {
                    LoadCustomFillData(CustomFillTypesRegistry, fillType.Item1, filename);
                }
            }
        }


        /// <summary>
        /// Saves the custom fill data to a JSON file.
        /// </summary>
        /// <param name="customFillTypes">The custom fill types.</param>
        /// <param name="type">The asteroid fill type.</param>
        /// <param name="fileName">The filename (optional).</param>
        public static void SaveCustomFillData(IDictionary<AsteroidFillType, List<object>> customFillTypes, AsteroidFillType type, string fileName = null)
        {
            var filePath = GetFilePath(fileName);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                var json = JsonSerializer.Serialize(customFillTypes[type]);
                File.WriteAllText(filePath, json);
            }
            catch (Exception)
            {
                Log.WriteLine($"Failed to save custom fill data for {type.Name}");
                throw new Exception("Failed to save custom fill data.");
            }
        }


        public static void DeleteCustomFillData(string fileName, bool deleteAll = false)
        {
            var file = $"AsteroidFillData_{fileName}.json";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), file);

            if (File.Exists(filePath) && deleteAll)
            {
                try
                {
                    File.Delete(filePath);
                    Log.WriteLine($"Deleted  File : {file}");
                }
                catch (Exception ex)
                {
                    Log.WriteLine($"Failed to delete custom fill data file: {ex.Message}");
                }
            }
        }
        public static void ClearCustomFillData() => CustomFillTypesRegistry.Clear();
        public static void RefreshCustomFillData()
        {
            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            var files = Directory.GetFiles(folderPath, "AsteroidFillData_*.json");

            foreach (var file in files)
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                var fillType = FillTypeRegistry.Values.FirstOrDefault(kvp => kvp.Item1.Name == filename);

                if (fillType != (null, null))
                {
                    LoadCustomFillData(CustomFillTypesRegistry, fillType.Item1, filename);
                }
            }
        }
        public static void RemoveCustomFillType(AsteroidFillType fillType) => RemoveCustomFillType(fillType, fillType.Name);
        public static void RemoveCustomFillType(AsteroidFillType fillType, string fileName)
        {
            string file = $"AsteroidFillData_{fileName}.json";
            var filePath = GetFilePath(file);

            if (CustomFillTypesRegistry.ContainsKey(fillType))
            {
                CustomFillTypesRegistry.Remove(fillType);

                Dictionary<AsteroidFillType, List<object>> fileData = File.Exists(filePath) ? JsonSerializer.Deserialize<Dictionary<AsteroidFillType, List<object>>>(File.ReadAllText(filePath)) : [];
                fileData.Remove(fillType);
                var json = JsonSerializer.Serialize(fileData);
                File.WriteAllText(filePath, json);

                if (fileData.Count == 0)
                {
                    File.Delete(filePath);
                }
            }
        }
    }
}

