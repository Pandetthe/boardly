<script>
    import { Check } from "lucide-svelte";

    export let ready = false;
    export let name;
    export let label;
    export let required = false;
    export let invalid = false;
</script>

<div class={[
    {"collapse-arrow": !ready},
    "collapse z-60 bg-component border border-border focus:bg-secondary transition-none",
    {"bg-red-bg border-red": invalid},
    {"hover:border-border-hover hover:bg-component-hover": !invalid}
    ]}>
    <input type="radio" name={name}/>
    {#if ready}
      <Check class="absolute right-4 top-5 h-5 text-green"/>
    {/if}
    <div class="collapse-title font-semibold flex gap-3 items-center">
        {label}
        {#if required}
            <span class="badge bg-blue-bg border-blue text-blue">Required</span>
        {/if}
    </div>
    <div class="collapse-content text-sm">
      <slot/>
    </div>
</div>