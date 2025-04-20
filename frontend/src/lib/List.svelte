<script lang="ts">
    import AddCardPopup from "$lib/AddCardPopup.svelte";
    import Sortable from "sortablejs";
    import { onMount } from "svelte";
    import Card from "$lib/Card.svelte";

    export let title;
    export let color;
    export let cards = [];
    export let cardRefs;
    export let tags;

    let list;
    let popup;

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
    
    function process(evt) {
        cardRefs[evt.item.dataset.id].process(color);
    }

    function addCard(title, description) {
        const newCard = {
            id: cardRefs.length + 1,
            title: title,
            description: description,
        };
        cards = [...cards, newCard];
        cardRefs[cardRefs.length] = newCard;
    }

    function showPopup() {
        popup.setVisible(true);
    }
</script>

<div class="w-full rounded-2xl bg-{color}-bg p-5 h-fit">
    <AddCardPopup callback={addCard} bind:this={popup} tags={tags}/>
    <h1 class="font-bold text-{color}">{title}</h1>
    <div class="divider mb-3 mt-0"></div>
    <ul bind:this={list} class="flex flex-col" data-list-id=1>
        {#each cards as card}
            <Card 
                bind:this={cardRefs[card.id]}
                id={card.id}
                color={color}
                title={card.title}
                description={card.description}
                tags={card.tags?.map((tagId: number) => tags.find((tag) => tag.id === tagId))}
                assignedUsers={card.assignedUsers}
                dueDate={card.dueDate}
            />
        {/each}
    </ul>
    <button 
    class="btn btn-dash h-15 w-full nodrag border-{color} text-{color} hover:bg-transparent border-2 rounded-2xl text-2xl"
    on:click={showPopup}
    >
        +
    </button>
</div>