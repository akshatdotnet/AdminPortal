using Email.Application.Interfaces;

namespace Email.Application.Templates;

public static class EmailTemplateEngine
{
    private static readonly Dictionary<EmailTemplate, (string Subject, string HtmlBody)> Templates = new()
    {
        [EmailTemplate.WelcomeEmail] = (
            "Welcome to ShopHub, {{FirstName}}!",
            "<h1>Welcome, {{FirstName}}!</h1><p>Your account is ready.</p><a href='{{ShopUrl}}'>Shop Now</a>"),

        [EmailTemplate.OrderConfirmation] = (
            "Order Confirmed - {{OrderNumber}}",
            "<h1>Order Confirmed!</h1><p>Order <strong>{{OrderNumber}}</strong> placed.</p><p>Total: <strong>{{Total}}</strong></p><a href='{{OrderUrl}}'>View Order</a>"),

        [EmailTemplate.OrderShipped] = (
            "Your order {{OrderNumber}} has shipped!",
            "<h1>Order Shipped!</h1><p>Order <strong>{{OrderNumber}}</strong> is on its way.</p><p>Tracking: <strong>{{TrackingNumber}}</strong></p>"),

        [EmailTemplate.OrderCancelled] = (
            "Order {{OrderNumber}} Cancelled",
            "<h1>Order Cancelled</h1><p>Order <strong>{{OrderNumber}}</strong> was cancelled.</p><p>Reason: {{Reason}}</p>"),

        [EmailTemplate.PasswordReset] = (
            "Reset your ShopHub password",
            "<h1>Password Reset</h1><p>Click to reset your password (expires in 1 hour):</p><a href='{{ResetUrl}}'>Reset Password</a>"),

        [EmailTemplate.RefundProcessed] = (
            "Refund processed for {{OrderNumber}}",
            "<h1>Refund Processed</h1><p>Refund of <strong>{{RefundAmount}}</strong> for order <strong>{{OrderNumber}}</strong> is complete.</p>"),
    };

    public static (string Subject, string HtmlBody) Render(EmailTemplate template, Dictionary<string, string> vars)
    {
        if (!Templates.TryGetValue(template, out var tmpl))
            throw new ArgumentException("Template '" + template + "' not found.");

        var subject = tmpl.Subject;
        var body = tmpl.HtmlBody;

        foreach (var (key, value) in vars)
        {
            subject = subject.Replace("{{" + key + "}}", value, StringComparison.OrdinalIgnoreCase);
            body = body.Replace("{{" + key + "}}", value, StringComparison.OrdinalIgnoreCase);
        }

        var year = DateTime.UtcNow.Year;
        return (subject, "<!DOCTYPE html><html><body style='font-family:sans-serif;max-width:600px;margin:0 auto;padding:20px'>" + body + "<hr/><p style='color:#888;font-size:12px'>ShopHub &copy; " + year + "</p></body></html>");
    }
}
