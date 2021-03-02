(function () {
    angular
        .module('umbraco')
        .controller('Cloudflare.DomainDialog.Controller', CloudflareDomainDialogController);

    CloudflareDomainDialogController.$inject = [
        '$scope',
        'cloudflareResource',
        'notificationsService',
        'dialogService'
    ];

    function CloudflareDomainDialogController($scope, cloudflareResource, notificationsService, dialogService) {
        var vm = this;
        $scope.vm = vm;

        ///////////////////////////// Domain Dialog Controller/////////////////////////////////
        vm.domainDialog = {};

        vm.domainDialog.checkboxWrapper = {
            allSelected: false
        };

        vm.domainDialog.allowedDomains = {};
        vm.domainDialog.selectedDomains = [];
        vm.domainDialog.ignoreSelectedDomainsWatch = true;
        vm.domainDialog.ignoreAllSelectedWatch = true;

        cloudflareResource.getAllowedDomains().success(function (domains) {
            vm.domainDialog.allowedDomains = domains;
        }).error(function (e) {
            notificationsService.error('There was an error getting the umbraco domains.');
        });

        $scope.$watch(
            'vm.domainDialog.selectedDomains',
            function selectedDomainsChanged(newValue, oldValue) {
                if (vm.domainDialog.ignoreSelectedDomainsWatch) {
                    vm.domainDialog.ignoreSelectedDomainsWatch = false;
                    return;
                }
                //See if all of them are selected.
                if (vm.domainDialog.allowedDomains != undefined && vm.domainDialog.allowedDomains.length == vm.domainDialog.selectedDomains.length) {
                    if (!vm.domainDialog.checkboxWrapper.allSelected) {
                        vm.domainDialog.ignoreAllSelectedWatch = true;
                        vm.domainDialog.checkboxWrapper.allSelected = true;
                    }
                } else {
                    if (vm.domainDialog.checkboxWrapper.allSelected) {
                        vm.domainDialog.ignoreAllSelectedWatch = true;
                        vm.domainDialog.checkboxWrapper.allSelected = false;
                    }
                }
            }, true);

        $scope.$watch(
            'vm.domainDialog.checkboxWrapper.allSelected',
            function allSelectedChanged(newValue, oldValue) {
                if (vm.domainDialog.ignoreAllSelectedWatch) {
                    vm.domainDialog.ignoreAllSelectedWatch = false;
                    return;
                }

                var shouldBeSelected = true;
                
                if (!newValue) {
                    shouldBeSelected = false;
                }

                angular.forEach(vm.domainDialog.allowedDomains, function (domain) {
                    var index = vm.domainDialog.selectedDomains.indexOf(domain);
                    if (index >= 0 && !shouldBeSelected) {
                        //It is in the array and should NOT be selected, remove it.
                        vm.domainDialog.selectedDomains.splice(index, 1);
                    } else if (index == -1 && shouldBeSelected) {
                        //It is not in the array and it should be.
                        vm.domainDialog.selectedDomains.push(domain);
                    }
                });
                vm.domainDialog.ignoreSelectedDomainsWatch = true;
            });

        vm.domainDialog.addDomains = function () {
            if (vm.domainDialog.selectedDomains.length > 0) {
                $scope.submit(vm.domainDialog.selectedDomains);
                vm.domainDialog.close();
            }
        };

        vm.domainDialog.close = function () {
            dialogService.close(domainDialog);
        }

        vm.domainDialog.toggleSelected = function (domain) {
            var index = vm.domainDialog.selectedDomains.indexOf(domain);
            if (index >= 0) {
                vm.domainDialog.selectedDomains.splice(index, 1);
            } else {
                vm.domainDialog.selectedDomains.push(domain);
            }
        }

        vm.domainDialog.isChecked = function (domain) {
            return vm.domainDialog.selectedDomains.indexOf(domain) > -1;
        }
    }
}
)();