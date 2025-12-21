namespace DrinkShop.Application.Helpers
{
    /// <summary>
    /// Lớp này dùng để nhận các tham số phân trang
    /// từ Query String của URL (ví dụ: ?pageNumber=1&pageSize=10)
    /// </summary>
    public class PaginationParams
    {
        // Kích thước trang tối đa để tránh client yêu cầu quá nhiều
        private const int MaxPageSize = 50;

        // Kích thước trang mặc định nếu client không gửi lên
        private int _pageSize = 10;

        /// <summary>
        /// Trang hiện tại, mặc định là trang 1
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Số lượng mục trên mỗi trang
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}