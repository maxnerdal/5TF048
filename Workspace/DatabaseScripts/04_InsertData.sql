-- 04_InsertDigitalAssets.sql
-- Insert all digital assets and sample data: 3 users and 3 digital assets per user

USE MyFirstDatabase;
GO

-- Insert all digital assets
INSERT INTO [DigitalAssets] ([Name], [Ticker]) VALUES 
('Bitcoin', 'BTC'),
('Ethereum', 'ETH'),
('Tether', 'USDT'),
('BNB', 'BNB'),
('Solana', 'SOL'),
('USDC', 'USDC'),
('XRP', 'XRP'),
('Lido Staked Ether', 'STETH'),
('Toncoin', 'TON'),
('Dogecoin', 'DOGE'),
('Cardano', 'ADA'),
('TRON', 'TRX'),
('Avalanche', 'AVAX'),
('Wrapped Bitcoin', 'WBTC'),
('Shiba Inu', 'SHIB'),
('Polkadot', 'DOT'),
('Bitcoin Cash', 'BCH'),
('Chainlink', 'LINK'),
('NEAR Protocol', 'NEAR'),
('Litecoin', 'LTC'),
('Polygon', 'MATIC'),
('Internet Computer', 'ICP'),
('Pepe', 'PEPE'),
('Uniswap', 'UNI'),
('Ethereum Classic', 'ETC'),
('Stellar', 'XLM'),
('Monero', 'XMR'),
('OKB', 'OKB'),
('Hedera', 'HBAR'),
('Filecoin', 'FIL'),
('Cosmos', 'ATOM'),
('Mantle', 'MNT'),
('VeChain', 'VET'),
('Arbitrum', 'ARB'),
('Cronos', 'CRO'),
('Immutable', 'IMX'),
('First Digital USD', 'FDUSD'),
('Optimism', 'OP'),
('Injective', 'INJ'),
('Render', 'RNDR'),
('Kaspa', 'KAS'),
('Maker', 'MKR'),
('The Graph', 'GRT'),
('Bittensor', 'TAO'),
('Fantom', 'FTM'),
('Theta Network', 'THETA'),
('Lido DAO', 'LDO'),
('Aave', 'AAVE'),
('Algorand', 'ALGO'),
('Rocket Pool', 'RPL'),
('Flow', 'FLOW'),
('FLOKI', 'FLOKI'),
('Quant', 'QNT'),
('Bitcoin SV', 'BSV'),
('ApeCoin', 'APE'),
('MultiversX', 'EGLD'),
('Stacks', 'STX'),
('The Sandbox', 'SAND'),
('Celestia', 'TIA'),
('Thorchain', 'RUNE'),
('Sei', 'SEI'),
('Axie Infinity', 'AXS'),
('Decentraland', 'MANA'),
('Tezos', 'XTZ'),
('Neo', 'NEO'),
('EOS', 'EOS'),
('Kava', 'KAVA'),
('IOTA', 'MIOTA'),
('Synthetix', 'SNX'),
('Chiliz', 'CHZ'),
('Zcash', 'ZEC'),
('Mina', 'MINA'),
('Terra Classic', 'LUNC'),
('Dash', 'DASH'),
('Compound', 'COMP'),
('1inch Network', 'INCH'),
('Blur', 'BLUR'),
('SushiSwap', 'SUSHI'),
('Curve DAO Token', 'CRV'),
('dYdX', 'DYDX'),
('PancakeSwap', 'CAKE'),
('Gnosis', 'GNO'),
('Kusama', 'KSM'),
('Enjin Coin', 'ENJ'),
('Helium', 'HNT'),
('Basic Attention Token', 'BAT'),
('Loopring', 'LRC'),
('Zilliqa', 'ZIL'),
('Waves', 'WAVES'),
('OriginTrail', 'TRAC'),
('Convex Finance', 'CVX'),
('JasmyCoin', 'JASMY'),
('SafeMoon', 'SAFEMOON'),
('Terra', 'LUNA'),
('Yearn.finance', 'YFI'),
('0x', 'ZRX'),
('Bancor', 'BNT'),
('Kyber Network Crystal', 'KNC'),
('Balancer', 'BAL'),
('Numeraire', 'NMR'),
('Arweave', 'AR'),
('Status', 'SNT'),
('district0x', 'DNT'),
('Civic', 'CVC'),
('Storj', 'STORJ');

