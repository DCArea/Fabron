namespace Fabron.Grains
{
    public enum JobStatus
    {
        NotCreated,
        Created,
        Running,
        RanToCompletion,
        Canceled,
        Faulted
    }
}
