var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Dapr
builder.Services.AddDaprClient();

builder.Services.AddControllers().AddDapr(); // 👈 Enables subscription

builder.WebHost.UseUrls("http://*:8080");

var app = builder.Build();

// Dapr
app.UseCloudEvents();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.MapSubscribeHandler(); // 👈 Dapr uses this to register topics

app.Run();
