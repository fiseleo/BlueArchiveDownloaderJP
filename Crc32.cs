using System.Security.Cryptography;
public class Crc32 : HashAlgorithm
{
    public const uint DefaultPolynomial = 0xedb88320u;
    public const uint DefaultSeed = 0xffffffffu;

    private uint hash;
    private readonly uint seed;
    private readonly uint[] table;
    private static uint[] defaultTable;

    public Crc32()
        : this(DefaultPolynomial, DefaultSeed)
    {
    }

    public Crc32(uint polynomial, uint seed)
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("Not implemented for big-endian platforms");
        table = InitializeTable(polynomial);
        this.seed = hash = seed;
    }

    public override void Initialize()
    {
        hash = seed;
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        hash = CalculateHash(table, hash, array, ibStart, cbSize);
    }

    protected override byte[] HashFinal()
    {
        var hashBuffer = UInt32ToBigEndianBytes(~hash);
        HashValue = hashBuffer;
        return hashBuffer;
    }

    public override int HashSize => 32;

    public uint Value => ~hash;

    public void Update(byte[] buffer, int start, int size)
    {
        hash = CalculateHash(table, hash, buffer, start, size);
    }

    private static uint[] InitializeTable(uint polynomial)
    {
        if (polynomial == DefaultPolynomial && defaultTable != null)
            return defaultTable;

        var createTable = new uint[256];
        for (var i = 0; i < 256; i++)
        {
            var entry = (uint)i;
            for (var j = 0; j < 8; j++)
                if ((entry & 1) == 1)
                    entry = (entry >> 1) ^ polynomial;
                else
                    entry >>= 1;
            createTable[i] = entry;
        }

        if (polynomial == DefaultPolynomial)
            defaultTable = createTable;

        return createTable;
    }

    private static uint CalculateHash(uint[] table, uint seed, IList<byte> buffer, int start, int size)
    {
        var crc = seed;
        for (var i = start; i < size - start; i++)
            crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
        return crc;
    }

    private byte[] UInt32ToBigEndianBytes(uint x)
    {
        return new[]
        {
            (byte)((x >> 24) & 0xff),
            (byte)((x >> 16) & 0xff),
            (byte)((x >> 8) & 0xff),
            (byte)(x & 0xff)
        };
    }
}