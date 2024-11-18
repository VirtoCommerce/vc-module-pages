angular.module('virtoCommerce.pagesModule')
    .factory('virtoCommerce.pagesModule.pagesApi', ['$resource', function ($resource) {
        return $resource('api/pages/search', null, {
            count: {
                method: 'POST',
                transformRequest: function (storeId) {
                    return JSON.stringify({
                        storeId: storeId,
                        take: 0,
                    });
                }
            },
            search: { method: 'POST', url: 'api/pages/search' },
            update: { method: 'PUT' }
        });
    }]);
