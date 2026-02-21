IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AdminUsers] (
    [Id] uniqueidentifier NOT NULL,
    [DisplayName] nvarchar(120) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AdminUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Categories] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(120) NOT NULL,
    [Slug] nvarchar(160) NOT NULL,
    [ParentCategoryId] uniqueidentifier NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedByAdminUserId] uniqueidentifier NULL,
    [UpdatedByAdminUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_AdminUsers_CreatedByAdminUserId] FOREIGN KEY ([CreatedByAdminUserId]) REFERENCES [AdminUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Categories_AdminUsers_UpdatedByAdminUserId] FOREIGN KEY ([UpdatedByAdminUserId]) REFERENCES [AdminUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Products] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Slug] nvarchar(220) NOT NULL,
    [Description] nvarchar(4000) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Condition] tinyint NOT NULL DEFAULT CAST(2 AS tinyint),
    [Status] tinyint NOT NULL DEFAULT CAST(1 AS tinyint),
    [CategoryId] uniqueidentifier NOT NULL,
    [SoldAt] datetime2 NULL,
    [OffShelvedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedByAdminUserId] uniqueidentifier NULL,
    [UpdatedByAdminUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Products_Price] CHECK ([Price] > 0),
    CONSTRAINT [FK_Products_AdminUsers_CreatedByAdminUserId] FOREIGN KEY ([CreatedByAdminUserId]) REFERENCES [AdminUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Products_AdminUsers_UpdatedByAdminUserId] FOREIGN KEY ([UpdatedByAdminUserId]) REFERENCES [AdminUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Inquiries] (
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [CustomerName] nvarchar(120) NULL,
    [Email] nvarchar(256) NULL,
    [PhoneNumber] nvarchar(40) NULL,
    [Message] nvarchar(3000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [EmailDeliveryStatus] tinyint NOT NULL DEFAULT CAST(1 AS tinyint),
    [DeliveredAt] datetime2 NULL,
    [DeliveryError] nvarchar(1000) NULL,
    [EmailSendAttempts] int NOT NULL DEFAULT 0,
    [NextRetryAt] datetime2 NULL,
    CONSTRAINT [PK_Inquiries] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Inquiries_AtLeastOneContact] CHECK ((NULLIF(LTRIM(RTRIM([Email])), '') IS NOT NULL) OR (NULLIF(LTRIM(RTRIM([PhoneNumber])), '') IS NOT NULL)),
    CONSTRAINT [FK_Inquiries_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProductImages] (
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [CloudStorageKey] nvarchar(500) NOT NULL,
    [Url] nvarchar(1024) NOT NULL,
    [AltText] nvarchar(300) NULL,
    [SortOrder] int NOT NULL,
    [IsPrimary] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedByAdminUserId] uniqueidentifier NULL,
    [UpdatedByAdminUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductImages_AdminUsers_CreatedByAdminUserId] FOREIGN KEY ([CreatedByAdminUserId]) REFERENCES [AdminUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ProductImages_AdminUsers_UpdatedByAdminUserId] FOREIGN KEY ([UpdatedByAdminUserId]) REFERENCES [AdminUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_AdminUsers_Email] ON [AdminUsers] ([Email]);

CREATE INDEX [IX_Categories_CreatedByAdminUserId] ON [Categories] ([CreatedByAdminUserId]);

CREATE INDEX [IX_Categories_ParentCategoryId_SortOrder] ON [Categories] ([ParentCategoryId], [SortOrder]);

CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);

CREATE INDEX [IX_Categories_UpdatedByAdminUserId] ON [Categories] ([UpdatedByAdminUserId]);

CREATE INDEX [IX_Inquiries_ProductId_CreatedAt] ON [Inquiries] ([ProductId], [CreatedAt]);

CREATE INDEX [IX_ProductImages_CreatedByAdminUserId] ON [ProductImages] ([CreatedByAdminUserId]);

CREATE UNIQUE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]) WHERE [IsPrimary] = 1;

CREATE INDEX [IX_ProductImages_ProductId_SortOrder] ON [ProductImages] ([ProductId], [SortOrder]);

CREATE INDEX [IX_ProductImages_UpdatedByAdminUserId] ON [ProductImages] ([UpdatedByAdminUserId]);

CREATE INDEX [IX_Products_CategoryId_Status] ON [Products] ([CategoryId], [Status]);

CREATE INDEX [IX_Products_CreatedByAdminUserId] ON [Products] ([CreatedByAdminUserId]);

CREATE UNIQUE INDEX [IX_Products_Slug] ON [Products] ([Slug]);

CREATE INDEX [IX_Products_Status_UpdatedAt] ON [Products] ([Status], [UpdatedAt]);

CREATE INDEX [IX_Products_UpdatedByAdminUserId] ON [Products] ([UpdatedByAdminUserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260221061722_InitialCreate', N'10.0.3');

COMMIT;
GO

