(function () {
    angular
        .module('umbraco')
        .controller('Cogworks.Umbracoflare.Filespicker.Controller', CogworksUmbracoflareFilespickerController);

    CogworksUmbracoflareFilespickerController.$inject = [
        '$scope'
    ];

    function CogworksUmbracoflareFilespickerController($scope) {
        var vm = this;

        /////////////////////////////File System Picker Dialog/////////////////////////////////
        //$scope.dialogEventHandler = $({});
        //$scope.dialogEventHandler.bind('treeNodeSelect', nodeSelectHandler);

        //vm.filesPicker = {};
        //vm.filesPicker.selectedValues = [];

        //function nodeSelectHandler(ev, args) {
        //    debugger;
        //    args.event.preventDefault();
        //    args.event.stopPropagation();

        //    var targetDiv = args.element.children('div').first();
        //    var path = buildFullPath(args.node, '');

        //    var indexOfPath = vm.filesPicker.selectedValues.indexOf(path);

        //    if (targetDiv.hasClass('umb-tree-node-checked')) {
        //        //it was already checked, uncheck it and remove it from the array
        //        targetDiv.removeClass('umb-tree-node-checked');

        //        //make sure we are in bounds
        //        if (vm.filesPicker.selectedValues.length - 1 >= indexOfPath) {
        //            vm.filesPicker.selectedValues.splice(indexOfPath, 1);
        //        }
        //    } else {
        //        targetDiv.addClass('umb-tree-node-checked');
        //        vm.filesPicker.selectedValues.push(path);
        //    }
        //};

        //function buildFullPath(node, path) {
        //    debugger;
        //    if (node.parentId === null) {
        //        //we have made it to the top, return
        //        return path;
        //    }

        //    path = '/' + node.name + path;

        //    return buildFullPath(node.parent(), path);
        //}

        //vm.filesPicker.selectFiles = function () {
        //    debugger;
        //    //Crazy how the dialog service works. If you submit, the value is passed back to the callback function that was defined when you opened the modal.
        //    $scope.submit(vm.filesPicker.selectedValues);
        //}
    }
}
)();