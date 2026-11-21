using QRCoder;

namespace TrainTicketWeb.Services;

/// <summary>Tạo mã QR (SVG) cho vé điện tử — sinh phía máy chủ, không phụ thuộc mạng.</summary>
public static class QrHelper
{
    public static string Svg(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) content = "TICKET";
        using var qg = new QRCodeGenerator();
        using var data = qg.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var svg = new SvgQRCode(data);
        return svg.GetGraphic(18, "#111827", "#FFFFFF", false);
    }
}
