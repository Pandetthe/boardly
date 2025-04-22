<script lang="ts">
	import ManageBoardPopup from "$lib/popup/ManageBoardPopup.svelte";
    import BoardCard from "$lib/BoardCard.svelte";
    import SideBar from "$lib/SideBar.svelte";

    let boards = [
        {
            id: 1,
            name: "Board 1",
        },
        {
            id: 2,
            name: "Board 2",
        },
        {
            id: 3,
            name: "Board 3",
        },
        {
            id: 4,
            name: "Board 4",
        },
        {
            id: 5,
            name: "Board 5",
        }
    ];

    let popup: ManageBoardPopup;

</script>


<div class="flex bg-background h-full">
    <ManageBoardPopup bind:boards={boards} bind:this={popup} />
    <SideBar />

    <div class="overflow-scroll w-full">
        <div class="w-full p-5 grid grid-cols-[repeat(auto-fill,_minmax(400px,_1fr))] gap-5 h-fit">
            {#each boards as board (board.id)}
                <BoardCard boardName={board.name} boardId={board.id} {popup}/>
            {/each}
        </div>
    </div>

    <div class="toast">
        <button class="btn btn-xl btn-primary rounded-2xl aspect-square " on:click={() => popup.show()}>
            +
        </button>
    </div>
</div>