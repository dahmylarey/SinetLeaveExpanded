
using AutoMapper;
using SinetLeaveManagement.Models;
using SinetLeaveManagement.Models.ViewModels;

namespace SinetLeaveManagement.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ViewModel → Entity
            CreateMap<LeaveRequestViewModel, LeaveRequest>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RequestedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.RequestingUserId, opt => opt.Ignore())
                .ForMember(dest => dest.RequestingUser, opt => opt.Ignore());

            // Entity → ViewModel
            CreateMap<LeaveRequest, LeaveRequestViewModel>().ReverseMap();

            

            // Add more mappings here if needed, e.g.
            CreateMap<ApplicationUser, UserViewModel>().ReverseMap();

        }
    }
}
