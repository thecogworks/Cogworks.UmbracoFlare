using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cogworks.UmbracoFlare.Core.Extensions;
using Umbraco.Core.IO;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;

//test
namespace Cogworks.UmbracoFlare.Core.FileSystemPickerControllers
{
    [PluginController("FileSystemPicker")]
    public class FileSystemPickerApiController : UmbracoAuthorizedJsonController
    {
        public IEnumerable<DirectoryInfo> GetFolders(string folder, string[] filter)
        {
            var path = IOHelper.MapPath("~/" + folder.TrimStart('~', '/'));

            if (!filter.HasValue() || filter[0] == ".")
            {
                return new DirectoryInfo(path).GetDirectories("*");
            }


            var directories = new DirectoryInfo(path).EnumerateDirectories();
            return directories.Where(x => x.EnumerateFiles().Any(f => filter.Contains(f.Extension, StringComparer.OrdinalIgnoreCase)));

        }

        public IEnumerable<FileInfo> GetFiles(string folder, string[] filter)
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

        public IEnumerable<FileInfo> GetFilesIncludingSubDirs(string path)
        {
            var queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                
                try
                {
                    foreach (var subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                FileInfo[] files = null;
                
                try
                {
                    files = new DirectoryInfo(path).GetFiles();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                if (!files.HasValue()) { continue; }

                foreach (var t in files)
                {
                    yield return t;
                }
            }
        }

       
    }
}