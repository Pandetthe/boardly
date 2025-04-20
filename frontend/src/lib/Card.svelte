
<script>
    import { User, Clock } from 'lucide-svelte';
    import { marked } from "marked";

    export let title;
    export let id;
    export let color;
    export let description = "";
    export let tags = [];
    export let dueDate = "";
    export let assignedUsers = [];

    $: color;

    export function process(newColor) {
        color = newColor;
    }

    let mdDescription = marked(description);

</script>

<li class="border-2 border-{color} bg-component rounded-xl mb-5 drop-shadow-2xl drop-shadow-{color}-shadow" data-id={id}>
    <div class="card">
        <div class="flex justify-between p-6">
        <h1 class="card-title text-{color} text-2xl drop-shadow-xl drop-shadow-{color}-shadow">
        {title}</h1>
        <button class="btn-xs btn-outline z-50 aspect-square p-1" aria-label="More options" on:click={() => {console.log("More options clicked")}}>
            <svg
                viewBox="0 0 16 16"
                xmlns="http://www.w3.org/2000/svg"
                fill="#FFFFFF"
                class="bi bi-three-dots-vertical"
            >
                <path
                    d="M9.5 13a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0zm0-5a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0zm0-5a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0z"
                />
            </svg>
        </button>
        </div>
        {#if tags.length > 0}
        <div class="flex pl-6 pr-6 gap-1 flex-wrap">
            {#each tags as tag}
                <span class="badge bg-{tag.color}-bg text-{tag.color} border-{tag.color} drop-shadow-{tag.color}-shadow drop-shadow-xl border-1 font-bold">{tag.title}</span>
            {/each}
        </div>
        {/if}
        {#if assignedUsers.length > 0 || dueDate.length > 0 || description.length > 0}
        <div class="card-body">
            {#if dueDate.length > 0}
            <div>
                <div class="text-text-secondary flex gap-2 items-center h-5"><Clock class="h-full"/> {dueDate}</div>
            </div>
            {/if}
            {#if assignedUsers.length > 0}
            <div>
                <div class="text-text-secondary flex gap-2 items-center h-5"><User class="h-full"/> {dueDate}</div>
            </div>
            {/if}
            {#if description.length > 0}
            <div class="mt-5 prose prose-sm">
                {@html mdDescription}
            </div>
            {/if}
        </div>
        {/if}
    </div>
</li>