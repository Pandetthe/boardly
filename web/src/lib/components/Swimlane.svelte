<script lang="ts">
  import List from "$lib/components/List.svelte";
	import type { DetailedSwimlane } from "$lib/types/api/swimlanes";

  let { swimlane }: { swimlane: DetailedSwimlane } = $props();
</script>

<div class="max-w:screen-lg mx-auto flex flex-col gap-5 md:flex-row justify-center">
  {#each swimlane.lists as list}
  <List
    list={list}
    swimlaneId={swimlane.id}
    swimlaneTags={swimlane.tags}
  />
  {/each}
</div>
