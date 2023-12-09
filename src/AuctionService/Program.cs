using AuctionService.Consumers;
using AuctionService.Data;
using MassTransit;
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

//add MassTransit
builder.Services.AddMassTransit(options => {
  options.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();
  options.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));
  
  options.AddEntityFrameworkOutbox<AuctionDbContext>(o =>
  {
    //every 10 seconds retry to send messages from Outbox to ServiceBus
    o.QueryDelay = TimeSpan.FromSeconds(10);
    //which database to use to store outbox
    o.UsePostgres();
    //send messges via outbox (not directly)
    o.UseBusOutbox();
  });

  options.UsingRabbitMq( (context, cfg) => {
    //configure endpoints for all defined consumer and activity types
    //using an optional endpoint name formatter
    //if not specified - DefualtEndpointNameFormatter is used
    cfg.ConfigureEndpoints(context);
  });
});

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
