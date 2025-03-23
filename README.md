# Delivery Fee Calculator

This application calculates delivery fees for food couriers based on regional base fees, vehicle types, and weather conditions. The calculator retrieves real-time weather data from the Estonian Environment Agency and applies business rules to determine delivery fees.

## Technologies Used

- .NET 8.0
- C#
- Entity Framework Core
- PostgreSQL
- Quartz.NET for scheduled tasks
- ASP.NET Core Web API

## Project Structure

- **DeliveryFeeCalculator.API**: Web API project containing controllers and scheduled job setup
- **DeliveryFeeCalculator.Core**: Core domain models, interfaces, and business logic
- **DeliveryFeeCalculator.Infrastructure**: Implementation of services, database context, and data access


