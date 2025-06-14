<script lang="ts">
  import PopupAccordion from "$lib/components/popup/PopupAccordion.svelte";
  import UserFinder from "$lib/components/UserFinder.svelte";
  import UserManager from "$lib/components/UserManager.svelte";
  import Popup from "$lib/components/popup/Popup.svelte";
	import type { Card, CreateCardRequest, UpdateCardRequest } from "$lib/types/api/cards";
  import type { Board, DetailedBoard } from "$lib/types/api/boards";
  import { getContext } from "svelte";
	import type { Tag } from "$lib/types/api/tags";
	import { get, writable, type Writable } from "svelte/store";
	import type { Member } from "$lib/types/api/members";
	import { on } from "events";

  const { swimlaneTags, listId, swimlaneId }: { swimlaneTags: Tag[]; listId: string; swimlaneId: string; } = $props();
  let currentCard: CreateCardRequest | UpdateCardRequest = $state({ title: '', description: null, tags: [], assignedUsers: [], dueDate: null });
  let currentCardId = $state<string | null>(null);
  let swimlaneTagsObjects = writable(swimlaneTags.map(t => ({ ...t, checked: false })));
  const board = getContext<Writable<DetailedBoard>>("board");
  const signalRConnection = getContext<Writable<signalR.HubConnection | undefined>>("signalRConnection");
  let visible = $state(false);
  let isEditMode = $state(false);
  let updatedAt: Date | null = null;

  export function show(card: Card | null=null) {
    visible = true;
    isEditMode = card !== null;
    if (!isEditMode) {
      currentCard.title = '';
      currentCard.description = null;
      currentCard.tags = [];
      currentCard.assignedUsers = [];
      currentCard.dueDate = null;
      currentCardId = null;
      swimlaneTagsObjects.update(tags => tags.map(tag => ({ ...tag, checked: false })));
      return;
    }
    if (!card)
      throw new Error("Board cannot be null in edit mode");
    currentCardId = card.id;
    currentCard.title = card.title;
    currentCard.dueDate = card.dueDate?.toISOString() ?? null;
    updatedAt = card.updatedAt;
    if (card.tags) {
      swimlaneTagsObjects.update(tags => tags.map(tag => ({ ...tag, checked: card.tags.some(t => t.id === tag.id) })));
    }
    currentCard.tags = card.tags.map(t => t.id);  
    currentCard.assignedUsers = card.assignedUsers.map(u => u.id);
    currentCard.description = card.description;
    const conn = get(signalRConnection);
    if (!conn) {
      console.error("SignalR connection is not established.");
      return;
    }
    conn.invoke("LockCard", card.id);
  }

  export async function onCreate() {
    const conn = get(signalRConnection);
    if (!conn) {
        console.error("SignalR connection is not established.");
        return;
    }
    const payload = {
      listId: listId,
      swimlaneId: swimlaneId,
      ...currentCard,
    } satisfies CreateCardRequest;
    conn.invoke("CreateCard", payload);
    visible = false;
  }


  export async function onDelete() {
    const conn = get(signalRConnection);
    if (!conn) {
        console.error("SignalR connection is not established.");
        return;
    }
    conn.invoke("DeleteCard", currentCardId, updatedAt);
    visible = false;
  }


  export async function onEdit() {
    const conn = get(signalRConnection);
    if (!conn) {
        console.error("SignalR connection is not established.");
        return;
    }
    conn.invoke("UpdateCard", currentCardId, currentCard, updatedAt);
    visible = false;
  }

  export function onCancel() {
    if (isEditMode) {
      const conn = get(signalRConnection);
      if (!conn) {
        console.error("SignalR connection is not established.");
        return;
      }
      conn.invoke("UnlockCard", currentCardId);
    }
    visible = false;
  }

  function onChangeTag(tag: Tag & { checked: boolean }) {
    return (e: Event) => {
      const target = e.target as HTMLInputElement;
      if (target.checked) {
        if (!currentCard.tags) {
          currentCard.tags = [];
        }
        currentCard.tags?.push(tag.id);
      } else {
        currentCard.tags = currentCard.tags?.filter(t => t !== tag.id) ?? [];
      }
    };
  }

  function addUser(member: Member) {
    if (currentCard.assignedUsers?.some(u => u === member.userId)) {
      return;
    }
    if (!currentCard.assignedUsers) {
      currentCard.assignedUsers = [];
    }
    currentCard.assignedUsers.push(member.userId);
  }

  function removeUser(member: Member) {
    currentCard.assignedUsers = currentCard.assignedUsers?.filter(u => u !== member.userId) ?? null;
  }

  let titleInvalid = $state(false);
</script>

<Popup title="Card" {isEditMode} {onCreate} {onDelete} {onCancel} {onEdit} bind:visible>
  <PopupAccordion label="Title" name="card-creation" ready={currentCard.title.length != 0} required invalid={titleInvalid && currentCard.title.length == 0}>
    <input type="text" class="input w-full bg-background-secondary" placeholder="Enter the board name" bind:value={currentCard.title} required oninvalid={e => { e.preventDefault(); titleInvalid = true; }}/>
  </PopupAccordion>

  <PopupAccordion label="Tags" name="card-creation" ready={false}>
    <div class="flex flex-wrap gap-2">
      {#each $swimlaneTagsObjects as tag}
      <input bind:checked={tag.checked} onchange={onChangeTag(tag)} type="checkbox" class="btn bg-{tag.color}-bg checked:border-{tag.color} checked:text-{tag.color} text-text-secondary badge drop-shadow-xl checked:drop-shadow-{tag.color}-shadow border-{tag.color}-bg" aria-label={tag.title} />
      {/each}
    </div>
  </PopupAccordion>

  <PopupAccordion label="Due date" name="card-creation" ready={currentCard.dueDate?.length != 0}>
    <input
      type="datetime-local"
      class="input w-full bg-background-secondary"
      placeholder="Enter the due date"
      bind:value={currentCard.dueDate}
    />
  </PopupAccordion>

  <PopupAccordion label="Assign to" name="card-creation" ready={false}>
  <UserFinder members={$board.members} blacklist={currentCard.assignedUsers ?? []} showRole={false} onSelect={(member) => addUser(member)} />
    <UserManager users={$board.members.filter(u => currentCard.assignedUsers?.includes(u.userId))} onRemove={(member) => removeUser(member)} />
  </PopupAccordion>

  <PopupAccordion label="Description" name="card-creation" ready={currentCard.description?.length != 0}>
    <textarea
      class="textarea textarea-bordered h-80 w-full resize-none bg-background-secondary"
      placeholder="Description"
      bind:value={currentCard.description}
    ></textarea>
  </PopupAccordion>
</Popup>