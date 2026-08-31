using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Retaguarda.Shared;

namespace Retaguarda.Web.Infrastructure;

/// <summary>
/// Traduz as mensagens do model binder (valor inválido, valor ausente, "deve ser um número"…).
/// Sem isso o ASP.NET Core responde EM INGLÊS — ex.: "The value '' is invalid." ao limpar um campo
/// numérico — antes mesmo da validação do FluentValidation rodar. Complementa o
/// SuppressImplicitRequiredAttributeForNonNullableReferenceTypes, que só cobre tipos de referência.
/// Vive como IConfigureOptions porque o provider é montado no startup e precisa do IStringLocalizer
/// resolvido pela DI. Ver padrao-crud.md §12.
/// </summary>
public sealed class LocalizedModelBindingMessages : IConfigureOptions<MvcOptions>
{
    private readonly IStringLocalizer<SharedResources> _localizer;

    public LocalizedModelBindingMessages(IStringLocalizer<SharedResources> localizer)
        => _localizer = localizer;

    public void Configure(MvcOptions options)
    {
        var provider = options.ModelBindingMessageProvider;

        provider.SetValueIsInvalidAccessor(
            value => Text("modelbinding_value_is_invalid", value));
        provider.SetAttemptedValueIsInvalidAccessor(
            (value, field) => Text("modelbinding_attempted_value_is_invalid", value, field));
        provider.SetValueMustBeANumberAccessor(
            field => Text("modelbinding_value_must_be_number", field));
        provider.SetValueMustNotBeNullAccessor(
            value => Text("modelbinding_value_must_not_be_null", value));
        provider.SetMissingBindRequiredValueAccessor(
            field => Text("modelbinding_missing_bind_required_value", field));
        provider.SetMissingKeyOrValueAccessor(
            () => Text("modelbinding_missing_key_or_value"));
        provider.SetMissingRequestBodyRequiredValueAccessor(
            () => Text("modelbinding_missing_request_body"));
        provider.SetUnknownValueIsInvalidAccessor(
            field => Text("modelbinding_unknown_value_is_invalid", field));
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(
            value => Text("modelbinding_non_property_attempted_value_is_invalid", value));
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(
            () => Text("modelbinding_non_property_unknown_value_is_invalid"));
        provider.SetNonPropertyValueMustBeANumberAccessor(
            () => Text("modelbinding_non_property_value_must_be_number"));
    }

    // Resolve a chave e aplica os argumentos posicionais (args sobrando são ignorados pelo Format).
    private string Text(string key, params object[] args)
    {
        var value = _localizer[key].Value;
        return args.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, args);
    }
}
