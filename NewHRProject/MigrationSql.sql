CREATE DATABASE NewHRProjectDb;
GO

use [NewHRProjectDb]
go

CREATE TABLE [dbo].[Users] (
    Id INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
    UserName VARCHAR(255) NOT NULL,
    FirstName VARCHAR(255) NOT NULL,
    LastName VARCHAR(255) NOT NULL,
    CreationDate DATETIME NOT NULL
);
GO

CREATE TABLE [dbo].[UserScores] (
    Id INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
    UserId  INT NOT NULL ,
    Score FLOAT NOT NULL,
    CreationDate DATETIME NOT NULL
);

GO
ALTER TABLE [dbo].[UserScores]
ADD CONSTRAINT FK_UserScores_Users
FOREIGN KEY (UserId) REFERENCES [dbo].[Users](Id);

GO



use [NewHRProjectDb]
Go
CREATE PROCEDURE GetUserRank
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    WITH ScoreTotals AS (
        SELECT
            [dbo].[UserScores].[UserId],
            SUM([dbo].[UserScores].[Score]) AS TotalScore
        FROM
            [dbo].[UserScores]
        WHERE
            YEAR([dbo].[UserScores].[CreationDate]) = YEAR(CURRENT_TIMESTAMP) AND
            MONTH([dbo].[UserScores].[CreationDate]) = MONTH(CURRENT_TIMESTAMP)
        GROUP BY
            [dbo].[UserScores].[UserId]
    ),
    RankedScores AS (
        SELECT
            UserId,
            TotalScore,
            RANK() OVER (ORDER BY TotalScore DESC) AS Rank
        FROM
            ScoreTotals
    )
    SELECT
        Rank
    FROM
        RankedScores
    WHERE
        UserId = @UserId;
END
GO
