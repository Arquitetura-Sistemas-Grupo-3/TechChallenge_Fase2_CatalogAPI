namespace Infra.Exceptions
{
    public abstract class ExceptionBase : Exception
    {
        protected ExceptionBase(string message) : base(message)
        {
        }

        public abstract int StatusCode { get; set; }
    }
}
