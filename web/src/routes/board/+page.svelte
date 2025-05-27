<script lang="ts">
	import SideBar from '$lib/SideBar.svelte';
	import Page from '$lib/Page.svelte';
	import ManagePagePopup from '$lib/popup/ManagePagePopup.svelte';
	import ManageCardPopup from '$lib/popup/ManageCardPopup.svelte';
	import { Menu, Plus } from 'lucide-svelte';
	import ManageBoardPopup from '$lib/popup/ManageBoardPopup.svelte';

	let pages = [
		{
			"name": "DevOps",
			"id": 0,
			"lists": [
			{
				"id": 1,
				"title": "To do",
				"color": "purple",
				"cards": [
				{ "id": 1, "title": "Set up CI/CD", description: "Use GitHub Actions for deployment.", tags: [1, 3], assignedUsers: [1], dueDate: "2023-08-20" },
				{ "id": 2, "title": "Configure Docker", tags: [2], assignedUsers: [2], dueDate: "2023-08-21" }
				]
			},
			{
				"id": 2,
				"title": "Monitoring",
				"color": "green",
				"cards": [
				{ "id": 3, "title": "Grafana Dashboard", tags: [3], assignedUsers: [3] }
				]
			},
			{
				"id": 3,
				"title": "Completed",
				"color": "red",
				"cards": [
				{ "id": 4, "title": "Set up AWS EC2", assignedUsers: [1, 4], dueDate: "2023-08-15" }
				]
			}
			],
			"tags": [
			{ "color": "blue", "title": "CI/CD", "id": 1 },
			{ "color": "yellow", "title": "Containers", "id": 2 },
			{ "color": "pink", "title": "Monitoring", "id": 3 }
			],
			"users": [
			{ "name": "John Connor", "id": 1 },
			{ "name": "Sarah Reese", "id": 2 },
			{ "name": "Kyle Brody", "id": 3 },
			{ "name": "Diana Prince", "id": 4 }
			]
			},
		{
			"name": "Backend",
			"id": 1,
			"lists": [
				{
				"id": 1,
				"title": "To do",
				"color": "red",
				"cards": [
					{ "id": 1, "title": "Fix DB migrations", tags: [2], assignedUsers: [2], dueDate: "2023-11-10" },
					{ "id": 2, "title": "Refactor API structure", description: "Align the new RESTful endpoints with the v2 contract.", tags: [3, 4], assignedUsers: [3, 5], dueDate: "2023-11-11" }
				]
				},
				{
				"id": 2,
				"title": "Code Review",
				"color": "teal",
				"cards": [
					{ "id": 3, "title": "Review auth module", tags: [1], assignedUsers: [1, 3], dueDate: "2023-11-12" }
				]
				},
				{
				"id": 3,
				"title": "Done",
				"color": "blue",
				"cards": [
					{ "id": 4, "title": "Implement logging", tags: [4], assignedUsers: [4], dueDate: "2023-11-08" }
				]
				}
			],
			"tags": [
				{ "color": "purple", "title": "Security", "id": 1 },
				{ "color": "green", "title": "Database", "id": 2 },
				{ "color": "yellow", "title": "API", "id": 3 },
				{ "color": "teal", "title": "Optimization", "id": 4 }
			],
			"users": [
				{ "name": "Alice Doe", "id": 1 },
				{ "name": "Bob Smith", "id": 2 },
				{ "name": "Charlie Brown", "id": 3 },
				{ "name": "David Johnson", "id": 4 },
				{ "name": "Eva Green", "id": 5 }
			]
		},
		{
			"name": "Mobile App",
			"id": 2,
			"lists": [
				{
				"id": 1,
				"title": "Backlog",
				"color": "yellow",
				"cards": [
					{ "id": 1, "title": "Push notifications", tags: [1, 3], dueDate: "2023-09-10" },
					{ "id": 2, "title": "In-app purchases", description: "Add Stripe support", tags: [2], assignedUsers: [1, 2] }
				]
				},
				{
				"id": 2,
				"title": "Development",
				"color": "pink",
				"cards": [
					{ "id": 3, "title": "UI Redesign", tags: [1, 4], assignedUsers: [3], dueDate: "2023-09-12" }
				]
				},
				{
				"id": 3,
				"title": "QA",
				"color": "green",
				"cards": [
					{ "id": 4, "title": "Bug: Splash screen crash", assignedUsers: [4] }
				]
				}
			],
			"tags": [
				{ "color": "red", "title": "UI", "id": 1 },
				{ "color": "blue", "title": "Payments", "id": 2 },
				{ "color": "purple", "title": "Notifications", "id": 3 },
				{ "color": "teal", "title": "UX", "id": 4 }
			],
			"users": [
				{ "name": "Anna Kowalska", "id": 1 },
				{ "name": "Tomasz Nowak", "id": 2 },
				{ "name": "Marta Wiśniewska", "id": 3 },
				{ "name": "Piotr Zieliński", "id": 4 }
			]
		}
    ];

	let pagePopup: ManagePagePopup, cardPopup: ManageCardPopup;
</script>

<div class="bg-background flex h-full">
	<SideBar />
	<ManagePagePopup bind:this={pagePopup} bind:pages={pages} />
	<div class="w-full overflow-y-scroll">
		<div class="tabs gap-3 p-3">
			{#each pages as page}
				<label
					class="tab border-border bg-component hover:bg-component-hover hover:border-border-hover w-40 flex-row justify-between rounded-md border-1 pr-2 [--tab-bg:orange]"
				>
					<input type="radio" name="tabs" checked={page.id === 0} />
					{page.name}
					<button
						class="btn btn-xs btn-ghost z-50 aspect-square p-0"
						aria-label="More options"
						on:click={() => {
							pagePopup.show(page.id);
						}}
					>
					<Menu />
				</label>
				<div class="tab-content">
					<div class="divider mt-0 pt-0"></div>
					<Page lists={page.lists} tags={page.tags} users={page.users}/>
				</div>
			{/each}
			<button
				class="tab btn btn-md btn-ghost border-border bg-component hover:bg-component-hover hover:border-border-hover rounded-md border-1 w-10 p-1"
				on:click={() => pagePopup.show()}
			>
				<Plus />
			</button>
		</div>
	</div>
</div>

