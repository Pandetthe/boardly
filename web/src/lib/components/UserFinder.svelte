<script lang="ts">
    import { tick } from "svelte";
    import { env } from "$env/dynamic/public";
	import type { User } from "$lib/types/api/users";

    let filteredUsers: User[] = [];

    let selected: User | null = null;

    let input: HTMLInputElement;


    async function search() {
        filteredUsers = await fetch(
            `api/finder?q=${encodeURIComponent(input.value)}`,
        ).then(response => response.json());
    }

    async function deselect() {
        selected = null;
        await tick();
        input.focus();
    }

</script>

<div class="w-full flex gap-2 border-1 border-border rounded-md">
    <div class="dropdown w-full">
    {#if selected == null}
    <input type="text" placeholder="Search for users" class="input w-full border-none" oninput={search} role="button" bind:this={input}/>
    <ul class="dropdown-content dropdown-open menu bg-component-hover w-full">
        {#each filteredUsers as user}
            <li class="text-sm">
                <button onclick={() => selected=user} class="font-bold flex gap-3">
                     <img src="https://img.daisyui.com/images/stock/photo-1534528741775-53994a69daeb.webp" class="h-10 rounded-full" alt="pfp"/>
                     {user.nickname}
                </button>
            </li>
        {/each}
    </ul>
    {:else}
        <button onclick={() => {deselect()}} class="w-full text-left h-10 p-2 pl-3 text-sm">
            {selected.nickname}
        </button>
    {/if}
    </div>
    <select class="h-10 p-2 text-text-secondary bg-component text-sm rounded-md">
        <option value="1" selected>Viewer</option>
        <option value="2">Editor</option>
        <option value="3">Admininstrator</option>
    </select>
    <button class="btn btn-primary">Add</button>
</div>