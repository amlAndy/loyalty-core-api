using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

namespace DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;

public interface IPermissionCatalogProvider
{
    AdminPermissionModel[] GetPermissions();
}
