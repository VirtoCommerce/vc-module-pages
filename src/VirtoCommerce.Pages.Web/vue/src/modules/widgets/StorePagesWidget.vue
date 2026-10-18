<template>
  <VcWidget
    icon="lucide-files"
    :title="$t('VIRTO_PAGES.WIDGETS.STORE')"
    :value="count"
    :disabled="!storeId"
    @click="open"
  />
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { injectBladeContext, useApiClient, useBlade, useWidgetTrigger } from "@vc-shell/framework";
import { VcWidget } from "@vc-shell/framework/ui";
import { PagesClient } from "../../api_client/virtocommerce.pages";

// the store shared by the Store module's blade via defineBladeContext({ item })
const ctx = injectBladeContext();
const storeId = computed(() => (ctx.value.item as { id?: string } | undefined)?.id);
const { openBlade } = useBlade();
const { getApiClient } = useApiClient(PagesClient);
const count = ref<number | string>("");

async function load() {
  if (!storeId.value) return;
  count.value = (await (await getApiClient()).search({ storeId: storeId.value, take: 0 }).catch(() => undefined))?.totalCount ?? "";
}
watch(storeId, load, { immediate: true });
useWidgetTrigger({ onRefresh: load });

const open = () => openBlade({ name: "VirtoPagesList", param: storeId.value });
</script>
