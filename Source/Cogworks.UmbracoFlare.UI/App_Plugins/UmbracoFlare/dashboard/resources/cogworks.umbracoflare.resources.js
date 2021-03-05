(function () {
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

        function purgeCacheForUrls(urls, domains) {
            return $http.post(
                API_ROOT + 'PurgeCacheForUrls',
                { Urls: urls, Domains: domains }
            );
        }

        function purgeAll() {
            return $http.post(API_ROOT + 'PurgeAll');
        }

        function purgeStaticFiles(staticFiles, domains) {
            return $http.post(
                API_ROOT + 'PurgeStaticFiles',
                { StaticFiles: staticFiles, SelectedDomains: domains }
            );
        }

        function getConfigurationStatus() {
            return $http.get(API_ROOT + 'GetConfig');
        }

        function purgeCacheForNodeId(nodeId, purgeChildren) {
            return $http.post(
                API_ROOT + 'PurgeCacheForContentNode',
                { nodeId: nodeId, purgeChildren: purgeChildren }
            );
        }

        function updateConfigurationStatus(on) {
            return $http.post(API_ROOT + 'UpdateConfigStatus', { on });
        }

        function getAllowedDomains() {
            return $http.get(API_ROOT + 'GetAllowedDomains');
        }
    }
})();