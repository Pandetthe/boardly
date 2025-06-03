<script lang="ts">
    import Sortable from "sortablejs";
    import { onMount } from "svelte";
    import Card from "$lib/components/Card.svelte";
	import { Plus } from "lucide-svelte";
    import ManageCardPopup from "$lib/components/popup/ManageCardPopup.svelte";

    export let title;
    export let color: string;
    export let cards: {
        id: number;
        title: string;
        color: string;
        description: string;
        tags: number[];
        assignedUsers: number[];
        dueDate: string;
    }[] = [];
    export let cardRefs: {
        [key: number]: {
            process: (newColor: string) => void;
        };
    } = {};
    export let tags: {
        id: number;
        title: string;
        color: string;
    }[] = [];
    export let users;
    let list: HTMLUListElement;
    let popup: ManageCardPopup;

    onMount(() => {
        Sortable.create(list, {
            group: "shared",
            animation: 150,
            emptyInsertThreshold: 50,
            ghostClass: "ghost",
            dragClass: "drag",
            onAdd: process,
            onMove: (evt) => {
                if (evt.related.classList.contains("nodrag")) {
                    return false;
                }
                return true;
            },
            filter: ".nodrag",
        });
    });
    
    function process(evt: any) {
        cardRefs[evt.item.dataset.id].process(color);
    }

</script>

<div class="w-full max-w-150 rounded-2xl bg-{color}-bg p-5 h-fit">
    <ManageCardPopup bind:this={popup} pageTags={tags} bind:list={cards}/>
    <h1 class="font-bold text-{color}">{title}</h1>
    <div class="divider mb-3 mt-0"></div>
    <ul bind:this={list} class="flex flex-col" data-list-id=1>
        {#each cards as card}
            <Card 
                bind:this={cardRefs[card.id]}
                {popup}
                id={card.id}
                color={color}
                title={card.title}
                description={card.description}
     
                assignedUsers={card.assignedUsers?.map((userId: number) => users.find((user:{ id: number }) => user.id === userId))}
                dueDate={card.dueDate}
            />
        {/each}
    </ul>
    <button 
    class="btn btn-dash h-15 w-full nodrag border-{color} text-{color} hover:bg-transparent border-2 rounded-2xl text-2xl"
    on:click={() => popup.show()}
    >
        <Plus />
    </button>
</div>