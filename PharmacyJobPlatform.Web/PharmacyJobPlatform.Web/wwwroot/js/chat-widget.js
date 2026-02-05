const panel = document.getElementById("chatPanel");

document.getElementById("chat-toggle").onclick = () =>
    panel.classList.toggle("d-none");

document.getElementById("chat-close").onclick = () =>
    panel.classList.add("d-none");

function loadConversations() {
    fetch("/Messages/WidgetConversations")
        .then(r => r.json())
        .then(data => {
            const box = document.getElementById("chat-messages");
            scrollWidgetToBottom();
            box.innerHTML = "";

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
    }).then(() => openConversation(convId));
}

function scrollWidgetToBottom() {
    const el = document.getElementById("chat-messages");
    if (el) {
        el.scrollTop = el.scrollHeight;
    }
}


loadConversations();
