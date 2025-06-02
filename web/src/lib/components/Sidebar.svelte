<script>
    import { goto } from '$app/navigation';
</script>

<div class="h-full w-26 py-5 bg-background-secondary flex flex-col justify-between items-center border-r-1 border-r-border">
    <button class="avatar" onclick={() => goto("/profile")} aria-label="Profile">
        <div class="rounded-full mx-5">
          <img src="https://img.daisyui.com/images/stock/photo-1534528741775-53994a69daeb.webp" alt="pfp"/>
        </div>
      </button>
    <ul class="menu">
        <li>
          <a class="tooltip tooltip-right" data-tip="Home" href="/">
            Home
          </a>
        </li>
        <li>
          <button
            class="tooltip tooltip-right"
            onclick={() => fetch('/api/auth/signout', { method: 'POST' }).then(() => goto('/signin', { invalidateAll: true }))}
          >
            Sign out
          </button>
        </li>
      </ul>
</div>