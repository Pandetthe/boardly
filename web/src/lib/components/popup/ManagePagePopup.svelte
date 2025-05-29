<script lang="ts">
    import PopupAccordion from "$lib/components/popup/PopupAccordion.svelte";
    import Popup from "$lib/components/popup/Popup.svelte";
    import { Check } from "lucide-svelte";

    export let pages: {
        id: number;
        name: string;
        lists: {
            color: string;
            title: string;
        }[];
        tags: {
            color: string;
            title: string;
        }[];
    }[];

    $: visible = false;
    $: isEditMode = false;
    $: currentPageName = "";
    let pageLists: { color: string; title: string; }[] = [];
    let pageTags: { color: string; title: string; }[] = [];

    let currentPageId: number | null = null;

    export function show(id: number|null=null) {
        visible = true;
        isEditMode = id !== null;
        if (!isEditMode) {
            currentPageName = "";
            pageLists = [];
            pageTags = [];
            currentPageId = null;
            return;
        }
        let curr = pages.find((page) => page.id === id);
        currentPageName = curr?.name || "";
        pageLists = curr?.lists || [];
        pageTags = curr?.tags || [];
        currentPageId = id;
    }

    export function onCreate() {
        pages = [...pages, {
            id: Math.random()*1000000,
            name: currentPageName,
            lists: pageLists,
            tags: pageTags
            }];
        visible = false;
    }


    export function onDelete() {
        pages = pages.filter((page) => page.id !== currentPageId);
        visible = false;
    }


    export function onEdit() {
        pages = pages.map((page) => {
            if (page.id === currentPageId) {
                return {
                    ...page,
                    name: currentPageName,
                    lists: pageLists,
                    tags: pageTags
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
    let listColorSelection = "blue";
    let tagColorSelection = "blue";
    let listName = "";
    let tagName = "";

</script>


<Popup title="Page" {isEditMode} {onCreate} {onDelete} {onCancel} {onEdit} bind:visible>
    <PopupAccordion label="Title" name="card-creation" ready={currentPageName.length != 0} required invalid={pageNameInvalid && currentPageName.length == 0}>
      <input type="text" class="input w-full bg-background-secondary" placeholder="Enter the page name" bind:value={currentPageName} required on:invalid|preventDefault={() => pageNameInvalid=true}/>
    </PopupAccordion>

    <PopupAccordion label="Lists" name="card-creation" ready={pageLists.length !== 0} required>
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
                <button class="btn btn-primary join-item" on:click={() => {pageLists = [...pageLists, {color:listColorSelection, title:listName}]}} type="button" >
                    <Check />
                </button>
            </div>
        </div>
    </PopupAccordion>

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
                <button class="btn btn-primary join-item" on:click={() => {pageTags = [...pageTags, {color:tagColorSelection, title:tagName}]}}  type="button" >
                    <Check />
                </button>
            </div>
        </div>
    </PopupAccordion>
</Popup>