<script lang="ts">
    import { tick } from "svelte";
	import type { User } from "$lib/types/api/users";
    import type { Member } from "$lib/types/api/members";
    import { BoardRole } from "$lib/types/api/members";
	import BoardCard from "./BoardCard.svelte";

    export let onSelect: (member: Member) => void = () => {};
    export let showRole = true;
    export let blacklist: string[] = [];
    export let members: Member[] | null = null;

    function addMember(e: MouseEvent) {
        e.preventDefault();
        e.stopPropagation();
        if (selected == null) {
            return;
        }

        const getRole = () => {
            switch(role.value) {
                case "1": return BoardRole.Viewer;  
                case "2": return BoardRole.Editor;
                case "3": return BoardRole.Admin;
                default: return BoardRole.Viewer;      
            }
        }

        const member: Member = { 
            userId: selected.id, nickname: selected.nickname, role: getRole(), isActive: false
        }

        onSelect(member);
    }

    function addMemberNoRole(e: MouseEvent) {
        e.preventDefault();
        e.stopPropagation();

        if (selected == null) {
            return;
        }

        const member = { 
            userId: selected.id, nickname: selected.nickname, isActive: false
        }

        onSelect(member as Member);
    }

    let filteredUsers: User[] = [];

    let selected: User | null = null;

    let input: HTMLInputElement;
    let role: HTMLSelectElement;


    async function search() {
        if (members != null) {
            filteredUsers = members
            .filter(
                x => x.nickname.toLocaleLowerCase().includes(input.value.toLocaleLowerCase())
            )
            .map(member => ({
                id: member.userId,
                nickname: member.nickname
            }));
            return;
        }

        const params = new URLSearchParams();

        if (input.value) {
            params.append("q", input.value);
        }

        for (const id of blacklist) {
            params.append("b", id);
        }

        try {
            const response = await fetch(`/api/users?${params.toString()}`, {
                method: "GET",
                headers: {
                    "Accept": "application/json"
                }
            });

            if (!response.ok) {
                throw new Error(await response.json());
            }

            filteredUsers = await response.json();
        } catch (error) {
            console.error("Fetch error:", error);
        }
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
    {#if showRole}
    <select class="h-10 p-2 text-text-secondary bg-component text-sm rounded-md" bind:this={role}>
        <option value="1" selected>Viewer</option>
        <option value="2">Editor</option>
        <option value="3">Admin</option>
    </select>
    {/if}
    <button class="btn btn-primary" onclick={(e) => showRole ? addMember(e) : addMemberNoRole(e)}>Add</button>
</div>