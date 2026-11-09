-- RaceDay Part 3: adds media/type columns required by the MVC front end.
-- Safe to re-run; each statement is guarded.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'EventType')
    ALTER TABLE Events ADD EventType NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'DistanceKM')
    ALTER TABLE Events ADD DistanceKM DECIMAL(6,2) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'BannerBlobName')
    ALTER TABLE Events ADD BannerBlobName NVARCHAR(300) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'BannerContentType')
    ALTER TABLE Events ADD BannerContentType NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'ProfilePictureBlobName')
    ALTER TABLE Users ADD ProfilePictureBlobName NVARCHAR(300) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'ProfilePictureContentType')
    ALTER TABLE Users ADD ProfilePictureContentType NVARCHAR(100) NULL;
GO
