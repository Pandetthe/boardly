<script lang="ts">
	import GeoPattern from 'geopattern';
	import { Menu } from "lucide-svelte";
	import ManageBoardPopup from '$lib/components/popup/ManageBoardPopup.svelte';
	import { goto } from '$app/navigation';
	import type { Board } from '$lib/types/api/boards';

    export let board: Board;
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
					<div
						class="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center text-xs font-bold border-2 border-white"
						title={member.nickname}
						style="background-color: {idToColor(member.nickname)}"
					>
						{member.nickname.slice(0,2)}
					</div>
				{/each}
			</div>
			<button class="btn btn-ghost z-10 w-10 p-0" onclick={showPopup}>
				<Menu />
			</button>
		</div>
		<p class="left text-left text-text-secondary">Updated at: {board.updatedAt.toLocaleString()}</p>
	</div>
</div>
