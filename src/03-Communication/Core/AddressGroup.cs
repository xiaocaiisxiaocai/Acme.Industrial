using Acme.Industrial.Communication.Abstractions.Models;

namespace Acme.Industrial.Communication.Core;

/// <summary>
/// 地址分组工具 - 将多个点位按协议规则合并为批量读写请求，减少通讯次数。
/// </summary>
public class AddressGroup
{
    private readonly List<AddressItem> _items = new();
    private readonly int _maxBatchSize;
    private readonly string _protocol;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="protocol">协议类型，用于确定分组规则。</param>
    /// <param name="maxBatchSize">最大批量大小。</param>
    public AddressGroup(string protocol, int maxBatchSize = 100)
    {
        _protocol = protocol?.ToUpperInvariant() ?? "GENERIC";
        _maxBatchSize = Math.Max(1, maxBatchSize);
    }

    /// <summary>
    /// 添加点位。
    /// </summary>
    public void Add(Tag tag)
    {
        if (tag == null) return;
        _items.Add(new AddressItem
        {
            Tag = tag,
            Address = tag.Address,
            Length = GetByteLength(tag),
            Priority = tag.ScanRate
        });
    }

    /// <summary>
    /// 添加多个点位。
    /// </summary>
    public void AddRange(IEnumerable<Tag> tags)
    {
        foreach (var tag in tags)
        {
            Add(tag);
        }
    }

    /// <summary>
    /// 获取可合并的批次。
    /// </summary>
    public IReadOnlyList<IReadOnlyList<AddressItem>> GetBatches()
    {
        var batches = new List<List<AddressItem>>();

        if (_items.Count == 0)
            return batches;

        switch (_protocol)
        {
            case "MODBUSTCP":
                batches = GroupByModbusRules();
                break;

            case "SIEMENSS7":
                batches = GroupByS7Rules();
                break;

            case "MITSUBISHIMC":
                batches = GroupByMitsubishiRules();
                break;

            default:
                // 通用分组
                batches = GroupGeneric();
                break;
        }

        return batches;
    }

