using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;
using ZstdSharp.Unsafe;

namespace SearchService;

[ApiController]
[Route("api/search")]
public class SearchController: ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<List<Entity>>> SearchItems(
    [FromQuery] SearchParams searchParams
  )
  {
    var query = DB.PagedSearch<Item, Item>();

    if(!string.IsNullOrEmpty(searchParams.SearchTerm))
    {
      //perform full text search for search term
      //sort results by the MetaTextScore
      query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
    }

    //add filtering
    query = searchParams.FilterBy switch {
      "finished" => query.Match(el => el.AuctionEnd < DateTime.UtcNow),
      "endingSoon" => query.Match(el => el.AuctionEnd > DateTime.UtcNow && el.AuctionEnd < DateTime.UtcNow.AddHours(6)),
      _ => query.Match(el => el.AuctionEnd > DateTime.UtcNow)
    };

    //add sorting
    query = searchParams.OrderBy switch {
      "make" => query.Sort( s => s.Ascending( el => el.Make)),
      "new" => query.Sort( s => s.Descending( el => el.CreatedAt)),
      _ => query.Sort( s => s.Ascending( el => el.AuctionEnd))
    };

    if(!string.IsNullOrEmpty(searchParams.Seller))
    {
      query = query.Match(el => el.Seller == searchParams.Seller);
    }

    if(!string.IsNullOrEmpty(searchParams.Winner))
    {
      query = query.Match(el => el.Winner == searchParams.Winner);
    }

    query.PageNumber(searchParams.PageNumber);
    query.PageSize(searchParams.PageSize);

    var result = await query.ExecuteAsync();
    return Ok(new {
      results = result.Results,
      pageCount = result.PageCount,
      totalCount = result.TotalCount
    });
  }
}

