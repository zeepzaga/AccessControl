namespace AccessControl.Web.Models;

public class SearchableSelectOptionViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
