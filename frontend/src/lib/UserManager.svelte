<script lang="ts">
    const users = [
        { id: 1, name: "janpawseltaszek" },
        { id: 2, name: "stawkey" },
        { id: 3, name: "kacper" },
        { id: 4, name: "adam" }
    ]
</script>

<div class="h-80 overflow-scroll">
<table class="table h-8">
    <tbody>
        {#each users as user}
            <tr>
                <th>
                    <img src="https://img.daisyui.com/images/stock/photo-1534528741775-53994a69daeb.webp" class="h-1/2 w-1/2 rounded-full" alt="pfp"/>
                </th>
                <th>
                    {user.name}
                </th>
                <th>
                    <select class="h-10 p-2 text-text-secondary bg-component font-normal rounded-md">
                        <option value="1" selected>
                            Viewer</option>
                        <option value="2">Editor</option>
                        <option value="3">Admininstrator</option>
                    </select>
                </th>
                <th>
                    <button class="btn btn-error ml-2 btn-soft">Remove</button>
                </th>
            </tr>
        {/each}
    </tbody>
</table>
</div>