<script lang="ts">
    import PopupAccordion from "$lib/components/popup/PopupAccordion.svelte";
    import UserFinder from "$lib/components/UserFinder.svelte";
    import UserManager from "$lib/components/UserManager.svelte";
    import Popup from "$lib/components/popup/Popup.svelte";
	import type { ICard } from "$lib/types/api/cards";

    export let pageTags: {
        id: number;
        color: string;
        title: string;
        checked?: boolean;
    }[];

    export let list: ICard[] = [];

    export let boardId: string;

    export let swimlaneId: string;
    export let listId: string;

    $: visible = false;
    $: isEditMode = false;
    $: currentCardName = "";
    $: currentDueDate = "";
    $: currentCardDescription = "";
    let assignedUsers: number[] = [];

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
                tag.checked = curr.tags.includes(tag.id);
            });
        }
        assignedUsers = curr?.assignedUsers || [];
        currentCardDescription = curr?.description || "";
        currentPageId = id;
    }

    export async function onCreate() {
        console.log(swimlaneId, listId);
        const rese = await fetch(`/api/boards/${boardId}/cards`, {
            method: "POST",
            body: JSON.stringify({
                listId: listId,
                swimlaneId: swimlaneId,
                title: currentCardName,
                color: "blue",
                description: currentCardDescription || null,
                tags: pageTags.filter((tag) => tag.checked).map((tag) => tag.id),
                assignedUsers: assignedUsers,
                dueDate: currentDueDate || null,
            }),
            headers: {
                "Content-Type": "application/json",
            }
        });
        if (!rese.ok) {
            console.error("Failed to create card");
            return;
        }
        const res: {id:string} = await rese.json();
        list = [
            ...list,
            {
                id: res.id,
                listId: listId,
                swimlaneId: swimlaneId,
                boardId: boardId,
                title: currentCardName,
                color: "blue",
                description: currentCardDescription || null,
                tags: pageTags.filter((tag) => tag.checked).map((tag) => tag.id),
                assignedUsers: assignedUsers,
                dueDate: currentDueDate || null,
            },
        ];
        visible = false;
    }


    export async function onDelete() {
        await fetch(`/api/boards/${boardId}/cards/${currentPageId}`, {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
            }
        });
        list = list.filter((page) => page.id !== currentPageId);
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
                assignedUsers: assignedUsers,
                dueDate: currentDueDate || null,
            }),
            headers: {
                "Content-Type": "application/json",
            }
        });
        list = list.map((page) => {
            if (page.id === currentPageId) {
                return {
                    ...page,
                    title: currentCardName,
                    color: "blue",
                    description: currentCardDescription,
                    tags: pageTags.filter((tag) => tag.checked).map((tag) => tag.id),
                    assignedUsers: [],
                    dueDate: currentDueDate,
                };
            }
            return page;
        });
        visible = false;
    }

    export function onCancel() {
        visible = false;
    }

    let pageNameInvalid = false;

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
      <UserFinder />
      <UserManager />
    </PopupAccordion>

    <PopupAccordion label="Description" name="card-creation" ready={currentCardDescription.length!=0}>
      <textarea
        class="textarea textarea-bordered h-80 w-full resize-none bg-background-secondary"
        placeholder="Description"
        bind:value={currentCardDescription}
      ></textarea>
    </PopupAccordion>
</Popup>