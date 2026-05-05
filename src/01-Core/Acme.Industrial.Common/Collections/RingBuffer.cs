namespace Acme.Industrial.Common.Collections;

/// <summary>
/// 环形缓冲区，用于固定大小的循环数据存储
/// 广泛应用于数据采集、报文缓冲等场景
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public class RingBuffer<T>
{
    private readonly T[] _buffer;
    private readonly int _capacity;
    private int _head;
    private int _tail;
    private int _count;
    private readonly object _lock = new();

    /// <summary>
    /// 创建一个环形缓冲区
    /// </summary>
    /// <param name="capacity">缓冲区容量（必须为 2 的幂次方以获得最佳性能）</param>
    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "容量必须大于 0");

        _capacity = capacity;
        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    /// <summary>
    /// 当前元素数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// 缓冲区容量
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// 是否已满
    /// </summary>
    public bool IsFull => Count == _capacity;

    /// <summary>
    /// 入队元素（如果已满则覆盖最旧的元素）
    /// </summary>
    public void Enqueue(T item)
    {
        lock (_lock)
        {
            _buffer[_tail] = item;
            _tail = (_tail + 1) % _capacity;

            if (_count < _capacity)
            {
                _count++;
            }
            else
            {
                _head = (_head + 1) % _capacity;
            }
        }
    }

    /// <summary>
    /// 出队元素
    /// </summary>
    public bool TryDequeue(out T item)
    {
        lock (_lock)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = _buffer[_head];
            _buffer[_head] = default!;
            _head = (_head + 1) % _capacity;
            _count--;
            return true;
        }
    }

    /// <summary>
    /// 批量入队
    /// </summary>
    public void EnqueueRange(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Enqueue(item);
        }
    }

    /// <summary>
    /// 批量出队
    /// </summary>
    public List<T> DequeueRange(int count)
    {
        var result = new List<T>();
        for (int i = 0; i < count; i++)
        {
            if (TryDequeue(out var item))
            {
                result.Add(item);
            }
            else
            {
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// 清空缓冲区
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _capacity);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }

    /// <summary>
    /// 获取所有元素（不移除）
    /// </summary>
    public IEnumerable<T> GetAll()
    {
        lock (_lock)
        {
            var result = new List<T>(_count);
            for (int i = 0; i < _count; i++)
            {
                var index = (_head + i) % _capacity;
                result.Add(_buffer[index]);
            }
            return result;
        }
    }

    /// <summary>
    /// 查看队首元素（不移除）
    /// </summary>
    public bool TryPeek(out T item)
    {
        lock (_lock)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = _buffer[_head];
            return true;
        }
    }
}
