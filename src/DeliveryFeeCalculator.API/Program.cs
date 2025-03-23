using DeliveryFeeCalculator.API.Jobs;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Infrastructure.Data;
using DeliveryFeeCalculator.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Delivery Fee Calculator API",
        Version = "v1",
        Description = "API for calculating delivery fees based on city, vehicle type, and weather conditions"
    });

    // Define the JWT security scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // Generate XML documentation for Swagger
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Configure database
builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient for weather data fetching
builder.Services.AddHttpClient();

// Configure Cookie authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Cookies";
    options.DefaultSignInScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.Name = "DeliveryFeeAuth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.LoginPath = "/api/Auth/login";
    options.LogoutPath = "/api/Auth/logout";
    options.AccessDeniedPath = "/api/Auth/forbidden";
    options.SlidingExpiration = true;
});

// Register services
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IDeliveryFeeCalculationService, DeliveryFeeCalculationService>();
builder.Services.AddScoped<AuthService>();

// Configure Quartz.NET for scheduled jobs
builder.Services.AddQuartz(q =>
{
    // Create a job for importing weather data
    var jobKey = new JobKey("WeatherDataImportJob");
    q.AddJob<WeatherDataImportJob>(opts => opts.WithIdentity(jobKey));

    // Create a trigger with a cron schedule (run every hour at 15 minutes past the hour)
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("WeatherDataImportJob-trigger")
        .WithCronSchedule(builder.Configuration.GetValue<string>("WeatherDataImport:CronSchedule") ?? "0 15 * * * ?"));
});

// Add the Quartz.NET hosted service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Enable CORS
app.UseCors("AllowAll");

