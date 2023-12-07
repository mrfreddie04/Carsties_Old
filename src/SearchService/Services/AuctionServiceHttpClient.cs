using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionServiceHttpClient
{
  private readonly HttpClient _httpClient;
  private readonly IConfiguration _config;

  public AuctionServiceHttpClient(HttpClient httpClient, IConfiguration config)
  {
    _httpClient = httpClient;
    _config = config;
  }

  public async Task<List<Item>> GetItemsForSearchDb()
  {
    // get date & time of last updated item
    var lastUpdated = await DB.Find<Item,string>()
      .Sort(s => s.Descending(el => el.UpdatedAt))
      .Project( el => el.UpdatedAt.ToString())
      .ExecuteFirstAsync();

    //call auction service
    var auctionServiceUrl = _config.GetValue<string>("AuctionServiceUrl");  
    return await _httpClient.GetFromJsonAsync<List<Item>>($"{auctionServiceUrl}/api/auctions?date={lastUpdated}");
  }
}
