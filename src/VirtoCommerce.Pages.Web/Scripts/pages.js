var moduleName = "virtoCommerce.pagesModule";

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .run(['platformWebApp.widgetService',
        function (widgetService) {
            widgetService.registerWidget({
                controller: 'virtoCommerce.pagesModule.storeWidgetController',
                template: 'Modules/$(VirtoCommerce.Pages)/Scripts/widgets/storeWidget.tpl.html'
            }, 'storeDetail');
        }]
    );
