import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule, registerExternalWidget } from "@vc-shell/framework";
import { markRaw } from "vue";
import StorePagesWidget from "./widgets/StorePagesWidget.vue";

// The AngularJS module's "Virto Pages" widget on the store blade ("storeDetail")
registerExternalWidget({
  id: "VirtoPagesStore",
  title: "Virto Pages",
  component: markRaw(StorePagesWidget),
  targetBlades: ["StoreDetails"],
  // not on a store that isn't saved yet
  isVisible: (blade) => !!blade?.param,
});

export default defineAppModule({
  blades,
  locales,
});
