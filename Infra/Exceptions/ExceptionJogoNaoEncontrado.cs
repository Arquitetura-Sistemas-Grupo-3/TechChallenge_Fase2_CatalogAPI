namespace Infra.Exceptions
{
    public class ExceptionJogoNaoEncontrado : ExceptionBase
    {
        public ExceptionJogoNaoEncontrado(string message) : base(message)
        {
        }
        public override int StatusCode { get; set; } = 404;
    }
}
