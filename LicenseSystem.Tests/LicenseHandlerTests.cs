using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Models;
using LicenseService.Handlers;
using LicenseService.Commands;
using LicenseService.Queries;
using Xunit;
using Moq;

namespace LicenseSystem.Tests;

public class LicenseHandlerTests
{
    private LicenseDbContext GetInMemoryDbContext(string tenantId)
    {
        var options = new DbContextOptionsBuilder<LicenseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new LicenseDbContext(options);
        context.TenantId = tenantId;
        return context;
    }

    [Fact]
    public async Task ApplyLicenseHandler_Should_Create_License_With_Correct_Tenant()
    {
        // Arrange
        var tenantId = "agency1";
        var context = GetInMemoryDbContext(tenantId);
        var handler = new ApplyLicenseHandler(context);
        
        var command = new ApplyLicenseCommand 
        ( 
            ApplicantName: "John Doe", 
            Agency: "Department of Health", 
            UserId: 1, 
            DocumentId: 10,
            DocumentFileName: "test.pdf"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("John Doe", result.ApplicantName);
        Assert.StartsWith("LIC-", result.LicenseNumber);
        
        var dbLicense = await context.Licenses.FindAsync(result.Id);
        Assert.NotNull(dbLicense);
        Assert.Equal(tenantId, dbLicense.TenantId);
    }

    [Fact]
    public async Task GetLicensesHandler_Should_Return_Only_Tenant_Data()
    {
        // Arrange
        var tenantId = "agency1";
        var context = GetInMemoryDbContext(tenantId);
        
        // Add data
        context.Licenses.Add(new License { ApplicantName = "Tenant 1 User", TenantId = "agency1", Agency = "Health", LicenseNumber = "L1" });
        await context.SaveChangesAsync();
        
        var handler = new GetLicensesHandler(context);
        var query = new GetLicensesQuery(Role: "Admin");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.All(result, l => Assert.Equal(tenantId, l.TenantId));
    }

    [Fact]

    public async Task UpdateLicenseStatusHandler_Should_Update_Status_Successfully()
    {
        // Arrange
        var tenantId = "agency1";
        var context = GetInMemoryDbContext(tenantId);
        
        var license = new License 
        { 
            ApplicantName = "John Doe", 
            Status = "Pending", 
            TenantId = tenantId, 
            LicenseNumber = "LIC-TEST",
            Agency = "Health"
        };
        context.Licenses.Add(license);
        await context.SaveChangesAsync();
        
        var handler = new UpdateLicenseStatusHandler(context);
        var command = new UpdateLicenseStatusCommand(license.Id, "Approved");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedLicense = await context.Licenses.FindAsync(license.Id);
        Assert.Equal("Approved", updatedLicense.Status);
        
        // Verify notification was created
        var notification = await context.Notifications.FirstOrDefaultAsync(n => n.UserId == license.UserId);
        Assert.NotNull(notification);
        Assert.Contains("Approved", notification.Message);
    }
}

