<script lang="ts">
	import ManageBoardPopup from "$lib/components/popup/ManageBoardPopup.svelte";
    import BoardCard from "$lib/components/BoardCard.svelte";
    import type { PageProps } from './$types';
	import { onMount } from "svelte";
	import { invalidate } from "$app/navigation";
	import { BoardRole } from "$lib/types/api/members";
    import { setContext } from "svelte";
	import { Plus } from "lucide-svelte";

    let popup: ManageBoardPopup | undefined = $state(undefined);
	let { data }: PageProps = $props();

    onMount(() => {
		const interval = setInterval(() => {
			invalidate('api:boards');
		}, 60_000);

		return () => clearInterval(interval); 
	});

    setContext('user', data.user);
</script>

<svelte:head>
    <title>Boardly</title> 
</svelte:head>

<ManageBoardPopup bind:this={popup} />

<div class="overflow-auto w-full">
    <div class="w-full p-5 grid grid-cols-[repeat(auto-fill,_minmax(400px,_1fr))] gap-5 h-fit">
        {#if data && data.boards && data.user}
            {#each data.boards.sort((a, b) => a.title.localeCompare(b.title)) as board (board.id)}
                <BoardCard board={board} popup={popup} editEnabled={board.members.some(u => u.userId == data.user.id && (u.role == BoardRole.Owner || u.role == BoardRole.Admin))}/>
            {/each}
        {/if}
    </div>
</div>

<div class="toast">
    <button class="btn btn-xl bg-blue-bg rounded-2xl aspect-square p-4 text-blue" onclick={() => popup?.show()}>
        <Plus class="w-12 h-12"/>
    </button>
</div>