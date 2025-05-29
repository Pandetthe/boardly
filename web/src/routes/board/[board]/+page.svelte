<script lang="ts">
	import Swimlane from '$lib/components/Swimlane.svelte';
	import ManagePagePopup from '$lib/components/popup/ManagePagePopup.svelte';
	import ManageCardPopup from '$lib/components/popup/ManageCardPopup.svelte';
	import { Menu, Plus } from 'lucide-svelte';
	import type { PageProps } from './$types';
	let { data }: PageProps = $props();

	let pagePopup: ManagePagePopup, cardPopup: ManageCardPopup;
</script>

<svelte:head>
    <title>{data.board.title}</title> 
</svelte:head>

<!-- <ManagePagePopup bind:this={pagePopup} bind:pages={pages} /> -->
<div class="w-full overflow-y-scroll">
	<div class="tabs gap-3 p-3">
		{#each data.board.swimlanes as swimlane}
			<label
				class="tab border-border bg-component hover:bg-component-hover hover:border-border-hover w-40 flex-row justify-between rounded-md border-1 pr-2 [--tab-bg:orange]"
			>
				<input type="radio" name="tabs" />
				{swimlane.title}
				<button
					class="btn btn-xs btn-ghost z-50 aspect-square p-0"
					aria-label="More options"
					onclick={() => pagePopup.show(swimlane.id)}
				>
				<Menu />
			</label>
			<div class="tab-content">
				<div class="divider mt-0 pt-0"></div>
				<Swimlane lists={swimlane.lists} tags={swimlane.tags} users={[]}/>
			</div>
		{/each}
		<button
			class="tab btn btn-md btn-ghost border-border bg-component hover:bg-component-hover hover:border-border-hover rounded-md border-1 w-10 p-1"
			onclick={() => pagePopup.show()}
		>
			<Plus />
		</button>
	</div>
</div>

