-- Insert extreme weather conditions for testing all fee rules
-- Clear existing test data (optional)
DELETE FROM "WeatherData" WHERE "StationName" IN ('Tallinn-Harku-Test', 'Tartu-Tõravere-Test', 'Pärnu-Test');

-- 1. Cold temperatures (below -10°C) for testing temperature extra fees
INSERT INTO "WeatherData" ("StationName", "WmoCode", "AirTemperature", "WindSpeed", "WeatherPhenomenon", "Timestamp")
VALUES 
('Tallinn-Harku-Test', '26038', -15.0, 5.0, 'Clear', NOW()),
('Tartu-Tõravere-Test', '26242', -12.5, 5.0, 'Clear', NOW()),
('Pärnu-Test', '41803', -18.0, 5.0, 'Clear', NOW());

-- 2. Medium cold temperatures (-10°C to 0°C) for testing temperature extra fees
INSERT INTO "WeatherData" ("StationName", "WmoCode", "AirTemperature", "WindSpeed", "WeatherPhenomenon", "Timestamp")
VALUES 
('Tallinn-Harku-Test', '26038', -5.0, 5.0, 'Clear', NOW() + INTERVAL '1 minute'),
('Tartu-Tõravere-Test', '26242', -8.0, 5.0, 'Clear', NOW() + INTERVAL '1 minute'),
('Pärnu-Test', '41803', -3.0, 5.0, 'Clear', NOW() + INTERVAL '1 minute');

-- 3. High wind speeds (10-20 m/s) for testing wind speed extra fees
INSERT INTO "WeatherData" ("StationName", "WmoCode", "AirTemperature", "WindSpeed", "WeatherPhenomenon", "Timestamp")
VALUES 
('Tallinn-Harku-Test', '26038', 5.0, 15.0, 'Clear', NOW() + INTERVAL '2 minutes'),
('Tartu-Tõravere-Test', '26242', 5.0, 12.0, 'Clear', NOW() + INTERVAL '2 minutes'),
('Pärnu-Test', '41803', 5.0, 18.0, 'Clear', NOW() + INTERVAL '2 minutes');

-- 4. Extreme wind speeds (>20 m/s) for testing the wind speed prohibition
INSERT INTO "WeatherData" ("StationName", "WmoCode", "AirTemperature", "WindSpeed", "WeatherPhenomenon", "Timestamp")
VALUES 
('Tallinn-Harku-Test', '26038', 5.0, 25.0, 'Clear', NOW() + INTERVAL '3 minutes'),
('Tartu-Tõravere-Test', '26242', 5.0, 22.0, 'Clear', NOW() + INTERVAL '3 minutes'),
('Pärnu-Test', '41803', 5.0, 30.0, 'Clear', NOW() + INTERVAL '3 minutes');

-- 5. Snow phenomena for testing weather phenomenon extra fees
INSERT INTO "WeatherData" ("StationName", "WmoCode", "AirTemperature", "WindSpeed", "WeatherPhenomenon", "Timestamp")
VALUES 
('Tallinn-Harku-Test', '26038', 0.0, 5.0, 'Light snow shower', NOW() + INTERVAL '4 minutes'),
('Tartu-Tõravere-Test', '26242', 0.0, 5.0, 'Heavy snowfall', NOW() + INTERVAL '4 minutes'),
('Pärnu-Test', '41803', 0.0, 5.0, 'Light snowfall', NOW() + INTERVAL '4 minutes');

-- 6. Rain phenomena for testing weather phenomenon extra fees
INSERT INTO "WeatherData" ("StationName", "WmoCode", "AirTemperature", "WindSpeed", "WeatherPhenomenon", "Timestamp")
VALUES 
('Tallinn-Harku-Test', '26038', 5.0, 5.0, 'Light rain', NOW() + INTERVAL '5 minutes'),
('Tartu-Tõravere-Test', '26242', 5.0, 5.0, 'Moderate rain', NOW() + INTERVAL '5 minutes'),
('Pärnu-Test', '41803', 5.0, 5.0, 'Heavy rain shower', NOW() + INTERVAL '5 minutes');

-- 7. Forbidden weather phenomena (glaze, hail, thunder)
INSERT INTO "WeatherData" ("StationName", "WmoCode", "AirTemperature", "WindSpeed", "WeatherPhenomenon", "Timestamp")
VALUES 
('Tallinn-Harku-Test', '26038', 5.0, 5.0, 'Glaze', NOW() + INTERVAL '6 minutes'),
('Tartu-Tõravere-Test', '26242', 5.0, 5.0, 'Hail', NOW() + INTERVAL '6 minutes'),
('Pärnu-Test', '41803', 5.0, 5.0, 'Thunder', NOW() + INTERVAL '6 minutes');

-- Insert data to match your specific example from requirements
INSERT INTO "WeatherData" ("StationName", "WmoCode", "AirTemperature", "WindSpeed", "WeatherPhenomenon", "Timestamp")
VALUES 
('Tartu-Tõravere-Test', '26242', -2.1, 4.7, 'Light snow shower', NOW() + INTERVAL '7 minutes');