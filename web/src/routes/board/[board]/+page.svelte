<script lang="ts">
	import Swimlane from '$lib/components/Swimlane.svelte';
	import ManageSwimlanePopup from '$lib/components/popup/ManageSwimlanePopup.svelte';
	import { Menu, Plus } from 'lucide-svelte';
	import { onMount } from 'svelte';
	import type { PageProps } from './$types';
	import * as signalR from '@microsoft/signalr';
	let { data }: PageProps = $props();

	let swimlanePopup: ManageSwimlanePopup | undefined = $state(undefined);

    let connection: signalR.HubConnection;

    function start() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/api/hubs/board?boardId=683f13ac4d0f10626e47c678")
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.start().catch(err => console.error("Error while starting connection: ", err));
    }

    onMount(() => {
        start();
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
				<Swimlane lists={swimlane.lists} users={[]} boardId={data.board.id}/>
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

