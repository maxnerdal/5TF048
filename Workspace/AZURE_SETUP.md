# Azure SQL Database Setup

## 1. Create Azure SQL Database
```bash
# Install Azure CLI
brew install azure-cli

# Login
az login

# Create resource group
az group create --name CryptoBotRG --location "West Europe"

# Create SQL Server
az sql server create \
  --name cryptobot-sql-server \
  --resource-group CryptoBotRG \
  --location "West Europe" \
  --admin-user sqladmin \
  --admin-password "MyPassword123#"

# Create database
az sql db create \
  --resource-group CryptoBotRG \
  --server cryptobot-sql-server \
  --name MyFirstDatabase \
  --edition Basic
```

## 2. Update Connection String
Replace in appsettings.json:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:cryptobot-sql-server.database.windows.net,1433;Initial Catalog=MyFirstDatabase;Persist Security Info=False;User ID=sqladmin;Password=MyPassword123#;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

## 3. Configure Firewall
```bash
# Allow your IP
az sql server firewall-rule create \
  --resource-group CryptoBotRG \
  --server cryptobot-sql-server \
  --name AllowMyIP \
  --start-ip-address YOUR_IP \
  --end-ip-address YOUR_IP
```