namespace FreeSql.Various.Utilitys;

public static class TimeRangeCalculator
{
    /// <summary>
    /// 根据输入时间、分库开始时间和周期计算所属数据库名称
    /// </summary>
    /// <param name="inputTime">需要计算的时间</param>
    /// <param name="startTime">分库开始时间</param>
    /// <param name="period">周期字符串，格式为"数值 单位"，如"1 Year"、"1 Month"、"1 Day"</param>
    /// <returns>所属数据库名称</returns>
    public static string GetTimeString(DateTime inputTime, DateTime startTime, string period)
    {
        // 解析周期信息
        var (periodValue, periodUnit) = ParsePeriod(period);

        // 计算输入时间所属的周期编号
        int periodNumber = CalculatePeriodNumber(inputTime, startTime, periodValue, periodUnit);

        // 计算该周期的起始时间
        DateTime periodStart = GetPeriodStartTime(startTime, periodNumber, periodValue, periodUnit);

        // 格式化生成数据库名称
        return Format(periodStart, periodUnit);
    }

    /// <summary>
    /// 根据日期范围查询对应名称集合
    /// </summary>
    /// <param name="startDate">查询开始日期</param>
    /// <param name="endDate">查询结束日期</param>
    /// <param name="shardingStartTime">分库开始时间</param>
    /// <param name="period">周期字符串，格式为"数值 单位"</param>
    /// <returns>日期范围内涉及的所有数据库名称</returns>
    public static List<string> GetTimeStringInRange(DateTime startDate, DateTime endDate,
        DateTime shardingStartTime, string period)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("开始日期不能晚于结束日期", nameof(startDate));
        }

        // 解析周期信息
        var (periodValue, periodUnit) = ParsePeriod(period);

        // 获取开始日期和结束日期所属的周期编号
        int startPeriodNumber = CalculatePeriodNumber(startDate, shardingStartTime, periodValue, periodUnit);
        
        int endPeriodNumber = CalculatePeriodNumber(endDate, shardingStartTime, periodValue, periodUnit);

        // 存储所有数据库名称的集合
        HashSet<string> dbNames = new HashSet<string>();

        // 遍历所有涉及的周期，收集数据库名称
        for (int i = startPeriodNumber; i <= endPeriodNumber; i++)
        {
            DateTime periodStart = GetPeriodStartTime(shardingStartTime, i, periodValue, periodUnit);
            string dbName = Format(periodStart, periodUnit);
            dbNames.Add(dbName);
        }

        // 返回排序后的结果
        return dbNames.OrderBy(name => name).ToList();
    }

    /// <summary>
    /// 解析周期字符串
    /// </summary>
    private static (int Value, PeriodUnit Unit) ParsePeriod(string period)
    {
        if (string.IsNullOrWhiteSpace(period))
        {
            throw new ArgumentException("周期字符串不能为空", nameof(period));
        }

        var parts = period.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException("无效的周期格式，正确格式应为'数值 单位'（例如'1 Year'）", nameof(period));
        }

        if (!int.TryParse(parts[0], out int value) || value <= 0)
        {
            throw new ArgumentException("周期数值必须是正整数", nameof(period));
        }

        if (!Enum.TryParse<PeriodUnit>(parts[1], true, out var unit))
        {
            throw new ArgumentException($"无效的周期单位，允许的单位：{string.Join(", ", Enum.GetNames<PeriodUnit>())}",
                nameof(period));
        }

        return (value, unit);
    }

    /// <summary>
    /// 计算输入时间所属的周期编号
    /// </summary>
    private static int CalculatePeriodNumber(DateTime inputTime, DateTime startTime, int periodValue,
        PeriodUnit periodUnit)
    {
        // 如果输入时间早于开始时间，属于第0个周期
        if (inputTime < startTime)
        {
            return 0;
        }

        switch (periodUnit)
        {
            case PeriodUnit.Year:
                return CalculateYearPeriods(inputTime, startTime, periodValue);
            case PeriodUnit.Month:
                return CalculateMonthPeriods(inputTime, startTime, periodValue);
            case PeriodUnit.Day:
                return CalculateDayPeriods(inputTime, startTime, periodValue);
            default:
                throw new ArgumentOutOfRangeException(nameof(periodUnit), periodUnit, null);
        }
    }

    /// <summary>
    /// 按年计算周期数
    /// </summary>
    private static int CalculateYearPeriods(DateTime inputTime, DateTime startTime, int periodValue)
    {
        int yearsDiff = inputTime.Year - startTime.Year;

        // 检查月份和日期，确定是否已过当年的周期起始点
        if (inputTime.Month < startTime.Month ||
            (inputTime.Month == startTime.Month && inputTime.Day < startTime.Day))
        {
            yearsDiff--;
        }

        return (int)Math.Floor((double)yearsDiff / periodValue);
    }

    /// <summary>
    /// 按月计算周期数
    /// </summary>
    private static int CalculateMonthPeriods(DateTime inputTime, DateTime startTime, int periodValue)
    {
        int monthsDiff = (inputTime.Year - startTime.Year) * 12 + (inputTime.Month - startTime.Month);

        // 检查日期，确定是否已过当月的周期起始点
        // 特殊处理月份最后一天的情况
        if (inputTime.Day < startTime.Day &&
            inputTime.Day != DateTime.DaysInMonth(inputTime.Year, inputTime.Month))
        {
            monthsDiff--;
        }

        return (int)Math.Floor((double)monthsDiff / periodValue);
    }

    /// <summary>
    /// 按天计算周期数
    /// </summary>
    private static int CalculateDayPeriods(DateTime inputTime, DateTime startTime, int periodValue)
    {
        TimeSpan diff = inputTime - startTime;
        int daysDiff = (int)diff.TotalDays;

        return daysDiff / periodValue;
    }

    /// <summary>
    /// 计算指定周期的起始时间
    /// </summary>
    private static DateTime GetPeriodStartTime(DateTime startTime, int periodNumber, int periodValue,
        PeriodUnit periodUnit)
    {
        switch (periodUnit)
        {
            case PeriodUnit.Year:
                return startTime.AddYears(periodNumber * periodValue);
            case PeriodUnit.Month:
                return startTime.AddMonths(periodNumber * periodValue);
            case PeriodUnit.Day:
                return startTime.AddDays(periodNumber * periodValue);
            default:
                throw new ArgumentOutOfRangeException(nameof(periodUnit), periodUnit, null);
        }
    }

    /// <summary>
    /// 根据周期起始时间和单位格式化数据库名称
    /// </summary>
    private static string Format(DateTime periodStartTime, PeriodUnit periodUnit)
    {
        string format = periodUnit switch
        {
            PeriodUnit.Year => "yyyy",
            PeriodUnit.Month => "yyyyMM",
            PeriodUnit.Day => "yyyyMMdd",
            _ => throw new ArgumentOutOfRangeException(nameof(periodUnit), periodUnit, null)
        };

        return periodStartTime.ToString(format);
    }

    /// <summary>
    /// 周期单位枚举
    /// </summary>
    private enum PeriodUnit
    {
        Year,
        Month,
        Day
    }
}