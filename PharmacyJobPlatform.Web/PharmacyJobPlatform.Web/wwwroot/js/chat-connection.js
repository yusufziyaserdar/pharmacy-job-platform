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
    const normalizedId = Number(conversationId);
    refreshUnreadCount();

    if (typeof window.refreshWidgetConversation === "function") {
        window.refreshWidgetConversation();
    }

    if (typeof window.refreshInboxConversation === "function") {
        window.refreshInboxConversation(normalizedId);
    }


    if (typeof window.refreshChatMessages === "function" &&
        Number(window.currentConversationId) === normalizedId) {
        window.refreshChatMessages();
    }
});

async function startConnection() {
    try {
        await connection.start();
        refreshUnreadCount();
    } catch (error) {
        setTimeout(startConnection, 5000);
    }
}

connection.onreconnected(() => {
    refreshUnreadCount();

    if (typeof window.refreshWidgetConversation === "function") {
        window.refreshWidgetConversation();
    }

    if (typeof window.refreshChatMessages === "function") {
        window.refreshChatMessages();
    }
});

startConnection();
