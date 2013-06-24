using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Windows.Media;
using Common.Automation;
using Console.Types;

namespace Console.Host
{
    /// <summary>
    ///     Simple PowerShell host.
    /// </summary>
    internal class DefaultHost : IHost
    {
        private readonly Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();
        private readonly ISolutionManager _solutionManager;
        private IConsole _console;

        public DefaultHost(ISolutionManager solutionManager)
        {
            _solutionManager = solutionManager;

            _commands.Add("npm", HandleNpmCommand);
            _commands.Add("clear", HandleClearCommand);
        }

        public bool IsCommandEnabled
        {
            get { return true; }
        }

        public void Initialize(IConsole console)
        {
            if (console == null)
            {
                throw new ArgumentNullException("console");
            }

            _console = console;

            DisplayDisclaimerAndHelpText();
        }

        public string Prompt
        {
            get { return "> "; }
        }

        public bool Execute(IConsole console, string command, object[] inputs)
        {
            return ExecuteCommand(command);
        }

        public void Abort()
        {
        }

        public string ActivePackageSource
        {
            get { return String.Empty; }
            set { }
        }

        public string[] GetPackageSources()
        {
            return new string[0];
        }

        public string DefaultProject
        {
            get { return String.Empty; }
        }

        public void SetDefaultProjectIndex(int index)
        {
        }

        public string[] GetAvailableProjects()
        {
            return new string[0];
        }

        public void SetDefaultRunspace()
        {
        }

        /// <summary>
        ///     Handles npm command.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        private void HandleNpmCommand(string[] parameters)
        {
            string setDirectory = string.Format(@"set-location ""{0}""", _solutionManager.ActiveProjectPath);
            var command = string.Format("npm {0}", string.Join(" ", parameters));
            Collection<PSObject> results;

            using (PowerShell shell = PowerShell.Create())
            {
                shell.AddScript(setDirectory);
                shell.AddScript(command);

                shell.Streams.Progress.DataAdded += OnProgress;
                shell.Streams.Error.DataAdded += OnError;
                shell.Streams.Verbose.DataAdded += OnVerbose;
                shell.Streams.Warning.DataAdded += OnWarning;

                results = shell.Invoke();
            }

            foreach (PSObject result in results)
            {
                WriteLine(result.ToString());
            }

            WriteLine();

            if (parameters.Length < 2)
            {
                return;
            }

            switch (parameters[0].ToLowerInvariant())
            {
                case "install":
                    {
                        var path = string.Format(@"{0}\node_modules\{1}", _solutionManager.ActiveProjectPath, parameters[1]);
                        _solutionManager.AddDirectory(path);
                    }
                    break;

                case "uninstall":
                    {
                        var path = string.Format(@"node_modules\{0}", parameters[1]);
                        _solutionManager.RemoveDirectory(path);
                    }
                    break;
            }
        }

        /// <summary>
        ///     Handles clear command.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        private void HandleClearCommand(string[] parameters)
        {
            if (_console == null)
            {
                return;
            }

            _console.Clear();
        }

        /// <summary>
        ///     Executes a command.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <returns>Result.</returns>
        private bool ExecuteCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }

            string[] segments = command.Split(' ');
            string commandName = segments[0].ToLowerInvariant();
            Action<string[]> commandHandler;

            if (_commands.TryGetValue(commandName, out commandHandler))
            {
                commandHandler(segments.Skip(1).ToArray());
                return true;
            }

            WriteLine("Invalid command: {0}", command);
            WriteLine("Currently available following commands:");

            foreach (string key in _commands.Keys)
            {
                WriteLine("- {0}", key);
            }

            WriteLine();

            return false;
        }

        private void OnWarning(object sender, DataAddedEventArgs e)
        {
            var data = (PSDataCollection<WarningRecord>) sender;
            WarningRecord record = data[e.Index];

            WriteLine(record.ToString());
        }

        private void OnVerbose(object sender, DataAddedEventArgs e)
        {
            var data = (PSDataCollection<VerboseRecord>) sender;
            VerboseRecord record = data[e.Index];

            Write(record.ToString());
        }

        private void OnError(object sender, DataAddedEventArgs e)
        {
            var data = (PSDataCollection<ErrorRecord>) sender;
            ErrorRecord record = data[e.Index];

            WriteError(record.ToString());
        }

        private void OnProgress(object sender, DataAddedEventArgs e)
        {
            var data = (PSDataCollection<ProgressRecord>) sender;
            ProgressRecord record = data[e.Index];

            Write(record.ToString());
        }

        private void Write(string message)
        {
            if (_console == null)
            {
                return;
            }

            _console.Write(message);
        }

        private void WriteError(string message)
        {
            if (_console == null)
            {
                return;
            }

            _console.Write(message, Colors.Black, Colors.Yellow);
        }

        private void WriteLine(string format = "", params object[] parameters)
        {
            if (_console == null)
            {
                return;
            }

            _console.WriteLine(string.Format(format, parameters));
        }

        private void DisplayDisclaimerAndHelpText()
        {
            WriteLine(Resources.Console_DisclaimerText);
            WriteLine();
        }
    }
}