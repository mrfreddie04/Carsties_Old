using System.Net;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService.Consumers;
using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

//register AuctionServiceHttpClient class as a Service
builder.Services
  .AddHttpClient<AuctionServiceHttpClient>()
  .AddPolicyHandler(GetPolicy());

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddMassTransit(options => {
  options.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
  //all endpoints will be prefixed with "search", and followed by the class name handled by a given consumer
  //AuctionCreatedConsumer handler AuctionCreated messages, hence the endpoint: search-auction-created
  options.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
  
  options.UsingRabbitMq( (context, cfg) => {
    //configure specific enpoint
    cfg.ReceiveEndpoint("search-auction-created", e =>
    {
      //retry 5 times, every 5 seconds
      e.UseMessageRetry(r => r.Interval(5, 5));
      //which consumer it is for?
      e.ConfigureConsumer<AuctionCreatedConsumer>(context);
    });

    //configure all endpoints based on consumers that we have
    cfg.ConfigureEndpoints(context);
  });
});  

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(
  async () => {
    //initialize MongoDb connection - provide name & settings
    try 
    {
      await DbInitializer.InitDb(app);
    } 
    catch(Exception ex)
    {
      Console.WriteLine(ex);
    }
  });

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy() 
  => HttpPolicyExtensions
      .HandleTransientHttpError()
      .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
      .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));

