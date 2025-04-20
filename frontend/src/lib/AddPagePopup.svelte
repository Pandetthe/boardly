<script lang="ts">
    import { Check } from "lucide-svelte";
	import PopupAccordion from "./PopupAccordion.svelte";


  let visible = false;
  let pageName = "";
  $: pageTags = [];
  $: pageLists = [];
  let tagName = "";
  let listName = "";

  export let callback: () => void;

  export function setVisible(v: boolean) {
      visible = v;
  }

  function quitPopup(isCreated: boolean) {
      setVisible(false);
      if (isCreated) {
          callback(pageName, pageLists, pageTags);
      }
  }

  let tagColorSelection = "blue";
let listColorSelection = "blue";
</script>


{#if visible}
<div class="fixed top-0 left-0 w-full h-full bg-background/30 flex items-center justify-center z-50 backdrop-blur-xs">
  <div class="bg-background-secondary border-border border-1 p-4 rounded-2xl shadow-lg w-3/4 max-w-xl gap-5 flex flex-col font-bold">
    <h2 class="text-xl font-bold mb-4">Create Card</h2>

    <PopupAccordion label="Title" name="card-creation" ready={pageName.length != 0} required>
      <input type="text" class="input w-full bg-background-secondary" placeholder="Enter the page name" bind:value={pageName} />
    </PopupAccordion>

    <PopupAccordion label="Lists" name="card-creation" ready={false} required>
        <div class="flex flex-col gap-2">
            {#each pageLists as list}
                <div class="bg-{list.color}-bg border-{list.color} text-{list.color} drop-shadow-xl drop-shadow-{list.color}-shadow w-full p-5">{list.title}</div>
            {/each}
            <div class="join border-border border-1 bg-{listColorSelection}-bg text-{listColorSelection}">
                <input type="text" class="input w-full join-item border-none bg-inherit focus:outline-none" placeholder="Enter the list name" bind:value={listName} />
                <select class="w-fit join-item" bind:value={listColorSelection}>
                    <option value="blue" class="bg-blue-bg text-blue hover:bg-blue hover:text-blue-bg">Blue</option>
                    <option value="green" class="bg-green-bg text-green hover:bg-green hover:text-green-bg">Green</option>
                    <option value="red" class="bg-red-bg text-red hover:bg-red hover:text-red-bg">Red</option>
                    <option value="yellow" class="bg-yellow-bg text-yellow hover:bg-yellow hover:text-yellow-bg">Yellow</option>
                    <option value="purple" class="bg-purple-bg text-purple hover:bg-purple hover:text-purple-bg">Purple</option>
                    <option value="pink" class="bg-pink-bg text-pink hover:bg-pink hover:text-pink-bg">Pink</option>
                    <option value="teal" class="bg-teal-bg text-teal hover:bg-teal hover:text-teal-bg">Teal</option>
                </select>
                <button class="btn btn-primary join-item" on:click={() => {pageLists = [...pageLists, {color:listColorSelection, title:listName}]}} >
                    <Check />
                </button>
            </div>
        </div>
    </PopupAccordion>

    <PopupAccordion label="Tags" name="card-creation" ready={false}>
        <div class="flex flex-col gap-2">
            {#each pageTags as tag}
                <div class="bg-{tag.color}-bg border-{tag.color} text-{tag.color} badge drop-shadow-xl drop-shadow-{tag.color}-shadow">{tag.title}</div>
            {/each}
            <div class="join border-border border-1 bg-{tagColorSelection}-bg text-{tagColorSelection}">
                <input type="text" class="input w-full join-item border-none bg-inherit focus:outline-none" placeholder="Enter the tag name" bind:value={tagName} />
                <select class="w-fit join-item" bind:value={tagColorSelection}>
                    <option value="blue" class="bg-blue-bg text-blue hover:bg-blue hover:text-blue-bg">Blue</option>
                    <option value="green" class="bg-green-bg text-green hover:bg-green hover:text-green-bg">Green</option>
                    <option value="red" class="bg-red-bg text-red hover:bg-red hover:text-red-bg">Red</option>
                    <option value="yellow" class="bg-yellow-bg text-yellow hover:bg-yellow hover:text-yellow-bg">Yellow</option>
                    <option value="purple" class="bg-purple-bg text-purple hover:bg-purple hover:text-purple-bg">Purple</option>
                    <option value="pink" class="bg-pink-bg text-pink hover:bg-pink hover:text-pink-bg">Pink</option>
                    <option value="teal" class="bg-teal-bg text-teal hover:bg-teal hover:text-teal-bg">Teal</option>
                </select>
                <button class="btn btn-primary join-item" on:click={() => {pageTags = [...pageTags, {color:tagColorSelection, title:tagName}]}} >
                    <Check />
                </button>
            </div>
        </div>
    </PopupAccordion>

    <div class="flex justify-end mt-4">
      <button class="btn btn-primary mr-2" on:click={() => quitPopup(true)}>Create</button>
      <button class="btn btn-primary btn-outline" on:click={() => quitPopup(false)}>Cancel</button>
    </div>
  </div>
</div>
{/if}