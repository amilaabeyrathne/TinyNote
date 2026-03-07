namespace TinyNote.Api.Exceptions
{
    public class ItemNotFoundException : Exception
    {
        public ItemNotFoundException(Guid id) : base($"The item wit ID {id} do not exists")
        {
        }
    }
}
