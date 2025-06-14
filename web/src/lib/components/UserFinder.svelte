<script lang="ts">
  import { tick } from "svelte";
	import type { SimplifiedUser, User, UserResponse } from "$lib/types/api/users";
  import type { Member } from "$lib/types/api/members";
  import { BoardRole } from "$lib/types/api/members";
	import { Check } from "lucide-svelte";
	import ProfileIcon from "./ProfileIcon.svelte";

  export let onSelect: (member: Member) => Promise<void> | void = () => {};
  export let showRole = true;
  export let blacklist: string[] = [];
  export let members: Member[] | null = null;

  async function addMember(e: MouseEvent) {
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

    await onSelect?.(member);
  }

  async function addMemberNoRole(e: MouseEvent) {
    e.preventDefault();
    e.stopPropagation();

    if (selected == null) {
      return;
    }

    const member = { 
      userId: selected.id, nickname: selected.nickname, isActive: false
    }

    await onSelect?.(member as Member);
  }

  let filteredUsers: SimplifiedUser[] = [];

  let selected: SimplifiedUser | null = null;

  let input: HTMLInputElement;
  let role: HTMLSelectElement;


  async function search() {
    if (members != null) {
      filteredUsers = members
      .filter(
        x => x.nickname.toLocaleLowerCase().includes(input.value.toLocaleLowerCase()) && !blacklist.includes(x.userId)
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
    console.log(params);
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
      filteredUsers = await response.json() as UserResponse[];
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

<div class="w-full flex gap-2 border-1 border-border bg-background-secondary rounded-md">
  <div class="dropdown w-full">
  {#if selected == null}
  <input type="text" placeholder="Search for users" class="input w-full border-none bg-background-secondary" onclick={search} oninput={search} role="button" bind:this={input}/>
  <ul class="dropdown-content dropdown-open menu w-full bg-background-secondary">
    {#each filteredUsers as user}
    <li class="text-sm">
      <button onclick={() => selected=user} class="font-bold flex gap-3">
        <ProfileIcon user={user} size="medium" />
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
  <select class="h-10 p-2 text-text-secondary text-sm rounded-md" bind:this={role}>
      <option value="1" selected>Viewer</option>
      <option value="2">Editor</option>
      <option value="3">Admin</option>
  </select>
  {/if}
  <button class="btn btn-primary" onclick={(e) => showRole ? addMember(e) : addMemberNoRole(e)}><Check /></button>
</div>