<template>
  <VcBlade
    :title="$t('VIRTO_PAGES.WIDGETS.STORE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <!-- the store's indexed Virto Pages (read-only: pages are published from the page builder);
         a row opens its search index document, as the AngularJS blade did -->
    <VcDataTable
      v-model:search-value="keyword"
      :active-item-id="activeChildParam"
      @row-click="(e: { data: PageDocument }) => openDocument(e.data.id!)"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      :state-key="'STORE_PAGES'"
      :empty-state="{ icon: 'lucide-files', title: $t('VIRTO_PAGES.EMPTY') }"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="permalink"
        :title="$t('VIRTO_PAGES.FIELDS.PERMALINK')"
        always-visible
      />
      <VcColumn
        id="title"
        :title="$t('VIRTO_PAGES.FIELDS.TITLE')"
      />
      <VcColumn
        id="cultureName"
        :title="$t('VIRTO_PAGES.FIELDS.LANGUAGE')"
      />
      <VcColumn
        id="source"
        :title="$t('VIRTO_PAGES.SOURCE')"
      />
      <VcColumn
        id="status"
        :title="$t('VIRTO_PAGES.FIELDS.STATUS')"
        always-visible
      >
        <template #body="{ data: page }">
          <div class="tw-inline-flex">
            <VcStatus :variant="page.status === 'Published' ? 'success' : 'info'">{{ page.status }}</VcStatus>
          </div>
        </template>
      </VcColumn>
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useApiClient, useAsync, useBlade, useDataTablePagination, useFunctions, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { PagesClient, type PageDocument } from "../../api_client/virtocommerce.pages";

defineBlade({
  url: "/virto-pages",
  name: "VirtoPagesList",
});

const { t } = useI18n({ useScope: "global" });
const { param, openBlade, activeChildParam } = useBlade();
const { debounce } = useFunctions();
const { getApiClient } = useApiClient(PagesClient);

const PAGE_SIZE = 20;
const items = ref<PageDocument[]>([]);
const totalCount = ref(0);
const keyword = ref("");
const pagination = useDataTablePagination({ pageSize: PAGE_SIZE, totalCount });

// the Search module's index document blade (its admin-shell plugin)
function openDocument(id: string) {
  openBlade({ name: "SearchIndexDocument", param: id, options: { documentType: "Pages", documentId: id } });
}

const { loading, action: load } = useAsync(async () => {
  const result = await (await getApiClient()).search({ storeId: param.value, keyword: keyword.value || undefined, skip: pagination.skip, take: PAGE_SIZE });
  items.value = result.results ?? [];
  totalCount.value = result.totalCount ?? 0;
});

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("VIRTO_PAGES.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
]);

// from page 2 setPage(1) triggers the skip watcher, which loads
watch(keyword, debounce(() => (pagination.skip ? pagination.setPage(1) : load()), 400));
watch(() => pagination.skip, () => load());
onMounted(load);
</script>
