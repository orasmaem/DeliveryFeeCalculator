# Delivery Fee Calculator

Calculates delivery fees based on location, vehicle type, and weather conditions using real-time data from Estonian Environment Agency.



## Setup

### Database
1. Install PostgreSQL
2. Create database: `DeliveryFeeCalculator`
3. The sqripts to create the database and test data are in the main folder.
4. Default connection string in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=DeliveryFeeCalculator;Username=postgres;Password=sql"
}
```

### Build & Run
```
dotnet restore
dotnet build
cd src/DeliveryFeeCalculator.API
dotnet run

App runs at `http://localhost:5001`

### Database Setup

- Script to create the WeatherData tablw is in create_weather_data_table.sql
- Run: "dotnet ef database update"
- Test data for extreme weather conditions is in insert_test_weather_data.sql


## Authentication
1. Authentication page: `POST http://localhost:5001/api/Auth/login`
   - Username: `admin`
   - Password: `admin123`


## API Documentation
Swagger UI: `http://localhost:5001/swagger/index.html`

-There you can test different functionalities.


### Background Jobs
Weather data updates hourly (configurable in `appsettings.json`)

## Testing

cd tests/DeliveryFeeCalculator.Tests
dotnet test




