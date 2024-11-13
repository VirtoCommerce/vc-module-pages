angular.module('virtoCommerce.pagesModule')
    .controller('virtoCommerce.pagesModule.pagesListController',
        ['$scope', '$injector', function ($scope, $injector) {

            // dependencies
            var bladeNavigationService = $injector.get('platformWebApp.bladeNavigationService');
            var bladeUtils = $injector.get('platformWebApp.bladeUtils');
            var dialogService = $injector.get('platformWebApp.dialogService');
            var uiGridHelper = $injector.get('platformWebApp.uiGridHelper');
            var searchApi = $injector.get('virtoCommerce.searchModule.searchIndexation');
            var pagesApi = $injector.get('virtoCommerce.pagesModule.pagesApi');
            var moment = $injector.get('moment');

            var blade = $scope.blade;

            blade.updatePermission = 'pages:update';
            blade.searchKeyword = null;
            blade.currentPage = 0;
            $scope.selectedNodeId = null;
            $scope.listEntries = [];

            blade.refresh = function () {
                blade.isLoading = true;

                // todo: var sort = uiGridHelper.getSortExpression($scope);

                pagesApi.search({
                    storeId: blade.storeId,
                    skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                    take: $scope.pageSettings.itemsPerPageCount,
                    keyword: blade.searchKeyword,
                    // sort: sort,
                }, function (data) {
                    $scope.listEntries = data.results;
                    $scope.pageSettings.totalItems = data.totalCount;
                    blade.isLoading = false;
                });
                // todo: error handling
            };

            $scope.selectNode = function (listItem) {
                openDetailsBlade(listItem);
            };

            $scope.delete = function (data) {
                // todo: implement
            };

            blade.getSelectedRows = function () {
                return $scope.gridApi.selection.getSelectedRows();
            }

            blade.toolbarCommands = [
                {
                    name: "platform.commands.refresh", icon: 'fa fa-refresh',
                    executeMethod: blade.refresh,
                    canExecuteMethod: function () {
                        return true;
                    }
                },
                {
                    name: "platform.commands.delete", icon: 'fa fa-trash-o',
                    executeMethod: function () { onDeleteList($scope.gridApi.selection.getSelectedRows()); },
                    canExecuteMethod: isItemsChecked,
                    permission: 'content:delete'
                }
            ];

            function openDetailsBlade(listItem) {
                $scope.selectedNodeId = listItem.id;
                const doc = {
                    documentType: 'Pages',
                    documentId: listItem.id
                };
                searchApi.getDocIndex(doc, function (data) {

                    var momentFormat = "YYYYMMDDHHmmss";
                    const index = data[0];
                    const modifiedDate = moment.utc(index.modifieddate, momentFormat);

                    const searchBlade = {
                        id: 'virtoPageIndexDetails',
                        currentEntityId: listItem.id,
                        currentEntity: {
                            id: listItem.id,
                            name: listItem.title,
                        },
                        data: index,
                        indexDate: modifiedDate,
                        documentType: 'Pages',
                        controller: 'virtoCommerce.searchModule.indexDetailController',
                        template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/index-detail.tpl.html'
                    };

                    bladeNavigationService.showBlade(searchBlade, blade);
                });
            }

            function onDeleteList(selection) {
                // todo: implement
            }

            function isItemsChecked() {
                return !blade.pasteMode && $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
            }

            // ui-grid
            $scope.setGridOptions = function (gridOptions) {
                uiGridHelper.initialize($scope, gridOptions,
                    function (gridApi) {
                        $scope.gridApi = gridApi;
                        // uiGridHelper.bindRefreshOnSortChanged($scope);
                    });
            };
            bladeUtils.initializePagination($scope, false);

            blade.refresh();
        }]);
