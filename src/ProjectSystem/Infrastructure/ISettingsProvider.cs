namespace ProjectSystem.Infrastructure
{
    public interface ISettingsProvider
    {
        /// <summary>
        ///     Gets an option value.
        /// </summary>
        /// <param name="name">Option name.</param>
        /// <returns>Value.</returns>
        string GetOption(string name);
    }
}