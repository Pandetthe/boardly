<script lang="ts">
	import { X } from "lucide-svelte";
	import { BoardRole, type Member } from "../types/api/members";
	import ProfileIcon from "./ProfileIcon.svelte";
    export let users: Member[];

    export let onRemove: (user: Member) => void = () => {};

    function removeUser(e: MouseEvent, user: Member) {
        e.preventDefault();
        e.stopPropagation();
        users = users.filter(u => u.userId !== user.userId);
        onRemove(user);
    }

</script>

<div class="h-80 overflow-scroll">
    <table class="table h-8 text-sm">
        <tbody>
            {#each users as user}
                <tr class="h-12">
                    <th class="w-8">
                        <ProfileIcon user={user} size="medium" />
                    </th>
                    <th class="w-fit">{user.nickname}</th>
                    {#if user.role}
                    <th>
                        <select class="h-10 p-2 text-text-secondary bg-transparent font-normal rounded-md" disabled={user.role == BoardRole.Owner} bind:value={user.role}>
                            {#if user.role == BoardRole.Owner}
                                <option value="owner" selected>Owner</option>
                            {/if}
                            <option value="viewer" selected={user.role == BoardRole.Viewer}>Viewer</option>
                            <option value="editor" selected={user.role == BoardRole.Editor}>Editor</option>
                            <option value="admin" selected={user.role == BoardRole.Admin}>Admin</option>
                        </select>
                    </th>
                    {/if}
                    {#if user.role != BoardRole.Owner}
                    <th class="w-12">
                        <button class="btn btn-error ml-2 btn-soft" onclick={(e) => removeUser(e, user)}><X /></button>
                    </th>
                    {/if}
                </tr>
            {/each}
        </tbody>
    </table>
</div>