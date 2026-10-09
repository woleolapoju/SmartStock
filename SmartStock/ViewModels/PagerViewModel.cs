namespace SmartStock.ViewModels
{
    /// <summary>
    /// Drives the shared "_Pager" partial. The partial preserves every existing
    /// query-string parameter except "page", so callers never need to pass route values.
    /// </summary>
    public class PagerViewModel
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public string ItemLabel { get; set; } = "items";

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        public int RangeStart => TotalCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
        public int RangeEnd => Math.Min(CurrentPage * PageSize, TotalCount);
    }
}
