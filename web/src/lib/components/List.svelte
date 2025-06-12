<script lang="ts">
    import Sortable from "sortablejs";
    import { onMount, onDestroy } from "svelte";
    import type { ICard } from "$lib/types/api/cards";
    import Card from "$lib/components/Card.svelte";
    import { Plus } from "lucide-svelte";
    import ManageCardPopup from "$lib/components/popup/ManageCardPopup.svelte";
    import { getContext } from "svelte";
    import type { Writable } from "svelte/store";
    import { derived } from "svelte/store";
	import { invalidate } from "$app/navigation";
	import { BoardRole } from "$lib/types/api/members";
  
    export let listId: string;
    export let swimlaneId: string;
    export let boardId: string;
    export let title: string;
    export let color: string;
    export let tags: { id: string; title: string; color: string }[];
  
    let list: HTMLUListElement;
    let popup: ManageCardPopup;

    const cardsContext = getContext<Writable<ICard[]>>("cards");

    let filteredCards = derived(cardsContext, ($cardsContext) =>
      $cardsContext
        .filter((card) => card.listId === listId && card.swimlaneId === swimlaneId)
        .sort((a, b) => a.id < b.id ? -1 : 1)
    );
  
    async function process(evt: any) {
      const movedCardId = evt.item.dataset.id;
      const newListId = evt.to.dataset.id;
  
      const res = await fetch(`/api/boards/${boardId}/cards/${movedCardId}/move`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          listId: newListId,
          swimlaneId: swimlaneId,
        }),
      });
  
      if (!res.ok) {
        console.error("Failed to move card");
        return;
      }
    }
  
    onMount(() => {
      const sortable = Sortable.create(list, {
        group: "shared",
        animation: 150,
        emptyInsertThreshold: 50,
        ghostClass: "ghost",
        dragClass: "drag",
        onAdd: process,
        onEnd: (evt) => {
            if (evt.from.dataset.id === evt.to.dataset.id) {
                return;
            }
            evt.item.remove();
            invalidate('api:board');
        },
        filter: ".nodrag",
      });

      onDestroy(() => {
        sortable.destroy();
      });
    });

    const me = getContext("user");
    const board = getContext("board");
  </script>
  
  <div class="w-full max-w-150 rounded-2xl bg-{color}-bg p-5 h-fit min-w-100">
    <ManageCardPopup
      bind:this={popup}
      pageTags={tags}
      bind:list={$filteredCards}
      boardId={boardId}
      listId={listId}
      swimlaneId={swimlaneId}
    />
    <h1 class="font-bold text-{color}">{title}</h1>
    <div class="divider mb-3 mt-0"></div>
    <ul bind:this={list} class="flex flex-col" data-id={listId}>
      {#each $filteredCards as card (card.id)}
        <Card
        {popup}
        id={card.id}
        color={color}
        title={card.title}
        tags={card.tags.map((tag) => tags.find((t) => t.id === tag.id))}
        description={card.description || undefined}
        assignedUsers={card.assignedUsers}
        dueDate={card.dueDate || undefined}
        />
      {/each}
    </ul>
    {#if $board.members.some(member => member.userId === me.id && member.role != BoardRole.Viewer)}
    <button
      class="btn btn-dash h-15 w-full nodrag border-{color} text-{color} hover:bg-transparent border-2 rounded-2xl text-2xl"
      on:click={() => popup.show()}
    >
      <Plus />
    </button>
    {/if}
  </div>
  