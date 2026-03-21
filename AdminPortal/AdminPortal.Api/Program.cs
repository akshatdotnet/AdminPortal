using AdminPortal.Api.MockData;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AdminPortal API", Version = "v1" });
});

// Register mock data store as singleton
builder.Services.AddSingleton<MockDataStore>();

// Allow the MVC web project to call this API (CORS)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcApp", policy =>
    {
        policy.WithOrigins("https://localhost:5001", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AdminPortal API v1"));

app.UseHttpsRedirection();
app.UseCors("AllowMvcApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
