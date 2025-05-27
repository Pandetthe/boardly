<script lang="ts">
	import ManageBoardPopup from "$lib/popup/ManageBoardPopup.svelte";
    import BoardCard from "$lib/BoardCard.svelte";
    import SideBar from "$lib/SideBar.svelte";
    import type { PageProps } from './$types';

    let popup: ManageBoardPopup;
	let { data }: PageProps = $props();
</script>


<div class="flex bg-background h-full">
    <ManageBoardPopup bind:boards={data.boards} bind:this={popup} />
    <SideBar />

    <div class="overflow-scroll w-full">
        <div class="w-full p-5 grid grid-cols-[repeat(auto-fill,_minmax(400px,_1fr))] gap-5 h-fit">
            {#each data.boards as board (board.id)}
                <BoardCard boardTitle={board.title} boardId={board.id} {popup}/>
            {/each}
        </div>
    </div>

    <div class="toast">
        <button class="btn btn-xl btn-primary rounded-2xl aspect-square " on:click={() => popup.show()}>
            +
        </button>
    </div>
</div>