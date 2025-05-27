<script lang="ts">
    import { goto } from '$app/navigation';

    let nickname = '';
    let password = '';
    let showPassword = false;

    async function handleSignIn() {
        const res = await fetch('/auth/signin', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ nickname, password }),
        });

        if (res.ok) {
            goto('/');
        } else {
            alert(JSON.stringify(await res.json()));
        }
    }
</script>

<div class="flex bg-gray-700 h-full justify-center items-center">
    <form on:submit|preventDefault={handleSignIn}>
        <fieldset class="fieldset w-md bg-gray-900 p-4 rounded-box">
            <div class="text-xl ">Sign in to boardly</div>
            
            <label class="fieldset-label" for="nickname-input">Nickname</label>
            <input type="text" bind:value={nickname} class="input w-full" id="nickname-input" placeholder="Enter your nickname" />
            
            <label class="fieldset-label" for="password-input">Password</label>
            <div class="flex items-center gap-2">
                <input
                    type={showPassword ? "text" : "password"}
                    class="input w-full"
                    id="password-input"
                    placeholder="Enter your password"
                    bind:value={password}
                />
                <button
                    type="button"
                    class="btn btn-sm"
                    on:click={() => (showPassword = !showPassword)}
                    aria-label={showPassword ? "Hide password" : "Show password"}
                >
                    {showPassword ? "🙈" : "👁️"}
                </button>
            </div>

            <button type="submit" class="btn btn-primary mt-4">Log in</button>
            <div class="divider">Don't have an account?</div>
            <button type="button" class="btn btn-outline mt-4"on:click={() => goto("/signup")}>Sign up</button>
        </fieldset>
    </form>
</div>