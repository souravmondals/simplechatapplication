--  <author>Sourav Mondal/author>
--  <summary>Chat application</summary>

-- ============================================================
-- SimpleChat Database Schema (SQL Server)
-- ============================================================

-- CREATE DATABASE SimpleChat;
-- GO
-- USE SimpleChat;
-- GO

-- 1) Users table -----------------------------------------------
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId       INT IDENTITY(1,1) PRIMARY KEY,
        UserName     NVARCHAR(100) NOT NULL,
        DisplayName  NVARCHAR(150) NOT NULL,
        CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    -- UserName is used to log in, so keep it unique
    CREATE UNIQUE INDEX UX_Users_UserName ON dbo.Users(UserName);
END
GO

-- 2) Messages table (1-to-1 chat history) ----------------------
IF OBJECT_ID('dbo.Messages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Messages
    (
        MessageId   BIGINT IDENTITY(1,1) PRIMARY KEY,
        SenderId    INT NOT NULL,
        ReceiverId  INT NOT NULL,
        Content     NVARCHAR(2000) NOT NULL,
        SentAt      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Messages_Sender   FOREIGN KEY (SenderId)   REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Messages_Receiver FOREIGN KEY (ReceiverId) REFERENCES dbo.Users(UserId)
    );

    -- Speeds up loading a conversation between two users
    CREATE INDEX IX_Messages_Conversation
        ON dbo.Messages(SenderId, ReceiverId, SentAt);
END
GO

-- 3) Seed some sample users ------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    INSERT INTO dbo.Users (UserName, DisplayName) VALUES
        ('sourav',  'Sourav Mondal'),
        ('arbaz',   'Arbaz Siddiqui'),
        ('partha',  'Partha Buragohain'),
        ('ganesh',  'Ganesh Kapse'),
        ('padma',   'Padmanabham KG');
END
GO
ALTER TABLE dbo.Messages ADD IsRead BIT NOT NULL DEFAULT 0;
GO
-- Helps the unread-count query
CREATE INDEX IX_Messages_Unread ON dbo.Messages(ReceiverId, IsRead, SenderId);
GO
