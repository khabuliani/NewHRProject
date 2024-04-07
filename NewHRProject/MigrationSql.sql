CREATE DATABASE NewHRProjectDb;
GO

use [NewHRProjectDb]
go

CREATE TABLE [dbo].[Users] (
    Id INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
    UserName VARCHAR(255) NOT NULL UNIQUE,
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



Use [NewHRProjectDb]
Go 
CREATE PROCEDURE GetUserRankAndTotalScore
    @UserId INT,
    @Rank INT OUTPUT,
    @TotalScore FLOAT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    WITH MonthlyScores AS (
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
    ScoreRanks AS (
        SELECT
            UserId,
            TotalScore,
            RANK() OVER (ORDER BY TotalScore DESC) AS UserRank
        FROM
            MonthlyScores
    )
    SELECT
        @Rank = UserRank,
        @TotalScore = TotalScore
    FROM
        ScoreRanks
    WHERE
        UserId = @UserId;
END
GO