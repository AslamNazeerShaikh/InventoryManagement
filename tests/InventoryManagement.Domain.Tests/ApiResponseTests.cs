using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Tests;

public class ApiResponseTests
{
    [Fact]
    public void Success_SetsIsSuccessAndData()
    {
        var response = ApiResponse<string>.Success("payload", "done");

        Assert.True(response.IsSuccess);
        Assert.Equal("payload", response.Data);
        Assert.Equal("done", response.Message);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public void Failure_SetsErrorsAndClearsData()
    {
        var errors = new List<string> { "e1", "e2" };

        var response = ApiResponse<string>.Failure("bad", errors);

        Assert.False(response.IsSuccess);
        Assert.Null(response.Data);
        Assert.Equal("bad", response.Message);
        Assert.Equal(errors, response.Errors);
    }

    [Theory]
    [InlineData(1, 10, 25, 3, true, false)]
    [InlineData(3, 10, 25, 3, false, true)]
    [InlineData(2, 10, 25, 3, true, true)]
    public void PagedResult_ComputedProperties_AreCorrect(
        int pageNumber,
        int pageSize,
        int totalCount,
        int expectedTotalPages,
        bool expectedHasNext,
        bool expectedHasPrevious
    )
    {
        var paged = new PagedResult<int>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };

        Assert.Equal(expectedTotalPages, paged.TotalPages);
        Assert.Equal(expectedHasNext, paged.HasNextPage);
        Assert.Equal(expectedHasPrevious, paged.HasPreviousPage);
    }
}
