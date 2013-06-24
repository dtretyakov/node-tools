using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DebugEngine.Node.Debugger.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Communication
{
    /// <summary>
    ///     Node debugger client.
    /// </summary>
    internal sealed class NodeDebuggerClient
    {
        /// <summary>
        ///     Debugger connection.
        /// </summary>
        private readonly IDebuggerConnection _connection;

        /// <summary>
        ///     Sequntial message indetifier.
        /// </summary>
        private int _messageId;

        /// <summary>
        ///     Current sent messages.
        /// </summary>
        private Dictionary<int, Tuple<TaskCompletionSource<IResponseMessage>, object[]>> _messages =
            new Dictionary<int, Tuple<TaskCompletionSource<IResponseMessage>, object[]>>();

        public NodeDebuggerClient(IDebuggerConnection connection)
        {
            _connection = connection;
            _connection.OutputMessage += OnOutputMessage;
        }

        private void OnOutputMessage(object sender, StringEventArgs eventArgs)
        {
            JObject message;

            try
            {
                message = JObject.Parse(eventArgs.Message);
            }
            catch (Exception e)
            {
                Debug.Fail(string.Format("Invalid event message: {0}", e));
                return;
            }
            
            switch ((string) message["type"])
            {
                case "event":
                    HandleEventMessage(message);
                    break;

                case "response":
                    HandleResponseMessage(message);
                    break;

                default:
                    Debug.Print("Unrecognized message type: {0}", message);
                    break;
            }
        }

        /// <summary>
        ///     Handles event message.
        /// </summary>
        /// <param name="message">Message.</param>
        private void HandleEventMessage(JObject message)
        {
            switch ((string) message["event"])
            {
                case "afterCompile":
                    {
                        IEventMessage messageData = NodeMessageFactory.CreateEventMessage(message);
                        var compileScriptMessage = messageData as CompileScriptMessage;
                        if (messageData == null || !messageData.IsSuccessful)
                        {
                            string errorMessage = string.Format("Invalid event message: {0}", message);
                            Debug.Fail(errorMessage);
                            break;
                        }

                        EventHandler<CompileScriptMessageEventArgs> compileEvent = CompileEvent;
                        if (compileEvent != null)
                        {
                            compileEvent(this, new CompileScriptMessageEventArgs(compileScriptMessage));
                        }
                    }
                    break;

                case "break":
                    {
                        IEventMessage messageData = NodeMessageFactory.CreateEventMessage(message);
                        var breakPointMessage = messageData as BreakpointMessage;
                        if (messageData == null || !messageData.IsSuccessful)
                        {
                            string errorMessage = string.Format("Invalid event message: {0}", message);
                            Debug.Fail(errorMessage);
                            break;
                        }

                        EventHandler<BreakpointMessageEventArgs> breakpointEvent = BreakpointEvent;
                        if (breakpointEvent != null)
                        {
                            breakpointEvent(this, new BreakpointMessageEventArgs(breakPointMessage));
                        }
                    }
                    break;

                case "exception":
                    {
                        IEventMessage messageData = NodeMessageFactory.CreateEventMessage(message);
                        var exceptionMessage = messageData as ExceptionMessage;
                        if (messageData == null || !messageData.IsSuccessful)
                        {
                            string errorMessage = string.Format("Invalid event message: {0}", message);
                            Debug.Fail(errorMessage);
                            break;
                        }

                        EventHandler<ExceptionMessageEventArgs> exceptionEvent = ExceptionEvent;
                        if (exceptionEvent != null)
                        {
                            exceptionEvent(this, new ExceptionMessageEventArgs(exceptionMessage));
                        }
                    }
                    break;

                default:
                    {
                        var errorMessage = (string) message["message"];
                        Dictionary<int, Tuple<TaskCompletionSource<IResponseMessage>, object[]>> messages =
                            Interlocked.Exchange(ref _messages, new Dictionary<int, Tuple<TaskCompletionSource<IResponseMessage>, object[]>>());

                        foreach (var kv in messages)
                        {
                            kv.Value.Item1.SetException(new Exception(errorMessage));
                        }

                        messages.Clear();

                        Debug.Print("Unrecognized event type: {0}", message);
                    }
                    break;
            }
        }

        /// <summary>
        ///     Handles response message.
        /// </summary>
        /// <param name="message">Message.</param>
        private void HandleResponseMessage(JObject message)
        {
            IResponseMessage response = NodeMessageFactory.CreateResponseMessage(message);
            Tuple<TaskCompletionSource<IResponseMessage>, object[]> promise;

            if (!_messages.TryGetValue(response.MessageId, out promise))
            {
                Debug.Print("Invalid response message identifier {0}: {1}", response.MessageId, message);
                return;
            }

            if (response.IsSuccessful)
            {
                response.Execute(promise.Item2);
            }

            promise.Item1.SetResult(response);
        }

        /// <summary>
        ///     Sends a request message.
        /// </summary>
        /// <typeparam name="T">Arguments type.</typeparam>
        /// <param name="command">Command name.</param>
        /// <param name="arguments">Arguments.</param>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Response.</returns>
        public Task<IResponseMessage> SendMessage<T>(string command, T arguments, params object[] parameters) where T : class
        {
            int id = Interlocked.Increment(ref _messageId);
            var request = new {seq = id, type = "request", command, arguments};

            return GetResponseAsync(id, request, parameters);
        }

        /// <summary>
        ///     Sends a request message.
        /// </summary>
        /// <param name="command">Command name.</param>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Response.</returns>
        public Task<IResponseMessage> SendMessage(string command, params object[] parameters)
        {
            int id = Interlocked.Increment(ref _messageId);
            var request = new {seq = id, type = "request", command};

            return GetResponseAsync(id, request, parameters);
        }

        private async Task<IResponseMessage> GetResponseAsync<T>(int id, T request, object[] parameters)
        {
            var promise = new TaskCompletionSource<IResponseMessage>();
            _messages.Add(id, new Tuple<TaskCompletionSource<IResponseMessage>, object[]>(promise, parameters));

            string message = JsonConvert.SerializeObject(request);
            await _connection.SendCommandAsync(message).ConfigureAwait(false);

            IResponseMessage response = await promise.Task.ConfigureAwait(false);
            _messages.Remove(id);

            return response;
        }

        /// <summary>
        ///     Break point event handler.
        /// </summary>
        public event EventHandler<BreakpointMessageEventArgs> BreakpointEvent;

        /// <summary>
        ///     Compile script event handler.
        /// </summary>
        public event EventHandler<CompileScriptMessageEventArgs> CompileEvent;

        /// <summary>
        ///     Exception event handler.
        /// </summary>
        public event EventHandler<ExceptionMessageEventArgs> ExceptionEvent;
    }
}