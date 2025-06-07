
<script lang="ts">
    import { User, Clock, Menu } from 'lucide-svelte';
    import { marked } from "marked";

    export let title;
    export let id;
    export let color: string;
    export let description = "";
    export let tags: {
        id: string;
        title: string;
        color: string;
    }[];
    export let dueDate = "";
    export let assignedUsers: {
        nickname: string;
        id: string;
    }[] = [];

    $: description;

    export let popup;

    $: color;

    export function process(newColor: string) {
        color = newColor;
    }

</script>

<li class="border-2 border-{color} bg-component rounded-xl mb-5 drop-shadow-2xl drop-shadow-{color}-shadow" data-id={id}>
    <div class="card">
        <div class="flex justify-between items-start p-6">
        <h1 class="card-title text-{color} text-2xl font-black drop-shadow-xl drop-shadow-{color}-shadow">
            {title}
        </h1>
        <button class="btn-xs btn-ghost z-50 aspect-square p-1" aria-label="More options" on:click={() => {popup.show(id)}}>
            <Menu />
        </button>
        </div>
        {#if tags.length > 0}
        <div class="flex pl-6 pr-6 gap-1 flex-wrap">
            {#each tags as tag}
                <span class="badge bg-{tag.color}-bg text-{tag.color} border-{tag.color} drop-shadow-{tag.color}-shadow drop-shadow-xl border-1 font-bold">{tag.title}</span>
            {/each}
        </div>
        {/if}
        {#if assignedUsers.length > 0 || dueDate?.length > 0 || description?.length > 0}
        <div class="card-body">
            {#if dueDate?.length > 0}
            <div>
                <div class="text-text-secondary flex gap-2 items-center h-5"><Clock class="h-full"/> {dueDate}</div>
            </div>
            {/if}
            {#if assignedUsers.length > 0 && assignedUsers[0]}
            <div>
                <div class="text-text-secondary flex gap-2 items-center h-5">
                    <User class="h-full"/>
                    <div class="avatar-group -space-x-4 overflow-visible">
                        {#each assignedUsers as user}
                        <div class="tooltip" data-tip={user.nickname}>
                            <div class="avatar h-8 w-8">
                                    <img src="https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcThmIFs-N4PT0D3gxuDINqgkKWhSxR6sNwJ6g&s" alt={user.nickname}/>
                                </div>
                            </div>
                        {/each}
                    </div>
                </div>
            </div>
            {/if}
            {#if description?.length > 0}
            <div class="mt-5 prose prose-sm">
                {@html marked(description)}
            </div>
            {/if}
        </div>
        {:else if tags.length > 0}
            <div class="h-8"></div>
        {/if}
    </div>
</li>