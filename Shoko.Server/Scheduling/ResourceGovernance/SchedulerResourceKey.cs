namespace Shoko.Server.Scheduling.ResourceGovernance;

public readonly record struct SchedulerResourceKey(string Provider, string Lane)
{
    public override string ToString() => $"{Provider}.{Lane}";
}
