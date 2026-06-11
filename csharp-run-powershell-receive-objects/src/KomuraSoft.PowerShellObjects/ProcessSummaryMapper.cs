using System.Globalization;
using System.Management.Automation;

namespace KomuraSoft.PowerShellObjects;

/// <summary>
/// PSObject から ProcessSummary への変換処理です（記事 6 章）。
/// Select-Object で絞った列を Properties から列名で取り出します。
/// </summary>
public static class ProcessSummaryMapper
{
    public static ProcessSummary ToProcessSummary(PSObject row)
    {
        string name = GetString(row, "Name");
        int id = GetInt32(row, "Id");
        double? cpu = GetNullableDouble(row, "CPU");
        long workingSet = GetInt64(row, "WorkingSet");

        return new ProcessSummary(name, id, cpu, workingSet);
    }

    public static string GetString(PSObject row, string propertyName)
    {
        return Convert.ToString(GetValue(row, propertyName), CultureInfo.InvariantCulture) ?? "";
    }

    public static int GetInt32(PSObject row, string propertyName)
    {
        return Convert.ToInt32(GetValue(row, propertyName), CultureInfo.InvariantCulture);
    }

    public static long GetInt64(PSObject row, string propertyName)
    {
        return Convert.ToInt64(GetValue(row, propertyName), CultureInfo.InvariantCulture);
    }

    public static double? GetNullableDouble(PSObject row, string propertyName)
    {
        object? value = GetValue(row, propertyName);
        return value is null ? null : Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static object? GetValue(PSObject row, string propertyName)
    {
        object? value = row.Properties[propertyName]?.Value;

        // CPU のような ScriptProperty の値は、PSObject に包まれたまま返ることがある。
        // Convert.To... に渡す前に中身（BaseObject）を取り出しておく。
        return value is PSObject wrapped ? wrapped.BaseObject : value;
    }
}
