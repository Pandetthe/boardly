<script lang="ts">
	import GeoPattern from 'geopattern';
	import { Menu } from "lucide-svelte";
	import ManageBoardPopup from '$lib/components/popup/ManageBoardPopup.svelte';
	import { goto, invalidate } from '$app/navigation';
	import type { Board } from '$lib/types/api/boards';
	import { idToColor } from '$lib/utils';
	import ProfileIcon from './ProfileIcon.svelte';

    export let board: Board;
	export let popup: ManageBoardPopup;
	export let editEnabled: boolean;

	async function showPopup(e: MouseEvent) {
		await invalidate('api:boards');
		popup.show(board);
		e.stopPropagation();
	}

	const color = idToColor(board.id);
    const src = GeoPattern.generate(board.title, {"color": color}).toDataUrl().slice(5, -2);

</script>

<div
 class="card w-full shadow-sm bg-component hover:bg-component-hover h-60 hover:border-border-hover h drop-shadow-2xl transition-transform border-1 border-border"
 onclick={() => goto(`/board/${board.id}`)}
 role="button"
 tabindex="0"
 onkeydown={(e) => { if (e.key === "Enter") goto("/board") }}>
	<figure>
		<img src={src} alt="Board" class="w-full h-40 object-cover"/>
	</figure>
	<div class="card-body">
		<div class="card-title flex justify-between items-center">
			{board.title}
			<div class="flex -space-x-2">
				{#each board.members.filter(x => x.isActive) as member (member.userId)}
					<ProfileIcon user={member} noTooltip={false}/>
				{/each}
			</div>
			{#if editEnabled}
				<button class="btn btn-ghost z-10 w-10 p-0" onclick={showPopup}>
					<Menu />
				</button>
			{/if}
		</div>
		<p class="left text-left text-text-secondary">Updated at: {board.updatedAt.toLocaleString()}</p>
	</div>
</div>
