using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<AuctionSvcHttpClient>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseAuthorization();

app.MapControllers();

try
{
await DbInitializer.InitDb(app);    
}
catch (Exception e)
{
   Console.WriteLine("Error initializing database: " + e.Message);
    throw;
}



app.Run();
