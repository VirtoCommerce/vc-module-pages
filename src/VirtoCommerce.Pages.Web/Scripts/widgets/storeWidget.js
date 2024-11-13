angular.module('virtoCommerce.pagesModule')
    .controller('virtoCommerce.pagesModule.storeWidgetController', ['$state', '$scope',
        'virtoCommerce.pagesModule.pagesApi',
        'platformWebApp.bladeNavigationService',
        function ($state, $scope, resources, bladeNavigationService) {
            var blade = $scope.widget.blade;

            $scope.count = '...';

            function initWidget() {
                blade.currentEntityId && resources.count(blade.currentEntityId, function (result) {
                    $scope.count = result.totalCount;
                });
            };

            $scope.openBlade = function () {
                var newBlade = {
                    id: "indexedPagesListBlade",
                    storeId: blade.currentEntityId,
                    headIcon: 'fa fa-file-o',
                    title: 'virto-pages.blades.pages-list.title',
                    titleValues: { name: store.name },
                    subtitle: 'virto-pages.blades.pages-list.subtitle',
                    controller: 'virtoCommerce.pagesModule.pagesListController',
                    template: 'Modules/$(VirtoCommerce.Pages)/Scripts/blades/pages/pages-list.tpl.html',
                };
                bladeNavigationService.showBlade(newBlade, blade);
            };

            initWidget();
        }]);
