using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController: ControllerBase
{
  private readonly AuctionDbContext _context;
  private readonly IMapper _mapper;
  private readonly IPublishEndpoint _publishEndpoint;

  public AuctionsController(AuctionDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
  {
    _context = context;
    _mapper = mapper;
    _publishEndpoint = publishEndpoint;
  }

  [HttpGet]
  public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions([FromQuery] string date)
  {
    //Need to chain AsQueryable()
    //otherwise it will return IOrderedQueryable result, which will not let 
    //us further modify the returned query object
    var query = _context.Auctions
      .OrderBy(a => a.Item.Make)
      .AsQueryable(); 

    if(!string.IsNullOrEmpty(date))  
    {
      query = query.Where(a => a.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0 );
    }

    // var auctions = await _context.Auctions
    //   .Include(a => a.Item)
    //   .OrderBy(a => a.Item.Make)
    //   .ToListAsync();

    //Use AutoMapper to include related Item
    var auctions = await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
    return Ok(auctions);
  }

  [HttpGet("{id:Guid}")]
  public async Task<ActionResult<AuctionDto>> GetAuctionById([FromRoute] Guid id)
  {
    var auction = await _context.Auctions
      .Include(a => a.Item)
      .FirstOrDefaultAsync(a => a.Id == id);

    if(auction is null) {
      return NotFound();
    }

    return Ok(_mapper.Map<AuctionDto>(auction));
  }  

  [HttpPost]
  public async Task<ActionResult<AuctionDto>> CreateAuction([FromBody] CreateAuctionDto auctionDto)
  {
    var auction = _mapper.Map<Auction>(auctionDto);
    //TODO: add current user as seller
    auction.Seller = "test";

    //add tracking 
    _context.Auctions.Add(auction);

    //map entity to dto
    var newAuction = _mapper.Map<AuctionDto>(auction);

    //send to message bus
    await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));
 
    //make sure at least one change was saved to the db - success
    var result = await _context.SaveChangesAsync() > 0;

    if(!result) {
      return BadRequest("Could not save changes to the DB");
    }

    // return location we can GET created resource 
    // - name of the GET action, parameters, DTO of created entity
    return CreatedAtAction(
      nameof(GetAuctionById), 
      new {Id = auction.Id},
      newAuction
    );
  }  

  [HttpPut("{id:Guid}")]
  public async Task<ActionResult> UpdateAuction([FromRoute] Guid id, [FromBody] UpdateAuctionDto updateAuctionDto) 
  {
    var auction = await _context.Auctions
      .Include(a => a.Item)
      .FirstOrDefaultAsync(a => a.Id == id);

    if(auction == null) return NotFound();

    //TODO: check if auction's seller matches the current user
    // if(auction.Seller != username)
    // {
    //   return Forbid();
    // }

    //update auction object
    auction.Item.Make = updateAuctionDto?.Make ?? auction.Item.Make;
    auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
    auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
    auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
    auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

    //send to message bus
    await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

    //auction was retrieved via EF and it is tracked in memory
    //we can just save the changes
    var result = await _context.SaveChangesAsync() > 0;

    if(result) return Ok();

    return BadRequest("Problem saving changes");
  }

  [HttpDelete("{id:Guid}")]
  public async Task<ActionResult> DeleteAuction([FromRoute] Guid id) 
  {
    var auction = await _context.Auctions
      .FindAsync(id);

    if(auction == null) return NotFound();     

    //TODO: check if auction's seller matches the current user
    // if(auction.Seller != username)
    // {
    //   return Forbid();
    // }  

    _context.Auctions.Remove(auction);

    //await _publishEndpoint.Publish(_mapper.Map<AuctionDeleted>(auction));
    await _publishEndpoint.Publish(new AuctionDeleted() { Id = auction.Id.ToString()});

    var result = await _context.SaveChangesAsync() > 0;

    if(!result) return BadRequest("Problem saving changes");
    
    return Ok();

  }
}
