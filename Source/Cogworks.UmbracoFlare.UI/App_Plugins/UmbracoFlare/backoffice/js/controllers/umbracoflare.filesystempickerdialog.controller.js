(function () {
    angular
        .module('umbraco')
        .controller('Umbraco.FileSystemPickerDialogController', UmbracoFileSystemPickerDialogController);

    UmbracoFileSystemPickerDialogController.$inject = [
        '$scope'
    ];

    function UmbracoFileSystemPickerDialogController($scope) {
        var vm = this;

        /////////////////////////////File System Picker Dialog/////////////////////////////////
        $scope.dialogEventHandler = $({});
        $scope.dialogEventHandler.bind('treeNodeSelect', nodeSelectHandler);

        vm.fileSystemPickerDialog = {};
        vm.fileSystemPickerDialog.selectedValues = [];

        function nodeSelectHandler(ev, args) {
            args.event.preventDefault();
            args.event.stopPropagation();

            var targetDiv = args.element.children('div').first();
            var path = buildFullPath(args.node, '');

            var indexOfPath = vm.fileSystemPickerDialog.selectedValues.indexOf(path);

            if (targetDiv.hasClass('umb-tree-node-checked')) {
                //it was already checked, uncheck it and remove it from the array
                targetDiv.removeClass('umb-tree-node-checked');

                //make sure we are in bounds
                if (vm.fileSystemPickerDialog.selectedValues.length - 1 >= indexOfPath) {
                    vm.fileSystemPickerDialog.selectedValues.splice(indexOfPath, 1);
                }
            } else {
                targetDiv.addClass('umb-tree-node-checked');
                vm.fileSystemPickerDialog.selectedValues.push(path);
            }
        };

        function buildFullPath(node, path) {
            if (node.parentId === null) {
                //we have made it to the top, return
                return path;
            }

            path = '/' + node.name + path;

            return buildFullPath(node.parent(), path);
        }

        vm.fileSystemPickerDialog.selectFiles = function () {
            //Crazy how the dialog service works. If you submit, the value is passed back to the callback function that was defined when you opened the modal.
            $scope.submit(vm.fileSystemPickerDialog.selectedValues);
        }
    }
}
)();