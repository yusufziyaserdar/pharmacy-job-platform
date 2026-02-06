const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .withAutomaticReconnect()
    .build();

async function refreshUnreadCount() {
    const badge = document.getElementById("unreadBadge");
    if (!badge) {
        return;
    }

    const response = await fetch("/Messages/UnreadCount");
    if (!response.ok) {
        return;
    }

    const data = await response.json();
    const count = data.unreadCount ?? 0;

    if (count > 0) {
        badge.textContent = count;
        badge.classList.remove("d-none");
    } else {
        badge.textContent = "";
        badge.classList.add("d-none");
    }
}

connection.on("RefreshMessages", (conversationId) => {
    refreshUnreadCount();

    if (typeof window.refreshWidgetConversation === "function") {
        window.refreshWidgetConversation();
    }

    if (typeof window.refreshChatMessages === "function" &&
        window.currentConversationId === conversationId) {
        window.refreshChatMessages();
    }
});

connection.start().catch(() => {
// Fail silently; UI will still work via manual reloads.
});
