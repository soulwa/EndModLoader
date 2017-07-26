﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Win32;

namespace EndModLoader
{
    public static class FileSystem
    {
        public static readonly object SteamPath = Registry.CurrentUser.GetValue(@"Software\Valve\Steam\SteamPath");

        private static readonly string[] ModFolders = { "audio", "data", "shaders", "swfs", "textures", "tilemaps" };
        private static FileSystemWatcher Watcher;

        public static IEnumerable<Mod> ReadModFolder(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.zip", SearchOption.TopDirectoryOnly))
            {
                var mod = Mod.FromZip(file);
                if (mod != null) yield return Mod.FromZip(file);
            }
        }

        public static void EnableWatching(
            string path, 
            FileSystemEventHandler addHandler, 
            FileSystemEventHandler removeHandler,
            RenamedEventHandler renameHandler
        ) {
            Watcher = new FileSystemWatcher
            {
                Path = path,
                Filter = "*.zip",
                NotifyFilter = NotifyFilters.LastAccess |
                               NotifyFilters.LastWrite |
                               NotifyFilters.CreationTime |
                               NotifyFilters.FileName
            };

            Watcher.Changed += addHandler;
            Watcher.Deleted += removeHandler;
            Watcher.Renamed += renameHandler;
            Watcher.EnableRaisingEvents = true;
        }

        public static void SetupDir(string path)
        {
             Directory.CreateDirectory(Path.Combine(path, "mods"));
        }

        public static void EnsureDir(string path, string folder)
        {
            var modPath = Path.Combine(path, folder);
            try
            {
                if (!Directory.Exists(modPath) && Directory.Exists(path))
                {
                    Directory.CreateDirectory(modPath);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                throw e;
            }
        }

        public static bool IsGamePathCorrect(string path) =>
            Directory.Exists(path) && File.Exists(Path.Combine(path, MainWindow.ExeName));

        public static void LoadMod(Mod mod, string path)
        {
            using (var zip = ZipFile.Open(mod.ModPath, ZipArchiveMode.Read))
            {
                // Only extracts directories listed in ModFolders to prevent littering the directory
                // and make it easier to delete it all afterwards.
                foreach (var entry in zip.Entries.Where(
                    e => ModFolders.Contains(Path.GetDirectoryName(e.FullName).Split('\\').First())
                    && !string.IsNullOrEmpty(e.Name)
                )) {
                    Directory.CreateDirectory(Path.Combine(path, Path.GetDirectoryName(entry.FullName)));
                    entry.ExtractToFile(Path.Combine(path, entry.FullName));
                }
            }
        }

        public static void UnloadAll(string path)
        {
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                if (ModFolders.Contains(new DirectoryInfo(dir).Name))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
        }

        public static string DefaultGameDirectory()
        {
            if (SteamPath != null)
            {
                return Path.Combine(SteamPath as string, @"steamapps\common\theendisnigh\");
            }
            else
            {
                // Return the users Program Files folder and let them find the directory themselves.
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
        }
    }
}
