namespace ModernRadar.Infrastructure.ExternalServices;

public static class IcaoRangeService
{
    private static readonly (int Min, int Max, string Name, string Code)[] Ranges = new[]
    {
        (0x000001, 0x000000, "Unknown", "??"),
        (0x004000, 0x0043FF, "Zimbabwe", "ZW"),
        (0x006000, 0x006FFF, "Malawi", "MW"),
        (0x008000, 0x008FFF, "Lesotho", "LS"),
        (0x00A000, 0x00AFFF, "Swaziland", "SZ"),
        (0x00C000, 0x00CFFF, "Madagascar", "MG"),
        (0x010000, 0x017FFF, "South Africa", "ZA"), // 010000-017FFF
        (0x018000, 0x018FFF, "Botswana", "BW"),
        (0x01A000, 0x01AFFF, "Congo", "CG"),
        (0x01C000, 0x01CFFF, "Burundi", "BI"),
        (0x020000, 0x020FFF, "Namibia", "NA"),
        (0x022000, 0x022FFF, "Eritrea", "ER"),
        (0x024000, 0x027FFF, "Tunisia", "TN"),
        (0x028000, 0x02BFFF, "Mauritania", "MR"),
        (0x02C000, 0x02FFFF, "Mali", "ML"),
        (0x030000, 0x033FFF, "Guinea", "GN"),
        (0x034000, 0x037FFF, "Guinea-Bissau", "GW"),
        (0x038000, 0x03BFFF, "Cape Verde", "CV"),
        (0x03C000, 0x03FFFF, "Congo (DRC)", "CD"),
        (0x040000, 0x043FFF, "Liberia", "LR"),
        (0x044000, 0x047FFF, "Sierra Leone", "SL"),
        (0x048000, 0x04BFFF, "Gambia", "GM"),
        (0x04C000, 0x04FFFF, "Equatorial Guinea", "GQ"),
        (0x050000, 0x053FFF, "Kenya", "KE"),
        (0x054000, 0x057FFF, "Somalia", "SO"),
        (0x058000, 0x05BFFF, "Congo (DRC)", "CD"),
        (0x05C000, 0x05FFFF, "Djibouti", "DJ"),
        (0x060000, 0x063FFF, "Seychelles", "SC"),
        (0x064000, 0x067FFF, "Sudan", "SD"),
        (0x068000, 0x06BFFF, "South Sudan", "SS"), // New
        (0x06C000, 0x06FFFF, "Rwanda", "RW"),
        (0x070000, 0x073FFF, "Ethiopia", "ET"),
        (0x074000, 0x077FFF, "Mauritius", "MU"),
        (0x078000, 0x07BFFF, "Cameroon", "CM"),
        (0x07C000, 0x07FFFF, "Central African Republic", "CF"),
        (0x080000, 0x083FFF, "Chad", "TD"),
        (0x084000, 0x087FFF, "Gabon", "GA"),
        (0x088000, 0x08BFFF, "Burkina Faso", "BF"),
        (0x08C000, 0x08FFFF, "Niger", "NE"),
        (0x090000, 0x093FFF, "Ivory Coast", "CI"),
        (0x094000, 0x097FFF, "Togo", "TG"),
        (0x098000, 0x09BFFF, "Benin", "BJ"),
        (0x0A0000, 0x0A3FFF, "Tanzania", "TZ"),
        (0x0A4000, 0x0A7FFF, "Uganda", "UG"),
        (0x0A8000, 0x0ABFFF, "Zambia", "ZM"),
        (0x0AC000, 0x0AFFFF, "Comoros", "KM"),
        (0x0C0000, 0x0C7FFF, "Angola", "AO"),
        (0x0C8000, 0x0CFFFF, "Kenya", "KE"),
        (0x0D0000, 0x0D7FFF, "Nigeria", "NG"),
        (0x0D8000, 0x0DBFFF, "Mozambique", "MZ"),
        (0x100000, 0x10FFFF, "Reunion", "RE"),
        (0x140000, 0x14FFFF, "Algeria", "DZ"),
        (0x180000, 0x18FFFF, "Libya", "LY"),
        (0x1C0000, 0x1CFFFF, "Morocco", "MA"),
        (0x200000, 0x20FFFF, "Senegal", "SN"),
        (0x300000, 0x33FFFF, "Italy", "IT"),
        (0x380000, 0x3BFFFF, "France", "FR"),
        (0x3C0000, 0x3FFFFF, "Germany", "DE"),
        (0x400000, 0x43FFFF, "United Kingdom", "GB"),
        (0x440000, 0x44FFFF, "Netherlands", "NL"),
        (0x450000, 0x45FFFF, "Denmark", "DK"),
        (0x460000, 0x46FFFF, "Finland", "FI"),
        (0x470000, 0x47FFFF, "Norway", "NO"),
        (0x480000, 0x48FFFF, "Poland", "PL"),
        (0x490000, 0x49FFFF, "Portugal", "PT"),
        (0x4A0000, 0x4AFFFF, "Spain", "ES"),
        (0x4B0000, 0x4BFFFF, "Sweden", "SE"),
        (0x4C0000, 0x4CFFFF, "Switzerland", "CH"),
        (0x4D0000, 0x4DFFFF, "Turkey", "TR"),
        (0x500000, 0x53FFFF, "Australia", "AU"),
        (0x600000, 0x6FFFFF, "South Africa", "ZA"),
        (0x700000, 0x73FFFF, "Afghanistan", "AF"),
        (0x740000, 0x77FFFF, "India", "IN"),
        (0x780000, 0x7BFFFF, "Japan", "JP"),
        (0x7C0000, 0x7FFFFF, "Australia", "AU"),
        (0x800000, 0x83FFFF, "China", "CN"),
        (0x840000, 0x87FFF, "China", "CN"),
        (0x880000, 0x8FFFFF, "Thailand", "TH"),
        (0xA00000, 0xAFFFFF, "United States", "US"),
        (0xC00000, 0xC3FFFF, "Canada", "CA"),
        (0xE00000, 0xE7FFFF, "Brazil", "BR"),
        (0xF00000, 0xFFFFFF, "Unknown", "??")
    };

    public static (string Name, string Code) GetCountry(string hex)
    {
        if (string.IsNullOrEmpty(hex) || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int val))
        {
            return ("Unknown", "??");
        }

        foreach (var range in Ranges)
        {
            if (val >= range.Min && val <= range.Max)
            {
                return (range.Name, range.Code);
            }
        }

        return ("Unknown", "??");
    }
}
