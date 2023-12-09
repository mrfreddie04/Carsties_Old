using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Contracts;

namespace AuctionService.RequestHelpers;

public class MappingProfiles: Profile
{
  public MappingProfiles()
  {
    //Auction->AuctionDto
    CreateMap<Auction,AuctionDto>().IncludeMembers(x => x.Item);

    //Need to map included members (Item)
    CreateMap<Item,AuctionDto>();
    
    //CreateAuctionDto->Auction, map the entire DTO to item field
    CreateMap<CreateAuctionDto,Auction>()
      .ForMember(
				dest => dest.Item,
				opt => opt.MapFrom(src => src));
    //Need to define how to map to Item entity
    CreateMap<CreateAuctionDto,Item>();

    //To publsh to the event bus
    CreateMap<AuctionDto,AuctionCreated>();

    CreateMap<Auction,AuctionUpdated>().IncludeMembers(x => x.Item);
    CreateMap<Item,AuctionUpdated>();

    //UpdateAuctionDto->Auction
    // CreateMap<UpdateAuctionDto,Auction>()
    //   .ForMember(
		// 		dest => dest.Item,
		// 		opt => opt.MapFrom(src => src));
    // CreateMap<UpdateAuctionDto,Item>();  
     
  }
}
