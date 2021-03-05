(function () {
        angular
            .module('umbraco')
            .controller('cogworks.umbracoflare.menu.controller', CogworksUmbracoflareMenuController);

        CogworksUmbracoflareMenuController.$inject = [
            '$scope',
            'cogworksUmbracoflareResource'
        ];

        function CogworksUmbracoflareMenuController($scope, cogworksUmbracoflareResource) {
            var vm = this;

            /////////////////////////////Menu/////////////////////////////////
            vm.menu = {};
            vm.menu.hiddenClass = '-hidden';
            vm.menu.busy = false;
            vm.menu.success = false;
            vm.menu.error = false;
            vm.menu.busyElement = document.getElementById('purge-menu-loader');
            vm.menu.errorElement = document.getElementById('purge-menu-error');
            vm.menu.successElement = document.getElementById('purge-menu-success');
            
            var dialogOptions = $scope.dialogOptions;
            var node = dialogOptions.currentNode;

            vm.menu.purge = function () {
                vm.menu.busy = true;
                vm.menu.busyElement.classList.remove(vm.menu.hiddenClass);
                
                cogworksUmbracoflareResource.purgeCacheForNodeId(node.id, $scope.purgeChildren)
                    .success(function (statusWithMessage) {
                        vm.menu.busy = false;
                        vm.menu.busyElement.classList.add(vm.menu.hiddenClass);

                        if (statusWithMessage.Success) {
                            vm.menu.error = false;
                            vm.menu.errorElement.classList.add(vm.menu.hiddenClass);
                            vm.menu.success = true;
                            vm.menu.successElement.classList.remove(vm.menu.hiddenClass);
                        
                        } else {
                            vm.menu.error = true;
                            vm.menu.errorElement.classList.remove(vm.menu.hiddenClass);
                            vm.menu.success = false;
                            vm.menu.successElement.classList.add(vm.menu.hiddenClass);
                            vm.menu.errorMsg = statusWithMessage.Message === undefined ? "We are sorry, we could not clear the cache at this time." : statusWithMessage.Message;
                        }
                    }).error(function (e) {
                        vm.menu.busy = false;
                        vm.menu.busyElement.classList.add(vm.menu.hiddenClass);
                        vm.menu.success = false;
                        vm.menu.successElement.classList.add(vm.menu.hiddenClass);
                        vm.menu.error = true;
                        vm.menu.errorElement.classList.remove(vm.menu.hiddenClass);
                        vm.menu.errorMessage = "We are sorry, we could not clear the cache at this time.";
                    });
            };
        }
    }
)();