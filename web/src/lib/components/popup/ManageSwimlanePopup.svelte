<script lang="ts">
    import PopupAccordion from "$lib/components/popup/PopupAccordion.svelte";
    import Popup from "$lib/components/popup/Popup.svelte";
    import { Check, X } from "lucide-svelte";
    import type { DetailedSwimlane, DetailedSwimlaneRequest, Swimlane, UpdateSwimlaneRequest } from "$lib/types/api/swimlanes";
    import { invalidate } from "$app/navigation";
    
    const { boardId } = $props<{ boardId: string }>();

    let isEditMode: boolean = $state(false);
    let visible: boolean = $state(false);
    let currentSwimlane: DetailedSwimlaneRequest | null = $state(null);
    let currentSwimlaneId: string | null = $state(null);
    let listsToAdd: { title: string, color: string }[] = $state([]);
    let listsToDelete: { id: string, title: string, color: string }[] = $state([]);
    let tagsToAdd: { title: string, color: string }[] = $state([]);
    let tagsToDelete: { id: string, title: string, color: string }[] = $state([]);

    export function show(swimlane: DetailedSwimlaneRequest | null = null) {
        console.log(swimlane);
        isEditMode = swimlane !== null;
        visible = true;
        if (!isEditMode) {
            currentSwimlane = { title: '', lists: [], tags: [] };
            return;
        }
        currentSwimlane = { title: swimlane!.title, lists: swimlane!.lists, tags: swimlane!.tags };
        currentSwimlaneId = swimlane!.id;
    }

    export async function onCreate() {
        if (!currentSwimlane) {
            return;
        }
        currentSwimlane.lists = listsToAdd;
        listsToAdd = [];
        await fetch(`/api/boards/${boardId}/swimlanes`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(currentSwimlane)
        });
        visible = false;
    }


    export async function onDelete() {
        await fetch(`/api/boards/${boardId}/swimlanes/${currentSwimlaneId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(currentSwimlane)
        });
        visible = false;
    }


    export async function onEdit() {
        await fetch(`/api/boards/${boardId}/swimlanes/${currentSwimlaneId}`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(currentSwimlane)
        });
        visible = false;
        
        for (const list of listsToDelete) {
            await fetch(`/api/boards/${boardId}/swimlanes/${currentSwimlaneId}/lists/${list.id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            currentSwimlane!.lists = currentSwimlane!.lists.filter(l => l.id !== list.id);
        }
        for (const list of currentSwimlane!.lists) {
            await fetch(`/api/boards/${boardId}/swimlanes/${currentSwimlaneId}/lists/${list.id}`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(list)
            });
        }
        for (const list of listsToAdd) {
            const res = await fetch(`/api/boards/${boardId}/swimlanes/${currentSwimlaneId}/lists`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(list)
            }).then(res => res.json());
            currentSwimlane!.lists = [...currentSwimlane!.lists, { id: res.id, title: list.title, color: list.color, maxWIP: 0, createdAt: "", updatedAt: "" }];
        }
        for (const tag of tagsToDelete) {
            await fetch(`/api/boards/${boardId}/swimlanes/${currentSwimlaneId}/tags/${tag.id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            currentSwimlane!.tags = currentSwimlane!.tags.filter(t => t.id !== tag.id);
        }
        for (const tag of currentSwimlane!.tags) {
            await fetch(`/api/boards/${boardId}/swimlanes/${currentSwimlaneId}/tags/${tag.id}`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(tag)
            });
        }
        for (const tag of tagsToAdd) {
            const res = await fetch(`/api/boards/${boardId}/swimlanes/${currentSwimlaneId}/tags`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(tag)
            }).then(res => res.json());
            currentSwimlane!.tags = [...currentSwimlane!.tags, { id: res.id, title: tag.title, color: tag.color }];
        }
        listsToAdd = [];
        tagsToAdd = [];
        tagsToDelete = [];
        
        await invalidate('api:boards');
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
        listsToAdd = [...listsToAdd, { title: listName, color: listColorSelection }];
        listName = "";
    }

</script>


<Popup title="swimlane" {onCreate} {onDelete} {onCancel} {onEdit} {isEditMode} bind:visible>
    <PopupAccordion
     label="Title"
     name="card-creation"
     ready={currentSwimlane!.title.length != 0}
     required
     invalid={swimlaneTitleInvalid && currentSwimlane!.title.length == 0}>
      <input
       type="text"
       class="input w-full bg-background-secondary"
       placeholder="Enter the swimlane name"
       bind:value={currentSwimlane!.title}
       required oninvalid="{(e) => { e.preventDefault(); swimlaneTitleInvalid = true; }}" />
    </PopupAccordion>

    <PopupAccordion label="Lists" name="card-creation" ready={currentSwimlane!.lists.length !== 0} required>
        <div class="flex flex-wrap gap-2">
            {#each currentSwimlane!.lists as list}
                <div class="bg-{list.color}-bg border-{list.color} text-{list.color} badge drop-shadow-xl drop-shadow-{list.color}-shadow w-full justify-between h-10">
                    <input type="text" class="bg-transparent" value={list.title} />
                    <div>
                        <select class="w-fit join-item" bind:value={list.color}>
                            <option value="blue" class="bg-blue-bg text-blue hover:bg-blue hover:text-blue-bg">Blue</option>
                            <option value="green" class="bg-green-bg text-green hover:bg-green hover:text-green-bg">Green</option>
                            <option value="red" class="bg-red-bg text-red hover:bg-red hover:text-red-bg">Red</option>
                            <option value="yellow" class="bg-yellow-bg text-yellow hover:bg-yellow hover:text-yellow-bg">Yellow</option>
                            <option value="purple" class="bg-purple-bg text-purple hover:bg-purple hover:text-purple-bg">Purple</option>
                            <option value="pink" class="bg-pink-bg text-pink hover:bg-pink hover:text-pink-bg">Pink</option>
                            <option value="teal" class="bg-teal-bg text-teal hover:bg-teal hover:text-teal-bg">Teal</option>
                        </select>
                        <button onclick={() => { currentSwimlane!.lists = currentSwimlane!.lists.filter(l => l !== list); listsToDelete.push(list)}} class="text-{list.color}">
                            <X class="w-6 h-6"/>
                        </button>
                    </div>
                </div>
            {/each}
            {#each listsToAdd as list}
                <div class="bg-{list.color}-bg border-{list.color} text-{list.color} badge drop-shadow-xl drop-shadow-{list.color}-shadow w-full justify-between h-10">
                    <input type="text" class="bg-transparent" value={list.title} />
                    <div>
                        <select class="w-fit join-item" bind:value={list.color}>
                            <option value="blue" class="bg-blue-bg text-blue hover:bg-blue hover:text-blue-bg">Blue</option>
                            <option value="green" class="bg-green-bg text-green hover:bg-green hover:text-green-bg">Green</option>
                            <option value="red" class="bg-red-bg text-red hover:bg-red hover:text-red-bg">Red</option>
                            <option value="yellow" class="bg-yellow-bg text-yellow hover:bg-yellow hover:text-yellow-bg">Yellow</option>
                            <option value="purple" class="bg-purple-bg text-purple hover:bg-purple hover:text-purple-bg">Purple</option>
                            <option value="pink" class="bg-pink-bg text-pink hover:bg-pink hover:text-pink-bg">Pink</option>
                            <option value="teal" class="bg-teal-bg text-teal hover:bg-teal hover:text-teal-bg">Teal</option>
                        </select>
                        <button onclick={() => { listsToAdd = listsToAdd.filter(l => l != list) } } class="text-{list.color}">
                            <X class="w-6 h-6"/>
                        </button>
                    </div>
                </div>
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

    <PopupAccordion label="Tags" name="card-creation" ready={currentSwimlane!.tags.length != 0 || tagsToAdd.length != 0}>
        <div class="flex flex-col gap-2">
            <div class="flex flex-wrap gap-2">
            {#each currentSwimlane!.tags as tag}
                <div class="bg-{tag.color}-bg border-{tag.color} text-{tag.color} badge drop-shadow-xl drop-shadow-{tag.color}-shadow w-full justify-between h-10">
                    <input type="text" class="bg-transparent" value={tag.title} />
                    <div>
                        <select class="w-fit join-item" bind:value={tag.color}>
                            <option value="blue" class="bg-blue-bg text-blue hover:bg-blue hover:text-blue-bg">Blue</option>
                            <option value="green" class="bg-green-bg text-green hover:bg-green hover:text-green-bg">Green</option>
                            <option value="red" class="bg-red-bg text-red hover:bg-red hover:text-red-bg">Red</option>
                            <option value="yellow" class="bg-yellow-bg text-yellow hover:bg-yellow hover:text-yellow-bg">Yellow</option>
                            <option value="purple" class="bg-purple-bg text-purple hover:bg-purple hover:text-purple-bg">Purple</option>
                            <option value="pink" class="bg-pink-bg text-pink hover:bg-pink hover:text-pink-bg">Pink</option>
                            <option value="teal" class="bg-teal-bg text-teal hover:bg-teal hover:text-teal-bg">Teal</option>
                        </select>
                        <button onclick={() => { currentSwimlane!.tags = currentSwimlane!.tags.filter(t => t !== tag); tagsToDelete.push(tag)}} class="text-{tag.color}">
                            <X class="w-6 h-6"/>
                        </button>
                    </div>
                </div>
            {/each}
            {#each tagsToAdd as tag}
                <div class="bg-{tag.color}-bg border-{tag.color} text-{tag.color} badge drop-shadow-xl drop-shadow-{tag.color}-shadow w-full justify-between h-10">
                    <input type="text" class="bg-transparent" value={tag.title} />
                    <div>
                        <select class="w-fit join-item" bind:value={tag.color}>
                            <option value="blue" class="bg-blue-bg text-blue hover:bg-blue hover:text-blue-bg">Blue</option>
                            <option value="green" class="bg-green-bg text-green hover:bg-green hover:text-green-bg">Green</option>
                            <option value="red" class="bg-red-bg text-red hover:bg-red hover:text-red-bg">Red</option>
                            <option value="yellow" class="bg-yellow-bg text-yellow hover:bg-yellow hover:text-yellow-bg">Yellow</option>
                            <option value="purple" class="bg-purple-bg text-purple hover:bg-purple hover:text-purple-bg">Purple</option>
                            <option value="pink" class="bg-pink-bg text-pink hover:bg-pink hover:text-pink-bg">Pink</option>
                            <option value="teal" class="bg-teal-bg text-teal hover:bg-teal hover:text-teal-bg">Teal</option>
                        </select>
                        <button onclick={() => { tagsToAdd.filter(t => t != tag) }} class="text-{tag.color}">
                            <X class="w-6 h-6"/>
                        </button>
                    </div>
                </div>
            {/each}
            </div>
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
                <button class="btn btn-primary join-item" onclick={() => {tagsToAdd = [...tagsToAdd, {color:tagColorSelection, title:tagName}]; tagName=""}}  type="button" >
                    <Check />
                </button>
            </div>
        </div>
    </PopupAccordion>
</Popup>