namespace SEToolbox.Interop
{
    public enum ContentPathType
    {
        Texture,
        Model,
        SandboxContent,
        SandboxSector,
    };

    public class ContentDataPath(ContentPathType contentType, string referencePath, string absolutePath, string zipFilePath)
    {
        public ContentPathType ContentType { get; set; } = contentType;
        public string ReferencePath { get; set; } = referencePath;
        public string AbsolutePath { get; set; } = absolutePath;
        public string ZipFilePath { get; set; } = zipFilePath;
    }
}