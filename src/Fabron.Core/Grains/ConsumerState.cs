namespace Fabron.Grains
{
    public record ConsumerState(
        long CurrentVersion = -1,
        long CommittedOffset = -1,
        long ConsumedOffset = -1
    );
}
