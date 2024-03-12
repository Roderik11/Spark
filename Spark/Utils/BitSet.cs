using Spark;
using System;

/// <summary>
/// variable length bitmask
/// </summary>
public struct BitSet
{
    const int ByteSize = 5;
    const int BitSize = (sizeof(uint) * 8) - 1;

    private uint[] bits;
    private bool clear;

    public bool IsClear => clear;

    public BitSet(int length)
    {
        clear = true;
        bits = new uint[length];
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < bits.Length; i++)
        {
            if (bits[i] > 0) return false;
        }

        return true;
    }

    public bool AllOf(BitSet other)
    {
        var otherBits = other.bits;
        int count = Math.Min(bits.Length, otherBits.Length);
        for (int i = 0; i < count; i++)
        {
            uint bit = bits[i];
            if ((bit & otherBits[i]) != bit)
                return false;
        }

        int tail = bits.Length - count;
        for (int i = count; i < tail; i++)
        {
            if (bits[i] != 0)
                return false;
        }

        return true;
    }

    public bool AnyOf(BitSet other)
    {
        var otherBits = other.bits;
        int count = Math.Min(bits.Length, otherBits.Length);
        for (int i = 0; i < count; i++)
        {
            uint bit = bits[i];

            if ((bit & otherBits[i]) != 0)
                return true;
        }

        return false;
    }

    public bool IsBitSet(int index)
    {
        int b = index >> ByteSize;
        if (b >= bits.Length)
            return false;

        return (bits[b] & (1 << (index & BitSize))) != 0;
    }

    public void SetBit(int index)
    {
        int b = index >> ByteSize;
        if (b >= bits.Length)
            Array.Resize(ref bits, b + 1);

        bits[b] |= 1u << (index & BitSize);
        clear = false;
    }

    public void ClearBit(int index)
    {
        int b = index >> ByteSize;
        if (b >= bits.Length)
            return;

        bits[b] &= ~(1u << (index & BitSize));
    }

    public void Clear()
    {
        Array.Clear(bits, 0, bits.Length);
        clear = true;
    }

    public override int GetHashCode()
    {
        if (IsEmpty()) return 0;

        int hash = 17;

        for (uint i = 0; i < bits.Length; i++)
            hash = hash * 23 + bits[i].GetHashCode();

        return hash;
    }
}