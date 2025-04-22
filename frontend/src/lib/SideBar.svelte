<script>
    import { goto } from '$app/navigation';
</script>

<div class="h-full w-20 p-5 bg-background-secondary flex flex-col justify-between items-center border-r-1 border-r-border">
    <button class="avatar" on:click={() => goto("/profile")} aria-label="Profile">
        <div class="rounded-full">
          <img src="https://img.daisyui.com/images/stock/photo-1534528741775-53994a69daeb.webp" alt="pfp"/>
        </div>
      </button>
    <ul class="menu">
        <li>
          <a class="tooltip tooltip-right" data-tip="Home" href="*">
            Home
          </a>
        </li>
        <li>
          <a class="tooltip tooltip-right" data-tip="Details" href="*">
            Settings
          </a>
        </li>
      </ul>
</div>