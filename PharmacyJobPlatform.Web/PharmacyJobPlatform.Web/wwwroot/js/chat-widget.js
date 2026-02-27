const panel = document.getElementById("chatPanel");
const toggleButton = document.getElementById("chat-toggle");
const closeButton = document.getElementById("chat-close");
const widgetStorageKey = "chatWidgetOpen";
let widgetConversationId = null;

function escapeHtml(value) {
    return (value ?? "")
        .toString()
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

function setWidgetOpenState(isOpen) {
    if (!panel) {
        return;
    }

    panel.classList.toggle("d-none", !isOpen);

    try {
        localStorage.setItem(widgetStorageKey, isOpen ? "1" : "0");
    } catch (error) {
    }
}

function isWidgetOpen() {
    if (!panel) {
        return false;
    }

    return !panel.classList.contains("d-none");
}

if (toggleButton) {
    toggleButton.onclick = () => {
        setWidgetOpenState(!isWidgetOpen());
    };
}

if (closeButton) {
    closeButton.onclick = () => {
        setWidgetOpenState(false);
    };
}

function loadConversations() {
    fetch("/Messages/WidgetConversations")
        .then(r => r.json())
        .then(data => {
            const box = document.getElementById("chat-messages");
            box.innerHTML = "";
            widgetConversationId = null;

            if (!data.length) {
                box.innerHTML = '<div class="chat-placeholder text-center pt-4">Henüz konuşma yok.</div>';
                return;
            }

            const list = document.createElement("div");
            list.className = "chat-list";

            data.forEach(c => {
                const item = document.createElement("button");
                item.type = "button";
                item.className = "chat-list-item";
                item.onclick = () => openConversation(c.conversationId);

                const safeName = escapeHtml(c.otherUserName);
                const safePreview = escapeHtml(c.lastMessage || "Mesaj bulunmuyor");

                item.innerHTML = `
                    <strong>
                        <a href="/Profile/${c.otherUserId}" class="chat-user-link" onclick="event.stopPropagation()">
                            ${safeName}
                        </a>
                    </strong>
                    <small class="chat-last-message">${safePreview}</small>`;

                list.appendChild(item);
            });

            box.appendChild(list);
        });
}

function openConversation(id) {
    widgetConversationId = id;
    fetch(`/Messages/WidgetChat?conversationId=${id}`)
        .then(r => r.text())
        .then(html => {
            document.getElementById("chat-messages").innerHTML = html;
            scrollWidgetToBottom();
        });
}

function sendWidgetMessage(e, convId) {
    e.preventDefault();
    const input = document.getElementById("widgetMessageInput");

    fetch("/Messages/SendMessage", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: `conversationId=${convId}&content=${encodeURIComponent(input.value)}`
    }).then(() => {
        input.value = "";
        openConversation(convId);
    });
}

function scrollWidgetToBottom() {
    const el = document.getElementById("chat-messages");
    if (el) {
        el.scrollTop = el.scrollHeight;
    }
}

function refreshWidgetConversation() {
    if (widgetConversationId) {
        openConversation(widgetConversationId);
    } else {
        loadConversations();
    }
}

window.loadConversations = loadConversations;
window.openConversation = openConversation;
window.refreshWidgetConversation = refreshWidgetConversation;


try {
    const savedState = localStorage.getItem(widgetStorageKey);
    if (savedState === "1") {
        setWidgetOpenState(true);
    }
} catch (error) {
}

loadConversations();
