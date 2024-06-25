using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace RunWatcher
{
    [BepInPlugin("cavemoss.RunWatcher", "RunWatcher", "1.0.0")]
    internal class Server : BaseUnityPlugin
    {
        private readonly List<HttpListenerContext> _clients = [];
        private readonly object _lock = new();
        private readonly HttpListener _listener;
        private readonly string _prefix;

        public Server(string prefix)
        {
            if (!HttpListener.IsSupported)
            {
                Logger.LogError("HttpListener is not supported on this platform.");
            }

            _listener = new HttpListener();
            _prefix = prefix.EndsWith("/") ? prefix : prefix + "/";
            _listener.Prefixes.Add(_prefix);
        }

        public void Start()
        {
            _listener.Start();
            Logger.LogWarning($"Listening for connections on {_prefix}");
            Thread listenerThread = new(new ThreadStart(Listen));
            listenerThread.Start();
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }

        private void Listen()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = _listener.GetContext();
                }
                catch (HttpListenerException exception)
                {
                    Logger.LogError(exception.Message);
                    return;
                }
                ThreadPool.QueueUserWorkItem(o => HandleRequest(context));
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            string urlPath = request.Url.AbsolutePath;

            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (urlPath == "/webhook")
            {
                if (request.HttpMethod == "POST")
                {
                    // Respond immediately to client registration
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.Close();
                    Logger.LogMessage("Client registration request received.");
                }
                else if (request.HttpMethod == "GET")
                {
                    // Register client for SSE
                    lock (_lock) { _clients.Add(context); }

                    // Keep the connection open
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.ContentType = "text/event-stream";
                    response.OutputStream.Flush();

                    Logger.LogMessage($"Client {context.Request.RemoteEndPoint} registered for updates.");
                    SendUpdate(RunData.GetCurrentRunData());
                }
                else if (request.HttpMethod == "PUT")
                {
                    // Handle updates
                    using StreamReader reader = new(request.InputStream, request.ContentEncoding);
                    string requestBody = reader.ReadToEnd();

                    NotifyClients(requestBody);
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.Close();
                }
            }
            else if (urlPath == "/api")
            {
                if (request.HttpMethod == "POST")
                {
                    using StreamReader reader = new(request.InputStream, request.ContentEncoding);
                    string dataStr = reader.ReadToEnd();

                    object dataObj = JsonConvert.DeserializeObject(dataStr); 
                    string dataJson = JsonConvert.SerializeObject(dataObj, Formatting.Indented);

                    string filePath = $@"{Path.GetDirectoryName(Info.Location)}\vue-app\public\item-info.json";
                    File.WriteAllText(filePath, dataJson);

                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.Close();
                }
                else if (request.HttpMethod == "GET")
                {
                    object gameData = Utils.GetGameData();
                    string json = JsonConvert.SerializeObject(gameData);

                    response.ContentType = "application/json";
                    response.ContentEncoding = Encoding.UTF8;

                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Close();
            }
        }

        private void NotifyClients(string updateMessage)
        {
            List<HttpListenerContext> clientsToRemove = [];

            lock (_lock)
            {
                foreach (var client in _clients)
                {
                    try
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes($"data: {updateMessage}\n\n");
                        client.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        client.Response.OutputStream.Flush();
                    }
                    catch (Exception)
                    {
                        clientsToRemove.Add(client);
                    }
                }

                foreach (var client in clientsToRemove)
                {
                    Logger.LogWarning($"Client {client.Request.RemoteEndPoint} removed.");
                    _clients.Remove(client);
                }
            }
        }

        public async void SendUpdate(string json)
        {
            using HttpClient client = new();

            // Create the content
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the PUT request
            HttpResponseMessage response = await client.PutAsync($"{_prefix}webhook", content);

            // Check the response status code
            if (response.IsSuccessStatusCode)
            {
                Logger.LogMessage($"Update on {_prefix}");
            }
            else
            {
                Logger.LogError($"Request failed with status code: {response.StatusCode}");
            }
        }
    }
}
