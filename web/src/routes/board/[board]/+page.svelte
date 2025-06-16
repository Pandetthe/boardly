<script lang="ts">
  import type { PageProps } from './$types';
  import { parseBoard, type Board, type BoardResponse, type DetailedBoard } from '$lib/types/api/boards';
	import Swimlane from '$lib/components/Swimlane.svelte';
	import ManageSwimlanePopup from '$lib/components/popup/ManageSwimlanePopup.svelte';
	import { Menu, Plus } from 'lucide-svelte';
	import { onDestroy, onMount } from 'svelte';
	import * as signalR from '@microsoft/signalr';
	import { setContext } from 'svelte'
	import { goto, invalidate } from '$app/navigation';
	import { writable } from 'svelte/store';
	import { BoardRole } from '$lib/types/api/members';
	import { parseCard, type Card, type CardResponse } from '$lib/types/api/cards';
	import type { SimplifiedUserResponse } from '$lib/types/api/users';
	import type { DetailedSwimlaneResponse, SwimlaneResponse } from '$lib/types/api/swimlanes';
	import type { ListResponse } from '$lib/types/api/lists';
	import type { Tag, TagResponse } from '$lib/types/api/tags';
	import Sidebar from '$lib/components/Sidebar.svelte';

	let { data }: PageProps = $props();

	const cards = writable<Card[]>(data.cards);
	const board = writable<DetailedBoard>(data.board);
	let conn: signalR.HubConnection | undefined;
	const connectionStore = writable<signalR.HubConnection | undefined>(undefined);
	setContext('cards', cards);
	setContext('board', board);
  setContext('user', data.user);
  setContext('signalRConnection', connectionStore);
	let swimlanePopup: ManageSwimlanePopup | undefined = $state(undefined);
  let selectedSwimlaneId = $state(data.board.swimlanes[0]?.id);

	async function start() {
		conn = new signalR.HubConnectionBuilder()
			.withUrl(`/api/hubs/board?boardId=${data.board.id}`)
			.withAutomaticReconnect()
			.configureLogging(signalR.LogLevel.Information)
			.build();
		connectionStore.set(conn);
		try {
			await conn.start();
			console.log('SignalR connection started');
		} catch (err) {
			console.error("Error while starting connection: ", err);
		}

		conn.on("BoardUpdated", (boardResponse: BoardResponse) => {
      console.log("BoardUpdated");
			board.update(b => ({ ...b, ...parseBoard(boardResponse) }));
		});

		conn.on("BoardDeleted", () => {
			goto('/');
		});

		conn.on("CardCreated", (cardResponse: CardResponse) => {
      console.log("CardCreated");
			cards.update(cards => [...cards, parseCard(cardResponse)]);
		});

		conn.on("CardUpdated", (cardResponse: CardResponse) => {
      console.log("CardUpdated");
			cards.update(cards => 
				cards.map(card => 
					card.id === cardResponse.id && card.swimlaneId === cardResponse.swimlaneId ? parseCard(cardResponse) : card
				)
			);
		});

		conn.on("CardDeleted", (cardId: string) => {
      console.log("CardDeleted");
			cards.update(cards => cards.filter(card => card.id !== cardId));
		});

		conn.on("CardMoved", (cardId: string, swimlaneId: string, listId: string, updatedAtStr: string) => {
      console.log("CardMoved");
			const updatedAt = new Date(updatedAtStr);
			cards.update(cards => 
				cards.map(card => 
					card.id === cardId && card.swimlaneId === swimlaneId ? { ...card, listId, updatedAt } : card
				)
			);
		});
    conn.on("CardLocked", (cardId: string, user: SimplifiedUserResponse) => {
      console.log("CardLocked");
      cards.update(cards => 
        cards.map(card => 
          card.id === cardId ? {
            ...card,
            lockedByUser: user
          } : card
        )
      );
    });
    conn.on("CardUnlocked", (cardId: string) => {
      console.log("CardUnlocked");
      cards.update(cards => 
        cards.map(card => 
          card.id === cardId ? {
            ...card,
            lockedByUser: null
          } : card
        )
      );
    });

		conn.on("SwimlaneCreated", (swimlaneResponse: DetailedSwimlaneResponse, updatedAtStr: string) => {
      console.log("SwimlaneCreated");
      const updatedAt = new Date(updatedAtStr);
			board.update(b => {
        b.swimlanes.push(swimlaneResponse);
        b.updatedAt = updatedAt;
        return b;
      });
		});

		conn.on("SwimlaneUpdated", (swimlaneResponse: SwimlaneResponse, updatedAtStr: string) => {
      console.log("SwimlaneUpdated");
			const updatedAt = new Date(updatedAtStr);
			board.update(b => {
        const index = b.swimlanes.findIndex(s => s.id === swimlaneResponse.id);
        if (index !== -1) {
          b.swimlanes[index] = { ...b.swimlanes[index], ...swimlaneResponse };
        }
        b.updatedAt = updatedAt;
        return b;
      });
		});

		conn.on("SwimlaneDeleted", (swimlaneId: string, updatedAtStr: string) => {
      console.log("SwimlaneDeleted");
			const updatedAt = new Date(updatedAtStr);
      board.update(b => {
        b.swimlanes = b.swimlanes.filter(s => s.id !== swimlaneId);
        b.updatedAt = updatedAt;
        return b;
      });
      if (selectedSwimlaneId === swimlaneId) {
        selectedSwimlaneId = $board.swimlanes[0]?.id;
      }
		});

		conn.on("ListUpdated", (swimlaneId: string, listResponse: ListResponse, updatedAtStr: string) => {
      console.log("ListUpdated");
			board.update(b => {
        const updatedAt = new Date(updatedAtStr);
        const swimlane = b.swimlanes.find(s => s.id === swimlaneId);
        if (swimlane) {
          const listIndex = swimlane.lists.findIndex(l => l.id === listResponse.id);
          if (listIndex !== -1) {
            swimlane.lists[listIndex] = listResponse;
          } else {
            invalidate('api:board');
          }
        } else {
          invalidate('api:board');
        }
        b.updatedAt = updatedAt;
        return b;
      });
		});
		conn.on("ListCreated", (swimlaneId: string, listResponse: ListResponse, updatedAtStr: string) => {
      console.log("ListCreated");
			board.update(b => {
        const updatedAt = new Date(updatedAtStr);
        const swimlane = b.swimlanes.find(s => s.id === swimlaneId);
        if (swimlane) {
          swimlane.lists.push(listResponse);
        } else {
          invalidate('api:board');
        }
        b.updatedAt = updatedAt;
        return b;
      });
		});
		conn.on("ListDeleted", (swimlaneId: string, listId: string, updatedAtStr: string) => {
      console.log("ListDeleted");
      const updatedAt = new Date(updatedAtStr);
      cards.update(cards => cards.filter(card => !(card.swimlaneId === swimlaneId && card.listId === listId)));
      board.update(b => {
        const swimlane = b.swimlanes.find(s => s.id === swimlaneId);
        if (swimlane) {
          swimlane.lists = swimlane.lists.filter(l => l.id !== listId);
        } else {
          invalidate('api:board');
        }
        b.updatedAt = updatedAt;
        return b;
      });
		});

		conn.on("TagUpdated", (swimlaneId: string, tagResponse: TagResponse, updatedAtStr: string) => {
      console.log("TagUpdated");
			const updatedAt = new Date(updatedAtStr);
      cards.update(cards => {
        cards.forEach(card => {
          if (card.swimlaneId === swimlaneId) {
            const tagIndex = card.tags.findIndex(t => t.id === tagResponse.id);
            if (tagIndex !== -1) {
              card.tags[tagIndex] = { ...card.tags[tagIndex], ...tagResponse };
              console.log("Updated tag in card", card.id, card.tags[tagIndex]);
            }
          }
        });
        return cards;
      })
			board.update(b => {
				const swimlane = b.swimlanes.find(s => s.id === swimlaneId);
				if (swimlane) {
					const tagIndex = swimlane.tags.findIndex(t => t.id === tagResponse.id);
					if (tagIndex !== -1) {
						swimlane.tags[tagIndex] = { ...swimlane.tags[tagIndex], ...tagResponse };
					} else {
						invalidate('api:board');
					}
				} else {
					invalidate('api:board');
				}
				b.updatedAt = updatedAt;
				return b;
			});
		});

		conn.on("TagCreated", (swimlaneId: string, tagResponse: TagResponse, updatedAtStr: string) => {
      console.log("TagCreated");
      const updatedAt = new Date(updatedAtStr);
      board.update(b => {
        const swimlane = b.swimlanes.find(s => s.id === swimlaneId);
        if (swimlane) {
          swimlane.tags.push(tagResponse);
        } else {
          invalidate('api:board');
        }
        b.updatedAt = updatedAt;
        return b;
      });
		});

		conn.on("TagDeleted", (swimlaneId: string, tagId: string, updatedAtStr: string) => {
      console.log("TagDeleted");
      const updatedAt = new Date(updatedAtStr);
      cards.update(cards => cards.map(card => {
        if (card.swimlaneId === swimlaneId) {
          card.tags = card.tags.filter(t => t.id !== tagId);
        }
        return card;
      }));
      board.update(b => {
        const swimlane = b.swimlanes.find(s => s.id === swimlaneId);
        if (swimlane) {
          swimlane.tags = swimlane.tags.filter(t => t.id !== tagId);
        } else {
          invalidate('api:board');
        }
        b.updatedAt = updatedAt;
        return b;
      });
		});
  }

	$effect(() => {
		if (data?.cards) {
			cards.set(data.cards);
		}
		if (data?.board) {
			board.set(data.board);
		}
	});

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
    <title>{$board.title}</title> 
