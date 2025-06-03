<script lang="ts">
	import ManageBoardPopup from "$lib/components/popup/ManageBoardPopup.svelte";
    import BoardCard from "$lib/components/BoardCard.svelte";
    import type { PageProps } from './$types';
	import { onMount } from "svelte";
	import { invalidate } from "$app/navigation";

    let popup: ManageBoardPopup | undefined = $state(undefined);
	let { data }: PageProps = $props();

    onMount(() => {
		const interval = setInterval(() => {
			invalidate('api:boards');
		}, 60_000);

		return () => clearInterval(interval); 
	});
</script>

<svelte:head>
    <title>Boardly</title> 
</svelte:head>

<ManageBoardPopup bind:this={popup} />

<div class="overflow-auto w-full">
    <div class="w-full p-5 grid grid-cols-[repeat(auto-fill,_minmax(400px,_1fr))] gap-5 h-fit">
        {#if data.boards}
            {#each data.boards.sort((a, b) => a.title.localeCompare(b.title)) as board (board.id)}
                <BoardCard board={board} popup={popup}/>
            {/each}
        {/if}
    </div>
</div>

<div class="toast">
    <button class="btn btn-xl btn-primary rounded-2xl aspect-square " onclick={() => popup?.show()}>
        +
    </button>
</div>