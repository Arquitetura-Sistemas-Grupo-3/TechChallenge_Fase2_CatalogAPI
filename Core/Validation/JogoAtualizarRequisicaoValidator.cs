using Core.Input;
using FluentValidation;

namespace Core.Validation
{
    public class JogoAtualizarRequisicaoValidator : AbstractValidator<JogoAtualizarRequisicao>
    {
        public JogoAtualizarRequisicaoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Preco)
                .GreaterThanOrEqualTo(0).WithMessage("O preço não pode ser negativo.");
        }
    }
}
