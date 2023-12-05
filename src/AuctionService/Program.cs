using AuctionService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); //services needed when we create WebApi controller

// Add service for DbContext
builder.Services.AddDbContext<AuctionDbContext>(options => {
  options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection")
  );
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

//map controllers - direct http requests to correct api endpoint
app.MapControllers(); 

//apply migrations & seed data
try
{
  DbInitializer.InitiDb(app);
}
catch(Exception e) 
{
  Console.WriteLine(e);
  //throw;
}

app.Run();
