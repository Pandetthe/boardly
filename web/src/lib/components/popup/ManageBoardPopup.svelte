<script lang="ts">
    import UserFinder from "$lib/components/UserFinder.svelte";
	import PopupAccordion from "$lib/components/popup/PopupAccordion.svelte";
	import UserManager from "$lib/components/UserManager.svelte";
    import Popup from "$lib/components/popup/Popup.svelte";
	import type { Board, CreateBoardRequest, UpdateBoardRequest } from "$lib/types/api/boards";
	import { invalidate } from "$app/navigation";

    let visible: boolean = $state(false);
    let isEditMode: boolean = $state(false);
    let currentBoard: CreateBoardRequest | UpdateBoardRequest = $state({ title: '', members: [] });
    let currentBoardId: string | null = $state(null);
    let boardNameInvalid = $state(false);

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
        currentBoard.title = board.title;
        currentBoard.members = board.members;
        currentBoardId = board.id;
    }

    export async function onCreate() {
        await fetch('/api/boards', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ title: currentBoard.title, members: currentBoard.members.map(x => ({ userId: x.userId, role: x.role })) } as CreateBoardRequest)
        });
        await invalidate('api:boards');
        visible = false;
    }


    export async function onDelete() {
        await fetch(`/api/boards/${currentBoardId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        await invalidate('api:boards');
        visible = false;
    }


    export async function onEdit() {
        const res = await fetch(`/api/boards/${currentBoardId}`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ title: currentBoard.title, members: currentBoard.members.map(x => ({ userId: x.userId, role: x.role })) } as CreateBoardRequest)
        });
        await invalidate('api:boards');
        visible = false;
    }

    export function onCancel() {
        visible = false;
    }
</script>


<Popup title="board" {isEditMode} {onCreate} {onDelete} {onCancel} {onEdit} bind:visible>
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
        <UserManager users={currentBoard.members} />
    </PopupAccordion>
</Popup>