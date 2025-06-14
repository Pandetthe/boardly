<script lang="ts">
  import Sortable, { type SortableEvent } from "sortablejs";
  import { onMount, onDestroy } from "svelte";
  import type { Card as CardType } from "$lib/types/api/cards";
  import Card from "$lib/components/Card.svelte";
  import { Plus } from "lucide-svelte";
  import ManageCardPopup from "$lib/components/popup/ManageCardPopup.svelte";
  import { getContext } from "svelte";
  import type { Writable } from "svelte/store";
  import { derived, get } from "svelte/store";
	import { BoardRole } from "$lib/types/api/members";
	import type { Board } from "$lib/types/api/boards";
	import type { Tag } from "$lib/types/api/tags";
	import type { User } from "$lib/types/api/users";
	import type { List as ListType } from "$lib/types/api/lists";
  
  export let list: ListType;
  export let swimlaneId: string;
  export let swimlaneTags: Tag[];

  let htmlList: HTMLUListElement;
  let popup: ManageCardPopup;

  const cardsContext = getContext<Writable<CardType[]>>("cards");
  const signalRConnection = getContext<Writable<signalR.HubConnection | undefined>>("signalRConnection");
  const me = getContext<User>("user");
  const board = getContext<Writable<Board>>("board");
  let filteredCards = derived(cardsContext, ($cardsContext) =>
    $cardsContext
      .filter((card) => card.listId === list.id && card.swimlaneId === swimlaneId)
      .sort((a, b) => a.id < b.id ? -1 : 1)
  );

  const cardsToDelete: { id: string; item: HTMLElement }[] = [];

  cardsContext.subscribe((cards) => {
    const remaining: typeof cardsToDelete = [];

    for (const { id, item } of cardsToDelete) {
      const card = cards.find(card => card.id === id && card.swimlaneId === swimlaneId);
      if (card && card.listId !== list.id) {
        item.remove();
      } else {
        remaining.push({ id, item });
      }
    }

    cardsToDelete.length = 0;
    cardsToDelete.push(...remaining);
  });

  async function process(evt: SortableEvent) {
    const conn = get(signalRConnection);
    if (!conn) {
      console.error("SignalR connection is not established.");
      return;
    }
    const movedCardId = evt.item.dataset.id as string;
    const movedCardUpdatedAt = evt.item.dataset.updatedat as string;
    const newListId = evt.to.dataset.id as string;
    await conn.invoke("MoveCard", swimlaneId, movedCardId, newListId, movedCardUpdatedAt);
  }

  onMount(() => {
    const sortable = Sortable.create(htmlList, {
      group: "shared",
      animation: 150,
      emptyInsertThreshold: 50,
      ghostClass: "ghost",
      dragClass: "drag",
      sort: false,
      onAdd: process,
      onEnd: (evt) => {
          if (evt.from.dataset.id !== evt.to.dataset.id && evt.item.dataset.id) 
            cardsToDelete.push({ id: evt.item.dataset.id, item: evt.item });
      },
      filter: ".nodrag",
    });

    onDestroy(() => {
      sortable.destroy();
    });
  });
</script>

<div class="w-full max-w-150 rounded-2xl bg-{list.color}-bg p-5 h-fit">
  <ManageCardPopup
    bind:this={popup}
    swimlaneTags={swimlaneTags}
    listId={list.id}
    swimlaneId={swimlaneId}
  />
  <h1 class="font-bold text-{list.color}">{list.title}</h1>
  {#if list.maxWIP}
    <p class="text-center text-gray-500">{$filteredCards.length}/{list.maxWIP}</p>
  {/if}
  <div class="divider mb-3 mt-0"></div>
  <ul bind:this={htmlList} class="flex flex-col" data-id={list.id}>
    {#each $filteredCards as card (card.id)}
      <Card
        {popup}
        card={card}
        color={list.color}
      />
    {/each}
  </ul>
  {#if $board.members.some(member => member.userId === me.id && member.role != BoardRole.Viewer) && $filteredCards.length < (list.maxWIP ?? Infinity)}
  <button
    class="btn btn-dash h-15 w-full nodrag border-{list.color} text-{list.color} hover:bg-transparent border-2 rounded-2xl text-2xl"
    onclick={() => popup?.show()}
  >
    <Plus />
  </button>
  {/if}
</div>
  