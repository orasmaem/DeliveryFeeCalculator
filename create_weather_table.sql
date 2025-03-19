-- Drop existing objects if needed
DROP INDEX IF EXISTS "IX_WeatherData_StationName_Timestamp";
DROP TABLE IF EXISTS "WeatherData";

-- Create the table for weather data
CREATE TABLE "WeatherData" (
    "Id" SERIAL PRIMARY KEY,
    "StationName" VARCHAR(100) NOT NULL,
    "WmoCode" VARCHAR(20) NOT NULL,
    "AirTemperature" DECIMAL(5, 2) NOT NULL,
    "WindSpeed" DECIMAL(5, 2) NOT NULL,
    "WeatherPhenomenon" VARCHAR(255) NOT NULL,
    "Timestamp" TIMESTAMP NOT NULL
);

-- Create index for faster queries
CREATE INDEX "IX_WeatherData_StationName_Timestamp" 
ON "WeatherData" ("StationName", "Timestamp");