</svelte:head>

<Sidebar me={data.user}/>
<ManageSwimlanePopup bind:this={swimlanePopup} boardId={data.board.id}/>
<div class="w-full overflow-y-auto">
	<div class="tabs gap-3 p-3">
		{#each $board.swimlanes as swimlane}
			<label
				class="tab border-border bg-component hover:bg-component-hover hover:border-border-hover w-40 flex-row justify-between rounded-md border-1 pr-2 [--tab-bg:orange]"
			>
				<input type="radio" name="tabs" value={swimlane.id} bind:group={selectedSwimlaneId} />
				{swimlane.title}
				{#if $board.members.some(member => member.userId === data.user.id && (member.role == BoardRole.Admin || member.role == BoardRole.Owner))}
				<button
					class="btn btn-xs btn-ghost z-50 aspect-square p-0"
					aria-label="More options"
					onclick={() => swimlanePopup?.show(swimlane)}
				>
				  <Menu />
				</button>
				{/if}
			</label>
			<div class="tab-content">
				<div class="divider mt-0 pt-0"></div>
				<Swimlane swimlane={swimlane} />
			</div>
		{/each}
		{#if $board.members.some(member => member.userId === data.user.id && (member.role == BoardRole.Admin || member.role == BoardRole.Owner))}
		<button
			class="tab btn btn-md btn-ghost border-border bg-component hover:bg-component-hover hover:border-border-hover rounded-md border-1 w-10 p-1"
			onclick={() => swimlanePopup?.show()}
		>
			<Plus />
		</button>
		{/if}
	</div>
</div>

