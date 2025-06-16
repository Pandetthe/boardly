<script lang="ts">
  import { X } from "lucide-svelte";

  export let title = "";
  export let visible = false;
  export let isEditMode = false;
  export let deleteButtonVisible = true;
  export let onCreate: () => Promise<void> | void;
  export let onDelete: () => Promise<void> | void;
  export let onCancel: () => Promise<void> | void;
  export let onEdit: () => Promise<void> | void;

  let locked = false;

  async function handleSubmit(e: Event) {
    e.preventDefault();
    locked = true;
    try {
      if (isEditMode) {
        await onEdit?.();
      } else {
        await onCreate?.();
      }
    } finally {
      locked = false;
    }
  }
  async function handleWithLock(fn: () => Promise<void> | void) {
    if (locked) return;
    locked = true;
    try {
      await fn?.();
    } finally {
      locked = false;
    }
  }
</script>

{#if visible}
<div class="fixed top-0 left-0 w-full h-full bg-background/30 flex items-center justify-center z-50 backdrop-blur-xs"
onkeydown={(e) => {
  if (e.key === "Enter" && !(e.target instanceof HTMLTextAreaElement)) {
    const isButton = (e.target as HTMLElement).closest("button[type='submit']");
    if (!isButton) {
      e.preventDefault();
    }
  }
}}
role="dialog"
aria-modal="true"
tabindex="-1"
>
  <form
    class="bg-background-secondary border-border border-1 p-4 rounded-2xl shadow-lg w-15/16 max-w-xl gap-5 flex flex-col font-bold"
    onsubmit={handleSubmit}
  >
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold">{isEditMode ? "Edit" : "Create"} {title}</h1>
      <button class="btn btn-ghost" disabled={locked} onclick={() => handleWithLock(onCancel)}>
        <X class="w-5 h-5" />
      </button>
    </div>
    <slot />
    <div class="flex justify-end mt-4 gap-4">
      {#if isEditMode}
      {#if deleteButtonVisible}
      <button disabled={locked} class="btn btn-error btn-outline" onclick={() => handleWithLock(onDelete)}>Delete</button>
      {/if}
      <button type="submit" disabled={locked} class="btn btn-primary">Save</button>
      {:else}
      <button type="submit" disabled={locked} class="btn btn-primary">Create</button>
      {/if}
    </div> 
  </form>
</div>
{/if}