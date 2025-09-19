-- First, check what's inside the .bak file
RESTORE FILELISTONLY
FROM DISK = '/var/opt/mssql/backup/Northwind.bak'
GO

-- Then restore the database (after running the above command, update the logical names)
RESTORE DATABASE Northwind
FROM DISK = '/var/opt/mssql/backup/Northwind.bak'
WITH 
    MOVE 'Northwind' TO '/var/opt/mssql/data/Northwind.mdf',
    MOVE 'Northwind_log' TO '/var/opt/mssql/data/Northwind.ldf',
    REPLACE
GO