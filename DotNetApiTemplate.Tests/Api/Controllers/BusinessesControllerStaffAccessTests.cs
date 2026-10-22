using System.Security.Claims;
using DotNetApiTemplate.Api.Controllers;
using DotNetApiTemplate.Application.Features.LoveBrackley.Authorization;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DotNetApiTemplate.Tests.Api.Controllers;

public sealed class BusinessesControllerStaffAccessTests
{
    [Fact]
    public async Task SearchStaffAccess_WhenAuthorized_ReturnsList()
    {
        var authorizationService = new StubBusinessAccessAuthorizationService(BusinessAccessAuthorizationResult.Allowed);
        var loyaltyService = new StubBusinessLoyaltyService
        {
            SearchResult = new PagedResponse<BusinessStaffAccessRecordDto>(
                [new BusinessStaffAccessRecordDto(Guid.NewGuid(), Guid.NewGuid(), "user-1", "Staff One", "Staff", "Active", "Active", "Recent activity", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)],
                1,
                20,
                1),
        };

        var controller = CreateController(authorizationService, loyaltyService);

        var action = await controller.SearchStaffAccess(Guid.NewGuid(), "Active", 1, 20, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var payload = Assert.IsType<DataResponse<PagedResponse<BusinessStaffAccessRecordDto>>>(ok.Value);
        Assert.Single(payload.Data.Items);
        Assert.True(loyaltyService.SearchCalled);
    }

    [Fact]
    public async Task CreateStaffAccess_WhenAuthorized_StillReturnsForbid_WithoutProvisioning()
    {
        var authorizationService = new StubBusinessAccessAuthorizationService(BusinessAccessAuthorizationResult.Allowed);
        var loyaltyService = new StubBusinessLoyaltyService();
        var controller = CreateController(authorizationService, loyaltyService);

        var action = await controller.CreateStaffAccess(
            Guid.NewGuid(),
            new CreateBusinessStaffAccessRequest("user-1", "Staff One", "Staff", "1234"),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(action.Result);
        Assert.False(loyaltyService.CreateCalled);
    }

    [Fact]
    public async Task ResetStaffPin_WhenAuthorized_StillReturnsForbid_WithoutProvisioning()
    {
        var authorizationService = new StubBusinessAccessAuthorizationService(BusinessAccessAuthorizationResult.Allowed);
        var loyaltyService = new StubBusinessLoyaltyService();
        var controller = CreateController(authorizationService, loyaltyService);

        var action = await controller.ResetStaffPin(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ResetStaffPinRequest("1234", "Owner attempted reset"),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(action.Result);
        Assert.False(loyaltyService.ResetCalled);
    }

    private static BusinessesController CreateController(
        IBusinessAccessAuthorizationService authorizationService,
        StubBusinessLoyaltyService loyaltyService)
    {
        var controller = new BusinessesController(authorizationService, loyaltyService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "owner-user"),
                        new Claim("active_role_id", "owner-role"),
                    ],
                    authenticationType: "test")),
                },
            },
        };

        return controller;
    }

    private sealed class StubBusinessAccessAuthorizationService : IBusinessAccessAuthorizationService
    {
        private readonly BusinessAccessAuthorizationResult _result;

        public StubBusinessAccessAuthorizationService(BusinessAccessAuthorizationResult result)
        {
            _result = result;
        }

        public Task<BusinessAccessAuthorizationResult> AuthorizeAsync(string? userId, string? activeRoleId, Guid businessId, BusinessAccessOperation operation, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class StubBusinessLoyaltyService : IBusinessLoyaltyService
    {
        public PagedResponse<BusinessStaffAccessRecordDto>? SearchResult { get; set; }
        public bool SearchCalled { get; private set; }
        public bool CreateCalled { get; private set; }
        public bool ResetCalled { get; private set; }

        public Task<BusinessDashboardDto?> GetDashboardAsync(Guid businessId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BusinessMemberSearchResponse?> SearchMembersAsync(Guid businessId, string? searchTerm, string? activityStatus, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BusinessMemberDetailDto?> GetMemberAsync(Guid businessId, Guid memberId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BusinessMemberDetailDto?> GetMemberByCardAsync(Guid businessId, string cardCode, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<LoyaltyTransactionDto?> EarnAsync(Guid businessId, EarnPointsRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<LoyaltyTransactionDto?> RedeemAsync(Guid businessId, RedeemPointsRequest request, string? actorUserId, string? activeRoleId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BusinessTransactionSearchResponse?> SearchTransactionsAsync(Guid businessId, string? searchTerm, string? dateRange, string? transactionType, Guid? staffMemberId, string? status, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BusinessStaffAccessOverviewDto?> GetStaffAccessOverviewAsync(Guid businessId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PagedResponse<BusinessStaffAccessRecordDto>?> SearchStaffAccessAsync(Guid businessId, string? accessStatus, int page, int pageSize, CancellationToken cancellationToken)
        {
            SearchCalled = true;
            return Task.FromResult<PagedResponse<BusinessStaffAccessRecordDto>?>(SearchResult);
        }

        public Task<BusinessStaffAccessRecordDto?> CreateStaffAccessAsync(Guid businessId, CreateBusinessStaffAccessRequest request, CancellationToken cancellationToken)
        {
            CreateCalled = true;
            return Task.FromResult<BusinessStaffAccessRecordDto?>(null);
        }

        public Task<BusinessStaffAccessRecordDto?> ResetStaffPinAsync(Guid businessId, Guid staffAccessId, ResetStaffPinRequest request, string? actorUserId, CancellationToken cancellationToken)
        {
            ResetCalled = true;
            return Task.FromResult<BusinessStaffAccessRecordDto?>(null);
        }

        public Task<BusinessPayoutOverviewDto?> GetPayoutOverviewAsync(Guid businessId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PagedResponse<BusinessPayoutDto>?> SearchPayoutsAsync(Guid businessId, string? status, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BusinessSettingsDto?> GetSettingsAsync(Guid businessId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BusinessSettingsDto?> UpdateSettingsAsync(Guid businessId, UpdateBusinessSettingsRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
