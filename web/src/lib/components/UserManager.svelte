<script lang="ts">
	import { BoardRole, type Member } from "../types/api/members";
    export let users: Member[];

    function removeUser(e: MouseEvent, user: Member) {
        e.preventDefault();
        e.stopPropagation();
        users = users.filter(u => u.userId !== user.userId);
    }

</script>

<div class="h-80 overflow-scroll">
    <table class="table h-8">
        <tbody>
            {#each users as user}
                <tr>
                    <th>
                        <img
                        src="https://img.daisyui.com/images/stock/photo-1534528741775-53994a69daeb.webp"
                        class="h-1/2 w-1/2 rounded-full"
                        alt="pfp"/>
                    </th>
                    <th>{user.nickname}</th>
                    <th>
                        <select class="h-10 p-2 text-text-secondary bg-component font-normal rounded-md" disabled={user.role == BoardRole.Owner} bind:value={user.role}>
                            {#if user.role == BoardRole.Owner}
                                <option value="0" selected>Owner</option>
                            {/if}
                            <option value="Viewer" selected={user.role == BoardRole.Viewer}>Viewer</option>
                            <option value="Editor" selected={user.role == BoardRole.Editor}>Editor</option>
                            <option value="Admin" selected={user.role == BoardRole.Admin}>Admin</option>
                        </select>
                    </th>
                    <th>
                        <button class="btn btn-error ml-2 btn-soft" onclick={(e) => removeUser(e, user)}>*</button>
                    </th>
                </tr>
            {/each}
        </tbody>
    </table>
</div>