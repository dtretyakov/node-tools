namespace Common.Automation
{
    public interface ISolutionManager
    {
        string Path { get; }

        string ActiveProjectPath { get; }

        void AddFile(string path);

        void AddDirectory(string path);

        void RemoveFile(string path);

        void RemoveDirectory(string path);
    }
}