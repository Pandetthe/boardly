<script lang="ts">
    import PopupAccordion from "$lib/components/popup/PopupAccordion.svelte";
    import UserFinder from "$lib/components/UserFinder.svelte";
    import UserManager from "$lib/components/UserManager.svelte";
    import Popup from "$lib/components/popup/Popup.svelte";
	import type { ICard } from "$lib/types/api/cards";
    import type { Board } from "$lib/types/api/boards";
    import { getContext } from "svelte";
	import type { Readable } from "svelte/store";

    export let pageTags;
    export let list: ICard[];
    export let listId: string;
    export let swimlaneId: string;
    export let boardId: string;

    const board = getContext<Board>("board");

    let visible = false;
    let isEditMode = false;
    let currentCardName = "";
    let currentDueDate = "";
    let currentCardDescription = "";
    let assignedUsers: {id: string, nickname:string}[] = [];


    let currentPageId: string | null = null;

    export function show(id: string | null=null) {
        visible = true;
        isEditMode = id !== null;
        if (!isEditMode) {
            currentCardName = "";
            currentDueDate = "";
            pageTags.forEach((tag) => {
                tag.checked = false;
            });
            currentCardDescription = "";
            currentPageId = null;
            assignedUsers = [];
            return;
        }
        let curr = list.find((page) => page.id === id);
        currentCardName = curr?.title || "";
        currentDueDate = curr?.dueDate || "";
        if (curr?.tags) {
            pageTags.forEach((tag) => {
                tag.checked = curr.tags.find((t) => t.id === tag.id) !== undefined;
            });
        }
        assignedUsers = curr?.assignedUsers || [];
        currentCardDescription = curr?.description || "";
        currentPageId = id;
    }

    export async function onCreate() {
        await fetch(`/api/boards/${boardId}/cards`, {
            method: "POST",
            body: JSON.stringify({
                listId: listId,
                swimlaneId: swimlaneId,
                title: currentCardName,
                color: "blue",
                description: currentCardDescription || null,
                tags: pageTags.filter((tag) => tag.checked).map((tag) => tag.id),
                assignedUsers: assignedUsers.map((user) => user.id),
                dueDate: currentDueDate || null,
            }),
            headers: {
                "Content-Type": "application/json",
            }
        });

        visible = false;
    }


    export async function onDelete() {
        await fetch(`/api/boards/${boardId}/cards/${currentPageId}`, {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
            }
        });
        visible = false;
    }


    export async function onEdit() {
        await fetch(`/api/boards/${boardId}/cards/${currentPageId}`, {
            method: "PATCH",
            body: JSON.stringify({
                title: currentCardName,
                color: "blue",
                description: currentCardDescription || null,
                tags: pageTags.filter((tag) => tag.checked).map((tag) => tag.id),
                assignedUsers: assignedUsers.map((user) => user.id),
                dueDate: currentDueDate || null,
            }),
            headers: {
                "Content-Type": "application/json",
            }
        });
        visible = false;
    }

    export function onCancel() {
        visible = false;
    }

    function addUser(member) {
        if (assignedUsers.some((user) => user.id === member.userId)) {
            return;
        }
        assignedUsers = [...assignedUsers, { id: member.userId, nickname: member.nickname }];
    }

    function removeUser(member) {
        assignedUsers = assignedUsers.filter((user) => user.id !== member.id);
    }

    let pageNameInvalid = false;

    let boardMembers;
    board.subscribe(b => {
        boardMembers = b.members;
    });

</script>

<Popup title="Card" {isEditMode} {onCreate} {onDelete} {onCancel} {onEdit} bind:visible>
    <PopupAccordion label="Title" name="card-creation" ready={currentCardName.length != 0} required invalid={pageNameInvalid && currentCardName.length == 0}>
      <input type="text" class="input w-full bg-background-secondary" placeholder="Enter the board name" bind:value={currentCardName} required on:invalid|preventDefault={() => pageNameInvalid=true}/>
    </PopupAccordion>

    <PopupAccordion label="Tags" name="card-creation" ready={false}>
      <div class="flex flex-wrap gap-2">
        {#each pageTags as tag}
          <input bind:checked={tag.checked} type="checkbox" class="btn bg-{tag.color}-bg checked:border-{tag.color} checked:text-{tag.color} text-text-secondary badge drop-shadow-xl checked:drop-shadow-{tag.color}-shadow border-{tag.color}-bg" aria-label={tag.title} />
        {/each}
      </div>
    </PopupAccordion>

    <PopupAccordion label="Due date" name="card-creation" ready={currentDueDate?.length != 0}>
      <input
        type="datetime-local"
        class="input w-full bg-background-secondary"
        placeholder="Enter the due date"
        bind:value={currentDueDate}
      />
    </PopupAccordion>

    <PopupAccordion label="Assign to" name="card-creation" ready={false}>
    <UserFinder members={boardMembers} showRole={false} onSelect={(member) => addUser(member)} />
      <UserManager users={assignedUsers} onRemove={(member) => removeUser(member)} />
    </PopupAccordion>

    <PopupAccordion label="Description" name="card-creation" ready={currentCardDescription.length!=0}>
      <textarea
        class="textarea textarea-bordered h-80 w-full resize-none bg-background-secondary"
        placeholder="Description"
        bind:value={currentCardDescription}
      ></textarea>
    </PopupAccordion>
</Popup>