#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using System.Threading.Tasks;

namespace onboard.devcade; 
using util;

public static class Client {

    public static event EventHandler<DevcadeGame> onBannerFinished = (_, _) => {
        logger.Trace("onBannerFinished Invoked");
    };
    public static event EventHandler<DevcadeGame> onIconFinished = (_, _) => {
        logger.Trace("onIconFinished Invoked");
    };
    public static event EventHandler<DevcadeGame> onGameFinished = (_, _) => {
        logger.Trace("onGameFinished Invoked");
    };
    
    
    /**
     * FS
     *   tmp/
     *   |- {game.id}/
     *       |- banner.png
     *       |- icon.png
     *       |- publish
     *          |- {game.name} (executable)
     */

    public static bool isProduction { get; private set; } = true;
    
    private static bool connected = false;
    
    private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.FullName);
    
    // Thread handle for the client
    private static readonly Thread clientThread;

    // Path to the working directory
    private static readonly string workingDir;
    
    // Unix FIFO pipe reader and writer for communication with backend
    private static StreamReader reader;
    private static StreamWriter writer;
    
    // Dictionary of started tasks by request id
    private static readonly Dictionary<uint, TaskCompletionSource<Response>> tasks = new();
    // List of requests to be sent to the backend. Only used when the backend is not connected yet.
    private static readonly List<Request> requests = new();

    /// <summary>
    /// Implicitly calls the static constructor
    /// </summary>
    public static void init() {
        // This method is called to implicitly call the static constructor
    }

    /// <summary>
    /// Static constructor for the client, initializes the client thread and path variables
    /// </summary>
    static Client() {
        logger.Info("Initializing Devcade Client");
        workingDir = Env.get("DEVCADE_PATH").match(
            v => v,
            () => {
                logger.Warn("DEVCADE_PATH not set, using default");
                return "/tmp/devcade";
            });
        logger.Info("DEVCADE_PATH: " + workingDir);

        clientThread = new Thread(start) {
            IsBackground = true
        };

        clientThread.Start();
    }

    /// <summary>
    /// Starts the client thread, which handles all communication with the backend
    /// </summary>
    [DoesNotReturn]
    private static void start() {
        logger.Info("Starting Devcade Client");
        
        // Open the read/write pipe to the backend
        
        var readerResult = tryOpenReader($"{workingDir}/write_onboard.pipe");
        while (readerResult.is_err()) {
            logger.Warn($"Failed to open read pipe, retrying in 500ms: {readerResult.unwrap_err()}");
            Thread.Sleep(500);
            readerResult = tryOpenReader($"{workingDir}/write_onboard.pipe");
        }
        reader = readerResult.unwrap();
        logger.Info($"Opened read pipe: {workingDir}/write_onboard.pipe");
        
        var writerResult = tryOpenWriter($"{workingDir}/read_onboard.pipe");
        while (writerResult.is_err()) {
            logger.Warn($"Failed to open write pipe, retrying in 500ms: {writerResult.unwrap_err()}");
            Thread.Sleep(500);
            writerResult = tryOpenWriter($"{workingDir}/read_onboard.pipe");
        }
        writer = writerResult.unwrap();
        logger.Info($"Opened write pipe: {workingDir}/read_onboard.pipe");

        repeatPing(5000, 5000);

        // Start the main loop
        while (true) {
            string message = read();

            if (message == "") {
                // sleep for a bit
                Thread.Sleep(100);
                continue;
            }
            
            logger.Trace("Received message: " + message);
            
            // Parse the message
            Response res = Response.deserialize(message);

            if (!tasks.ContainsKey(res.id)) {
                logger.Warn("Received response for unknown request id: " + res.id);
                continue;
            }
            
            // Get the task associated with the request id
            var task = tasks[res.id];
            
            // Remove the task from the dictionary
            tasks.Remove(res.id);
            
            // Set the result of the task
            task.SetResult(res);

            // Log the response
            switch (res.type) {
                case Response.ResponseType.Pong:
                    logger.Trace($"Received pong response for request {res.id}");
                    break;
                case Response.ResponseType.Err:
                    // Result is always an error here, so type parameter doesn't matter
                    logger.Error($"Received error response for request {res.id}: {res.into_result<uint>().unwrap_err()}");
                    break;
                case Response.ResponseType.Ok:
                    logger.Debug($"Received ok response for request {res.id}");
                    break;
                case Response.ResponseType.Game:
                    logger.Debug($"Received game response for request {res.id}");
                    break;
                case Response.ResponseType.GameList:
                    logger.Debug($"Received game list response for request {res.id} (contained {res.into<List<DevcadeGame>>().Count} games)");
                    break;
                default:
                    logger.Warn($"Received unknown response type for request {res.id}: {res.type}");
                    break;
            }
        }
    }
    
    
    #region Backend Communication

    /// <summary>
    /// Tries to open a read pipe to the backend, returning an error if it fails
    /// </summary>
    /// <param name="path">The path at which to open a StreamReader</param>
    /// <returns>A Result containing either a StreamReader or an Error</returns>
    private static Result<StreamReader, Exception> tryOpenReader(string path) {
        try {
            var _reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.ReadWrite));
            return Result<StreamReader, Exception>.Ok(_reader);
        } catch (Exception e) {
            return Result<StreamReader, Exception>.Err(e);
        }
    }

    /// <summary>
    /// Tries to open a write pipe to the backend, returning an error if it fails
    /// </summary>
    /// <param name="path">The path at which to open a streamWriter</param>
    /// <returns>A Result containing either a StreamWriter or an Error</returns>
    private static Result<StreamWriter, Exception> tryOpenWriter(string path) {
        try {
            var _writer = new StreamWriter(new FileStream(path, FileMode.Open, FileAccess.Write));
            return Result<StreamWriter, Exception>.Ok(_writer);
        } catch (Exception e) {
            return Result<StreamWriter, Exception>.Err(e);
        }
    }
    
    /// <summary>
    /// Read the contents of the read pipe, returning an empty string if the pipe is empty
    /// </summary>
    /// <returns></returns>
    private static string read() {
        return reader?.ReadLine() ?? "";
    }

    /// <summary>
    /// Writes a message to the backend
    /// </summary>
    /// <param name="message"></param>
    private static void write(string message) {
        writer?.WriteLine(message);
        writer?.Flush();
        writer?.BaseStream.Flush();
    }
    
    #endregion
    
    #region Request Methods

    /// <summary>
    /// Sends a request to the backend to get the game list for the current database mode.
    /// </summary>
    /// <returns>A Task that will be completed when the backend responds, containing a list of games or an error</returns>
    public static Task<Response> getGameList() {
        logger.Debug("Getting game list");
        return sendRequest(Request.GetGameList());
    }
    
    /// <summary>
    /// Sends a request to the backend to get the game with the given id.
    /// </summary>
    /// <param name="id">The id of the game to fetch</param>
    /// <returns>A Task that will be completed when the backend responds, containing a game or an error</returns>
    public static Task<Response> getGame(string id) {
        logger.Debug($"Getting game with id {id}");
        return sendRequest(Request.GetGame(id));
    }
    
    /// <summary>
    /// Sends a request to the backend to download the banner for the game with the given id.
    /// Once the banner has been downloaded, invokes the onBannerFinished event.
    /// </summary>
    /// <param name="id">The id of the game to download the banner for</param>
    /// <returns>A Task that will be completed once the banner has been downloaded</returns>
    public static Task downloadBanner(string id) {
        logger.Debug($"Downloading banner for game with id {id}");
        return sendRequest(Request.DownloadBanner(id))
            .ContinueWith(response => {
                onBannerFinished.Invoke(null, getGame(id).Result.into_option<DevcadeGame>().unwrap_or(new DevcadeGame()));
            });
    }
    
    /// <summary>
    /// Sends a request to the backend to download the icon for the game with the given id.
    /// Once the icon has been downloaded, invokes the onIconFinished event.
    /// </summary>
    /// <param name="id">The id of the game to download the icon for</param>
    /// <returns>A Task that will be completed once the icon has been downloaded</returns>
    public static Task downloadIcon(string id) {
        logger.Debug($"Downloading icon for game with id {id}");
        return sendRequest(Request.DownloadIcon(id))
            .ContinueWith(_ => {
                onIconFinished.Invoke(null, getGame(id).Result.into_option<DevcadeGame>().unwrap_or(new DevcadeGame()));
            });
    }
    
    /// <summary>
    /// Sends a request to the backend to download the game with the given id.
    /// Once the game has been downloaded, invokes the onGameFinished event.
    /// </summary>
    /// <param name="id">The id of the game to download</param>
    /// <returns>A Task that will be completed once the game has been downloaded</returns>
    public static Task downloadGame(string id) {
        logger.Debug($"Downloading game with id {id}");
        return sendRequest(Request.DownloadGame(id))
            .ContinueWith(_ => {
                onGameFinished.Invoke(null, getGame(id).Result.into_option<DevcadeGame>().unwrap_or(new DevcadeGame()));
            });
    }

    /// <summary>
    /// Sends a request to the backend to launch the game with the given id. If the game is not yet downloaded, the
    /// backend will download it first.
    /// </summary>
    /// <param name="id">The id of the game to launch</param>
    /// <returns>A Task that will be completed once the game has exited</returns>
    public static Task<Response> launchGame(string id) {
        logger.Debug($"Launching game with id {id}");
        return sendRequest(Request.LaunchGame(id));
    }

    /// <summary>
    /// Sends a request to the backend to set the production mode.
    /// </summary>
    /// <param name="prod">Whether the backend should use the production or development database</param>
    /// <returns>A Task that will be completed when the backend has responded</returns>
    public static Task setProduction(bool prod) {
        string prodStr = prod ? "Production" : "Development";
        logger.Info($"Setting API to {prodStr}");
        return sendRequest(Request.SetProduction(prod)).ContinueWith(res => {
            if (res.Result.type == Response.ResponseType.Ok) {
                isProduction = prod;
            }
        });
    }

    /// <summary>
    /// Repeatedly pings the backend to check if it is still connected
    /// </summary>
    /// <param name="intervalMillis">The time between one ping finishing and another ping starting</param>
    /// <param name="timeoutMillis">The timeout to wait for a ping before giving up</param>
    private static void repeatPing(int intervalMillis, int timeoutMillis) {
        void ping(int _intervalMillis, int _timeoutMillis) {
            DateTime start = DateTime.Now;
            forceSendRequest(Request.Ping()).WaitAsync(TimeSpan.FromMilliseconds(_timeoutMillis))
                .ContinueWith(res => {
                    if (res is { IsCompletedSuccessfully: true, Result.type: Response.ResponseType.Pong }) {
                        DateTime end = DateTime.Now;
                        logger.Trace($"Ping successful ({(int)Math.Round((end - start).TotalMilliseconds)}ms)");
                        if (!connected) {
                            sendQueuedRequests();
                        }
                        connected = true;
                    }
                    else {
                        logger.Error($"Failed to ping backend (no response after {_timeoutMillis}ms)");
                        connected = false;
                    }
                })
                .Wait();
            Thread.Sleep(_intervalMillis);
        }

        Task.Run(() => {
            while (true) {
                ping(intervalMillis, timeoutMillis);
            }
        });
    }

    #endregion
    
    /// <summary>
    /// Internal method to send a request to the backend, if the backend is not connected, the request will be queued
    /// and sent when the backend is connected
    /// </summary>
    /// <param name="req">The request object to send</param>
    /// <returns>A Task that will be completed when the backend responds</returns>
    private static Task<Response> sendRequest(Request req) {
        if (connected) {
            write(req.serialize());
        }
        else {
            // Add the request to the queue, it will be sent when the backend is connected
            requests.Add(req);
        }
        
        TaskCompletionSource<Response> tcs = new();
        
        tasks.Add(req.id, tcs);
        
        return tcs.Task;
    }

    /// <summary>
    /// Internal method to send a request to the backend. Will attempt to write to the backend, even if the backend is
    /// not connected. This should only be used for pings, as it will not queue the request if the backend is not
    /// connected. The request will be lost if the backend connects after the request is sent.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    private static Task<Response> forceSendRequest(Request req) {
        write(req.serialize());
        
        TaskCompletionSource<Response> tcs = new();
        
        tasks.Add(req.id, tcs);
        
        return tcs.Task;
    }

    /// <summary>
    /// Sets all current tasks to canceled, this can be used when the backend is disconnected to prevent the tasks from
    /// hanging forever. Given that the backend is more stable than the frontend, this should not be necessary.
    /// </summary>
    private static void killAllTasks() {
        foreach(var t in tasks.Values) {
            try {
                t.TrySetCanceled();
            } catch (Exception e) {
                logger.Error("Failed to cancel task: " + e);
            }
        }
    }
    
    /// <summary>
    /// Sends all queued requests to the backend. This should only be called when the backend is connected. This is only
    /// necessary if the frontend is started before the backend.
    /// </summary>
    private static void sendQueuedRequests() {
        foreach (Request req in requests) {
            write(req.serialize());
        }
        requests.Clear();
    }
}
