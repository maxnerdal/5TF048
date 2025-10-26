import pymssql

conn = pymssql.connect(
    server='localhost',
    port=1433,
    user='sa',
    password='MyPassword123#',
    database='MyFirstDatabase'
)

cursor = conn.cursor()
cursor.execute("SELECT * FROM Users")
rows = cursor.fetchall()

print(rows)

cursor.close()
conn.close()