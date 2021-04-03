using Cogworks.UmbracoFlare.Core.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Core.IO;

namespace Cogworks.UmbracoFlare.Core.Helpers
{
    public static class UmbracoFlareFileHelper
    {
        private static readonly IEnumerable<string> ExcludedPaths = new List<string> { "app_data", "app_browsers", "app_data", "app_code", "app_plugins", "properties", "bin", "config", "media", "obj", "umbraco", "views" };
        private static readonly IEnumerable<string> ExcludedExtensions = new List<string> { ".config", ".asax", ".user", ".nuspec", ".dll", ".pdb", ".lic", ".csproj", ".aspx" };

        public static IEnumerable<DirectoryInfo> GetFolders(string folder)
        {
            var path = IOHelper.MapPath("~/" + folder.TrimStart('~', '/'));
            var directory = new DirectoryInfo(path);

            if (!directory.Exists)
            {
                return Enumerable.Empty<DirectoryInfo>();
            }

            var directories = directory.EnumerateDirectories().Where(x => !ExcludedPaths.Contains(x.Name.ToLowerInvariant())).ToList();
            var allowedDirectories = directories.Where(x => x.EnumerateFiles().Any(f => !ExcludedExtensions.Contains(f.Extension)));

            return allowedDirectories;
        }

        public static IEnumerable<FileInfo> GetFiles(string folder)
        {
            var path = IOHelper.MapPath("~/" + folder.TrimStart('~', '/'));
            var directory = new DirectoryInfo(path);

            if (!directory.Exists)
            {
                return Enumerable.Empty<FileInfo>();
            }

            var files = directory.EnumerateFiles().Where(f => !ExcludedExtensions.Contains(f.Extension));

            return files;
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