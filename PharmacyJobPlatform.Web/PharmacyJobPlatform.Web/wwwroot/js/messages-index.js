const inboxThread = document.getElementById("inboxThread");
const inboxConversations = document.getElementById("inboxConversations");
let activeConversationId = null;

function setEmptyThread() {
    if (!inboxThread) {
        return;
    }

    inboxThread.innerHTML = `
        <div class="messages-empty">
            <i class="bi bi-chat-square-text"></i>
            <p>Bir konuşma seçerek mesajları görüntüleyin.</p>
        </div>`;
}

function highlightConversation(id) {
    if (!inboxConversations) {
        return;
    }

    inboxConversations.querySelectorAll(".messages-conversation").forEach(item => {
        item.classList.toggle("active", item.dataset.id === String(id));
    });
}

function scrollThreadToBottom() {
    const body = document.getElementById("messagesThreadBody");
    if (body) {
        body.scrollTop = body.scrollHeight;
    }
}

function openInboxConversation(id) {
    activeConversationId = Number(id);
    fetch(`/Messages/InboxMessages?conversationId=${id}`)
        .then(r => r.text())
        .then(html => {
            if (inboxThread) {
                inboxThread.innerHTML = html;
                scrollThreadToBottom();
            }
            highlightConversation(id);
        });
}

function refreshInboxList() {
    if (!inboxConversations) {
        return;
    }

    fetch("/Messages/InboxConversations")
        .then(r => r.text())
        .then(html => {
            inboxConversations.innerHTML = html;
            if (activeConversationId) {
                highlightConversation(activeConversationId);
            }
            const count = inboxConversations.querySelectorAll(".messages-conversation").length;
            const countEl = document.getElementById("conversationCount");
            if (countEl) {
                countEl.textContent = count;
            }
        });
}

function refreshInboxConversation(conversationId) {
    const normalizedId = Number(conversationId);
    refreshInboxList();

    if (activeConversationId && activeConversationId === normalizedId) {
        openInboxConversation(normalizedId);
    }
}

function sendInboxMessage(e, conversationId) {
    e.preventDefault();
    const input = document.getElementById("inboxMessageInput");
    if (!input || !input.value.trim()) {
        return;
    }

    const payload = new URLSearchParams();
    payload.set("conversationId", conversationId);
    payload.set("content", input.value);

    fetch("/Messages/SendMessageAjax", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: payload.toString()
    }).then(response => {
        if (response.ok) {
            input.value = "";
            openInboxConversation(conversationId);
            updateConversationPreview(conversationId, payload.get("content"));
        }
    });
}

function updateConversationPreview(conversationId, message) {
    if (!inboxConversations) {
        return;
    }

    const item = inboxConversations.querySelector(`.messages-conversation[data-id="${conversationId}"]`);
    if (!item) {
        return;
    }

    const preview = item.querySelector(".messages-conversation-preview");
    if (preview) {
        preview.textContent = message;
    }
}

function deleteConversation(conversationId) {
    if (!confirm("Bu konuşmayı silmek istediğinize emin misiniz?")) {
        return;
    }

    fetch("/Messages/DeleteConversation", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: `conversationId=${conversationId}`
    }).then(response => {
        if (!response.ok) {
            return;
        }

        const item = inboxConversations?.querySelector(`.messages-conversation[data-id="${conversationId}"]`);
        item?.remove();

        if (activeConversationId === conversationId) {
            activeConversationId = null;
            setEmptyThread();
        }
    });
}

function deleteMessage(messageId, conversationId) {
    fetch("/Messages/DeleteMessage", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: `messageId=${messageId}`
    }).then(response => {
        if (response.ok) {
            openInboxConversation(conversationId);
        }
    });
}

function refreshInboxConversations() {
    window.location.reload();
}

window.openInboxConversation = openInboxConversation;
window.sendInboxMessage = sendInboxMessage;
window.deleteConversation = deleteConversation;
window.deleteMessage = deleteMessage;
window.refreshInboxConversations = refreshInboxConversations;
window.refreshInboxConversation = refreshInboxConversation;
window.refreshInboxList = refreshInboxList;

if (window.initialConversationId && window.initialConversationId > 0) {
    openInboxConversation(window.initialConversationId);
} else {
    setEmptyThread();
}


document.addEventListener("click", event => {
    document.querySelectorAll(".messages-menu[open]").forEach(menu => {
        if (!menu.contains(event.target)) {
            menu.removeAttribute("open");
        }
    });
});