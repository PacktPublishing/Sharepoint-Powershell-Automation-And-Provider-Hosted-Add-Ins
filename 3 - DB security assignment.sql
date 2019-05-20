-- To be run against PnPWebHookDemo DB
CREATE USER [WebHooksAdmin] FOR LOGIN [WebHooksAdmin] WITH DEFAULT_SCHEMA = dbo

-- Add user to the database reader/writer roles
EXEC sp_addrolemember 'db_datareader', 'WebHooksAdmin'
EXEC sp_addrolemember 'db_datawriter', 'WebHooksAdmin'