<script lang="ts">
	import Sidebar from '$lib/components/Sidebar.svelte';
    import type { PageProps } from './$types';

	let { data }: PageProps = $props();
</script>

<svelte:head>
    <title>My profile</title> 
</svelte:head>

<Sidebar me={data.user}/>
<div class="w-full p-5 gap-5 h-fit">
    <div class="bg-white rounded shadow p-4">
        <h2 class="text-xl text-black font-bold mb-2">{data.user.nickname}</h2>
        <p class="text-gray-900">Utworzono: {new Date(data.user.createdAt).toLocaleString()}</p>
        <p class="text-gray-900">Zaktualizowano: {new Date(data.user.updatedAt).toLocaleString()}</p>
    </div>
</div>