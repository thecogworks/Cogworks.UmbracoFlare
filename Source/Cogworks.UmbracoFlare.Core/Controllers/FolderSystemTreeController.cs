using System.Linq;
using System.Net.Http.Formatting;
using Cogworks.UmbracoFlare.Core.Helpers;
using Umbraco.Core.IO;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Mvc;
using Umbraco.Web.Trees;

namespace Cogworks.UmbracoFlare.Core.Controllers
{
    [Tree("settings", "fileSystemTree")]
    [PluginController("FileSystemPicker")]
    public class FolderSystemTreeController : TreeController
    {
        protected override TreeNodeCollection GetTreeNodes(string id, FormDataCollection queryStrings)
        {
            var rootPath = id != "-1" ? $"/{id}" : string.Empty;

            var tempTree = AddFolders(rootPath, queryStrings);
            tempTree.AddRange(AddFiles(rootPath, queryStrings));

            return tempTree;
        }

        protected override MenuItemCollection GetMenuForNode(string id, FormDataCollection queryStrings)
        {
            return new MenuItemCollection();
        }

        protected override TreeNode CreateRootNode(FormDataCollection queryStrings)
        {
            var node = CreateTreeNode("-1", null, null, "fileSystemTree");
            node.HasChildren = false;
            node.MenuUrl = null;
            node.Path = null;
            node.CssClasses.Add("hidden");
            
            return node;
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