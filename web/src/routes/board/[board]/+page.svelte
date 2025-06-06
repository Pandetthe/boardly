<script lang="ts">
	import Swimlane from '$lib/components/Swimlane.svelte';
	import ManageSwimlanePopup from '$lib/components/popup/ManageSwimlanePopup.svelte';
	import { Menu, Plus } from 'lucide-svelte';
	import { onDestroy, onMount } from 'svelte';
	import type { PageProps } from './$types';
	import * as signalR from '@microsoft/signalr';
	import { setContext } from 'svelte'
	import { invalidate } from '$app/navigation';
	let { data }: PageProps = $props();

	setContext('cards', data.cards);

	let swimlanePopup: ManageSwimlanePopup | undefined = $state(undefined);

    let connection: signalR.HubConnection;

    async function start() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl(`/api/hubs/board?boardId=${data.board.id}`)
			.withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

		try {
			await connection.start();
		} catch (err) {
			console.error("Error while starting connection: ", err);
		}
    }

onMount(async () => {
	await start();
});
onDestroy(async () => {
	if (connection) {
		await connection.stop();
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
				<Swimlane lists={swimlane.lists} users={[]} boardId={data.board.id} swimlaneId={swimlane.id}/>
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

