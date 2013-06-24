using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DebugEngine.Node.Debugger.Communication
{
    /// <summary>
    ///     Connection with a node.js debugger.
    /// </summary>
    internal sealed class NodeDebuggerConnection : IDebuggerConnection
    {
        private readonly Regex _contentLength = new Regex(@"Content-Length: (\d+)", RegexOptions.Compiled);
        private readonly Encoding _encoding = Encoding.GetEncoding("latin1");
        private readonly StreamReader _streamReader;
        private readonly StreamWriter _streamWriter;
        private TcpClient _tcpClient;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="host">Debugger host.</param>
        /// <param name="port">Debugger port.</param>
        public NodeDebuggerConnection(string host, int port)
        {
            _tcpClient = new TcpClient(host, port);
            _streamReader = new StreamReader(_tcpClient.GetStream(), _encoding);
            _streamWriter = new StreamWriter(_tcpClient.GetStream(), _encoding);

            Task.Factory.StartNew(ReadStreamAsync);
        }

        /// <summary>
        ///     Send command asynchronously.
        /// </summary>
        /// <param name="message">Command name.</param>
        public async Task SendCommandAsync(string message)
        {
            Debug.Print(message);

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            char[] chars = _encoding.GetChars(bytes);
            string messageText = string.Format("Content-Length: {0}{1}{1}", chars.Length, Environment.NewLine);

            await _streamWriter.WriteAsync(messageText).ConfigureAwait(false);
            await _streamWriter.WriteAsync(chars, 0, chars.Length).ConfigureAwait(false);
            await _streamWriter.FlushAsync().ConfigureAwait(false);
        }

        public event EventHandler<StringEventArgs> OutputMessage;
        public event EventHandler<EventArgs> ConnectionClosed;

        public void Dispose()
        {
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }
        }

        private async Task<T> HandleExceptionsAsync<T>(Task<T> action)
        {
            try
            {
                return await action;
            }
            catch (Exception e)
            {
                Debug.Print("Connection failed: {0}", e);

                EventHandler<EventArgs> connectionClosed = ConnectionClosed;
                if (connectionClosed != null)
                {
                    connectionClosed(this, EventArgs.Empty);
                }

                return default(T);
            }
        }

        /// <summary>
        ///     Asynchronous read of the debugger output stream.
        /// </summary>
        private async void ReadStreamAsync()
        {
            while (_tcpClient != null)
            {
                // Read message header
                string result = await HandleExceptionsAsync(_streamReader.ReadLineAsync());
                if (result == null)
                {
                    break;
                }

                // Check whether result is content length header
                Match match = _contentLength.Match(result);
                if (!match.Success)
                {
                    continue;
                }

                await HandleExceptionsAsync(_streamReader.ReadLineAsync());

                // Retrieve body length
                int length = int.Parse(match.Groups[1].Value);
                if (length == 0)
                {
                    continue;
                }

                // Read message body
                var buffer = new char[length];
                int count = await HandleExceptionsAsync(_streamReader.ReadBlockAsync(buffer, 0, length));
                if (count == 0)
                {
                    break;
                }

                // Notify subscribers
                byte[] bytes = _encoding.GetBytes(buffer, 0, count);
                string message = Encoding.UTF8.GetString(bytes);

                Debug.Print(message);

                EventHandler<StringEventArgs> outputMessage = OutputMessage;
                if (outputMessage != null)
                {
                    outputMessage(this, new StringEventArgs(message));
                }
            }
        }
    }
}