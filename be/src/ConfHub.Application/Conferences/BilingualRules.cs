using FluentValidation;

namespace ConfHub.Application.Conferences;

public static class BilingualRules
{
    // Một là cả hai bỏ trống, hai là đều phải có nội dung
    public static IRuleBuilderOptions<T, string?> TranslationOf<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        Func<T, string?> getOtherLanguage)
    {
        return ruleBuilder
            .Must((command, text) => !string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(getOtherLanguage(command)))
            .WithErrorCode("TranslationRequired")
            .WithMessage("A translation is required when the other language has content.");
    }
}
