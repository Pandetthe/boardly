<script lang="ts">
  import { goto } from '$app/navigation';
	import { Home, LogOut } from 'lucide-svelte';
  import ProfileIcon from './ProfileIcon.svelte';
  export let me;
</script>

<div class="w-full h-20 lg:h-full lg:w-20 p-5 bg-background-secondary flex lg:flex-col justify-between items-center border-b-1 lg:border-r-1 border-r-border border-b-border">
  <button class="avatar" onclick={() => goto("/profile")} aria-label="Profile">
    <ProfileIcon user={me} size="large"></ProfileIcon>
  </button>
  <ul class="menu flex-row lg:flex-col">
    <li>
      <a class="tooltip tooltip-bottom lg:tooltip-right" data-tip="Home" href="/">
        <Home />
      </a>
    </li>
    <li>
      <button
       class="tooltip tooltip-bottom lg:tooltip-right" data-tip="Log out"
       onclick={() => fetch('/api/auth/signout', { method: 'POST' }).then(() => goto('/signin', { invalidateAll: true }))}
      >
        <LogOut />
      </button>
    </li>
  </ul>
</div>