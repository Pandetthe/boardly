
<script lang="ts">
	import { BoardRole } from '$lib/types/api/members';
  import { Clock, Menu, User } from 'lucide-svelte';
  import { marked } from "marked";
	import { getContext } from 'svelte';
	import ProfileIcon from './ProfileIcon.svelte';
	import type { Card } from '$lib/types/api/cards';
	import type { Writable } from 'svelte/store';
	import type { Board } from '$lib/types/api/boards';
	import ManageCardPopup from './popup/ManageCardPopup.svelte';
	import type { User as UserType } from '$lib/types/api/users';

  export let card: Card;
  export let color: string;
  export let popup: ManageCardPopup;
  export function process(newColor: string) {
    color = newColor;
  }

  const me = getContext<UserType>('user');
  const board = getContext<Writable<Board>>('board');
</script>

<li class="border-2 border-{color} bg-component rounded-xl mb-5 drop-shadow-2xl drop-shadow-{color}-shadow 
{card.lockedByUser != null || card.assignedUsers.some(u => u.id == me.id) || $board.members.some(member => member.userId === me.id && member.role != BoardRole.Viewer)? '' : 'nodrag'}"
    data-id={card.id} data-updatedAt={card.updatedAt.toISOString()}>
    {#if card.lockedByUser}
    <div class="flex gap-2 absolute top-0 left-0 z-50 rounded-xl w-full h-full bg-radial from-component-hover to-transparent justify-center items-center text-text text-xl">
        <ProfileIcon user={card.lockedByUser} size="medium" /> is editing
    </div>
    {/if}
    <div class="card gap-6 p-6">
        <div class="flex justify-between items-start">
            <h1 class="card-title text-{color} text-2xl font-black drop-shadow-xl drop-shadow-{color}-shadow">
                {card.title}
            </h1>
            {#if $board.members.some(member => member.userId === me.id && member.role != BoardRole.Viewer)}
            <button class="btn btn-xs btn-ghost z-50 aspect-square p-0" aria-label="More options" disabled={card.lockedByUser !== null} onclick={() => {popup.show(card)}}>
                <Menu />
            </button>
            {/if}
        </div>
        {#if card.tags.length > 0}
        <div class="flex gap-1 flex-wrap">
            {#each card.tags as tag}
                <span class="badge bg-{tag.color}-bg text-{tag.color} border-{tag.color} drop-shadow-{tag.color}-shadow drop-shadow-xl border-1 font-bold">{tag.title}</span>
            {/each}
        </div>
        {/if}
        {#if card.dueDate != null}
        <div>
            <div class="text-text-secondary flex gap-2 items-center h-5"><Clock class="h-full"/> {card.dueDate.toLocaleString()}</div>
        </div>
        {/if}
        {#if card.assignedUsers.length > 0 && card.assignedUsers[0]}
        <div>
            <div class="text-text-secondary flex gap-2 items-center h-5">
                <User class="h-full"/>
                <div class="avatar-group -space-x-4 overflow-visible">
                    {#each card.assignedUsers as user}
                        <ProfileIcon user={user} size="small" noTooltip={false} />
                    {/each}
                </div>
            </div>
        </div>
        {/if}
        {#if card.description}
        <div class="prose prose-sm">
            {@html marked(card.description)}
        </div>
        {/if}
    </div>
</li>