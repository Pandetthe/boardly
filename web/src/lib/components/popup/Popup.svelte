<script lang="ts">
    import { X } from "lucide-svelte";

    export let title = "";
    export let visible = false;
    export let isEditMode = false;
    export let onCreate: () => void;
    export let onDelete: () => void;
    export let onCancel: () => void;
    export let onEdit: () => void;

    function handleKeydown(e: KeyboardEvent) {
        if (e.key === "Escape") {
            visible = false;
        }
    }

    function handleSubmit(e: Event) {
        e.preventDefault();
        if (isEditMode) {
            onEdit();
        } else {
            onCreate();
        }
    }
</script>

{#if visible}
    <div class="fixed top-0 left-0 w-full h-full bg-background/30 flex items-center justify-center z-50 backdrop-blur-xs">
        <form 
         class="bg-background-secondary border-border border-1 p-4 rounded-2xl shadow-lg w-15/16 max-w-xl gap-5 flex flex-col font-bold"
         onsubmit={handleSubmit}>
            <div class="flex justify-between items-center">
                <h1 class="text-2xl font-bold">{isEditMode ? "Edit" : "Create"} {title}</h1>
                <button class="btn btn-ghost" onclick={onCancel}>
                    <X class="w-5 h-5" />
                </button>
            </div>
            <slot />
            <div class="flex justify-end mt-4 gap-4">
                {#if isEditMode}
                    <button type="submit" class="btn btn-error btn-outline" onclick={onDelete}>Delete</button>
                    <button type="submit" class="btn btn-primary">Save</button>
                {:else}
                    <button type="submit" class="btn btn-primary">Create</button>
                {/if}
            </div> 
        </form>
    </div>
{/if}

<svelte:window onkeydown={handleKeydown} />