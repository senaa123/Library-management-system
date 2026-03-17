using LibraryM.Application.Abstractions.Payments;
using LibraryM.Application.Common;
using LibraryM.Application.Fines.Models;
using Stripe;
using Stripe.Checkout;
using System.Net.Http;

namespace LibraryM.Infrastructure.Payments;

public sealed class StripeFineCheckoutGateway : IFineCheckoutGateway
{
    private readonly StripeOptions _options;

    public StripeFineCheckoutGateway(StripeOptions options)
    {
        _options = options;
        StripeConfiguration.ApiKey = options.SecretKey;
    }

    public async Task<OperationResult<CreateFineCheckoutSessionResult>> CreateSessionAsync(
        int memberId,
        string memberName,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return OperationResult<CreateFineCheckoutSessionResult>.Failure("Stripe is not configured on the server.", FailureType.Conflict);
        }

        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        var unitAmount = decimal.ToInt64(roundedAmount * 100m);
        var successUrl = $"{_options.ClientBaseUrl.TrimEnd('/')}/fines?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{_options.ClientBaseUrl.TrimEnd('/')}/fines?payment=cancelled";

        try
        {
            var sessionService = new SessionService();
            var session = await sessionService.CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "payment",
                    ClientReferenceId = memberId.ToString(),
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    LineItems =
                    [
                        new SessionLineItemOptions
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = _options.Currency,
                                UnitAmount = unitAmount,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Library fine payment",
                                    Description = $"Outstanding library fines for {memberName}"
                                }
                            }
                        }
                    ],
                    Metadata = new Dictionary<string, string>
                    {
                        ["memberId"] = memberId.ToString()
                    }
                },
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(session.Url))
            {
                return OperationResult<CreateFineCheckoutSessionResult>.Failure("Stripe did not return a checkout URL.", FailureType.Conflict);
            }

            return OperationResult<CreateFineCheckoutSessionResult>.Success(
                new CreateFineCheckoutSessionResult(session.Id, session.Url, roundedAmount));
        }
        catch (StripeException)
        {
            return OperationResult<CreateFineCheckoutSessionResult>.Failure(
                "Stripe checkout is unavailable right now. Please try again in a moment.",
                FailureType.Conflict);
        }
        catch (HttpRequestException)
        {
            return OperationResult<CreateFineCheckoutSessionResult>.Failure(
                "Stripe checkout is unavailable right now. Please check the network connection and try again.",
                FailureType.Conflict);
        }
    }

    public async Task<OperationResult<VerifiedFineCheckoutResult>> VerifyCompletedSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return OperationResult<VerifiedFineCheckoutResult>.Failure("Stripe is not configured on the server.", FailureType.Conflict);
        }

        try
        {
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(
                sessionId,
                new SessionGetOptions(),
                cancellationToken: cancellationToken);

            var paymentMethod = "Stripe Checkout";
            var amountPaid = (session.AmountTotal ?? 0m) / 100m;

            return OperationResult<VerifiedFineCheckoutResult>.Success(
                new VerifiedFineCheckoutResult(
                    session.Id,
                    amountPaid,
                    paymentMethod,
                    session.Id,
                    string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)));
        }
        catch (StripeException)
        {
            return OperationResult<VerifiedFineCheckoutResult>.Failure(
                "Stripe could not verify this payment right now. Please try again in a moment.",
                FailureType.Conflict);
        }
        catch (HttpRequestException)
        {
            return OperationResult<VerifiedFineCheckoutResult>.Failure(
                "Stripe could not be reached to verify the payment. Please try again once the network is available.",
                FailureType.Conflict);
        }
    }
}
