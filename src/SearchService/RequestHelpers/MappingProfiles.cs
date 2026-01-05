using AutoMapper;
using Contracts;
using SearchService.Models;

namespace SearchService.RequestHelpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<AuctionCreated, Item>()
            .ForMember(d => d.ID, opt => opt.MapFrom(s => s.Id.ToString()));
        CreateMap<AuctionUpdated, Item>()
            .ForMember(d => d.ID, opt => opt.MapFrom(s => s.Id));
    }
}