using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class GhostAPIServer : MonoBehaviour
{
    private const string Prefix = "http://127.0.0.1:31555/ghost/command/";
    private HttpListener listener;
    private Thread listenerThread;
    private volatile bool isRunning;

    [SerializeField]
    private VRMController vrmController;

    private void Start()
    {
        StartServer();
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private void StartServer()
    {
        if (listener != null)
            return;

        listener = new HttpListener();
        listener.Prefixes.Add(Prefix);
        listener.Start();

        isRunning = true;
        listenerThread = new Thread(ListenLoop);
        listenerThread.IsBackground = true;
        listenerThread.Start();

        Debug.Log("Ghost API Server started: " + Prefix);
    }

    private void StopServer()
    {
        isRunning = false;
        try
        {
            listener?.Stop();
        }
        catch (Exception)
        {
        }

        try
        {
            listenerThread?.Join(500);
        }
        catch (Exception)
        {
        }

        listenerThread = null;
        listener = null;
    }

    private void ListenLoop()
    {
        while (isRunning)
        {
            HttpListenerContext context = null;
            try
            {
                context = listener.GetContext();
            }
            catch (HttpListenerException)
            {
                if (!isRunning)
                    return;
                continue;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (context != null)
                ProcessRequest(context);
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        try
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                context.Response.Close();
                return;
            }

            string json;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                json = reader.ReadToEnd();
            }

            GhostCommand command = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    command = JsonUtility.FromJson<GhostCommand>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Ghost API invalid JSON: " + ex.Message);
                }
            }

            if (command != null)
            {
                MainThreadQueue.Enqueue(() => ExecuteCommand(command));
            }

            WriteOkResponse(context);
        }
        catch (Exception ex)
        {
            Debug.LogError("Ghost API error: " + ex.Message);
            try
            {
                WriteOkResponse(context);
            }
            catch (Exception)
            {
            }
        }
        finally
        {
            try
            {
                context.Response.Close();
            }
            catch (Exception)
            {
            }
        }
    }

    private void WriteOkResponse(HttpListenerContext context)
    {
        var responseBytes = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = responseBytes.Length;
        context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        context.Response.OutputStream.Flush();
    }

    private void ExecuteCommand(GhostCommand command)
    {
        if (vrmController == null)
            return;

        if (string.Equals(command.type, "talk", StringComparison.OrdinalIgnoreCase))
        {
            vrmController.ApplyTalk(command);
        }
    }
}

[Serializable]
public class GhostCommand
{
    public string type;
    public int characterId;
    public string expression;
    public string text;
}