    /// <summary>
    /// 按 Modbus 规则分组（连续地址合并）。
    /// </summary>
    private List<List<AddressItem>> GroupByModbusRules()
    {
        var batches = new List<List<AddressItem>>();

        if (_items.Count == 0)
            return batches;

        // 按起始地址排序
        var sorted = _items
            .Select((item, index) => new { item, OriginalIndex = index })
            .OrderBy(x => ParseModbusAddress(x.item.Address))
            .ThenBy(x => x.OriginalIndex)
            .Select(x => x.item)
            .ToList();

        var currentBatch = new List<AddressItem>();

        foreach (var item in sorted)
        {
            var addr = ParseModbusAddress(item.Address);

            if (currentBatch.Count == 0)
            {
                currentBatch.Add(item);
            }
            else
            {
                var lastAddr = ParseModbusAddress(currentBatch[^1].Address);
                var lastLen = currentBatch[^1].Length;

                // 检查是否连续
                if (addr == lastAddr + lastLen / 2 && currentBatch.Count < _maxBatchSize)
                {
                    currentBatch.Add(item);
                }
                else
                {
                    // 新批次
                    batches.Add(currentBatch);
                    currentBatch = new List<AddressItem> { item };
                }
            }
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// 按西门子 S7 规则分组（同一 DB 块内连续区域合并）。
    /// </summary>
    private List<List<AddressItem>> GroupByS7Rules()
    {
        var batches = new List<List<AddressItem>>();

        if (_items.Count == 0)
            return batches;

        // 按 DB 号和偏移量排序
        var sorted = _items
            .Select((item, index) => new { item, OriginalIndex = index })
            .OrderBy(x => GetS7DbNumber(x.item.Address))
            .ThenBy(x => GetS7Offset(x.item.Address))
            .ThenBy(x => x.OriginalIndex)
            .Select(x => x.item)
            .ToList();

        var currentBatch = new List<AddressItem>();

        foreach (var item in sorted)
        {
            if (currentBatch.Count == 0)
            {
                currentBatch.Add(item);
            }
            else
            {
                var lastItem = currentBatch[^1];
                var sameDb = GetS7DbNumber(item.Address) == GetS7DbNumber(lastItem.Address);
                var lastEnd = GetS7Offset(lastItem.Address) + lastItem.Length;

                if (sameDb && GetS7Offset(item.Address) == lastEnd && currentBatch.Count < _maxBatchSize)
                {
                    currentBatch.Add(item);
                }
                else
                {
                    batches.Add(currentBatch);
                    currentBatch = new List<AddressItem> { item };
                }
            }
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// 按三菱 MC 规则分组（同一类型软元件连续区域合并）。
    /// </summary>
    private List<List<AddressItem>> GroupByMitsubishiRules()
    {
        var batches = new List<List<AddressItem>>();

        if (_items.Count == 0)
            return batches;

        // 按软元件类型和地址排序
        var sorted = _items
            .Select((item, index) => new { item, OriginalIndex = index })
            .OrderBy(x => GetMcAreaType(x.item.Address))
            .ThenBy(x => GetMcAddress(x.item.Address))
            .ThenBy(x => x.OriginalIndex)
            .Select(x => x.item)
            .ToList();

        var currentBatch = new List<AddressItem>();

        foreach (var item in sorted)
        {
            if (currentBatch.Count == 0)
            {
                currentBatch.Add(item);
            }
            else
            {
                var lastItem = currentBatch[^1];
                var sameType = GetMcAreaType(item.Address) == GetMcAreaType(lastItem.Address);
                var lastEnd = GetMcAddress(lastItem.Address) + lastItem.Length / 2;

                if (sameType && GetMcAddress(item.Address) == lastEnd && currentBatch.Count < _maxBatchSize)
                {
                    currentBatch.Add(item);
                }
                else
                {
                    batches.Add(currentBatch);
                    currentBatch = new List<AddressItem> { item };
                }
            }
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// 通用分组（按顺序尽可能合并）。
    /// </summary>
    private List<List<AddressItem>> GroupGeneric()
    {
        var batches = new List<List<AddressItem>>();
        var currentBatch = new List<AddressItem>();

        foreach (var item in _items)
        {
            if (currentBatch.Count >= _maxBatchSize)
            {
                batches.Add(currentBatch);
                currentBatch = new List<AddressItem>();
            }
            currentBatch.Add(item);
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// 解析 Modbus 地址。
    /// </summary>
    private static int ParseModbusAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return 0;

        // 格式: 4XXXX, 3XXXX, 0XXXX, 1XXXX
        var numPart = address.Length > 1 ? address.Substring(1) : "0";
        return int.TryParse(numPart, out var addr) ? addr : 0;
    }

    /// <summary>
    /// 获取西门子 S7 DB 号。
    /// </summary>
    private static int GetS7DbNumber(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return 0;

        // 格式: DB1.DBD0, DB1.DBW0, M0.0, I0.0, Q0.0
        if (address.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
        {
            var parts = address.Split('.');
            if (parts.Length > 0)
            {
                var dbPart = parts[0].Substring(2);
                return int.TryParse(dbPart, out var db) ? db : 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// 获取西门子 S7 偏移量。
    /// </summary>
    private static int GetS7Offset(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return 0;

        // 格式: DB1.DBD0, DB1.DBW0
        if (address.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
        {
            var parts = address.Split('.');
            if (parts.Length >= 2)
            {
                var offsetPart = parts[1];
                // 移除 DBD/DBW/DBB 前缀
                var numStr = offsetPart;
                foreach (var prefix in new[] { "DBD", "DBW", "DBB", "DBX" })
                {
                    if (offsetPart.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        numStr = offsetPart.Substring(prefix.Length);
                        break;
                    }
                }
                return int.TryParse(numStr, out var offset) ? offset : 0;
            }
        }

        // M0.0, I0.0, Q0.0
        var letter = address[0];
        var numStr2 = address.Substring(1).Split('.')[0];
        return int.TryParse(numStr2, out var off) ? off : 0;
    }

    /// <summary>
    /// 获取三菱 MC 软元件类型。
    /// </summary>
    private static string GetMcAreaType(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "";

        // 获取前缀字母
        var prefix = new string(address.TakeWhile(char.IsLetter).ToArray());
        return prefix.ToUpperInvariant();
    }

    /// <summary>
    /// 获取三菱 MC 地址。
    /// </summary>
    private static int GetMcAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return 0;

        var numPart = new string(address.SkipWhile(char.IsLetter).ToArray());
        var dotIndex = numPart.IndexOf('.');
        if (dotIndex >= 0)
        {
            numPart = numPart.Substring(0, dotIndex);
        }

        return int.TryParse(numPart, out var addr) ? addr : 0;
    }

    /// <summary>
    /// 获取点位字节长度。
    /// </summary>
    private static ushort GetByteLength(Tag tag)
    {
        return tag.DataType switch
        {
            DataType.Bool => 1,
            DataType.Byte or DataType.SByte => 1,
            DataType.Int16 or DataType.UInt16 => 2,
            DataType.Int32 or DataType.UInt32 or DataType.Float => 4,
            DataType.Int64 or DataType.UInt64 or DataType.Double => 8,
            DataType.String or DataType.ByteArray => (ushort)(tag.Length > 0 ? tag.Length : 1),
            _ => 2
        };
    }

    /// <summary>
    /// 地址项。
    /// </summary>
    public class AddressItem
    {
        /// <summary>
        /// 点位定义。
        /// </summary>
        public Tag Tag { get; init; } = null!;

        /// <summary>
        /// 地址字符串。
        /// </summary>
        public string Address { get; init; } = string.Empty;

        /// <summary>
        /// 字节长度。
        /// </summary>
        public ushort Length { get; init; }

        /// <summary>
        /// 优先级（扫描周期越小优先级越高）。
        /// </summary>
        public int Priority { get; init; }
    }
}

/// <summary>
/// 地址分组器 - 用于批量优化。
/// </summary>
public static class AddressGrouper
{
    /// <summary>
    /// 将点位列表分组为可批量读取的批次。
    /// </summary>
    /// <param name="tags">点位列表。</param>
    /// <param name="protocol">协议类型。</param>
    /// <param name="maxBatchSize">最大批次大小。</param>
    /// <returns>分组后的批次列表。</returns>
    public static IReadOnlyList<IReadOnlyList<Tag>> Group(
        IEnumerable<Tag> tags,
        string protocol,
        int maxBatchSize = 100)
    {
        var group = new AddressGroup(protocol, maxBatchSize);
        group.AddRange(tags);
        return group.GetBatches()
            .Select(batch => (IReadOnlyList<Tag>)batch.Select(i => i.Tag).ToList())
            .ToList();
    }

    /// <summary>
    /// 按扫描周期对点位进行分组。
    /// </summary>
    /// <param name="tags">点位列表。</param>
    /// <returns>按扫描周期分组的字典。</returns>
    public static ILookup<int, Tag> GroupByScanRate(IEnumerable<Tag> tags)
    {
        return tags.ToLookup(t => t.ScanRate > 0 ? t.ScanRate : 1000);
    }

    /// <summary>
    /// 计算优化后的预计读取次数。
    /// </summary>
    public static int EstimateReadCount(IEnumerable<Tag> tags, string protocol)
    {
        var groups = Group(tags, protocol);
        return groups.Count;
    }
}
