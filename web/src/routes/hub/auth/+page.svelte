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
            .withUrl(env.PUBLIC_API_SERVER + "hubs/chathub", {
                accessTokenFactory: () => 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJkNDQ0NjVlZC1iOWIxLTQxN2UtOWFlNy02N2RkMTc4N2UwNTYiLCJzdWIiOiI2ODM1ZjYyNmEzN2E4MjBhY2YyODBhNDMiLCJuaWNrbmFtZSI6IkFsZWtzR3J6eWJlayIsImV4cCI6MTc0ODg5Njc2MCwiaXNzIjoiQm9hcmRseSIsImF1ZCI6ImJvYXJkbHktYXBwLWNsaWVudCJ9.dvUx6nnf3hWtARg1HVrloRiPHJjpqc-iUYXWU0HnWJ0'
            })
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