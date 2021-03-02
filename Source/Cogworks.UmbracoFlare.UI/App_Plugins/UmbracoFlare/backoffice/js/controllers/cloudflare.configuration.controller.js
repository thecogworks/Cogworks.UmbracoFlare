(function () {
    angular
        .module('umbraco')
        .controller('Cloudflare.Configuration.Controller', CloudflareConfigurationController);

    CloudflareConfigurationController.$inject = [
        '$scope',
        '$timeout',
        'cloudflareResource',
        'notificationsService',
        'modals'
    ];

    function CloudflareConfigurationController($scope, $timeout, cloudflareResource, notificationsService, modals) {
        var vm = this;

        /////////////////////////////Configuration/////////////////////////////////
        vm.configuration = {};
        vm.configuration.newConfig = {};
        vm.configuration.currentApiKey = '';
        vm.configuration.currentAccountEmail = '';
        vm.configuration.currentPurgeCacheOn = false;
        vm.configuration.credentialsAreValid = false;

        vm.configuration.updatingCredentials = false;
        vm.configuration.updatedCredentials = false;
        vm.configuration.updatedAutoPurge = false;
        vm.configuration.updatingAutoPurge = false;

        getCloudflareStatus();

        function getCloudflareStatus() {
            cloudflareResource.getConfigurationStatus()
                .success(function (configFromServer) {
                    vm.configuration.newConfig = configFromServer;
                    vm.configuration.currentApiKey = vm.configuration.newConfig.ApiKey;
                    vm.configuration.currentAccountEmail = vm.configuration.newConfig.AccountEmail;
                    vm.configuration.currentPurgeCacheOn = vm.configuration.newConfig.PurgeCacheOn;
                    vm.configuration.credentialsAreValid = vm.configuration.newConfig.CredentialsAreValid;
                });
        }

        vm.configuration.UpdateCredentials = function (autoPurge) {
            if (!autoPurge) {
                vm.configuration.updatingCredentials = true;
            }

            vm.configuration.newConfig.ApiKey = vm.configuration.currentApiKey;
            vm.configuration.newConfig.AccountEmail = vm.configuration.currentAccountEmail;
            vm.configuration.newConfig.PurgeCacheOn = vm.configuration.currentPurgeCacheOn;

            cloudflareResource.updateConfigurationStatus(vm.configuration.newConfig)
                .success(function (configFromServer) {
                    if (configFromServer === null || configFromServer === undefined) {
                        notificationsService.error("We could not update the configuration.");
                    } else if (!configFromServer.CredentialsAreValid) {
                        notificationsService.error("We could not validate your credentials.");
                        vm.configuration.credentialsAreValid = false;
                    } else {
                        notificationsService.success("Successfully updated your configuration!");
                        vm.configuration.newConfig = configFromServer;
                    }

                    if (autoPurge) {
                        vm.configuration.updatingAutoPurge = false;
                        vm.configuration.updatedAutoPurge = true;
                    } else {
                        vm.configuration.updatingCredentials = false;
                        vm.configuration.updatedCredentials = true;
                    }

                    vm.configuration.refreshStateAfterTime();
                });
        };

        vm.configuration.togglePurgeCacheOn = function () {
            vm.configuration.updatingAutoPurge = true;
            vm.configuration.currentPurgeCacheOn = !vm.configuration.currentPurgeCacheOn;
            vm.configuration.UpdateCredentials(true);
        };

        vm.configuration.openModal = function (type) {
            modals.open(type)
        }

        vm.configuration.refreshStateAfterTime = function () {
            $timeout(function () {
                vm.configuration.updatedAutoPurge = false;
                vm.configuration.updatedCredentials = false;
            }, 5000);
        }
    };
})();