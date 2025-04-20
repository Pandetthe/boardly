<script lang="ts">
	import UserFinder from "$lib/UserFinder.svelte";
	import UserManager from "./UserManager.svelte";

    let visible = false;
    let boardName = "";

    export let callback: (name: string) => void;

    export function setVisible(v: boolean) {
        visible = v;
    }

    function quitPopup(isCreated: boolean) {
        setVisible(false);
        if (isCreated) {
            callback(boardName);
        }
    }
</script>


{#if visible}
<div class="fixed top-0 left-0 w-full h-full bg-background/30 flex items-center justify-center z-50 backdrop-blur-xs">
  <div class="bg-component p-4 rounded-2xl shadow-lg w-3/4 max-w-xl gap-5 flex flex-col">
    <h2 class="text-xl font-bold mb-4">Create Board</h2>

    <label class="fieldset-label">
      Board name
    </label>
    <input type="text" class="input w-full" placeholder="Enter the board name" bind:value={boardName}/>
    
    <label class="fieldset-label">
      Add users
    </label>
    <UserFinder />

    <label class="fieldset-label">
      Manage users
    </label>
    <UserManager />

    <div class="flex justify-end mt-4">
      <button class="btn btn-primary mr-2" on:click={() => quitPopup(true)}>Create</button>
      <button class="btn btn-primary btn-outline" on:click={() => quitPopup(false)}>Cancel</button>
    </div>
  </div>
</div>
{/if}