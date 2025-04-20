<script lang="ts">
	import { Check } from "lucide-svelte";
	import UserFinder from "$lib/UserFinder.svelte";
	import UserManager from "$lib/UserManager.svelte";
	import PopupAccordion from "./PopupAccordion.svelte";


  let visible = false;
  let cardTitle = "";
  let cardDescription = "";
  let cardTags = [];
  let cardAssignedUsers = [];
  let cardDueDate = "";

  export let callback: (name: string, description:string) => void;

  export function setVisible(v: boolean) {
      visible = v;
  }

  function quitPopup(isCreated: boolean) {
      setVisible(false);
      if (isCreated) {
          callback(cardTitle, cardDescription);
      }
  }

  export let tags;

</script>


{#if visible}
<div class="fixed top-0 left-0 w-full h-full bg-background/30 flex items-center justify-center z-50 backdrop-blur-xs">
  <div class="bg-background-secondary border-border border-1 p-4 rounded-2xl shadow-lg w-3/4 max-w-xl gap-5 flex flex-col">
    <h2 class="text-xl font-bold mb-4">Create Card</h2>

    <PopupAccordion label="Title" name="card-creation" ready={cardTitle.length != 0} required>
      <input type="text" class="input w-full bg-background-secondary" placeholder="Enter the board name" bind:value={cardTitle} />
    </PopupAccordion>

    <PopupAccordion label="Tags" name="card-creation" ready={false}>
      <div class="flex flex-wrap gap-2">
        {#each tags as tag}
          <input type="checkbox" bind:checked={tag.selected} class="btn bg-{tag.color}-bg checked:border-{tag.color} checked:text-{tag.color} text-text-secondary badge drop-shadow-xl checked:drop-shadow-{tag.color}-shadow border-{tag.color}-bg" aria-label={tag.title} />
        {/each}
      </div>
    </PopupAccordion>

    <PopupAccordion label="Due date" name="card-creation" ready={cardDueDate.length != 0}>
      <input
        type="datetime-local"
        class="input w-full bg-background-secondary"
        placeholder="Enter the due date"
        bind:value={cardDueDate}
      />
    </PopupAccordion>

    <PopupAccordion label="Assign to" name="card-creation" ready={false}>
      <UserFinder />
      <UserManager />
    </PopupAccordion>

    <PopupAccordion label="Description" name="card-creation" ready={cardDescription.length!=0}>
      <textarea
        class="textarea textarea-bordered h-80 w-full resize-none bg-background-secondary"
        placeholder="Description"
        bind:value={cardDescription}
      ></textarea>
    </PopupAccordion>

    <div class="flex justify-end mt-4">
      <button class="btn btn-primary mr-2" on:click={() => quitPopup(true)}>Create</button>
      <button class="btn btn-primary btn-outline" on:click={() => quitPopup(false)}>Cancel</button>
    </div>
  </div>
</div>
{/if}