using Kentos.SharedKernel.Pagination;
using Shouldly;

namespace Kentos.Modules.Settlement.UnitTests;

public sealed class PaginationTests
{
    [Fact]
    public void Page_is_clamped_to_minimum_one()
    {
        var request = new PagedRequest { Page = 0 };
        request.Page.ShouldBe(1);
    }

    [Fact]
    public void PageSize_falls_back_to_default_when_out_of_range()
    {
        new PagedRequest { PageSize = 0 }.PageSize.ShouldBe(PagedRequest.DefaultPageSize);
        new PagedRequest { PageSize = 9999 }.PageSize.ShouldBe(PagedRequest.DefaultPageSize);
    }

    [Fact]
    public void Skip_is_computed_from_page_and_size()
    {
        var request = new PagedRequest { Page = 3, PageSize = 25 };
        request.Skip.ShouldBe(50);
    }

    [Fact]
    public void Response_computes_total_pages()
    {
        var response = new PagedResponse<int>([1, 2, 3], totalCount: 41, page: 1, pageSize: 20);
        response.TotalPages.ShouldBe(3);
    }
}
