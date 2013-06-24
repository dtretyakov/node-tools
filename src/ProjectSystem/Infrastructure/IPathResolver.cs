namespace ProjectSystem.Infrastructure
{
    public interface IPathResolver
    {
        string FindFilePath(string filename);
    }
}