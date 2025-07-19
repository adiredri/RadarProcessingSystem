using RadarProcessing.API.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        // Add CORS for dashboard access
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowDashboard", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Register RadarDataProcessor as a singleton service for real-time processing
        builder.Services.AddSingleton<IRadarDataProcessor, RadarDataProcessor>();

        // Add background service for continuous radar simulation
        builder.Services.AddHostedService<BackgroundRadarService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // Enable CORS
        app.UseCors("AllowDashboard");

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}