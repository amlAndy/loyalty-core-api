using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests.Admin;
using DotNetApiTemplate.Api.Contracts.Responses.Admin;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;

namespace DotNetApiTemplate.Api.Mapping;

public sealed class AdminMappingProfile : Profile
{
    public AdminMappingProfile()
    {
        CreateMap<AuditSearchCriteriaDto, AuditSearchCriteria>();
        CreateMap<AuditChangeDetailModel, AuditChangeDetailDto>();
        CreateMap<AuditEntryModel, AuditEntryDto>();
        CreateMap<AuditFilterOptionModel, AuditFilterOptionDto>();
        CreateMap<AuditFilterOptionsModel, AuditFilterOptionsDto>();
        CreateMap<AuditSearchResult, AuditSearchResponseDto>();
        CreateMap<AdminRoleSearchCriteriaDto, AdminRoleSearchCriteria>();
        CreateMap<CreateAdminRoleRequestDto, AdminRoleUpsertModel>();
        CreateMap<UpdateAdminRoleRequestDto, AdminRoleUpsertModel>();
        CreateMap<ValidateRolePermissionsRequestDto, ValidateRolePermissionsModel>();
        CreateMap<AdminPermissionModel, AdminPermissionDto>();
        CreateMap<AdminRoleChildModel, AdminRoleChildDto>();
        CreateMap<AdminRoleDeleteEligibilityModel, AdminRoleDeleteEligibilityDto>();
        CreateMap<AdminRoleDetailModel, AdminRoleDetailDto>();
        CreateMap<AdminRoleFormOptionsModel, AdminRoleFormOptionsDto>();
        CreateMap<AdminRoleParentOptionModel, AdminRoleParentOptionDto>();
        CreateMap<AdminRoleSearchResult, AdminRoleSearchResponseDto>();
        CreateMap<AdminRoleSummaryModel, AdminRoleSummaryDto>();
        CreateMap<AdminUserSearchCriteriaDto, AdminUserSearchCriteria>();
        CreateMap<AdminUserUpsertDto, AdminUserUpsertModel>();
        CreateMap<AdminRoleOptionModel, AdminRoleOptionDto>();
        CreateMap<AdminUserDetailModel, AdminUserDetailDto>();
        CreateMap<AdminUserFormOptionsModel, AdminUserFormOptionsDto>();
        CreateMap<AdminUserSummaryModel, AdminUserSummaryDto>();
        CreateMap<AdminUserSearchResult, AdminUserSearchResponseDto>();
    }
}
