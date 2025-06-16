<script lang="ts">
  import UserFinder from "$lib/components/UserFinder.svelte";
	import PopupAccordion from "$lib/components/popup/PopupAccordion.svelte";
	import UserManager from "$lib/components/UserManager.svelte";
  import Popup from "$lib/components/popup/Popup.svelte";
	import type { Board, CreateBoardRequest, UpdateBoardRequest } from "$lib/types/api/boards";
	import { invalidate } from "$app/navigation";
	import { getContext } from "svelte";
	import type { User } from "$lib/types/api/users";
	import { globalError } from "$lib/stores/ErrorStore";

  let visible: boolean = $state(false);
  let isEditMode: boolean = $state(false);
  let currentBoard: CreateBoardRequest | UpdateBoardRequest = $state({ title: '', members: [] });
  let updatedAt: Date | null = $state(null);
  let currentBoardId: string | null = $state(null);
  let boardNameInvalid = $state(false);
  let deleteButtonVisible = $state(true);

  const me = getContext<User>("user");

  export async function show(board: Board | null = null) {
    boardNameInvalid = false;
    visible = true;
    isEditMode = board !== null;
    if (!isEditMode) {
      currentBoard.title = '';
      currentBoard.members = [];
      currentBoardId = null;
      return;
    }
    if (!board)
      throw new Error("Board cannot be null in edit mode");
    deleteButtonVisible = board.members.some(m => m.userId === me.id && m.role === 'owner');
    currentBoard.title = board.title;
    currentBoard.members = board.members;
    currentBoardId = board.id;
    updatedAt = board.updatedAt;
  }

  export async function onCreate() {
    const res = await fetch('/api/boards', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ title: currentBoard.title, members: currentBoard.members.map(x => ({ userId: x.userId, role: x.role })) } as CreateBoardRequest)
    });
    if (!res.ok) {
      globalError.set(await res.json());
    }
    await invalidate('api:boards');
    visible = false;
  }


  export async function onDelete() {
    const updatedAtStr = updatedAt!.toISOString();
    const res = await fetch(`/api/boards/${currentBoardId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        'If-Match': updatedAtStr
      }
    });
    if (!res.ok) {
      globalError.set(await res.json());
    }
    await invalidate('api:boards');
    visible = false;
  }


  export async function onEdit() {
    const updatedAtStr = updatedAt!.toISOString();
    const res = await fetch(`/api/boards/${currentBoardId}`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        'If-Match': updatedAtStr
      },
      body: JSON.stringify({ title: currentBoard.title, members: currentBoard.members.map(x => ({ userId: x.userId, role: x.role })) } as CreateBoardRequest)
    });
    if (!res.ok) {
      globalError.set(await res.json());
    }
    await invalidate('api:boards');
    visible = false;
  }

  export function onCancel() {
    visible = false;
  }
</script>


<Popup title="board" {isEditMode} {onCreate} {onDelete} {onCancel} {onEdit} {deleteButtonVisible} bind:visible>
  <PopupAccordion
   label="Title"
   name="board-creation"
   ready={currentBoard.title.length != 0}
   invalid={boardNameInvalid && currentBoard.title.length == 0}
   required>
    <input
     type="text"
     class="input w-full bg-background-secondary"
     placeholder="Enter the board name"
     bind:value={currentBoard.title}
     oninvalid={(e) => { e.preventDefault(); boardNameInvalid = true; }}
     required />
  </PopupAccordion>
  
  <PopupAccordion label="Users" name="board-creation" ready={false}>
    <UserFinder onSelect={(user) => {
      if (currentBoard.members.some(m => m.userId === user.userId)) {
        return;
      }
      currentBoard.members.push(user);
      }} blacklist={currentBoard.members.map(x => x.userId)}/>
    <UserManager users={currentBoard.members} onRemove={m => { currentBoard.members = currentBoard.members.filter(u => m.userId != u.userId)}} />
  </PopupAccordion>
</Popup>