<script>
	import GeoPattern from 'geopattern';
	import { goto } from '$app/navigation';

    export let boardId = Math.random().toString();
	export let boardName;


	function hashString(str) {
		let hash = 0;
		for (let i = 0; i < str.length; i++) {
			hash = (hash << 5) - hash + str.charCodeAt(i);
			hash |= 0;
		}
		return Math.abs(hash);
	}

	function idToColor(id) {
		const choices = [
			"#0070F3",
			"#FFB224",
			"#FFB224",
			"#46A758",
			"#12A594",
			"#8E4EC6",
			"#E93D82",
		];
		const index = hashString(String(id)) % choices.length;
		return choices[index];
	}

	const color = idToColor(boardName);
    const src = GeoPattern.generate(boardName, {"color": color}).toDataUrl().slice(5, -2);

</script>

<button class="card w-full shadow-sm bg-component hover:bg-component-hover h-60 hover:border-border-hover h drop-shadow-2xl transition-transform border-1 border-border" on:click={() => goto("/board")}>
	<figure>
		<img {src} alt="Board" class="w-full h-40 object-cover"/>
	</figure>
	<div class="card-body">
		<h2 class="card-title">{boardName}</h2>
		<p class="left text-left text-text-secondary">Modified by: AL:FIHJakuhwlkahdfliuka</p>
	</div>
</button>
