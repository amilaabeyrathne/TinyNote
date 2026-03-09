namespace TinyNote.Api.Exceptions
{
    public class ItemNotFoundException : Exception
    {
        public ItemNotFoundException(Guid id) : base($"The item with ID {id} does not exist")
        {
        }
    }
}
