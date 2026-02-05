"use strict";

window.onChatMessageReceived = function (data) {

    const activeConvId =
        document.getElementById("messageInput")?.dataset.conversationId;

    if (!activeConvId || parseInt(activeConvId) !== data.conversationId)
        return;

    appendMessage(data.senderId, data.content, data.sentAt);
};

document.getElementById("sendMessageBtn")?.addEventListener("click", async () => {

    const input = document.getElementById("messageInput");
    const conversationId = parseInt(input.dataset.conversationId);
    const message = input.value.trim();

    if (!message) return;

    await chatConnection.invoke("SendMessage", conversationId, message);
    input.value = "";
});

function appendMessage(senderId, message, sentAt) {
    const list = document.getElementById("chatMessages");

    const li = document.createElement("li");
    li.className = "list-group-item border-0 text-end";
    li.innerHTML = `
        <div class="d-inline-block p-2 rounded bg-primary text-white">
            ${message}
            <small class="d-block text-muted">${sentAt}</small>
        </div>
    `;

    list.appendChild(li);
    list.scrollTop = list.scrollHeight;
}
