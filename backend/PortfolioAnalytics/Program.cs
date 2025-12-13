using Microsoft.EntityFrameworkCore;
using PortfolioAnalytics.Data;
using PortfolioAnalytics.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register custom services
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AggregationService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHostedService<DataRetentionBackgroundService>();

// Configure SMTP settings from appsettings.json and environment variables
builder.Services.Configure<PortfolioAnalytics.Models.SmtpSettings>(options =>
{
    // Load from appsettings.json first
    builder.Configuration.GetSection("SmtpSettings").Bind(options);

    // Override with environment variables if present (for Docker/production)
    var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
    var smtpPort = Environment.GetEnvironmentVariable("SMTP_PORT");
    var smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME");
    var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
    var smtpSenderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME");
    var smtpRecipientEmail = Environment.GetEnvironmentVariable("SMTP_RECIPIENT_EMAIL");
    var smtpEnableSsl = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL");

    if (!string.IsNullOrEmpty(smtpHost))
        options.Host = smtpHost;

    if (!string.IsNullOrEmpty(smtpPort) && int.TryParse(smtpPort, out var port))
        options.Port = port;

    if (!string.IsNullOrEmpty(smtpUsername))
        options.Username = smtpUsername;

    if (!string.IsNullOrEmpty(smtpPassword))
        options.Password = smtpPassword;

    if (!string.IsNullOrEmpty(smtpSenderName))
        options.SenderName = smtpSenderName;

    if (!string.IsNullOrEmpty(smtpRecipientEmail))
        options.RecipientEmail = smtpRecipientEmail;

    if (!string.IsNullOrEmpty(smtpEnableSsl) && bool.TryParse(smtpEnableSsl, out var enableSsl))
        options.EnableSsl = enableSsl;
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AnalyticsPolicy", policy =>
    {
        var isDevelopment = builder.Environment.IsDevelopment();

        if (isDevelopment)
        {
            // Development: Allow all local network origins for testing
            policy.SetIsOriginAllowed(origin =>
                  {
                      var uri = new Uri(origin);
                      var host = uri.Host;

                      // Allow localhost
                      if (host == "localhost" || host == "127.0.0.1")
                          return true;

                      // Allow local network IPs (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
                      if (System.Text.RegularExpressions.Regex.IsMatch(host, @"^192\.168\.\d{1,3}\.\d{1,3}$"))
                          return true;
                      if (System.Text.RegularExpressions.Regex.IsMatch(host, @"^10\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
                          return true;
                      if (System.Text.RegularExpressions.Regex.IsMatch(host, @"^172\.(1[6-9]|2[0-9]|3[0-1])\.\d{1,3}\.\d{1,3}$"))
                          return true;

                      // Allow .local domains and machine names with hyphens (e.g., surface-jan)
                      if (host.EndsWith(".local") || host.Contains("-"))
                          return true;

                      return false;
                  })
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Production: Only allow specific domains
            policy.WithOrigins(
                      "https://app.jan-huelsbrink.de",
                      "https://portfolio.jan-huelsbrink.de",
                      "http://app.jan-huelsbrink.de",
                      "http://portfolio.jan-huelsbrink.de"
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply CORS policy
app.UseCors("AnalyticsPolicy");

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();

    // Always use migrations (ensures schema stays in sync)
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
