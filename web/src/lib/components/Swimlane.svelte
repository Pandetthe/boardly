<script lang="ts">
    import List from "$lib/components/List.svelte";
	import type { DetailedSwimlane } from "$lib/types/api/swimlanes";

    export let swimlane: DetailedSwimlane;
</script>

<div class="max-w:screen-lg mx-auto flex flex-col gap-5 md:flex-row justify-center">
    {#each swimlane.lists as list}
        <List
            title={list.title}
            color={list.color.toLowerCase()}
            swimlaneId={swimlane.id}
            listId={list.id}
            swimlaneTags={swimlane.tags}
            maxWIP={list.maxWIP}
        />
    {/each}
</div>
