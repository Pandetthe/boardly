<script lang="ts">
	import Sidebar from '$lib/components/Sidebar.svelte';
	import '../app.css';

	let { data, children } = $props();
</script>

<div class="flex bg-background h-full">
	{#if data.isAuthenticated}
		<Sidebar />
	{/if}
	{@render children()}
</div>