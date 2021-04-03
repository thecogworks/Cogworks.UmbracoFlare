using Cogworks.UmbracoFlare.Core.Helpers;
using System.Linq;
using System.Net.Http.Formatting;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Mvc;
using Umbraco.Web.Trees;

namespace Cogworks.UmbracoFlare.Core.FileSystemPickerControllers
{
    [Tree("dummy", "fileSystemTree", "File System")]
    [PluginController("FileSystemPicker")]
    public class FolderSystemTreeController : TreeController
    {
        protected override TreeNodeCollection GetTreeNodes(string id, FormDataCollection queryStrings)
        {
            var folder = id == "-1" ? queryStrings.Get("startfolder") : id;
            folder = folder.EnsureStartsWith("/");

            var tempTree = AddFolders(folder, queryStrings);
            tempTree.AddRange(AddFiles(folder, queryStrings));

            return tempTree;
        }

        protected override MenuItemCollection GetMenuForNode(string id, FormDataCollection queryStrings)
        {
            return new MenuItemCollection();
        }

        private TreeNodeCollection AddFolders(string parent, FormDataCollection queryStrings)
        {
            var treeNodeCollection = new TreeNodeCollection();
            var rootFolderPath = IOHelper.MapPath("~");
            var folders = UmbracoFlareFileHelper.GetFolders(parent);

            foreach (var folder in folders)
            {
                var folderFullName = folder.FullName.Replace(rootFolderPath, "").Replace("\\", "/");
                var folderHasFiles = folder.EnumerateDirectories().Any() || UmbracoFlareFileHelper.GetFiles(folderFullName).Any();
                var treeNode = CreateTreeNode(folderFullName, parent, queryStrings, folder.Name, "icon-folder", folderHasFiles);

                treeNodeCollection.Add(treeNode);
            }

            return treeNodeCollection;
        }

        private TreeNodeCollection AddFiles(string folder, FormDataCollection queryStrings)
        {
            var path = IOHelper.MapPath(folder);
            var rootPath = IOHelper.MapPath("~");
            var treeNodeCollection = new TreeNodeCollection();
            var files = UmbracoFlareFileHelper.GetFiles(folder);

            foreach (var file in files)
            {
                var nodeTitle = file.Name;
                var filePath = file.FullName.Replace(rootPath, "").Replace("\\", "/");
                var treeNode = CreateTreeNode(filePath, path, queryStrings, nodeTitle, "icon-document", false);

                treeNodeCollection.Add(treeNode);
            }

            return treeNodeCollection;
        }
    }
}