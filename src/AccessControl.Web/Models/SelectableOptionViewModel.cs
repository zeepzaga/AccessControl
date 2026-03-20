namespace AccessControl.Web.Models;

public class SelectableOptionViewModel
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
