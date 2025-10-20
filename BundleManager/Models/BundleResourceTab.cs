namespace BundleManager.Models;

public class BundleResourceTab
{
	private string id = string.Empty;
	public required string Id { get => id; set => id = value; }
	public string ShortId { get => id.Length > 35 ? id.Substring(0, 32) + "..." : id; }
	public required StackableViewContainer ContentContainer { get; set; }
	public bool IsCloseButtonVisible { get => ContentContainer.Content.GetType() != typeof(Views.BundleResourceList); }
}
