# SimpleChat 💬

A **simple, real-time 1-to-1 web chat application** built with **ASP.NET Core 8 + SignalR + SQL Server + Dapper** and a lightweight **HTML/JS** frontend.

Inspired by (but much simpler than) the `signalr-chat-master` sample — **no channels**, just direct user-to-user messaging.

## Features
- 🔐 **Login by User ID** — the ID must exist in the SQL `Users` table
- 🔍 **Search users** and pick anyone to chat with
- ⚡ **Real-time** direct messaging via SignalR
- 💾 **Message history** persisted in SQL Server
- 🌐 Host it anywhere — anyone with the URL can log in

## Tech Stack
| Layer     | Technology |
|-----------|------------|
| Backend   | ASP.NET Core 8, SignalR |
| Database  | SQL Server |
| Data access | Dapper (micro ORM) |
| Frontend  | HTML, CSS, vanilla JavaScript, SignalR JS client |

## Project Structure
```
SimpleChat/
├── SimpleChat.csproj
├── Program.cs                # App startup + REST endpoints + SignalR mapping
├── appsettings.json          # SQL connection string
├── Hubs/
│   └── ChatHub.cs            # Real-time 1-to-1 messaging hub
├── Models/
│   ├── User.cs
│   └── Message.cs
├── Data/
│   └── ChatRepository.cs     # Dapper SQL data access
├── Database/
│   └── schema.sql            # Tables + sample seed users
└── wwwroot/
    ├── index.html            # Login page
    ├── chat.html             # Chat UI
    ├── js/chat.js            # Frontend logic + SignalR client
    └── css/style.css
```

## Getting Started

### 1. Create the database
Open `Database/schema.sql` in SQL Server Management Studio (or `sqlcmd`),
create a database called `SimpleChat`, and run the script. It creates the
`Users` and `Messages` tables and seeds a few sample users.

### 2. Configure the connection string
Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "ConnectionString": "Server=YOUR_SERVER;Database=SimpleChat;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Run the app
```bash
dotnet restore
dotnet run
```
Then open the URL shown in the console (e.g. `https://localhost:5001`).

### 4. Log in
Use one of the seeded IDs: `sourav`, `arbaz`, `partha`, `ganesh`, `padma`.
Open a second browser (or incognito) and log in as a different user to chat in real time.

## Adding real users
Just insert rows into `dbo.Users`:
```sql
INSERT INTO dbo.Users (UserName, DisplayName)
VALUES ('john', 'John Doe');
```
The `UserName` is what people type on the login screen.

## Deploying
Publish and host on IIS, Azure App Service, or any container:
```bash
dotnet publish -c Release -o ./publish
```
Point the connection string at your production SQL Server. Anyone with the
site URL can log in with a valid User ID.

## Notes / Possible Enhancements
- Add real authentication (passwords / Azure AD) — currently ID-only for simplicity.
- Show online/offline status (the hub already tracks online users).
- Add "unread" badges and typing indicators.
- Add group chat later if ever needed.
