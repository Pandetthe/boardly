<script lang="ts">
    import List from "./List.svelte";
	import Card from "./Card.svelte";

    let cardRefs: Record<number, Card> = {};
    export let lists;    
    export let tags;
    export let users;
</script>

<div class="max-w:screen-lg mx-auto flex flex-col gap-5 lg:flex-row">
    {#each lists as list}
        <List
            cardRefs={cardRefs}
            title={list.title}
            color={list.color}
            cards={list.cards}
            tags={tags}
            users={users}
        />
    {/each}
</div>
