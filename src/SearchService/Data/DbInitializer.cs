using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.Services;

namespace SearchService.Data;

public class DbInitializer
{
  public static async Task InitDb(WebApplication app)
  {
    //initialize MongoDb connection - provide name & settings
    await DB.InitAsync(
        "SearchDb",
        MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection"))
    );
    
    //Create index for our Item collection
    await DB.Index<Item>()
      .Key(d => d.Make, KeyType.Text)
      .Key(d => d.Model, KeyType.Text)      
      .Key(d => d.Color, KeyType.Text)
      .CreateAsync();

    //seed data
    var count = await DB.CountAsync<Item>(); 
    // if(count == 0)
    // {
    //   //Seed
    //   Console.WriteLine("No data - will attempt to seed");
      
    //   //read as text
    //   var itemData = await File.ReadAllTextAsync("Data/auctions.json");
      
    //   //deserialized into list of items
    //   var options = new JsonSerializerOptions() {
    //     PropertyNameCaseInsensitive=true,
    //   };

    //   var items = JsonSerializer.Deserialize<List<Item>>(itemData, options);

    //   //save to DB
    //   await DB.SaveAsync(items);
    // }

    //use SearchService to populate local database
    using var scope = app.Services.CreateScope();
    var httpClient = scope.ServiceProvider.GetRequiredService<AuctionServiceHttpClient>();
    var items = await httpClient.GetItemsForSearchDb();
    
    Console.WriteLine(items.Count + " returned from the aution service");
    if(items.Count > 0) 
    {
      await DB.SaveAsync(items);
    }
  }
}
