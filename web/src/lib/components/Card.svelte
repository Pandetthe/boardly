
<script lang="ts">
	import { BoardRole } from '$lib/types/api/members';
    import { User, Clock, Menu } from 'lucide-svelte';
    import { marked } from "marked";
	import { getContext } from 'svelte';
	import ProfileIcon from './ProfileIcon.svelte';

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

    const me = getContext('user');
    const board = getContext('board');
</script>

<li class="border-2 border-{color} bg-component rounded-xl mb-5 drop-shadow-2xl drop-shadow-{color}-shadow {assignedUsers.some(u => u.id == me.id) || $board.members.some(member => member.userId === me.id && member.role != BoardRole.Viewer)? '' : 'nodrag'}" data-id={id}>
    <div class="card gap-6 p-6">
        <div class="flex justify-between items-start">
            <h1 class="card-title text-{color} text-2xl font-black drop-shadow-xl drop-shadow-{color}-shadow">
                {title}
            </h1>
            {#if $board.members.some(member => member.userId === me.id && member.role != BoardRole.Viewer)}
            <button class="btn-xs btn-ghost z-50 aspect-square p-1" aria-label="More options" on:click={() => {popup.show(id)}}>
                <Menu />
            </button>
            {/if}
        </div>
        {#if tags.length > 0}
        <div class="flex gap-1 flex-wrap">
            {#each tags as tag}
                <span class="badge bg-{tag.color}-bg text-{tag.color} border-{tag.color} drop-shadow-{tag.color}-shadow drop-shadow-xl border-1 font-bold">{tag.title}</span>
            {/each}
        </div>
        {/if}
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
                        <ProfileIcon user={user} size="small" noTooltip={false} />
                    {/each}
                </div>
            </div>
        </div>
        {/if}
        {#if description?.length > 0}
        <div class="prose prose-sm">
            {@html marked(description)}
        </div>
        {/if}
    </div>
</li>