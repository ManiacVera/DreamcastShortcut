using System;

namespace DreamcastShortcutApp.DTO.Enum
{
    [Flags]
    public enum OptionStringCompareEnum
    {   
        EQUAL           = 1,    //  1 << 0,
        CONTAINS        = 2,    //  1 << 1,
        START_WITH      = 4,    //  1 << 2,
        WORDS           = 8,    //  1 << 3,
        ALL             = 16
    }
}
