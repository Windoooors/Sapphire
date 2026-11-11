using System.Globalization;
using System.Numerics;

namespace Sapphire.Utils;

public static class FloatConverter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static float ToSingle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Input cannot be empty.");

        value = value.Trim();

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var hex = value.Substring(2);

            if (hex.Length != 8)
                throw new FormatException("Invalid Single hexadecimal length.");

            if (!uint.TryParse(hex, NumberStyles.AllowHexSpecifier,
                    Invariant, out var bits))
                throw new FormatException("Invalid Single hexadecimal representation.");

            return BitConverter.Int32BitsToSingle(unchecked((int)bits));
        }

        if (value.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            var binary = value.Substring(2);

            if (binary.Length != 32)
                throw new FormatException("Invalid Single binary length.");

            var bits = ParseBinary32(binary);

            return BitConverter.Int32BitsToSingle(unchecked((int)bits));
        }

        var result = float.Parse(value, NumberStyles.Float, Invariant);

        if (float.IsInfinity(result) && !IsExplicitInfinity(value))
            throw new OverflowException("Value was outside the range of Single.");

        return result;
    }

    public static double ToDouble(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Input cannot be empty.");

        value = value.Trim();

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var hex = value.Substring(2);

            if (hex.Length != 16)
                throw new FormatException("Invalid Double hexadecimal length.");

            if (!ulong.TryParse(hex, NumberStyles.AllowHexSpecifier,
                    Invariant, out var bits))
                throw new FormatException("Invalid Double hexadecimal representation.");

            return BitConverter.Int64BitsToDouble(unchecked((long)bits));
        }

        if (value.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            var binary = value.Substring(2);

            if (binary.Length != 64)
                throw new FormatException("Invalid Double binary length.");

            var bits = ParseBinary64(binary);

            return BitConverter.Int64BitsToDouble(unchecked((long)bits));
        }

        var result = double.Parse(value, NumberStyles.Float, Invariant);

        if (double.IsInfinity(result) && !IsExplicitInfinity(value))
            throw new OverflowException("Value was outside the range of Double.");

        return result;
    }

    public static string ToDecimal(float value)
    {
        if (float.IsNaN(value))
            return "NaN";

        if (float.IsPositiveInfinity(value))
            return "Infinity";

        if (float.IsNegativeInfinity(value))
            return "-Infinity";

        if (value == 0f && BitConverter.SingleToInt32Bits(value) < 0)
            return "-0";

        return value.ToString("G9", Invariant);
    }

    public static string ToDecimal(double value)
    {
        if (double.IsNaN(value))
            return "NaN";

        if (double.IsPositiveInfinity(value))
            return "Infinity";

        if (double.IsNegativeInfinity(value))
            return "-Infinity";

        if (value == 0d && BitConverter.DoubleToInt64Bits(value) < 0)
            return "-0";

        return value.ToString("G17", Invariant);
    }

    public static string ToBinary(float value)
    {
        var bits = unchecked((uint)BitConverter.SingleToInt32Bits(value));

        return "0b" + Convert.ToString(bits, 2).PadLeft(32, '0');
    }

    public static string ToBinary(double value)
    {
        var bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));

        return "0b" + Convert.ToString(unchecked((long)bits), 2).PadLeft(64, '0');
    }

    public static string ToHex(float value)
    {
        var bits = unchecked((uint)BitConverter.SingleToInt32Bits(value));

        return "0x" + bits.ToString("X8", Invariant);
    }

    public static string ToHex(double value)
    {
        var bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));

        return "0x" + bits.ToString("X16", Invariant);
    }

    private static uint ParseBinary32(string binary)
    {
        uint result = 0;

        foreach (var c in binary)
        {
            if (c != '0' && c != '1')
                throw new FormatException("Invalid binary representation.");

            result = (result << 1) | (byte)(c - '0');
        }

        return result;
    }

    private static ulong ParseBinary64(string binary)
    {
        ulong result = 0;

        foreach (var c in binary)
        {
            if (c != '0' && c != '1')
                throw new FormatException("Invalid binary representation.");

            result = (result << 1) | (byte)(c - '0');
        }

        return result;
    }

    private static bool IsExplicitInfinity(string value)
    {
        return string.Equals(value, "Infinity", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "+Infinity", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "-Infinity", StringComparison.OrdinalIgnoreCase);
    }
}

public static class IntegerConverter
{
    private static readonly CultureInfo Invariant =
        CultureInfo.InvariantCulture;

    public static sbyte ToSByte(string value)
    {
        return (sbyte)Parse(value, 8, true);
    }

    public static byte ToByte(string value)
    {
        return (byte)Parse(value, 8, false);
    }

    public static short ToInt16(string value)
    {
        return (short)Parse(value, 16, true);
    }

    public static ushort ToUInt16(string value)
    {
        return (ushort)Parse(value, 16, false);
    }

    public static int ToInt32(string value)
    {
        return (int)Parse(value, 32, true);
    }

    public static uint ToUInt32(string value)
    {
        return (uint)Parse(value, 32, false);
    }

    public static long ToInt64(string value)
    {
        return (long)Parse(value, 64, true);
    }

    public static ulong ToUInt64(string value)
    {
        return (ulong)Parse(value, 64, false);
    }

    private static BigInteger Parse(
        string value,
        int bitWidth,
        bool signed)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Input cannot be empty.");

        value = value.Trim();

        BigInteger result;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var hex = value.Substring(2);

            if (hex.Length != bitWidth / 4)
                throw new FormatException("Invalid hexadecimal length.");

            if (!ulong.TryParse(
                    hex,
                    NumberStyles.AllowHexSpecifier,
                    Invariant,
                    out var bits))
                throw new FormatException("Invalid hexadecimal representation.");

            result = new BigInteger(bits);

            if (signed && (bits & (1UL << (bitWidth - 1))) != 0)
                result -= BigInteger.One << bitWidth;
        }
        else if (value.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            var binary = value.Substring(2);

            if (binary.Length != bitWidth)
                throw new FormatException("Invalid binary length.");

            ulong bits = 0;

            foreach (var c in binary)
            {
                if (c != '0' && c != '1')
                    throw new FormatException("Invalid binary representation.");

                bits = (bits << 1) | (byte)(c - '0');
            }

            result = new BigInteger(bits);

            if (signed && (bits & (1UL << (bitWidth - 1))) != 0)
                result -= BigInteger.One << bitWidth;
        }
        else
        {
            if (!BigInteger.TryParse(
                    value,
                    NumberStyles.AllowLeadingSign,
                    Invariant,
                    out result))
                throw new FormatException("Invalid decimal representation.");
        }

        var min = signed
            ? -(BigInteger.One << (bitWidth - 1))
            : BigInteger.Zero;

        var max = signed
            ? (BigInteger.One << (bitWidth - 1)) - 1
            : (BigInteger.One << bitWidth) - 1;

        if (result < min || result > max)
            throw new OverflowException("Integer value is out of range.");

        return result;
    }
}