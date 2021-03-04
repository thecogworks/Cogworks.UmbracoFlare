(function () {
    angular
        .module('umbraco')
        .controller('Cloudflare.Dashboard.Controller', CloudflareDashboardController);

    CloudflareDashboardController.$inject = [
        '$timeout',
        'cloudflareResource',
        'notificationsService',
        'dialogService',
        'modals'
    ];

    function CloudflareDashboardController($timeout, cloudflareResource, notificationsService, dialogService, modals) {
        var vm = this;

        /////////////////////////////Dashboard/////////////////////////////////
        vm.dashboard = {};
        vm.dashboard.credentialsAreValid = false;
        vm.dashboard.state = '';
        vm.dashboard.urls = [];
        vm.dashboard.selectedFiles = [];
        vm.dashboard.newConfig = {};
        vm.dashboard.currentApiKey = '';
        vm.dashboard.currentAccountEmail = '';
        vm.dashboard.currentPurgeCacheOn = false;
        vm.dashboard.credentialsAreValid = false;
        vm.dashboard.updatingCredentials = false;
        vm.dashboard.updatedCredentials = false;
        vm.dashboard.updatedAutoPurge = false;
        vm.dashboard.updatingAutoPurge = false;
        vm.dashboard.allowedDomains = {};
        vm.dashboard.selectedDomains = [];

        vm.dashboard.purgeStaticBusy = 'purge-static-busy';
        vm.dashboard.purgeStaticSuccess = 'purge-static-success';
        vm.dashboard.purgeUrlsBusy = 'purge-urls-busy';
        vm.dashboard.purgeUrlsSuccess = 'purge-urls-success';

        getCloudflareStatus();

        function getCloudflareStatus() {
            cloudflareResource.getConfigurationStatus()
                .success(function (configFromServer) {
                    vm.dashboard.newConfig = configFromServer;
                    vm.dashboard.currentApiKey = vm.dashboard.newConfig.ApiKey;
                    vm.dashboard.currentAccountEmail = vm.dashboard.newConfig.AccountEmail;
                    vm.dashboard.currentPurgeCacheOn = vm.dashboard.newConfig.PurgeCacheOn;
                    vm.dashboard.credentialsAreValid = vm.dashboard.newConfig.CredentialsAreValid;
                    vm.dashboard.selectedDomains = vm.dashboard.newConfig.SelectedDomains;

                    if (vm.dashboard.credentialsAreValid) {
                        getAllowedDomains();
                    }
                });
        }

        function getAllowedDomains() {
            cloudflareResource.getAllowedDomains()
                .success(function (domains) {
                    vm.dashboard.allowedDomains = domains;
                });
        }

        var refreshStateAfterTime = function () {
            $timeout(function () {
                vm.dashboard.state = '';
                vm.dashboard.updatedAutoPurge = false;
                vm.dashboard.updatedCredentials = false;
            }, 5000);
        }
        
        vm.dashboard.updateCredentials = function (autoPurge) {
            if (!autoPurge) {
                vm.dashboard.updatingCredentials = true;
            }

            vm.dashboard.newConfig.ApiKey = vm.dashboard.currentApiKey;
            vm.dashboard.newConfig.AccountEmail = vm.dashboard.currentAccountEmail;
            vm.dashboard.newConfig.PurgeCacheOn = vm.dashboard.currentPurgeCacheOn;
            vm.dashboard.newConfig.SelectedDomains = vm.dashboard.selectedDomains;

            cloudflareResource.updateConfigurationStatus(vm.dashboard.newConfig)
                .success(function (configFromServer) {
                    if (configFromServer === null || configFromServer === undefined) {
                        notificationsService.error("We could not update the configuration.");
                    } else if (!configFromServer.CredentialsAreValid) {
                        notificationsService.error("We could not validate your credentials.");
                        vm.dashboard.credentialsAreValid = false;
                    } else {
                        notificationsService.success("Successfully updated your configuration!");
                        vm.dashboard.newConfig = configFromServer;
                    }

                    if (autoPurge) {
                        vm.dashboard.updatingAutoPurge = false;
                        vm.dashboard.updatedAutoPurge = true;
                    } else {
                        vm.dashboard.updatingCredentials = false;
                        vm.dashboard.updatedCredentials = true;
                    }

                    refreshStateAfterTime();
                });
        };

        vm.dashboard.togglePurgeCacheOn = function () {
            vm.dashboard.updatingAutoPurge = true;
            vm.dashboard.currentPurgeCacheOn = !vm.dashboard.currentPurgeCacheOn;
            vm.dashboard.updateCredentials(true);
        };

        vm.dashboard.openModal = function (type) {
            modals.open(type);
        }

        vm.dashboard.toggleSelectedDomain = function (domain) {
            var index = vm.dashboard.selectedDomains.indexOf(domain);
            if (index >= 0) {
                vm.dashboard.selectedDomains.splice(index, 1);
            } else {
                vm.dashboard.selectedDomains.push(domain);
            }

            vm.dashboard.updateCredentials(false);
        }

        vm.dashboard.isChecked = function (domain) {
            return vm.dashboard.selectedDomains.indexOf(domain) > -1;
        }

        vm.dashboard.purgeSite = function () {
            modals.open('confirmModal').then(function () {
                cloudflareResource.purgeAll().success(function (statusWithMessage) {
                    if (statusWithMessage.Success) {
                        notificationsService.success('Purged Cache Successfully!');
                    } else {
                        notificationsService.error(statusWithMessage.Message);
                    }
                }).error(function () {
                    notificationsService.error('Sorry, we could not purge the cache, please check the error logs for details.');
                });
            });
        }

        vm.dashboard.purgeStaticFiles = function (selectedFiles) {
            if (vm.dashboard.selectedDomains.length > 0) {
                vm.dashboard.state = vm.dashboard.purgeStaticBusy;
                cloudflareResource.purgeStaticFiles(selectedFiles, vm.dashboard.selectedDomains)
                    .success(function (statusWithMessage) {
                        if (statusWithMessage.Success) {
                            vm.dashboard.state = vm.dashboard.purgeStaticSuccess;
                            notificationsService.success(statusWithMessage.Message);
                            vm.dashboard.removeSelectedValues();
                        } else {
                            notificationsService.error(statusWithMessage.Message);
                        }
                        refreshStateAfterTime();
                    }).error(function () {
                        notificationsService.error('Sorry, we could not purge the cache for the selected static files.');
                        refreshStateAfterTime();
                    });
            } else {
                notificationsService.error('Please select domain(s) to purge');
            }
        };

        vm.dashboard.openFilePicker = function () {
            fileSystemPickerTreeDialog = dialogService.open({
                template: '/App_Plugins/UmbracoFlare/backoffice/directiveViews/filesystem-picker-dialog.html',
                callback: function (data) {
                    vm.dashboard.selectedFiles = data;
                }
            });
        };

        vm.dashboard.removeSelectedValues = function (item) {
            var index = vm.dashboard.selectedFiles.indexOf(item);
            if (index !== -1) {
                vm.dashboard.selectedFiles.splice(index, 1);
            } else {
                vm.dashboard.selectedFiles = [];
            }
        };

        vm.dashboard.purgeUrls = function (urls) {
            var noBeginningSlash = false;

            angular.forEach(urls, function (value) {
                if (value.indexOf('/') !== 0) {
                    noBeginningSlash = true;
                }
            });

            if (noBeginningSlash) {
                notificationsService.error('Your urls must begin with /');
                return;
            }

            if (vm.dashboard.selectedDomains.length > 0) {

                vm.dashboard.state = vm.dashboard.purgeUrlsBusy;

                cloudflareResource.purgeCacheForUrls(urls, vm.dashboard.selectedDomains)
                    .success(function (statusWithMessage) {
                    if (statusWithMessage.Success) {
                        vm.dashboard.state = vm.dashboard.purgeUrlsSuccess;
                        notificationsService.success(statusWithMessage.Message);
                        vm.dashboard.urls = [];
                    } else {
                        notificationsService.error(statusWithMessage.Message);
                    }
                    refreshStateAfterTime();
                }).error(function () {
                    notificationsService.error('Sorry, we could not purge the cache for the given urls.');
                    refreshStateAfterTime();
                });
            }else {
                notificationsService.error('Please select domain(s) to purge');
            }

        };
    }
}
)();