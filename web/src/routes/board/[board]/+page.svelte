<script lang="ts">
	import Swimlane from '$lib/components/Swimlane.svelte';
	import ManageSwimlanePopup from '$lib/components/popup/ManageSwimlanePopup.svelte';
	import { Menu, Plus } from 'lucide-svelte';
	import { onDestroy, onMount } from 'svelte';
	import type { PageProps } from './$types';
	import * as signalR from '@microsoft/signalr';
	import { setContext } from 'svelte'
	import { goto, invalidate, invalidateAll } from '$app/navigation';

	let { data }: PageProps = $props();

	import { writable } from 'svelte/store';

	const cards = writable(data.cards);
	const board = writable(data.board);

	$effect(() => {
		if (data?.cards) {
			cards.set(data.cards);
		}
		if (data?.board) {
			board.set(data.board);
		}
	});

	setContext('cards', cards);
	setContext('board', board);

	let swimlanePopup: ManageSwimlanePopup | undefined = $state(undefined);

	let conn: signalR.HubConnection;

	async function start() {
		conn = new signalR.HubConnectionBuilder()
			.withUrl(`/api/hubs/board?boardId=${data.board.id}`)
			.withAutomaticReconnect()
			.configureLogging(signalR.LogLevel.Information)
			.build();

		try {
			await conn.start();
			console.log('SignalR connection started');
		} catch (err) {
			console.error("Error while starting connection: ", err);
		}

		conn.on("BoardUpdate", () => {
			console.log("BoardUpdate received");
			invalidate('api:board');
		});

		conn.on("BoardDelete", () => {
			console.log("BoardDelete received");
			goto('/');
		});

		conn.on("CardCreate", () => {
			console.log("CardCreate received");
			invalidate('api:board');
		});

		conn.on("CardUpdate", () => {
			console.log("CardUpdate received");
			invalidate('api:board');
		});

		conn.on("CardDelete", () => {
			console.log("CardDelete received");
			invalidate('api:board');
		});

		conn.on("CardMove", async () => {
			console.log("CardMove received");
			invalidate('api:board');
		});

		conn.on("SwimlaneCreate", () => {
			console.log("SwimlaneCreate received");
			invalidate('api:board');
		});

		conn.on("SwimlaneUpdate", () => {
			console.log("SwimlaneUpdate received");
			invalidate('api:board');
		});

		conn.on("SwimlaneDelete", () => {
			console.log("SwimlaneDelete received");
			invalidate('api:board');
		});

		conn.on("ListUpdate", () => {
			console.log("ListUpdate received");
			invalidate('api:board');
		});
		conn.on("ListCreate", () => {
			console.log("ListCreate received");
			invalidate('api:board');
		});
		conn.on("ListDelete", () => {
			console.log("ListDelete received");
			invalidate('api:board');
		});

		conn.on("TagUpdate", () => {
			console.log("TagUpdate received");
			invalidate('api:board');
		});

		conn.on("TagCreate", () => {
			console.log("TagCreate received");
			invalidate('api:board');
		});

		conn.on("TagDelete", () => {
			console.log("TagDelete received");
			invalidate('api:board');
		});
    }

	onMount(async () => {
		await start();
	});

	onDestroy(() => {
		if (conn) {
			conn.stop().then(() => {
				console.log('SignalR connection stopped');
			}).catch(err => {
				console.error("Error while stopping connection: ", err);
			});
		}
	});

</script>

<svelte:head>
    <title>{data.board.title}</title> 
</svelte:head>

<ManageSwimlanePopup bind:this={swimlanePopup} boardId={data.board.id}/>
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
					onclick={() => swimlanePopup?.show(swimlane)}
				>
				<Menu />
			</label>
			<div class="tab-content">
				<div class="divider mt-0 pt-0"></div>
				<Swimlane tags={swimlane.tags} lists={swimlane.lists} users={[]} boardId={data.board.id} swimlaneId={swimlane.id}/>
			</div>
		{/each}
		<button
			class="tab btn btn-md btn-ghost border-border bg-component hover:bg-component-hover hover:border-border-hover rounded-md border-1 w-10 p-1"
			onclick={() => swimlanePopup?.show()}
		>
			<Plus />
		</button>
	</div>
</div>

