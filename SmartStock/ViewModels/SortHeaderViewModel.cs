namespace SmartStock.ViewModels
{
    /// <summary>
    /// Drives the shared "_SortHeader" partial: one clickable column header.
    /// The partial preserves every existing query-string parameter except sort/dir/page.
    /// </summary>
    public class SortHeaderViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string CurrentSort { get; set; } = string.Empty;
        public string CurrentDir { get; set; } = "asc";
    }
}
