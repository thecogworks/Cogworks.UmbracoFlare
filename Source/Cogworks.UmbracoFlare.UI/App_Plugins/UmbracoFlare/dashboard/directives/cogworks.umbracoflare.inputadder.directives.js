(function () {
    angular
        .module('umbraco.directives')
        .directive('inputAdder', inputAdderDirective);

    inputAdderDirective.$inject = [
        'notificationsService'
    ];

    function inputAdderDirective(notificationsService) {
        function link(scope) {
            scope.activeInput = '';

            scope.add = function (item) {
                if (item === '') {
                    notificationsService.error('The url field needs to have a value.');
                    return;
                }

                if (!item.startsWith('/')) {
                    notificationsService.error('The url field needs to start with "/".');
                    return;
                }

                if (scope.collection.indexOf(item) === -1) {
                    scope.collection.push(item);
                    scope.activeInput = '';
                } else {
                    notificationsService.error('This url is already in the list.');
                }
            }

            scope.remove = function (item) {
                var index = scope.collection.indexOf(item);
                if (index > -1) {
                    scope.collection.splice(index, 1);
                }
            }
        }

        return {
            restrict: 'E',
            scope: {
                collection: '=ngModel',
                submit: '&onSubmit',
                state: '=state'
            },
            templateUrl: '/App_Plugins/UmbracoFlare/dashboard/views/cogworks.umbracoflare.inputadder.html',
            link: link
        }
    }
}
)();