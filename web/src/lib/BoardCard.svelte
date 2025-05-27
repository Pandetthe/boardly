<script lang="ts">
	import GeoPattern from 'geopattern';
	import { Menu } from "lucide-svelte";
	import ManageBoardPopup from '$lib/popup/ManageBoardPopup.svelte';
	import { goto } from '$app/navigation';

    export let boardId: string;
	export let boardTitle: string;
	export let popup: ManageBoardPopup;

	function hashString(str: string) {
		let hash = 0;
		for (let i = 0; i < str.length; i++) {
			hash = (hash << 5) - hash + str.charCodeAt(i);
			hash |= 0;
		}
		return Math.abs(hash);
	}

	function idToColor(id: string) {
		const colors = [
			"#0070F3",
			"#FFB224",
			"#FFB224",
			"#46A758",
			"#12A594",
			"#8E4EC6",
			"#E93D82",
		];
		const index = hashString(id) % colors.length;
		return colors[index];
	}

	function showPopup(e: MouseEvent) {
		popup.show(boardId);
		e.stopPropagation();
	}

	const color = idToColor(boardId);
    const src = GeoPattern.generate(boardTitle, {"color": color}).toDataUrl().slice(5, -2);

</script>

<div class="card w-full shadow-sm bg-component hover:bg-component-hover h-60 hover:border-border-hover h drop-shadow-2xl transition-transform border-1 border-border" on:click={() => goto("/board")} role="button" tabindex="0" on:keydown={(e) => { if (e.key === "Enter") goto("/board") }}>
	<figure>
		<img {src} alt="Board" class="w-full h-40 object-cover"/>
	</figure>
	<div class="card-body">
		<div class="card-title flex justify-between">{boardTitle}
			<button class="btn btn-ghost z-10 w-10 p-0" on:click={showPopup}>
				<Menu />
			</button>
		</div>
		<p class="left text-left text-text-secondary">Modified by: AL:FIHJakuhwlkahdfliuka</p>
	</div>
</div>


<style>
	.card {
		background-image: url({src});
	}
</style>