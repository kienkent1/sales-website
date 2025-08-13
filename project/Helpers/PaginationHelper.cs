using Microsoft.EntityFrameworkCore;

namespace project.Helpers
{
    public interface IPagedResult
    {
        int PageNumber { get; }
        int PageSize { get; }
        int TotalPages { get; }
        int TotalItems { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }

    public static class PaginationHelper
    {
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> source, int pageNumber, int pageSize)
        {
            // 1. Đếm tổng số phần tử MÀ KHÔNG TẢI DỮ LIỆU VỀ

            var totalItems = await source.CountAsync();

            // 2. Tính toán tổng số trang
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo pageNumber và pageSize hợp lệ
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;


            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 4. Trả về đối tượng kết quả
            return new PagedResult<T>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            };
        }

    public class PagedResult<T>: IPagedResult
        {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }

        /// <summary>
        /// Danh sách các phần tử của trang hiện tại.
        /// </summary>
        public List<T> Items { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
    //end pagination
}
}