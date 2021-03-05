using Cogworks.UmbracoFlare.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Core.IO;

namespace Cogworks.UmbracoFlare.Core.Helpers
{
    public static class FilesHelper
    {
        public static IEnumerable<DirectoryInfo> GetFolders(string folder, string[] filter)
        {
            var path = IOHelper.MapPath("~/" + folder.TrimStart('~', '/'));

            if (!filter.HasValue() || filter[0] == ".")
            {
                return new DirectoryInfo(path).GetDirectories("*");
            }

            var directories = new DirectoryInfo(path).EnumerateDirectories();

            return directories.Where(x => x.EnumerateFiles().Any(f => filter.Contains(f.Extension, StringComparer.OrdinalIgnoreCase)));
        }

        public static IEnumerable<FileInfo> GetFiles(string folder, string[] filter)
        {
            var path = IOHelper.MapPath("~/" + folder.TrimStart('~', '/'));
            var directory = new DirectoryInfo(path);
            var files = directory.EnumerateFiles();

            if (filter.HasValue() && filter[0] != ".")
            {
                return files.Where(f => filter.Contains(f.Extension, StringComparer.OrdinalIgnoreCase));
            }

            return new DirectoryInfo(path).GetFiles();
        }

        public static IEnumerable<FileInfo> GetFilesIncludingSubDirs(string path)
        {
            var queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                path = queue.Dequeue();

                foreach (var subDir in Directory.GetDirectories(path))
                {
                    queue.Enqueue(subDir);
                }

                var files = new DirectoryInfo(path).GetFiles();
                if (!files.HasValue()) { continue; }

                foreach (var file in files)
                {
                    yield return file;
                }
            }
        }
    }
}