const panel = document.getElementById("chatPanel");
const toggleButton = document.getElementById("chat-toggle");
const closeButton = document.getElementById("chat-close");
const widgetStorageKey = "chatWidgetOpen";
let widgetConversationId = null;

function setWidgetOpenState(isOpen) {
    if (!panel) {
        return;
    }

    panel.classList.toggle("d-none", !isOpen);

    try {
        localStorage.setItem(widgetStorageKey, isOpen ? "1" : "0");
    } catch (error) {
        // Ignore storage issues and keep UI responsive.
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
            scrollWidgetToBottom();
            box.innerHTML = "";
            widgetConversationId = null;

            data.forEach(c => {
                box.innerHTML += `
    <div class="border-bottom p-2"
         style="cursor:pointer"
         onclick="openConversation(${c.conversationId})">

        <strong>
            <a href="/Profile/${c.otherUserId}"
               class="text-decoration-none"
               onclick="event.stopPropagation()">
                ${c.otherUserName}
            </a>
        </strong><br/>

        <small>${c.lastMessage ?? ""}</small>
    </div>`;

            });
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
    // Ignore storage issues and keep UI responsive.
}

loadConversations();
