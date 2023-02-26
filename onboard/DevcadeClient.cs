﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework; // FIXME: Is this necessary for the client code?

// For making requests to the API
using System.Net.Http;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace onboard
{
    public class DevcadeGame
    {
        public string id { get; set; }
        public string author { get; set; }
        public DateTime uploadDate { get; set; }
        public string name { get; set; }
        public string hash { get; set; }
        public string description { get; set; }
        public string iconLink { get; set; }
        public string bannerLink { get; set; }
    }

    public class DevcadeClient
    {
        private readonly string _apiDomain;

        public DevcadeClient() {
            _apiDomain = Environment.GetEnvironmentVariable("DEVCADE_API_DOMAIN");
        }
        
        public List<DevcadeGame> GetGames() {
            using var client = new HttpClient();
            try {
                string uri = $"https://{_apiDomain}/api/games/gamelist/"; // TODO: Env variable URI tld 
                using Task<string> responseBody = client.GetStringAsync(uri);
                List<DevcadeGame> games = JsonConvert.DeserializeObject<List<DevcadeGame>>(responseBody.Result);
                // TODO: Add error handling if there is no games from the API
                if(games == null || games.Count == 0)
                {
                    Console.WriteLine("Where the games at?");
                }
                return games;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
            return new List<DevcadeGame>();
        }

        public void GetBanner(DevcadeGame game)
        {
            // Path to where the banner image will be saved
            // Making this game.name will name the downloaded image have that name, could set it to anything like id etc..
            string path = $"/tmp/{game.id}Banner.png";

            Console.WriteLine($"Downloading banner for: {game.name}");

            using var client = new HttpClient();
            try
            {
                // Download the image from this uri, save it to the path
                string uri = $"https://{_apiDomain}/api/games/download/banner/{game.id}";
                using Task<Stream> s = client.GetStreamAsync(uri);
                using var fs = new FileStream(path, FileMode.OpenOrCreate);
                s.Result.CopyTo(fs);
            }
            catch(HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }

        private void getBanner(object callback) {
            var game = (DevcadeGame)callback;
            GetBanner(game);
            Menu.instance.notifyTextureAvailable(game.id);
        }

        public void getBannerAsync(DevcadeGame game) {
            ThreadPool.QueueUserWorkItem(getBanner, game);
        }

        // Returns true if success and false otherwise
        // permissions can be an int or a string. For example it can also be +x, -x etc..
        private static void Chmod(string filePath, string permissions = "700", bool recursive = false)
        {
            string cmd = recursive ? $"chmod -R {permissions} {filePath}" : $"chmod {permissions} {filePath}";

            try {
                using Process proc = Process.Start("/bin/bash", $"-c \"{cmd}\"");
                proc?.WaitForExit();
            }
            catch {
                // ignored
            }
        }

        public void startGame(DevcadeGame game) {
            ThreadPool.QueueUserWorkItem(DownloadGame, game);
        }

        private void DownloadGame(object gameObj) {
            var game = (DevcadeGame)gameObj;
            string gameName = game.name.Replace(' ', '_');
            Console.WriteLine($"Game is: {gameName}");
            string path = $"/tmp/{gameName}.zip";
            string URI = $"https://{_apiDomain}/api/games/download/{game.id}";
            Console.WriteLine($"Getting {game.name} from {URI}");
            
            using var client = new HttpClient();
            using Task<Stream> s = client.GetStreamAsync(URI);
            using var fs = new FileStream(path, FileMode.OpenOrCreate);
            s.Result.CopyTo(fs);
            notifyDownloadComplete(game);
        }
        
        private static void notifyDownloadComplete(DevcadeGame game) {
            string gameName = game.name.Replace(' ', '_');
            string path = $"/tmp/{gameName}.zip";
            try {
                Console.WriteLine($"Extracting {path}");
                if (Directory.Exists($"/tmp/{gameName}")) {
                    Directory.Delete($"/tmp/{gameName}", true);
                }
                Directory.CreateDirectory($"/tmp/{gameName}");
                ZipFile.ExtractToDirectory(path, $"/tmp/{gameName}");
            } catch (Exception e) {
                Console.WriteLine($"Error extracting {path}: {e.Message}");
            }

            try {
                string execPath = $"/tmp/{gameName}/publish/{gameName.Replace("_", " ")}";
                Console.WriteLine($"Running {execPath}");
                Chmod(execPath, "+x");
                Process proc = new() {
                    StartInfo = new ProcessStartInfo(execPath) {
                        WindowStyle = ProcessWindowStyle.Normal,
                        WorkingDirectory = Path.GetDirectoryName(execPath) ?? string.Empty,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                    }
                };
                // Redirect stdout and stderr to the console
                proc.OutputDataReceived += (_, args) => Console.WriteLine($"[{game.name}] {args.Data}");
                proc.ErrorDataReceived += (_, args) => Console.WriteLine($"[{game.name}] {args.Data}");
                proc.Start();
                Game1.instance.setActiveProcess(proc);
            } catch (System.ComponentModel.Win32Exception e) {
                Game1.instance.notifyLaunchError(e);
            }
        }
    }
}
