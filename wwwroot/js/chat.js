// <author>Sourav Mondal/author>
// <summary>Chat application</summary>

// ---- Get the logged-in user (set on the login page) --------------------
const me = JSON.parse(sessionStorage.getItem('chatUser') || 'null');
if (!me) {
    window.location.href = '/'; // not logged in -> back to login
}

let selectedUser = null; // the user we are currently chatting with

let unread = {};   // { userId: count } — now loaded from the server
const onlineUsers = new Set();

const conversationUsers = new Map();      // 👈 userId -> user (left panel)
const searchResultsEl = document.getElementById('searchResults');  // 👈 ADD

// Load unread counts from DB and refresh the list
async function loadUnread() {
    const res = await fetch(`/api/unread?me=${me.userId}`);
    const rows = await res.json();
    unread = {};
   
    rows.forEach(r => { unread[r.senderId] = r.count; });   // lowercase!
    console.log('Unread loaded:', unread);                  // ADD THIS to debug
    
}

// ---- DOM refs ----------------------------------------------------------
const meNameEl    = document.getElementById('meName');
const searchEl    = document.getElementById('search');
const userListEl  = document.getElementById('userList');
const chatWithEl  = document.getElementById('chatWith');
const messagesEl  = document.getElementById('messages');
const composerEl  = document.getElementById('composer');
const messageInput= document.getElementById('messageInput');

meNameEl.textContent = me.displayName;

// ---- SignalR connection ------------------------------------------------
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/chatHub')
    .withAutomaticReconnect()
    .build();

connection.on('ReceiveMessage', (msg) => {
    const isOpen = selectedUser &&
        ((msg.senderId === me.userId && msg.receiverId === selectedUser.userId) ||
            (msg.senderId === selectedUser.userId && msg.receiverId === me.userId));

    if (isOpen) {
        appendMessage(msg);
        // instantly mark as read since the chat is open
        if (msg.receiverId === me.userId) {
            fetch('/api/read', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ me: me.userId, other: selectedUser.userId })
            });
        }
    } else if (msg.receiverId === me.userId) {
        unread[msg.senderId] = (unread[msg.senderId] || 0) + 1;
        // If this is someone new, reload conversations so they appear
        if (!conversationUsers.has(msg.senderId)) {
            loadConversations();          // pulls the new partner from DB
        } else {
            renderConversations();
        }
    }

});

async function startConnection() {
    await connection.start();
    await connection.invoke('Register', me.userId);

    // 👇 fetch who is already online
    const online = await connection.invoke('GetOnlineUsers');
    online.forEach(id => onlineUsers.add(id));
    loadUsers(searchEl.value.trim());
}
startConnection().catch(console.error);

// 👇 react to live presence changes
connection.on('PresenceChanged', (userId, isOnline) => {
    if (isOnline) onlineUsers.add(userId);
    else onlineUsers.delete(userId);
    renderConversations();                                   // 👈 was loadUsers(...)
    if (searchEl.value.trim()) searchUsers(searchEl.value.trim());
});

// ---- Search users ------------------------------------------------------

// Left panel: only people you've chatted with
async function loadConversations() {
    await loadUnread();
    const res = await fetch(`/api/conversations?me=${me.userId}`);
    const partners = await res.json();
    partners.forEach(u => conversationUsers.set(u.userId, u));
    renderConversations();
}

// Render the left list from the conversationUsers map
function renderConversations() {
    userListEl.innerHTML = '';
    const users = Array.from(conversationUsers.values())
        .sort((a, b) => a.displayName.localeCompare(b.displayName));

    users.forEach(u => {
        const li = document.createElement('li');
        li.className = selectedUser && selectedUser.userId === u.userId ? 'active' : '';
        li.onclick = () => selectUser(u);

        const dot = document.createElement('span');
        dot.className = 'status-dot ' + (onlineUsers.has(u.userId) ? 'online' : 'offline');
        li.appendChild(dot);

        const name = document.createElement('span');
        name.textContent = u.displayName;
        name.style.flex = '1';
        li.appendChild(name);

        const count = unread[u.userId] || 0;
        if (count > 0) {
            const badge = document.createElement('span');
            badge.className = 'badge';
            badge.textContent = count;
            li.appendChild(badge);
        }
        userListEl.appendChild(li);
    });
}

// Add a user to the left panel if not already there
function addConversation(user) {
    if (!conversationUsers.has(user.userId)) {
        conversationUsers.set(user.userId, user);
        renderConversations();
    }
}


async function searchUsers(term) {
    if (!term) { searchResultsEl.innerHTML = ''; return; }
    const res = await fetch(`/api/users/search?term=${encodeURIComponent(term)}&me=${me.userId}`);
    const users = await res.json();
    searchResultsEl.innerHTML = '';

    users.forEach(u => {
        const li = document.createElement('li');

        const dot = document.createElement('span');
        dot.className = 'status-dot ' + (onlineUsers.has(u.userId) ? 'online' : 'offline');
        li.appendChild(dot);

        const name = document.createElement('span');
        name.textContent = u.displayName;
        li.appendChild(name);

        li.onclick = () => {
            addConversation(u);          // add to left panel
            searchEl.value = '';         // clear search
            searchResultsEl.innerHTML = '';
            selectUser(u);               // open the chat
        };
        searchResultsEl.appendChild(li);
    });
}

let searchTimer;
searchEl.addEventListener('input', () => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(() => searchUsers(searchEl.value.trim()), 250);
});

// ---- Open a conversation ----------------------------------------------
async function selectUser(user) {
    selectedUser = user;
    addConversation(user);   // 👈 make sure they're in the left panel
    const status = onlineUsers.has(user.userId) ? '🟢 Online' : '⚪ Offline';
    chatWithEl.textContent = `Chat with ${user.displayName} · ${status}`;
    composerEl.style.display = 'flex';

    await fetch('/api/read', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ me: me.userId, other: user.userId })
    });
    unread[user.userId] = 0;
    renderConversations();   // 👈 was loadUsers(...)

    const res = await fetch(`/api/messages?me=${me.userId}&other=${user.userId}`);
    const history = await res.json();
    messagesEl.innerHTML = '';
    history.forEach(appendMessage);
}

// ---- Render a message --------------------------------------------------
function appendMessage(msg) {
    const div = document.createElement('div');
    const mine = msg.senderId === me.userId;
    div.className = `bubble ${mine ? 'mine' : 'theirs'}`;
    const time = new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    div.innerHTML = `<div class="text"></div><div class="time">${time}</div>`;
    div.querySelector('.text').textContent = msg.content; // safe from XSS
    messagesEl.appendChild(div);
    messagesEl.scrollTop = messagesEl.scrollHeight;
}

// ---- Send a message ----------------------------------------------------
composerEl.addEventListener('submit', async (e) => {
    e.preventDefault();
    const text = messageInput.value.trim();
    if (!text || !selectedUser) return;
    await connection.invoke('SendMessage', me.userId, selectedUser.userId, text);
    messageInput.value = '';
});

// ---- Logout ------------------------------------------------------------
document.getElementById('logout').onclick = () => {
    sessionStorage.removeItem('chatUser');
    window.location.href = '/';
};

// Initial load
//loadUsers();
loadConversations();   // 👈 was loadUsers();
setInterval(() => loadUsers(searchEl.value.trim()), 15000);
startConnection();

