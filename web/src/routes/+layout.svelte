<script lang="ts">
	import '../app.css';
	import { globalError } from '$lib/stores/ErrorStore';
	import { TriangleAlert } from 'lucide-svelte';

	let err = $state<string | null>("hi");

	globalError.subscribe(async (error) => {

		if (error) {
			console.log('Global error body:', error);
			if (error.status == 412) {
				err = error.detail;
				setTimeout(() => {
					err = null;
				}, 3000);
			}
		} else {
			err = "hi";
		}
	});

	let { children } = $props();

</script>

{#if err}
<div class="toast z-50">
	<div class="alert alert-error">
	  <span class="flex gap-6"><TriangleAlert />{err}</span>
	</div>
  </div>
{/if}

<div class="flex bg-background h-full flex-col lg:flex-row w-full">
	{@render children()}
</div>