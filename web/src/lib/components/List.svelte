<script lang="ts">
    import Sortable from "sortablejs";
    import { onMount } from "svelte";
    import type { ICard } from "$lib/types/api/cards";
    import Card from "$lib/components/Card.svelte";
	import { Plus } from "lucide-svelte";
    import ManageCardPopup from "$lib/components/popup/ManageCardPopup.svelte";
	import { getContext } from 'svelte';
    
    let cards: ICard[];
    export let listId: string;
    export let swimlaneId: string;

    export let boardId: string;
    export let title;
    export let color: string;

    export let cardRefs: {
        [key: string]: {
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
        cards = getContext<ICard[]>('cards').filter(card => card.listId === listId && card.swimlaneId === swimlaneId);

    });
    
    async function process(evt: any) {
        const res = await fetch(`/api/boards/${boardId}/cards/${evt.item.dataset.id}/move`, {
            method: "PATCH",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                listId: evt.to.dataset.id,
                swimlaneId: swimlaneId,
            }),
        });
        if (!res.ok) {
            console.error("Failed to move card");
            return;
        }
        cardRefs[evt.item.dataset.id].process(color);
    }

</script>

<div class="w-full max-w-150 rounded-2xl bg-{color}-bg p-5 h-fit">
    <ManageCardPopup bind:this={popup} pageTags={tags} bind:list={cards} boardId={boardId} listId={listId} swimlaneId={swimlaneId}/>
    <h1 class="font-bold text-{color}">{title}</h1>
    <div class="divider mb-3 mt-0"></div>
    <ul bind:this={list} class="flex flex-col" data-id={listId}>
        {#each cards as card}
            <Card 
                bind:this={cardRefs[card.id]}
                {popup}
                id={card.id}
                color={color}
                title={card.title}
                description={card.description || undefined}
                assignedUsers={card.assignedUsers}
                dueDate={card.dueDate || undefined}
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