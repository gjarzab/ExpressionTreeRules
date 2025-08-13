namespace Metadata
{
    public class AvailableMethods(string path, params string[] methods)
    {
        public readonly string Path = path;
        public readonly string[] Methods = methods ?? [];
    }
}