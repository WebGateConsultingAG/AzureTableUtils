namespace WebGate.Azure.TableUtils.Tests.Fixtures;

public class UnsupportedTypePoco
{
    public short ShortValue { set; get; }

    public char CharValue { set; get; }

    public UnsupportedTypePoco()
    {
        ShortValue = 2;
        CharValue = 'C';
    }
}