<script lang="ts">
	import '../app.css';
	import { globalError } from '$lib/stores/ErrorStore';
	import { TriangleAlert } from 'lucide-svelte';

	let err = $state<string | null>(null);

	globalError.subscribe(async (error) => {

		if (error) {
			if (error.status == 412) {
				err = error.detail;
			} else {
				err = error.message ?? error.title;
				if (error.errors) {
					err = error.errors.Title[0];
				}
			}
			setTimeout(() => {
					err = null;
			}, 3000);
		} else {
			err = null;
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