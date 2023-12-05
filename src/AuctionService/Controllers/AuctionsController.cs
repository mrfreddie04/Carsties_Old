using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController: ControllerBase
{
  private readonly AuctionDbContext _context;
  private readonly IMapper _mapper;

  public AuctionsController(AuctionDbContext context, IMapper mapper)
  {
    _context = context;
    _mapper = mapper;
  }

  [HttpGet]
  public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
  {
    var auctions = await _context.Auctions
      .Include(a => a.Item)
      .OrderBy(a => a.Item.Make)
      .ToListAsync();
    return Ok(_mapper.Map<List<AuctionDto>>(auctions));
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
      _mapper.Map<AuctionDto>(auction)
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

    var result = await _context.SaveChangesAsync() > 0;

    if(!result) return BadRequest("Problem saving changes");
    
    return Ok();

  }
}
