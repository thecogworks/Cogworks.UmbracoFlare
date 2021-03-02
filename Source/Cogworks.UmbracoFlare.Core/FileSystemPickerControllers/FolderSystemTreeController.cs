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
            var menu = new MenuItemCollection();

            menu.Items.Add(new MenuItem("create", "Create"));

            return menu;
        }

        private TreeNodeCollection AddFiles(string folder, FormDataCollection queryStrings)
        {
            var pickerApiController = new FileSystemPickerApiController();

            if (string.IsNullOrWhiteSpace(folder))
            {
                return null;
            }

            var filter = queryStrings.Get("filter").Split(',').Select(a => a.Trim().EnsureStartsWith(".")).ToArray();

            var path = IOHelper.MapPath(folder);
            var rootPath = IOHelper.MapPath(queryStrings.Get("startfolder"));
            var treeNodeCollection = new TreeNodeCollection();

            foreach (var file in pickerApiController.GetFiles(folder, filter))
            {
                var nodeTitle = file.Name;
                var filePath = file.FullName.Replace(rootPath, "").Replace("\\", "/");
                var treeNode = CreateTreeNode(filePath, path, queryStrings, nodeTitle, "icon-document", false);

                treeNodeCollection.Add(treeNode);
            }

            return treeNodeCollection;
        }

        private TreeNodeCollection AddFolders(string parent, FormDataCollection queryStrings)
        {
            var pickerApiController = new FileSystemPickerApiController();

            var filter = queryStrings.Get("filter").Split(',').Select(a => a.Trim().EnsureStartsWith(".")).ToArray();

            var treeNodeCollection = new TreeNodeCollection();
            treeNodeCollection.AddRange(pickerApiController.GetFolders(parent, filter)
                .Select(dir => CreateTreeNode(dir.FullName.Replace(IOHelper.MapPath("~"), "").Replace("\\", "/"),
                    parent, queryStrings, dir.Name,
                    "icon-folder", filter[0] == "." ? dir.EnumerateDirectories().Any() || pickerApiController.GetFiles(dir.FullName.Replace(IOHelper.MapPath("~"), "").Replace("\\", "/"), filter).Any() : pickerApiController.GetFiles(dir.FullName.Replace(IOHelper.MapPath("~"), "").Replace("\\", "/"), filter).Any())));

            return treeNodeCollection;
        }
    }
}