// Setup the database and seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Database setup - Create tables
        logger.LogInformation("Setting up database and tables...");
        
        // Skip migrations and just create the database
        bool created = dbContext.Database.EnsureCreated();
        logger.LogInformation("Database created: {Created}", created);

        // Create weather_data table - this ensures the structure is correct
        if (created)
        {
            logger.LogInformation("New database was created - seeding test data...");

            // Create the weather station test data
            await SeedTestWeatherData(dbContext, logger);
        }
        
        // Log the tables that exist in the database
        var tableCountSql = @"
            SELECT COUNT(*) FROM information_schema.tables 
            WHERE table_schema = 'public' AND table_name = 'WeatherData'";
        
        var tableCount = await dbContext.Database.ExecuteSqlRawAsync(tableCountSql);
        logger.LogInformation("Weather data table exists: {HasTable}", tableCount > 0);
        
        // Import initial weather data - ALWAYS do this to ensure we have the latest data
        logger.LogInformation("Importing live weather data...");
        await weatherService.ImportWeatherDataAsync();
        logger.LogInformation("Weather data import completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error setting up database");
        Console.WriteLine($"Database setup error: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Define a helper method to seed test weather data
async Task SeedTestWeatherData(WeatherDbContext dbContext, ILogger logger)
{
    try
    {
        // First, try to delete existing test data in case this is a re-run
        logger.LogInformation("Removing any existing test weather data...");
        await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"WeatherData\" WHERE \"StationName\" IN ('Tallinn-Harku-Test', 'Tartu-Tõravere-Test', 'Pärnu-Test')");

        // Insert test data with different conditions
        logger.LogInformation("Seeding test weather data...");
        
        // 1. Cold temperatures (below -10°C) for testing temperature extra fees
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""WeatherData"" (""StationName"", ""WmoCode"", ""AirTemperature"", ""WindSpeed"", ""WeatherPhenomenon"", ""Timestamp"")
            VALUES 
                ('Tallinn-Harku-Test', '26038', -15.0, 5.0, 'Clear', NOW()),
                ('Tartu-Tõravere-Test', '26242', -12.5, 5.0, 'Clear', NOW()),
                ('Pärnu-Test', '41803', -18.0, 5.0, 'Clear', NOW())
        ");
        
        // 2. Medium cold temperatures (-10°C to 0°C) for testing temperature extra fees
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""WeatherData"" (""StationName"", ""WmoCode"", ""AirTemperature"", ""WindSpeed"", ""WeatherPhenomenon"", ""Timestamp"")
            VALUES 
                ('Tallinn-Harku-Test', '26038', -5.0, 5.0, 'Clear', NOW() + INTERVAL '1 minute'),
                ('Tartu-Tõravere-Test', '26242', -8.0, 5.0, 'Clear', NOW() + INTERVAL '1 minute'),
                ('Pärnu-Test', '41803', -3.0, 5.0, 'Clear', NOW() + INTERVAL '1 minute')
        ");

        // 3. High wind speeds (10-20 m/s) for testing wind speed extra fees
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""WeatherData"" (""StationName"", ""WmoCode"", ""AirTemperature"", ""WindSpeed"", ""WeatherPhenomenon"", ""Timestamp"")
            VALUES 
                ('Tallinn-Harku-Test', '26038', 5.0, 15.0, 'Clear', NOW() + INTERVAL '2 minutes'),
                ('Tartu-Tõravere-Test', '26242', 5.0, 12.0, 'Clear', NOW() + INTERVAL '2 minutes'),
                ('Pärnu-Test', '41803', 5.0, 18.0, 'Clear', NOW() + INTERVAL '2 minutes')
        ");

        // 4. Extreme wind speeds (>20 m/s) for testing the wind speed prohibition
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""WeatherData"" (""StationName"", ""WmoCode"", ""AirTemperature"", ""WindSpeed"", ""WeatherPhenomenon"", ""Timestamp"")
            VALUES 
                ('Tallinn-Harku-Test', '26038', 5.0, 25.0, 'Clear', NOW() + INTERVAL '3 minutes'),
                ('Tartu-Tõravere-Test', '26242', 5.0, 22.0, 'Clear', NOW() + INTERVAL '3 minutes'),
                ('Pärnu-Test', '41803', 5.0, 30.0, 'Clear', NOW() + INTERVAL '3 minutes')
        ");

        // 5. Snow phenomena for testing weather phenomenon extra fees
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""WeatherData"" (""StationName"", ""WmoCode"", ""AirTemperature"", ""WindSpeed"", ""WeatherPhenomenon"", ""Timestamp"")
            VALUES 
                ('Tallinn-Harku-Test', '26038', 0.0, 5.0, 'Light snow shower', NOW() + INTERVAL '4 minutes'),
                ('Tartu-Tõravere-Test', '26242', 0.0, 5.0, 'Heavy snowfall', NOW() + INTERVAL '4 minutes'),
                ('Pärnu-Test', '41803', 0.0, 5.0, 'Light snowfall', NOW() + INTERVAL '4 minutes')
        ");

        // 6. Rain phenomena for testing weather phenomenon extra fees
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""WeatherData"" (""StationName"", ""WmoCode"", ""AirTemperature"", ""WindSpeed"", ""WeatherPhenomenon"", ""Timestamp"")
            VALUES 
                ('Tallinn-Harku-Test', '26038', 5.0, 5.0, 'Light rain', NOW() + INTERVAL '5 minutes'),
                ('Tartu-Tõravere-Test', '26242', 5.0, 5.0, 'Moderate rain', NOW() + INTERVAL '5 minutes'),
                ('Pärnu-Test', '41803', 5.0, 5.0, 'Heavy rain shower', NOW() + INTERVAL '5 minutes')
        ");

        // 7. Forbidden weather phenomena (glaze, hail, thunder)
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""WeatherData"" (""StationName"", ""WmoCode"", ""AirTemperature"", ""WindSpeed"", ""WeatherPhenomenon"", ""Timestamp"")
            VALUES 
                ('Tallinn-Harku-Test', '26038', 5.0, 5.0, 'Glaze', NOW() + INTERVAL '6 minutes'),
                ('Tartu-Tõravere-Test', '26242', 5.0, 5.0, 'Hail', NOW() + INTERVAL '6 minutes'),
                ('Pärnu-Test', '41803', 5.0, 5.0, 'Thunder', NOW() + INTERVAL '6 minutes')
        ");

        // 8. Example from requirements
        await dbContext.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""WeatherData"" (""StationName"", ""WmoCode"", ""AirTemperature"", ""WindSpeed"", ""WeatherPhenomenon"", ""Timestamp"")
            VALUES 
                ('Tartu-Tõravere-Test', '26242', -2.1, 4.7, 'Light snow shower', NOW() + INTERVAL '7 minutes')
        ");

        logger.LogInformation("Test weather data seeded successfully");
        
        // Check how many test records we have
        var sql = "SELECT COUNT(*) FROM \"WeatherData\" WHERE \"StationName\" LIKE '%-Test'";
        var count = await dbContext.Database.ExecuteSqlRawAsync(sql);
        logger.LogInformation("Total test weather data records: {Count}", count);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding test weather data");
    }
}