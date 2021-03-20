(function () {
    angular
        .module('umbraco')
        .controller('Cogworks.Umbracoflare.Dashboard.Controller', CogworksUmbracoflareDashboardController);

    CogworksUmbracoflareDashboardController.$inject = [
        '$timeout',
        'Cogworks.Umbracoflare.Resources',
        'notificationsService',
        'editorService'
    ];

    function CogworksUmbracoflareDashboardController($timeout, cogworksUmbracoflareResources, notificationsService, editorService) {
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

        vm.dashboard.purgeConfirmationMessage = false;
        vm.dashboard.purgeSiteDone = false;
        vm.dashboard.purgeSiteBusy = false;

        vm.dashboard.purgeStaticBusy = 'purge-static-busy';
        vm.dashboard.purgeStaticSuccess = 'purge-static-success';
        vm.dashboard.purgeUrlsBusy = 'purge-urls-busy';
        vm.dashboard.purgeUrlsSuccess = 'purge-urls-success';

        vm.dashboard.currentDomain = window.location.hostname;
        vm.dashboard.currentDomainIsValid = false;
        vm.dashboard.loading = true;

        getCloudflareStatus();

        function getCloudflareStatus() {
            cogworksUmbracoflareResources.getConfigurationStatus()
                .then(function (configFromServer) {
                    vm.dashboard.newConfig = configFromServer.data;
                    vm.dashboard.currentApiKey = vm.dashboard.newConfig.ApiKey;
                    vm.dashboard.currentAccountEmail = vm.dashboard.newConfig.AccountEmail;
                    vm.dashboard.currentPurgeCacheOn = vm.dashboard.newConfig.PurgeCacheOn;
                    vm.dashboard.credentialsAreValid = vm.dashboard.newConfig.CredentialsAreValid;
                    if (vm.dashboard.credentialsAreValid) {
                        getAllowedDomains();
                    }
                    vm.dashboard.loading = false;
                }, function (error) {
                    vm.dashboard.loading = false;
                });
        }

        function getAllowedDomains() {
            cogworksUmbracoflareResources.getAllowedDomains()
                .then(function (domains) {
                    vm.dashboard.currentDomainIsValid = domains.data.indexOf(vm.dashboard.currentDomain) > -1;
                });
        }

        var refreshStateAfterTime = function () {
            $timeout(function () {
                vm.dashboard.state = '';
                vm.dashboard.updatedAutoPurge = false;
                vm.dashboard.updatedCredentials = false;
                vm.dashboard.purgeSiteDone = false;
                vm.dashboard.purgeSiteBusy = false;
            }, 5000);
        }

        vm.dashboard.updateCredentials = function (isAutoPurgeCall) {
            if (!isAutoPurgeCall) {
                vm.dashboard.updatingCredentials = true;
            }

            vm.dashboard.newConfig.ApiKey = vm.dashboard.currentApiKey;
            vm.dashboard.newConfig.AccountEmail = vm.dashboard.currentAccountEmail;
            vm.dashboard.newConfig.PurgeCacheOn = vm.dashboard.currentPurgeCacheOn;

            cogworksUmbracoflareResources.updateConfigurationStatus(vm.dashboard.newConfig)
                .then(function (configFromServer) {
                    if (configFromServer === null || configFromServer === undefined) {
                        notificationsService.error("We could not update the configuration.");
                    } else if (!configFromServer.data.CredentialsAreValid) {
                        notificationsService.error("We could not validate your credentials.");
                        vm.dashboard.credentialsAreValid = false;
                    } else {
                        notificationsService.success("Successfully updated your configuration!");
                        vm.dashboard.newConfig = configFromServer.data;
                        vm.dashboard.credentialsAreValid = true;
                    }

                    if (isAutoPurgeCall) {
                        vm.dashboard.updatingAutoPurge = false;
                        vm.dashboard.updatedAutoPurge = true;
                    } else {
                        vm.dashboard.updatingCredentials = false;
                        vm.dashboard.updatedCredentials = true;
                    }

                    refreshStateAfterTime();
                    getCloudflareStatus();
                });
        };

        vm.dashboard.togglePurgeCacheOn = function () {
            vm.dashboard.updatingAutoPurge = true;
            vm.dashboard.currentPurgeCacheOn = !vm.dashboard.currentPurgeCacheOn;
            vm.dashboard.updateCredentials(true);
        };

        vm.dashboard.purgeSiteConfirmation = function () {
            vm.dashboard.purgeConfirmationMessage = true;
        }

        vm.dashboard.purgeSiteCancel = function () {
            vm.dashboard.purgeConfirmationMessage = false;
        }

        vm.dashboard.purgeSite = function () {
            vm.dashboard.purgeConfirmationMessage = false;
            vm.dashboard.purgeSiteBusy = true;

            cogworksUmbracoflareResources.purgeAll(vm.dashboard.currentDomain)
                .then(function (statusWithMessage) {
                    vm.dashboard.purgeSiteBusy = false;
                    if (statusWithMessage.data.Success) {
                        notificationsService.success('Purged Cache Successfully!');
                        vm.dashboard.purgeSiteDone = true;
                    } else {
                        notificationsService.error(statusWithMessage.data.Message);
                    }
                }, function (error) {
                    notificationsService.error('Sorry, we could not purge the cache, please check the error logs for details.');
                });

            refreshStateAfterTime();
        }

        vm.dashboard.purgeStaticFiles = function (selectedFiles) {
            debugger;
            vm.dashboard.state = vm.dashboard.purgeStaticBusy;
            cogworksUmbracoflareResources.purgeStaticFiles(selectedFiles, vm.dashboard.currentDomain)
                .then(function (statusWithMessage) {
                    debugger;
                    if (statusWithMessage.data.Success) {
                        vm.dashboard.state = vm.dashboard.purgeStaticSuccess;
                        notificationsService.success(statusWithMessage.data.Message);
                        vm.dashboard.removeSelectedValues();
                    } else {
                        notificationsService.error(statusWithMessage.data.Message);
                    }
                    refreshStateAfterTime();
                }, function (error) {
                    notificationsService.error('Sorry, we could not purge the cache for the selected static files.');
                    refreshStateAfterTime();
                });
        };

        vm.dashboard.openFilePicker = function () {
            debugger;

            var filePicker = {
                title: 'File Picker',
                section: 'settings',
                treeAlias: 'fileSystemTree',
                multiPicker: true,
                select: function (node) {
                    debugger;
                    node.selected = !node.selected;
                    var id = decodeURIComponent(node.id.replace(/\+/g, " "));
                    var index = vm.dashboard.selectedFiles.indexOf(id);

                    if (node.selected) {
                        if (index === -1) {
                            vm.dashboard.selectedFiles.push(id);
                        }
                    } else {
                        vm.dashboard.selectedFiles.splice(index, 1);
                    }

                    console.log(vm.dashboard.selectedFiles);
                    //editorService.close();
                },
                submit: function (data) {
                    console.log(data);
                    //processSelections(data.selection);
                    editorService.close();
                },
                close: function () {
                    editorService.close();
                }
            };

            editorService.treePicker(filePicker);

        };

        vm.dashboard.removeSelectedValues = function (item) {
            debugger;
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

            vm.dashboard.state = vm.dashboard.purgeUrlsBusy;

            cogworksUmbracoflareResources.purgeCacheForUrls(urls, vm.dashboard.currentDomain)
                .then(function (statusWithMessage) {
                    if (statusWithMessage.data.Success) {
                        vm.dashboard.state = vm.dashboard.purgeUrlsSuccess;
                        notificationsService.success(statusWithMessage.data.Message);
                        vm.dashboard.urls = [];
                    } else {
                        notificationsService.error(statusWithMessage.data.Message);
                    }
                    refreshStateAfterTime();
                }, function (error) {
                    notificationsService.error('Sorry, we could not purge the cache for the given urls.');
                    refreshStateAfterTime();
                });
        };
    }
}
)();