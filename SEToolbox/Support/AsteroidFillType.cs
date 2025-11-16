using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SEToolbox.Support
{
    public interface IAsteroidFillType
    {
        string Name { get; }
        int Id { get; }
    }

    public class AsteroidFillType : IAsteroidFillType
    {
        public string Name { get; }
        public int Id { get; }
        
        /// <summary>
        /// Enumerates the types of fills that can be applied to an asteroid
        /// </summary>
        public enum AsteroidFills
        {
            Custom = -1,
            None = 0,
            ByteFiller = 1,
        }
        private static readonly Dictionary<int, AsteroidFills> FillTypeIdMap = Enum.GetValues(typeof(AsteroidFills))
            .Cast<AsteroidFills>()
            .ToDictionary(fill => (int)fill, fill => fill);

    
        public static AsteroidFills GetByIdOrName(int id, string name)
        {  
            if (FillTypeIdMap.TryGetValue(id, out var fill))
                return fill;
            if (Enum.TryParse(name, out fill))
                return fill;

            return AsteroidFills.None;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AsteroidFillType"/> class
        /// </summary>
        /// <param name="id">The id of the fill type</param>
        /// <param name="name">The name of the fill type</param>
        public AsteroidFillType(int id, string name)
        {

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Name cannot be null or empty");

            Id = id;
            Name = name;
        }  

        private static readonly Dictionary<(int, string), AsteroidFillType>FillTypeRegistry =
        new(EqualityComparer<(int, string)>.Default);
        public static Dictionary<AsteroidFillType, object[]> CustomFillTypesRegistry { get; private set; } = [];

        public static IEnumerable<KeyValuePair<AsteroidFillType, object[]>> CustomFillTypesUnion =>
            FillTypeRegistry.Values.Select(kvp => new KeyValuePair<AsteroidFillType, object[]>(kvp, kvp.Id == -1 ? [] : [kvp.Id])).Concat(CustomFillTypesRegistry);

        public static AsteroidFillType Register(int id, string name)
        {
            if (FillTypeRegistry.ContainsKey((id, name)))
                throw new ArgumentException($"AsteroidFillType with id {id} already exists.");

            var type = new AsteroidFillType(id, name);
                FillTypeRegistry.Add((id, name), type);
            return type;
        }

       
        public static IEnumerable<AsteroidFillType> GetAll() => FillTypeRegistry.Values;

        public static void Clear() => FillTypeRegistry.Clear();


        public static bool IsCustomFill(IAsteroidFillType type) => type.Id == -1;

        public static void RegisterCustomType(bool isCustomFill, AsteroidFillType type, AsteroidFillType id, params object[] data)
        {
            if (isCustomFill && IsCustomFill(type))
            {
                if (CustomFillTypesRegistry.ContainsKey(type) || CustomFillTypesRegistry.Any(t => t.Key.Name == type.Name || t.Key.Id == id.Id))
                    throw new ArgumentException($"AsteroidFillType {type.Name} already exists.");

                CustomFillTypesRegistry[type] = data;
            }
        }

        public static void RegisterNewId(AsteroidFillType type, AsteroidFillType newId)
        {
            if (CustomFillTypesRegistry.TryGetValue(type, out var data))
                CustomFillTypesRegistry[newId] = data;
        }


        public static void LoadCustomFillData(Dictionary<AsteroidFillType, object[]> customFillTypes, AsteroidFillType type, string fileName)
        {
           
            fileName ??= $"AsteroidFillData_{type.Name}.json";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileName);
            try
            {
                if (File.Exists(filePath))
                {

                    var json = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<List<object>>(json);
                    customFillTypes[type] = data?.ToArray();
                }
                else
                {
                    SConsole.WriteLine($"No custom fill data found for {type.Name}");
                }
            }
            catch (JsonException ex)
            {
                SConsole.WriteLine($"Failed to load custom fill data for {type.Name}: {ex.Message}");
            }
        }


        /// <summary>
        /// Saves the custom fill data to a JSON file.
        /// </summary>
        /// <param name="customFillTypes">The custom fill types.</param>
        /// <param name="type">The asteroid fill type.</param>
        /// <param name="filename">The filename (optional).</param>
        public static void SaveCustomFillData(Dictionary<AsteroidFillType, object[]> customFillTypes, AsteroidFillType type, string filename = null)
        {
            var filePath = GetFilePath(filename);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                var json = JsonSerializer.Serialize(customFillTypes[type]);
                File.WriteAllText(filePath, json);
            }
            catch (Exception)
            {
                throw new Exception("Failed to save custom fill data.");
            }
        }

        private static string GetFilePath(string filename)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"AsteroidFillData_{filename}.json");
        }

        public static void DeleteCustomFillData(string filename)
        {
            var file = $"AsteroidFillData_{filename}.json";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), file);
            if (File.Exists(filePath))
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    SConsole.WriteLine($"Failed to delete custom fill data : {ex.Message}");
                }
        }
    }
}

