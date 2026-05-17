namespace TrackerPlus.Core.Models;

/// <summary>會員自訂標籤（UDLabel）</summary>
public class UdLabel
{
    public int TbKey { get; set; }
    public int MemberTbKey { get; set; }
    public string LabelName { get; set; } = string.Empty;
}

/// <summary>Tracker 與標籤的指派</summary>
public class TrackerLabelAssignment
{
    public int TrackerTbKey { get; set; }
    public List<int> SelectedLabelTbKeys { get; set; } = new();
}
