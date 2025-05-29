<script lang="ts">
    import UserFinder from "$lib/UserFinder.svelte";
	import PopupAccordion from "$lib/components/popup/PopupAccordion.svelte";
	import UserManager from "$lib/UserManager.svelte";
    import Popup from "$lib/components/popup/Popup.svelte";
	import type { Board, CreateBoardRequest, UpdateBoardRequest } from "$lib/types/api/boards";
	import { invalidate } from "$app/navigation";

    let visible: boolean = $state(false);
    let isEditMode: boolean = $state(false);
    let currentBoard: Omit<Board, 'createdAt' | 'updatedAt'> = $state({ id: '', title: '' });
    let boardNameInvalid = $state(false);

    export function show(board: Board | null = null) {
        boardNameInvalid = false;
        visible = true;
        isEditMode = board !== null;
        if (!isEditMode) {
            currentBoard = { id: '', title: '' };
            return;
        }
        if (!board)
            throw new Error("Board cannot be null in edit mode");
        currentBoard = { id: board.id, title: board.title };
    }

    export async function onCreate() {
        await fetch('/api/boards', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ title: currentBoard.title } as CreateBoardRequest)
        });
        await invalidate('api:boards');
        visible = false;
    }


    export async function onDelete() {
        await fetch(`/api/boards/${currentBoard.id}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        await invalidate('api:boards');
        visible = false;
    }


    export async function onEdit() {
        await fetch(`/api/boards/${currentBoard.id}`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ title: currentBoard.title } as UpdateBoardRequest)
        });
        await invalidate('api:boards');
        visible = false;
    }

    export function onCancel() {
        visible = false;
    }
</script>


<Popup title="Board" {isEditMode} {onCreate} {onDelete} {onCancel} {onEdit} bind:visible>
    <PopupAccordion label="Title" name="board-creation" ready={currentBoard.title.length != 0} invalid={boardNameInvalid && currentBoard.title.length == 0} required>
        <input type="text" class="input w-full bg-background-secondary" placeholder="Enter the board name" bind:value={currentBoard.title} oninvalid={(e) => { e.preventDefault(); boardNameInvalid = true; }} required/>
    </PopupAccordion>
    
    <PopupAccordion label="Users" name="board-creation" ready={false}>
        <UserFinder />
        <UserManager />
    </PopupAccordion>
</Popup>