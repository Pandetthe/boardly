<script lang="ts">
	import UserFinder from "$lib/UserFinder.svelte";
	import PopupAccordion from "$lib/popup/PopupAccordion.svelte";
	import UserManager from "$lib/UserManager.svelte";
  import Popup from "$lib/popup/Popup.svelte";

  export let boards: {
      id: string;
      name: string;
  }[];

  $: visible = false;
  $: isEditMode = false;
  $: currentBoardName = "";
  let currentBoardId: string | null = null;

  export function show(id: string | null = null) {
    visible = true;
    isEditMode = id !== null;
    if (!isEditMode) {
        currentBoardName = "";
        currentBoardId = null;
        return;
    }
    currentBoardName = boards.find((board) => board.id === id)?.name || "";
    currentBoardId = id;
  }

  export function onCreate() {
        boards = [
            ...boards,
            {
                id: 'test',
                name: currentBoardName,
            }
        ];
        visible = false;
    }


  export function onDelete() {
      boards = boards.filter((board) => board.id !== currentBoardId);
      visible = false;
  }


  export function onEdit() {
      boards = boards.map((board) => {
          if (board.id === currentBoardId) {
              return { id: currentBoardId, name: currentBoardName };
          }
          return board;
      });
      visible = false;
  }

  export function onCancel() {
      visible = false;
  }

  let boardNameInvalid = false;

</script>


<Popup title="Board" {isEditMode} {onCreate} {onDelete} {onCancel} {onEdit} bind:visible>
    <PopupAccordion label="Title" name="board-creation" ready={currentBoardName.length != 0} invalid={boardNameInvalid && currentBoardName.length == 0} required>
      <input type="text" class="input w-full bg-background-secondary" placeholder="Enter the board name" bind:value={currentBoardName} on:invalid|preventDefault={() => boardNameInvalid = true} required/>
    </PopupAccordion>
    
    <PopupAccordion label="Users" name="board-creation" ready={false}>
      <UserFinder />
      <UserManager />
    </PopupAccordion>
</Popup>