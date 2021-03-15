﻿(function () {
    'use strict';

    angular
        .module('umbraco.resources')
        .factory('Cogworks.Umbracoflare.Resources', CogworksUmbracoflareResourcess);

    CogworksUmbracoflareResourcess.$inject = ['$http'];

    function CogworksUmbracoflareResourcess($http) {
        var API_ROOT = '/umbraco/backoffice/umbracoFlare/cloudflareUmbracoApi/';

        var service = {
            purgeCacheForUrls: purgeCacheForUrls,
            purgeAll: purgeAll,
            purgeStaticFiles: purgeStaticFiles,
            getConfigurationStatus: getConfigurationStatus,
            purgeCacheForNodeId: purgeCacheForNodeId,
            updateConfigurationStatus: updateConfigurationStatus,
            getAllowedDomains: getAllowedDomains
        };

        return service;

        function purgeCacheForUrls(urls, currentDomain) {
            return $http.post(
                API_ROOT + 'PurgeCacheForUrls',
                { Urls: urls, CurrentDomain: currentDomain }
            );
        }

        function purgeAll(currentDomain) {
            return $http.post(
                API_ROOT + 'PurgeAll/?currentDomain=' + currentDomain
            );
        }

        function purgeStaticFiles(staticFiles, currentDomain) {
            return $http.post(
                API_ROOT + 'PurgeStaticFiles',
                { StaticFiles: staticFiles, CurrentDomain: currentDomain }
            );
        }

        function getConfigurationStatus() {
            return $http.get(API_ROOT + 'GetConfig');
        }

        function purgeCacheForNodeId(nodeId, purgeChildren, currentDomain) {
            return $http.post(
                API_ROOT + 'PurgeCacheForContentNode',
                { nodeId: nodeId, purgeChildren: purgeChildren, CurrentDomain: currentDomain }
            );
        }

        function updateConfigurationStatus(configObject) {
            return $http.post(API_ROOT + 'UpdateConfigStatus',
                { PurgeCacheOn : configObject.PurgeCacheOn, ApiKey: configObject.ApiKey,
                    AccountEmail: configObject.AccountEmail, SelectedDomains: configObject.SelectedDomains });
        }
        
        function getAllowedDomains() {
            return $http.get(API_ROOT + 'GetAllowedDomains');
        }
    }
})();