PRINT 'Digital assets inserted successfully! Total: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' assets';

-- Insert 3 sample users
INSERT INTO [Users] ([Username], [Email], [PasswordHash]) VALUES 
('alice_trader', 'alice@example.com', 'hashed_password_alice123'),
('bob_investor', 'bob@example.com', 'hashed_password_bob456'),
('charlie_hodler', 'charlie@example.com', 'hashed_password_charlie789');

PRINT 'Users inserted successfully! Total: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' users';

-- Insert portfolio items for User 1 (Alice)
INSERT INTO [Portfolio] ([user_id], [asset_id], [qty], [buyprice], [datepurchased]) VALUES 
((SELECT Id FROM Users WHERE Username = 'alice_trader'), (SELECT AssetId FROM DigitalAssets WHERE Ticker = 'BTC'), 0.5, 45000.00, '2024-01-15'),
((SELECT Id FROM Users WHERE Username = 'alice_trader'), (SELECT AssetId FROM DigitalAssets WHERE Ticker = 'ETH'), 2.75, 2800.00, '2024-02-20'),
((SELECT Id FROM Users WHERE Username = 'alice_trader'), (SELECT AssetId FROM DigitalAssets WHERE Ticker = 'ADA'), 1500.0, 0.65, '2024-03-10');

-- Insert portfolio items for User 2 (Bob)
INSERT INTO [Portfolio] ([user_id], [asset_id], [qty], [buyprice], [datepurchased]) VALUES 
((SELECT Id FROM Users WHERE Username = 'bob_investor'), (SELECT AssetId FROM DigitalAssets WHERE Ticker = 'SOL'), 25.0, 95.50, '2024-01-25'),
((SELECT Id FROM Users WHERE Username = 'bob_investor'), (SELECT AssetId FROM DigitalAssets WHERE Ticker = 'DOT'), 100.0, 8.75, '2024-02-15'),
((SELECT Id FROM Users WHERE Username = 'bob_investor'), (SELECT AssetId FROM DigitalAssets WHERE Ticker = 'LINK'), 75.5, 18.25, '2024-03-05');

-- Insert portfolio items for User 3 (Charlie)
INSERT INTO [Portfolio] ([user_id], [asset_id], [qty], [buyprice], [datepurchased]) VALUES 
((SELECT Id FROM Users WHERE Username = 'charlie_hodler'), (SELECT AssetId FROM DigitalAssets WHERE Ticker = 'LTC'), 10.0, 85.00, '2024-01-30'),
((SELECT Id FROM Users WHERE Username = 'charlie_hodler'), (SELECT AssetId FROM DigitalAssets WHERE Ticker = 'MATIC'), 2500.0, 1.15, '2024-02-25'),
((SELECT Id FROM Users WHERE Username = 'charlie_hodler'), (SELECT AssetId FROM DigitalAssets WHERE Ticker = 'AVAX'), 50.0, 32.50, '2024-03-15');

PRINT 'Portfolio items inserted successfully! Total: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' portfolio entries';

-- Summary of inserted data
SELECT 'Summary' AS DataType, 'Users' AS Category, COUNT(*) AS Count FROM Users
UNION ALL
SELECT 'Summary', 'DigitalAssets', COUNT(*) FROM DigitalAssets
UNION ALL
SELECT 'Summary', 'Portfolio', COUNT(*) FROM Portfolio;

PRINT 'Sample data insertion completed successfully!';
