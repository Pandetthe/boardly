<script lang="ts">
    import PopupAccordion from "$lib/components/popup/PopupAccordion.svelte";
    import Popup from "$lib/components/popup/Popup.svelte";
    import { Check } from "lucide-svelte";
	import type { CreateSwimlaneRequest, DetailedSwimlane, Swimlane, UpdateSwimlaneRequest } from "$lib/types/api/swimlanes";

    let visible: boolean = $state(false);
    let isEditMode: boolean = $state(false);
    let currentSwimlane: CreateSwimlaneRequest = $state({ title: '', lists: [] });
    let currentSwimlaneId: string | null = $state(null);

    export function show(swimlane: DetailedSwimlane | null = null) {
        visible = true;
        if (!isEditMode) {
            currentSwimlane = { title: '', lists: [] };
            currentSwimlaneId = null;
            return;
        }
        if (!swimlane)
            throw new Error("Swimlane cannot be null in edit mode");
        currentSwimlaneId = swimlane.id
        currentSwimlane = { title: swimlane.title, lists: [] };//swimlane.lists.map(list => ({ title: list.title })) };
    }

    export function onCreate() {

        visible = false;
    }


    export function onDelete() {

        visible = false;
    }


    export function onEdit() {

        visible = false;
    }

    export function onCancel() {
        visible = false;
    }

    let swimlaneTitleInvalid = false;
    let listColorSelection = "blue";
    let tagColorSelection = "blue";
    let listName = "";
    let tagName = "";

    function addList() {
        currentSwimlane.lists = [...currentSwimlane.lists, { title: listName, color: listColorSelection }];
        listName = "";
    }

</script>


<Popup title="swimlane" {isEditMode} {onCreate} {onDelete} {onCancel} {onEdit} bind:visible>
    <PopupAccordion
     label="Title"
     name="card-creation"
     ready={currentSwimlane.title.length != 0}
     required
     invalid={swimlaneTitleInvalid && currentSwimlane.title.length == 0}>
      <input
       type="text"
       class="input w-full bg-background-secondary"
       placeholder="Enter the swimlane name"
       bind:value={currentSwimlane.title}
       required oninvalid="{(e) => { e.preventDefault(); swimlaneTitleInvalid = true; }}" />
    </PopupAccordion>

    <PopupAccordion label="Lists" name="card-creation" ready={currentSwimlane.lists.length !== 0} required>
        <div class="flex flex-col gap-2">
            {#each currentSwimlane.lists as list}
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
                <button class="btn btn-primary join-item" onclick={addList} type="button" >
                    <Check />
                </button>
            </div>
        </div>
    </PopupAccordion>
<!-- 
    <PopupAccordion label="Tags" name="card-creation" ready={pageTags.length != 0}>
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
                <button class="btn btn-primary join-item" onclick={() => {pageTags = [...pageTags, {color:tagColorSelection, title:tagName}]}}  type="button" >
                    <Check />
                </button>
            </div>
        </div>
    </PopupAccordion> -->
</Popup>