namespace Scheduling.Domain.EventSourcing
{
    public class Command<T> : Value<T> where T : Value<T>
    {

    }
}
