<script lang="ts">
    import List from "$lib/components/List.svelte";
	import Card from "$lib/components/Card.svelte";

    let cardRefs: Record<number, Card> = {};
    export let lists;    
    export let users;
    export let boardId: string;
</script>

<div class="max-w:screen-lg mx-auto flex flex-col gap-5 lg:flex-row justify-center">
    {#each lists as list}
        <List
            cardRefs={cardRefs}
            title={list.title}
            color={list.color.toLowerCase()}
            cards={list.cards}
            users={users}
            boardId={boardId}
        />
    {/each}
</div>
