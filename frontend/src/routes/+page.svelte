<script lang="ts">
	import AddBoardPopup from "$lib/AddBoardPopup.svelte";
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

    $: boards;

    function addBoard(name: string) {
        boards = [
            ...boards,
            {
                id: boards.length + 1,
                name: name,
            }
        ];
    }

    let popup: AddBoardPopup;
</script>


<div class="flex bg-background h-full">
    <AddBoardPopup bind:this={popup} callback={addBoard}/>
    <SideBar />
    <div class="overflow-scroll w-full">
        <div class="w-full p-5 grid grid-cols-[repeat(auto-fill,_minmax(400px,_1fr))] gap-5 h-fit">
            {#each boards as board (board.id)}
                <BoardCard boardName={board.name} boardId={board.id} />
            {/each}
        </div>
    </div>
    <div class="toast">
        <button class="btn btn-xl btn-primary rounded-2xl aspect-square " on:click={() => popup.setVisible(true)}>
            +
        </button>
    </div>
</div>