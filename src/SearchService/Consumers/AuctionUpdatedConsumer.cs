using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
  private readonly IMapper _mapper;

  public AuctionUpdatedConsumer(IMapper mapper)
  {
    _mapper = mapper;
  }

  public async Task Consume(ConsumeContext<AuctionUpdated> context)
  {
    Console.WriteLine($"--> Consuming auction updated: {context.Message.Id}");

    var item = _mapper.Map<Item>(context.Message);

    var result = await DB.Update<Item>()
      .MatchID(context.Message.Id)
      .ModifyOnly(el => new {
        el.Color,
        el.Make,
        el.Model,
        el.Year,
        el.Mileage
      },item)
      .ExecuteAsync();

    if (!result.IsAcknowledged) 
      throw new MessageException(typeof(AuctionUpdated), "Problem updating mongodb");    
  }
}
