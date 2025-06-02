<script lang="ts">
    import { tick } from "svelte";

    const users = [
        { id: 1, name: "janpawseltaszek" },
        { id: 2, name: "stawkey" },
        { id: 3, name: "kacper" },
        { id: 4, name: "adam" }
    ]
    let filteredUsers = users;

    let selected: { id: number; name: string } | null = null;

    let input: HTMLInputElement;

    $: filteredUsers;

    function search(event: HTMLInputElement) {
        const input = event.target.value.toLowerCase();
        filteredUsers = users.filter(user => user.name.toLowerCase().includes(input));
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
    <input type="text" placeholder="Search for users" class="input w-full border-none" on:input={search} role="button" bind:this={input}/>
    <ul class="dropdown-content dropdown-open menu bg-component-hover w-full">
        {#each filteredUsers as user}
            <li class="text-sm">
                <button on:click={() => selected=user} class="font-bold flex gap-3">
                     <img src="https://img.daisyui.com/images/stock/photo-1534528741775-53994a69daeb.webp" class="h-10 rounded-full" alt="pfp"/>
                     {user.name}
                </button>
            </li>
        {/each}
    </ul>
    {:else}
        <button on:click={() => {deselect()}} class="w-full text-left h-10 p-2 pl-3 text-sm">
            {selected.name}
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