(function () {
    angular
        .module('umbraco')
        .controller('Cogworks.Umbracoflare.Menu.Controller', CogworksUmbracoflareMenuController);

    CogworksUmbracoflareMenuController.$inject = [
        '$scope',
        'navigationService',
        'Cogworks.Umbracoflare.Resources'
    ];

    function CogworksUmbracoflareMenuController($scope, navigationService, cogworksUmbracoflareResources) {
        var vm = this;

        /////////////////////////////Menu/////////////////////////////////
        vm.menu = {};
        vm.menu.busy = false;
        vm.menu.success = false;
        vm.menu.error = false;
        vm.menu.currentDomain = window.location.hostname;
        vm.menu.includeDescendants = false;

        vm.menu.includeDescendantsToggle = function () {
            vm.menu.includeDescendants = !vm.menu.includeDescendants;
        }

        var purgeSuccess = function (statusWithMessage) {
            vm.menu.busy = false;

            if (statusWithMessage.data.Success) {
                vm.menu.error = false;
                vm.menu.success = true;
            } else {
                vm.menu.error = true;
                vm.menu.success = false;
                vm.menu.errorMsg = statusWithMessage.data.Message === undefined ? "We are sorry, we could not clear the cache at this time." : statusWithMessage.data.Message;
            }
        }

        var purgeError = function (error) {
            vm.menu.busy = false;
            vm.menu.success = false;
            vm.menu.error = true;
            vm.menu.errorMessage = "We are sorry, we could not clear the cache at this time.";
        }

        vm.menu.purgeEverything = function () {
            vm.menu.busy = true;

            cogworksUmbracoflareResources.purgeAll(vm.menu.currentDomain)
                .then(purgeSuccess, purgeError);
        }

        vm.menu.purge = function () {
            vm.menu.busy = true;

            cogworksUmbracoflareResources.purgeCacheForNodeId($scope.currentNode.id, vm.menu.includeDescendants, vm.menu.currentDomain)
                .then(purgeSuccess, purgeError);
        };

        vm.menu.closeDialog = function () {
            navigationService.hideDialog();
        };
    }
}
)();