<script>
    import { goto } from '$app/navigation';
</script>


<div class="flex bg-gray-700 h-full justify-center items-center">
    <fieldset class="fieldset w-md bg-gray-900 p-4 rounded-box">
        <div class="text-xl ">Sign up to Offenbach</div>
        
        <label class="fieldset-label">Username</label>
        <input type="text" class="input w-full" placeholder="Enter your username" />
        
        <label class="fieldset-label">Password</label>
        <input type="text" class="input w-full" placeholder="Enter your password" />

        <label class="fieldset-label">Repeat Password</label>
        <input type="text" class="input w-full" placeholder="Repeat your password" />

        <input type="button" class="btn btn-primary mt-4" value="Sign up" on:click={() => goto("/")} />
        <div class="divider">Already have an account?</div>
        <input type="button" class="btn btn-outline mt-4" value="Log in" on:click={() => goto("/login")} />

    </fieldset>
</div>