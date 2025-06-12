<script lang="ts">
    import List from "$lib/components/List.svelte";
	import Card from "$lib/components/Card.svelte";

    let cardRefs: Record<number, Card> = {};
    export let lists;    
    export let users;
    export let boardId: string;
    export let swimlaneId: string;
    export let tags;
</script>

<div class="max-w:screen-lg mx-auto flex flex-col gap-5 md:flex-row justify-center overflow-scroll h-full">
    {#each lists as list}
        <List
            cardRefs={cardRefs}
            title={list.title}
            color={list.color.toLowerCase()}
            users={users}
            boardId={boardId}
            swimlaneId={swimlaneId}
            listId={list.id}
            tags={tags}
        />
    {/each}
</div>
