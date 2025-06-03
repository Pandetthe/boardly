<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import * as signalR from '@microsoft/signalr';
    import { env } from '$env/dynamic/public';

    let content: string[] = [];

    let connection: signalR.HubConnection;

    function start() {
        console.log(document.cookie.split('; ').find(row => row.startsWith('accessToken='))
                    ?.split('=')[1] || '');

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/api/hubs/chathub")
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.on("ReceiveMessage", (message: string) => {
            content = [...content, message];
        });

        connection.start().catch(err => console.error("Error while starting connection: ", err));
    }

    onMount(() => {
        start();
    });

    onDestroy(() => {
    if (connection) {
        connection.stop();
    }
    });
    
</script>
<button class="btn btn-primary mb-4" on:click={() => connection.invoke("SendMessage", "cokolwiek", "Hello from Svelte!")}>
    Send Message
</button>
{#each content as item}
    <p>{item}</p>
{/each}