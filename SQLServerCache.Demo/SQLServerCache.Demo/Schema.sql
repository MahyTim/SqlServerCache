CREATE SCHEMA [Cache]
GO

CREATE TABLE [Cache].[CacheItemMetaData](
	[InternalId] [bigint] IDENTITY(1,1) NOT NULL,
	[Key] [nvarchar](255) NOT NULL,
	[CreatedTimestamp] [datetime2](7) NOT NULL,
	[UpdatedTimestamp] [datetime2](7) NOT NULL,
	[ExpirationTimestamp] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_CacheItemMetaData] PRIMARY KEY CLUSTERED 
(
	[InternalId] ASC,
	[ExpirationTimestamp] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

CREATE TABLE [Cache].[CacheItem](
	[InternalId] [bigint] NOT NULL,
	[Content] [varbinary](max) NOT NULL,
	[IsEmpty] bit default 1
PRIMARY KEY CLUSTERED 
(
	[InternalId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

CREATE PROCEDURE [Cache].[GetCacheItemMetaData]
(
	@key as NVARCHAR(255)
)
AS
DECLARE @Now DATETIME2
SET @Now = SYSUTCDATETIME()

SELECT TOP 1 [InternalId],[UpdatedTimestamp],IIF(DATEDIFF(millisecond,ExpirationTimestamp,@Now) > 0,1,0) AS IsExpired FROM [Cache].[CacheItemMetaData]
WHERE [Key] = @key

GO
CREATE PROCEDURE [Cache].[AddOrRenewCacheItem]
(
	@key as NVARCHAR(255),
	@expireAfterMinutes INT
)
AS 
SET NOCOUNT ON;

DECLARE @Now DATETIME2
SET @Now = SYSUTCDATETIME()

DECLARE @ExpirationDate DATETIME2
SET @ExpirationDate = DATEADD (minute,@expireAfterMinutes,@Now)

DECLARE @Output AS TABLE (Id BIGINT,ActionTaken nvarchar(10))


MERGE [Cache].[CacheItemMetaData] AS target
USING (SELECT @Key, @ExpirationDate) AS source ([Key], [ExpirationTimestamp])
ON (target.[Key] = source.[Key])
WHEN MATCHED THEN 
        UPDATE SET [ExpirationTimestamp] = source.[ExpirationTimestamp], 
				   [UpdatedTimestamp] = @Now
WHEN NOT MATCHED THEN
    INSERT ([Key], [CreatedTimestamp],[UpdatedTimestamp],[ExpirationTimestamp])
    VALUES (source.[Key],@Now,@Now, source.[ExpirationTimestamp])
OUTPUT Inserted.InternalId, $action INTO @Output;

INSERT INTO [Cache].[CacheItem] (InternalId,Content)
SELECT [Id],0x00 FROM @Output WHERE ActionTaken = 'INSERT' 

EXEC [Cache].[GetCacheItemMetaData] @key


GO
CREATE NONCLUSTERED INDEX [IX_Key_Unique] ON [Cache].[CacheItemMetaData]
(
	[Key] ASC
)
INCLUDE ( 	[UpdatedTimestamp],
	[ExpirationTimestamp]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


