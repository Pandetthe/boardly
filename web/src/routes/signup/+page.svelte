<script lang="ts">
    import { goto } from '$app/navigation';

    let nickname = '';
    let password = '';
    let repeatedPassword = '';
    let showPassword = false;
	let showRepeatedPassword = false;

    async function handleSignUp() {
        if (password !== repeatedPassword) {
            alert("Passwords do not match");
            return;
        }
        const res = await fetch('/api/auth/signup', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ nickname, password }),
        });

        if (res.ok) {
            goto('/', { invalidateAll: true });
        } else {
            alert(JSON.stringify(await res.json()));
        }
    }
</script>


<div class="flex bg-gray-700 h-full w-full justify-center items-center">
    <form on:submit|preventDefault={handleSignUp}>
        <fieldset class="fieldset w-md bg-gray-900 p-4 rounded-box">
            <div class="text-xl ">Sign up to Boardly</div>
            
            <label class="fieldset-label" for="nickname-input">Nickname</label>
            <input type="text" class="input w-full" id="nickname-input" placeholder="Enter your nickname" bind:value={nickname} />

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

            <label class="fieldset-label" for="repeated-password-input">Repeat Password</label>
            <div class="flex items-center gap-2">
                <input
                    type={showRepeatedPassword ? "text" : "password"}
                    class="input w-full"
                    id="repeated-password-input"
                    placeholder="Repeat your password"
                    bind:value={repeatedPassword} />
                <button
                    type="button"
                    class="btn btn-sm"
                    on:click={() => (showRepeatedPassword = !showRepeatedPassword)}
                    aria-label={showRepeatedPassword ? "Hide repeated password" : "Show repeated password"}
                >
                    {showRepeatedPassword ? "🙈" : "👁️"}
                </button>
            </div>

            <button type="submit" class="btn btn-primary mt-4">Sign up</button>
            <div class="divider">Already have an account?</div>
            <button type="button" class="btn btn-outline mt-4" on:click={() => goto("/signin")}>Sign in</button>
        </fieldset>
    </form>
</div>