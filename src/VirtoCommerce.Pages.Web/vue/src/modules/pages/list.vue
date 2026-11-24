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
      v-model:sort-field="sortField"
      v-model:sort-order="sortOrder"
      :active-item-id="activeChildParam"
      @row-click="(e: { data: PageDocument }) => openDocument(e.data.id!)"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      search-bar
      :state-key="STATE_KEY"
      :global-filters="globalFilters"
      :empty-state="{ icon: 'lucide-files', title: $t('VIRTO_PAGES.EMPTY') }"
      @pagination-click="pagination.goToPage"
      @filter="onFilter"
    >
      <VcColumn
        id="permalink"
        :title="$t('VIRTO_PAGES.FIELDS.PERMALINK')"
        always-visible
        sortable
      />
      <VcColumn
        id="title"
        :title="$t('VIRTO_PAGES.FIELDS.TITLE')"
      />
      <VcColumn
        id="cultureName"
        :title="$t('VIRTO_PAGES.FIELDS.LANGUAGE')"
        sortable
      >
        <template #body="{ data: page }">{{ page.cultureName || $t("VIRTO_PAGES.ANY_LANGUAGE") }}</template>
      </VcColumn>
      <VcColumn
        id="status"
        :title="$t('VIRTO_PAGES.FIELDS.STATUS')"
        always-visible
        sortable
      >
        <template #body="{ data: page }">
          <div class="tw-inline-flex">
            <VcStatus :variant="statusVariant(page.status)">{{ statusLabel(page.status) }}</VcStatus>
          </div>
        </template>
      </VcColumn>
      <VcColumn
        id="visibility"
        :title="$t('VIRTO_PAGES.FIELDS.VISIBILITY')"
        sortable
      >
        <template #body="{ data: page }">{{ visibilityLabel(page.visibility) }}</template>
      </VcColumn>
      <VcColumn
        id="startDate"
        :title="$t('VIRTO_PAGES.FIELDS.START_DATE')"
        type="date"
        sortable
      />
      <VcColumn
        id="endDate"
        :title="$t('VIRTO_PAGES.FIELDS.END_DATE')"
        type="date"
        sortable
      />
      <VcColumn
        id="modifiedDate"
        :title="$t('VIRTO_PAGES.FIELDS.MODIFIED')"
        type="datetime"
        sortable
      />
      <VcColumn
        id="source"
        :title="$t('VIRTO_PAGES.SOURCE')"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  useApiClient,
  useAsync,
  useBlade,
  useDataTablePagination,
  useDataTableSort,
  useFunctions,
  type GlobalFilterConfig,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { PagesClient, PageDocumentStatus, type PageDocument } from "../../api_client/virtocommerce.pages";

defineBlade({
  url: "/virto-pages",
  name: "VirtoPagesList",
});

const { t } = useI18n({ useScope: "global" });
const { param, openBlade, activeChildParam } = useBlade();
const { debounce } = useFunctions();
const { getApiClient } = useApiClient(PagesClient);

const PAGE_SIZE = 20;
const STATE_KEY = "STORE_PAGES";
const items = ref<PageDocument[]>([]);
const totalCount = ref(0);
const keyword = ref("");
const pagination = useDataTablePagination({ pageSize: PAGE_SIZE, totalCount });
// editors look for what changed last; every filterable index field sorts (the title is full-text only)
const { sortField, sortOrder, sortExpression } = useDataTableSort({
  stateKey: STATE_KEY,
  initialField: "modifiedDate",
  initialDirection: "DESC",
});

// deleted pages leave the index, so Deleted is never listed
const statuses = Object.values(PageDocumentStatus).filter((s) => s !== PageDocumentStatus.Deleted);
const statusLabel = (status?: string) => (status ? t(`VIRTO_PAGES.STATUSES.${status.toUpperCase()}`) : "");
const visibilityLabel = (visibility?: string) => (visibility ? t(`VIRTO_PAGES.VISIBILITIES.${visibility.toUpperCase()}`) : "");
function statusVariant(status?: string) {
  if (status === PageDocumentStatus.Published) return "success";
  if (status === PageDocumentStatus.Archived || status === PageDocumentStatus.Unpublished) return "warning";
  return "info";
}

// The API filters by one status, and by one moment within the visibility dates (certainDate): an editor checks
// which pages are in their date window now (with Published: what the storefront shows). Its language filter also
// returns the store's default-language and all-language pages, and a signed-in admin always gets both
// visibilities, so neither is a filter here.
const filters = ref<{ status?: PageDocumentStatus; withinDates?: boolean }>({});
// the search bar takes toggle fields, which vc-shell 2.13 does not yet type in GlobalFilterConfig
const globalFilters = computed(
  () =>
    [
      {
        id: "status",
        label: t("VIRTO_PAGES.FIELDS.STATUS"),
        filter: { options: statuses.map((s) => ({ value: s, label: statusLabel(s) })) },
      },
      { id: "withinDates", label: t("VIRTO_PAGES.WITHIN_DATES"), filter: { toggle: true } },
    ] as unknown as GlobalFilterConfig[],
);

function onFilter(event: { filters: Record<string, unknown> }) {
  filters.value = {
    status: (event.filters.status as PageDocumentStatus | undefined) || undefined,
    withinDates: event.filters.withinDates === true || undefined,
  };
}

// the index stores an open start / end as the minimum / maximum date
function openDate(value?: Date | string) {
  if (!value) return undefined;
  const year = new Date(value).getUTCFullYear();
  return year > 1 && year < 9999 ? value : undefined;
}

// the Search module's index document blade (its admin-shell plugin)
function openDocument(id: string) {
  openBlade({ name: "SearchIndexDocument", param: id, options: { documentType: "Pages", documentId: id } });
}

const { loading, action: load } = useAsync(async () => {
  const result = await (await getApiClient()).search({
    storeId: param.value,
    keyword: keyword.value || undefined,
    status: filters.value.status,
    certainDate: filters.value.withinDates ? new Date() : undefined,
    sort: sortExpression.value,
    skip: pagination.skip,
    take: PAGE_SIZE,
  });
  items.value = (result.results ?? []).map((x) => ({ ...x, startDate: openDate(x.startDate), endDate: openDate(x.endDate) }) as PageDocument);
  totalCount.value = result.totalCount ?? 0;
});

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("VIRTO_PAGES.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
]);

// from page 2 setPage(1) triggers the skip watcher, which loads
const firstPage = () => (pagination.skip ? pagination.setPage(1) : load());
watch(keyword, debounce(firstPage, 400));
watch([filters, sortExpression], firstPage);
watch(() => pagination.skip, () => load());
onMounted(load);
</script>
