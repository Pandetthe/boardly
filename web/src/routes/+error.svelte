<script lang="ts">
  import { page } from "$app/state";
</script>

<svelte:head>
  <title>Error {page.status}</title>
</svelte:head>
<div class="flex flex-col items-center gap-2 justify-center min-h-screen text-center px-4 w-full">
  {#if page.status === 404}
    <h1 class="text-6xl font-bold">Page Not Found</h1>
    <p class="text-gray-600 text-lg">The page you are looking for does not exist.</p>
  {:else}
    <h1 class="text-6xl font-bold">An Error Occurred</h1>
    <p class="text-gray-600 text-lg">Something went wrong. Please try again later.</p>
  {/if}
</div>