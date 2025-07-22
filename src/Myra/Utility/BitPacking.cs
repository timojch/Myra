using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Utility;
internal static class BitPacking
{
    public static uint UnsignedIntFromBools(params ReadOnlySpan<bool> bools)
    {
        uint mask = 0x01;
        uint result = 0x00;
        foreach (var value in bools)
        {
            if (value)
            {
                result |= mask;
            }

            mask <<= 1;
        }

        return result;
    }
}
