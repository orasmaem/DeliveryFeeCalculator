using DeliveryFeeCalculator.API.Jobs;
using DeliveryFeeCalculator.Core.Interfaces;
using DeliveryFeeCalculator.Infrastructure.Data;
using DeliveryFeeCalculator.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Quartz;
using System.Reflection;
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

    // Generate XML documentation for Swagger
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Configure database
builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient for weather data fetching
builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IDeliveryFeeCalculationService, DeliveryFeeCalculationService>();

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

// Create database and table structure directly
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Attempting to ensure database is created with all tables");
        // Skip migrations and just create the database
        bool created = dbContext.Database.EnsureCreated();
        logger.LogInformation("Database created: {Created}", created);
        
        // Log the tables that exist in the database
        var tables = dbContext.Database.ExecuteSqlRaw(@"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public'");
            
        logger.LogInformation("Database tables check completed");
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
app.UseAuthorization();
app.MapControllers();

// Run the weather data import job at startup
using (var scope = app.Services.CreateScope())
{
    var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Importing initial weather data...");
        await weatherService.ImportWeatherDataAsync();
        logger.LogInformation("Initial weather data import completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error importing initial weather data");
        // Output detailed error to console for debugging
        Console.WriteLine($"Error importing initial weather data: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
        }
    }
}

app.Run();